
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// ApplicationUpdater.cs
//
// Public class and only API for the UAB.
// 
// For more information see the Updater Application Block Implementation Overview. 
// 
//============================================================================================================
// Copyright (C) 2000-2001 Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR
// FITNESS FOR A PARTICULAR PURPOSE.
//============================================================================================================



using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Security;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.ApplicationBlocks.ApplicationUpdater.Downloaders;
using Microsoft.ApplicationBlocks.ApplicationUpdater.Validators;
using Microsoft.ApplicationBlocks.ApplicationUpdater.Interfaces;

using Microsoft.ApplicationBlocks.ExceptionManagement;



namespace Microsoft.ApplicationBlocks.ApplicationUpdater
{

	#region Delegate & Callback Definitions

	//***
	//***
	//  DEFINE DELEGATE FOR USE BY ALL EVENTS & CALLBACKS
	//***
	//***
	
	/// <summary>
	/// This delegate provides the signature for UpdaterAction events
	/// </summary>
	public delegate void UpdaterActionEventHandler(object sender, UpdaterActionEventArgs e);

	internal delegate void BadExitCallback( string appName );

	#endregion


	/// <summary>
	/// The main Application Updater class; entry point for all applications that will consume the Updater's API;
	/// coordinates activities of DownloaderManager(s) 
	/// </summary>
	public class ApplicationUpdateManager
	{
		#region DnldMgrHolder Struct to hold DownloadManager + its thread

		private struct DnldMgrHolder
		{
			public DnldMgrHolder( Thread dnldT, DownloaderManager dnldM )
			{
				dnldThread = dnldT;
				dnldMgr = dnldM;
				restartTimer = null;
			}
			public Thread dnldThread;
			public DownloaderManager dnldMgr;
			public Timer restartTimer;
		}

		
		#endregion
		
		#region Declarations

		private HybridDictionary		_dnldHolders					= null;
		//  guid for uniquely tagging thread names
		private readonly Guid			_guidUniqueName					= new Guid( new byte[] { 0x37, 0x2A, 0x4D, 0x69, 0x63, 0x68, 0x61, 0x65, 0x6C, 0x53, 0x74, 0x75, 0x61, 0x72, 0x74, 0x2A } );
		private const int				TIMEOUT_THREAD_JOIN				= 2 * 1000;

		//  this time is how long the Updater waits before restarting a badly failed download.  The reason to have a delay is that
		//  if we have a disastrous error that is recurrent, we can get into futile loops and a) fill event log and b) make a huge log file
		//  Set this time greater or less depending on how aggressively you want to restart bad updates.  Only set it shorter if you have
		//  some mechanism in place to monitor bad failures, and can correct them before they put much in the event log etc.
		private const int				TIMEOUT_RESTART_UPDATE			= 300 * 1000;
		private const int				TIMEOUT_FIRE_STOPUPDATER		= 0;

		#endregion

		#region Default constructor

		/// <summary>
		/// Default constructor
		/// </summary>
		public ApplicationUpdateManager()
		{		
			DnldMgrHolder holder;

			//  initialize the hybriddict we're using to hold DnldMgrHolder structs
			_dnldHolders = new HybridDictionary();

			#region Configuration Testing

			//  attempt to load configuration first.  If there is an error, throw here and go no further--config is essential
			try
			{
				UpdaterConfiguration config = UpdaterConfiguration.Instance;
			}
			catch( Exception e )
			{
				//  throw right away after logging, we cannot continue
				ExceptionManager.Publish( e );
				throw e;
			}
			#endregion
			
			#region Set Up File-Based Logging

			SetupLogging();

			#endregion

			#region Create Managers, Create Validator + Downloader, Construct Managers with their respective objects (downloader + validator)
			
			//  iterate through applications array (which in turn uses config to get this info)
			//  at each application, create a new DownloaderManager + its objects + attendant thread
			foreach( ApplicationConfiguration application in UpdaterConfiguration.Instance.Applications )
			{
				//  use helper function to create + package a complete DnldMgrHolder + the inner DownloaderManager and all its parts
				holder = SetupDownloadManager( application );
				//  check if app of this name exists already; CANNOT HAVE two apps of same name being updated
				if( _dnldHolders.Contains( application.Name ) )
				{
					string error = ApplicationUpdateManager.TraceWrite( "[ApplicationUpdateManager.ctor]", "RES_EXCEPTION_DuplicateApplicationName", application.Name );
					ConfigurationException configE = new ConfigurationException( error );
					ExceptionManager.Publish( configE );
					throw configE;
				}
				//  add the holder to the hashtable for starting later
				_dnldHolders.Add( application.Name,  holder );
			}
			
			#endregion

			#region Hook ProcessExit

			//  wrap in try in case security not allowing us to hook this, log and continue it's not crucial
			try
			{
				//  hook the AppDomain.ProcessExit event so that we can gracefully interrupt threads and clean up if 
				//  process is killed
				AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
			}
			catch( SecurityException se )
			{
				//  log and continue
				ApplicationUpdateManager.TraceWrite( se, "[ApplicationUpdateManager.CTOR]", "RES_EXCEPTION_ConfigCantLoadInAUConstructor" );
				ExceptionManager.Publish ( se );
			}

			#endregion
		}

	
		/// <summary>
		/// Does all the setup of a DownloaderManager, including using Factories to construct Validators and Downloaders that it will use.
		/// Builds all the stuff up, constructs the DownloaderManager.  Packages the Mgr with its Thread in a DnldMgrHolder struct and returns it.
		/// **NOTE:  this follows a variant of the Builder Pattern, where we delegate constuction of a complex object.
		/// could also have been in a separate Builder or Factory class but small enough to put here.
		/// </summary>
		/// <param name="application">the name of the Updater target application</param>
		/// <returns>DnldMgrHolder struct containing Built DownloaderManager and the Thread it will run on</returns>
		private DnldMgrHolder SetupDownloadManager( ApplicationConfiguration application )
		{	
			DownloaderManager dnldMgr = null;
					
			//  create an instance of a Downloader for DownloaderManager to use:
			IDownloader downloader = DownloaderFactory.Create( UpdaterConfiguration.Instance );			

			//  create instance of Validator for DownloaderManager to use:
			IValidator validator = ValidatorFactory.Create( UpdaterConfiguration.Instance );

			//  populate application jobentry for shared
			DownloadJobStatusEntry dnldJob = new DownloadJobStatusEntry( application.Name, Guid.Empty, JobStatus.Ready );

			//  create the ManualResetEvent "mustStop" which we will use later if Stopping to signal "you must stop" to dnld mgr
			ManualResetEvent mustStop = new ManualResetEvent( false );

			//  create the callback delegate pointed at our RestartUpdater method so we can restart if something fails badly:
			BadExitCallback badExitCbk = new BadExitCallback( this.RestartUpdater );

			//  create the download manager, feed it our instance of actual downloader
			dnldMgr = new DownloaderManager( downloader, validator, dnldJob, mustStop, badExitCbk );

			//  create the thread that this dnldMgr + its other objects will run on
			Thread dnldThread = new Thread( new ThreadStart( dnldMgr.RunDownloader ) );
			dnldThread.Name = "DownloadManagerThread_" + application.Name + "_" + _guidUniqueName.ToString();

			//  create new holder struct for thread + dnldmgr
			DnldMgrHolder holder = new DnldMgrHolder( dnldThread, dnldMgr );

			#region    Event Registrations  

			//  ***  NOTE:  we facade all these underlying events through this class to 
			//  ***			a)  avoid exposing dnldMgr class 
			//  ***			b)  so later if we choose to use a better threading model--that is, NOT allow 
			//  ***				our threads to "venture outside" and possibly dawdle out there (get blocked for instance) 
			//  ***				then it will be easy to spawn a dedicated eventing thread in here.
			//  ***  NOTE:  IF we have more than one app we're updating, then we must hook EACH DOWNLOADMANAGER's events.
			//  ***  the sinking app will have to distinguish which app fired which event from the event args

			//  REGISTER for DownloaderManager's events--we do not want to expose downloader directly, so 
			//  we shuttle its events through this class
			dnldMgr.ServerManifestDownloaded += new UpdaterActionEventHandler( OnServerManifestDownloaded );
			dnldMgr.UpdateAvailable += new UpdaterActionEventHandler( OnUpdateAvailable );
			dnldMgr.DownloadStarted += new UpdaterActionEventHandler( OnDownloadStarted );
			dnldMgr.DownloadCompleted += new UpdaterActionEventHandler( OnDownloadCompleted );
			dnldMgr.ManifestValidated +=new UpdaterActionEventHandler( OnManifestValidated );
			dnldMgr.ManifestValidationFailed +=new UpdaterActionEventHandler( OnManifestValidationFailed );
			dnldMgr.FilesValidated +=new UpdaterActionEventHandler( OnFilesValidated );
			dnldMgr.FilesValidationFailed +=new UpdaterActionEventHandler( OnFilesValidationFailed );

			#endregion

			//  return the packaged DnldMgr
			return holder;
		}


		#endregion

		#region Static Trace Wraps for Logging

		internal static string TraceWrite( Exception e, string location )
		{
			return TraceWrite( e, location, null, null );
		}
		
		
		internal static string TraceWrite( Exception e, string location, string messageKey, params object[] args )
		{
			string message = "";
			//  get recursive error dump string
			StringBuilder sb = new StringBuilder( 5000 );
			ApplicationUpdateManager.RecurseErrorStack( e, ref sb );
			string error = sb.ToString();
			
			if( null != messageKey && null != args )
			{
				message = FormatMessage( messageKey, args );
			}
			Trace.WriteLine("");
			Trace.WriteLine( location + " : " );
			Trace.Indent();
			Trace.WriteLine( message );
			Trace.WriteLine( error );
			Trace.WriteLine("");
			Trace.Unindent();	

			return message ;
		}

		
		internal static string TraceWrite( string location, string messageKey, params object[] args )
		{
			string message = FormatMessage( messageKey, args );
			Trace.WriteLine("");
			Trace.WriteLine( location + " : " );
			Trace.Indent();
			Trace.WriteLine( message );
			Trace.Unindent();	
			return message;
		}
		
		
		private static string FormatMessage( string messageKey, object[] args )
		{
			string message = "";
			if( args.Length < 1 )
			{
				message = Resource.ResourceManager[ messageKey ];
			}
			else
			{
				message = String.Format( CultureInfo.CurrentCulture, Resource.ResourceManager[ messageKey ], args ) ;
			}
			return message;
		}

		
		private static void RecurseErrorStack( Exception e, ref StringBuilder sb )
		{
			if( null != e )
			{
				sb.Append( 
					String.Format( CultureInfo.CurrentCulture, 
					"ERROR: {0}{1}STACK:{2}{3}", 
					e.Message, Environment.NewLine, e.StackTrace, Environment.NewLine ) );
				RecurseErrorStack( e.InnerException, ref sb );
			}
		}

		
		#endregion

		#region Logging Setup

		private void SetupLogging()
		{
			if( null != UpdaterConfiguration.Instance.Logging )
			{
				FileStream fs = null;
			
				//  create time-based log file name
				//  get the dir path for the log file
				string path = UpdaterConfiguration.Instance.Logging.LogPath ;	

				//  if no path return
				if( path.Length > 0 )
				{
					path = Path.GetDirectoryName( UpdaterConfiguration.Instance.Logging.LogPath );
				}
				else
				{
					return;
				}
				
				//  first, see if dir specified exists:
				if( !Directory.Exists( path ) )
				{
					string error = ApplicationUpdateManager.TraceWrite( "[ApplicationUpdateManager.SetupLogging]", "RES_MESSAGE_UpdaterLogDirectoryNotFound", path );
					ExceptionManager.Publish( new ConfigurationException( error ) );
					return;
				}

				//  strip off all except actual file name
				string logName = Path.GetFileNameWithoutExtension( UpdaterConfiguration.Instance.Logging.LogPath );

				//  append the month-day-year-hour-minute-second for uniqueness
				logName = 
					logName + 
					"_" + 
					DateTime.Now.ToString( Resource.ResourceManager[ "RESX_DateTimeToStringFormatLogFile" ],  CultureInfo.CurrentCulture  ) + 
					".log";

				//  recombine path
				logName = Path.Combine( path, logName );

				//  attempt to create the file with the given path
				try
				{
					fs = File.Create( logName );
				}
				catch( Exception ex )
				{
					//  if we can't trace, and move along--logging not worth pitching the whole update
					string error = ApplicationUpdateManager.TraceWrite( ex, "[ApplicationUpdateManager.SetupLogging]", "RES_MESSAGE_UpdaterLogCannotBeWritten", logName );
					ExceptionManager.Publish( new ConfigurationException( error, ex ) );
					return;
				}
				finally
				{
					if ( null != fs )
					{
						fs.Close();
					}
				}

				//  add this log file to trace listeners
				Trace.Listeners.Add( new TextWriterTraceListener( logName ) );

				//  set up trace to auto-flush
				Trace.AutoFlush = true;
			}
		}


		#endregion

		#region AppDomain ProcessExit Event Handler
		
		private void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			//  when Process is exiting, we should stop the threads so they can clean up any resources they are using:
			StopUpdater();
		}

		
		#endregion

		#region Event Definitions

		// *
		//**    COMMENT FOR ALL FOLLOWING EVENTS:    ***
		// The underlying DownloadManager raises these events; 
		// here in ApplicationUpdateManager we sink the internal class events and re-raise them:
		// provides a facade so we don't have to expose the events publicly and complicate the API with internal classes.
		// *
		// *

		// *
		// *    EVENTS FOR DOWNLOADER    *
		// *
		
		//    ServerManifestDownloaded EVENT    
		/// <summary>
		/// signifies the Server Manifest has been downloaded locally and is ready for processing
		/// </summary>
		public event UpdaterActionEventHandler ServerManifestDownloaded;		
		private void OnServerManifestDownloaded( object sender, UpdaterActionEventArgs e )
		{
			if( null != ServerManifestDownloaded )
			{
				ServerManifestDownloaded( this, e );
			}
		}

		/// <summary>
		/// signals that a new version is available for download on the server.
		/// </summary>
		public event UpdaterActionEventHandler UpdateAvailable;		
		private void OnUpdateAvailable( object sender, UpdaterActionEventArgs e )
		{
			if( null != UpdateAvailable )
			{
				UpdateAvailable( this, e );
			}
		}
		
		//    DownloadStarted EVENT    
		/// <summary>
		///signals download started with UpdaterActionEventArgs containing manifest info
		/// </summary>
		public event UpdaterActionEventHandler DownloadStarted;		
		private void OnDownloadStarted( object sender, UpdaterActionEventArgs e )
		{
			if( null != DownloadStarted )
			{
				DownloadStarted( this, e );
			}
		}


		
		//    DownloadCompleted EVENT    
		/// <summary>
		/// signals download complete with server manifest info
		/// </summary>
		public event UpdaterActionEventHandler DownloadCompleted;		
		private void OnDownloadCompleted( object sender, UpdaterActionEventArgs e )
		{
			if( null != DownloadCompleted )
			{
				DownloadCompleted( this, e );
			}
		}

		

		
		// *
		// *    EVENTS FOR VALIDATOR    *
		// *

		//    ManifestValidated EVENT    
		/// <summary>
		/// signals the server manifest validated successfully.
		/// </summary>
		public event UpdaterActionEventHandler ManifestValidated;		
		private void OnManifestValidated( object sender, UpdaterActionEventArgs e )
		{
			if( null != ManifestValidated )
			{
				ManifestValidated(  this, e  );
			}
		}


		//    ManifestValidationFailed EVENT    
		/// <summary>
		/// signals the server manifest FAILED to validate.
		/// </summary>
		public event UpdaterActionEventHandler ManifestValidationFailed;
		private void OnManifestValidationFailed( object sender, UpdaterActionEventArgs e )
		{
			if( null != ManifestValidationFailed )
			{
				ManifestValidationFailed( this, e );
			}
		}


		//    FilesValidated EVENT    
		/// <summary>
		/// signals the downloaded files validated successfully.
		/// </summary>
		public event UpdaterActionEventHandler FilesValidated;
		private void OnFilesValidated( object sender, UpdaterActionEventArgs e )
		{
			if( null != FilesValidated )
			{
				FilesValidated( this, e );
			}
		}

		//    FilesValidationFailed EVENT    		
		/// <summary>
		/// signals the downloaded files FAILED to validate successfully.
		/// </summary>
		public event UpdaterActionEventHandler FilesValidationFailed;
		private void OnFilesValidationFailed( object sender, UpdaterActionEventArgs e )
		{
			if( null != FilesValidationFailed )
			{
				FilesValidationFailed( this, e );
			}

		}
		#endregion



		#region Start/Stop methods

 
		/// <summary>
		/// Updater start method
		/// </summary>
		/// <remarks>
		/// This method iterates through the applications to update, and starts each on its own thread
		/// </remarks>
		public void StartUpdater()
		{

            //  lock the holder collection while we iterate through it
			lock( _dnldHolders.SyncRoot )
			{
				//  cycle through collection of holders, starting each thread 
				foreach( DictionaryEntry de in _dnldHolders )
				{
					DnldMgrHolder holder = (DnldMgrHolder)de.Value;
					//  start the thread
					holder.dnldThread.Start();

					//  announce we have started:
					ApplicationUpdateManager.TraceWrite( 
						"[ApplicationUpdateManager.StartUpdater]", 
						"RESX_MESSAGE_UpdaterStarted", 
						holder.dnldMgr.ApplicationName, 
						DateTime.Now.ToString(  Resource.ResourceManager[ "RESX_DateTimeToStringFormat" ],  CultureInfo.CurrentCulture  )  );
				}
			}
		}


		/// <summary>
		/// Stops all updates
		/// </summary>
		public void StopUpdater()
		{
			StopUpdater( null );
		}

		
		/// <summary>
		/// Stops a specific update by application name
		/// </summary>
		/// <param name="appName">the name of the application to stop updating</param>
		public void StopUpdater( string appName )
		{
			//  use background threadpool thread to fire actual StopUpdaterHelper;
			//  IN CASE we are called from one of our own threads via a UI, we need to uncouple here and 
			//  return as quickly as possible to defend against this...
			ThreadPool.QueueUserWorkItem( new WaitCallback( this.StopUpdaterHelper ), (object)appName );
		}


		/// <summary>
		/// stops all updates if no appname was specified, but only one if appname specified to original StopUpdater( name ) call
		/// </summary>
		private void StopUpdaterHelper( object state )
		{
			//  unbundle appname string from object passed in 			
			string appName = (string)state;

			//  try-catch this, it is possible that multiple reentries will try to stop same app, 
			//  just swallow and log
			try
			{
				//  lock the collection, in all cases we want to prevent other accesses to it while we're in here.
				lock( _dnldHolders.SyncRoot )
				{
					if( null != appName )
					{
						//  stop JUST the named app
						StopUpdaterHelper( appName );
						//  remove from collection
						_dnldHolders.Remove( appName );
					}
						//  no name passed in, so stop ALL updates
					else
					{ 
						//  cycle through holder collection, stopping ALL UPDATES
						foreach( DictionaryEntry de in _dnldHolders )
						{
							StopUpdaterHelper( ((DnldMgrHolder)de.Value).dnldMgr.ApplicationName );
						}

						//  clear collection
						_dnldHolders.Clear();			
					}
				}
			}
			catch( Exception ex )
			{
				//  then log error but don't propagate error
				string error = ApplicationUpdateManager.TraceWrite( 
					ex,
					"[ApplicationUpdateManager.StopUpdaterHelper]", 
					"RES_EXCEPTION_ErrorStopUpdater", appName );
				Exception e = new Exception( error, ex );
				ExceptionManager.Publish( e );
			}

			//  flush trace
			Trace.Flush();
		}


		/// <summary>
		/// Internal helper, stops the specified DownloadManager by application name 
		/// </summary>
		private void StopUpdaterHelper( string appName )
		{
			bool isGoodStop = false;
			            
			//  unpackage objects from holder 
			DownloaderManager dnldMgr = ((DnldMgrHolder)_dnldHolders[ appName ]).dnldMgr;
			Thread dnldThread = ((DnldMgrHolder)_dnldHolders[ appName ]).dnldThread;				
			
			//  signal downloaderMgr it's time to stop, chance to exit gracefully:
			if( null != dnldMgr )
			{
				dnldMgr.MustStopUpdating.Set();
			}

			//  check if the thread is event started
			if( ( null != dnldThread ) && ( System.Threading.ThreadState.Unstarted != dnldThread.ThreadState ) )
			{
				//  wait to join for reasonable timeout
				isGoodStop = dnldThread.Join( TIMEOUT_THREAD_JOIN );
			}
			else
			{
				isGoodStop = true;
			}

			//  if it's not a clean join, then interrupt thread
			if( !isGoodStop )
			{
				dnldThread.Interrupt();				
				//  log problem
				ApplicationUpdateManager.TraceWrite( "[ApplicationUpdateManager.StopUpdater]", "RES_StopUpdaterInterruptThread", dnldThread.Name );
			}	
		

			//  announce we are stopping:
			ApplicationUpdateManager.TraceWrite( 
				"[ApplicationUpdateManager.StopUpdater]", 
				"RESX_MESSAGE_UpdaterStopped", 
				dnldMgr.ApplicationName, 
				DateTime.Now.ToString(  Resource.ResourceManager[ "RESX_DateTimeToStringFormat" ],  CultureInfo.CurrentCulture  )  );
				
	
		}


		/// <summary>
		/// Restarts a badly-exited DownloaderManager--managers may throw fatal errors, we want to restart them to 
		/// be sure target app gets updated.  Uses a Timer internally to provide a delay before restarting to prevent 
		/// rapid-cycling of restarts; the timer in turn calls this method's helper to actually restart.
		/// </summary>
		/// <param name="appName">name of the application update to restart (name defined in config)</param>
		private void RestartUpdater( string appName )
		{
			//    NOTE:  to prevent runaway activity, we must wait to restart the update.  If the app is misconfigured in certain
			//  ways, it will do runaway restarts (that is, if DnldMgr has fatal exception in tight loop)
			//  therefore use timer to delay a while (actually whatever value in milliseconds by constant) before restarting

			DnldMgrHolder holder;
			Timer restartTimer = null;

			//  lock table
			lock( _dnldHolders.SyncRoot )
			{
				//  remove the offending entry from our holder of managers:
				if( _dnldHolders.Contains( appName ) )
				{
					_dnldHolders.Remove( appName );
				}
				//  get another
				holder = SetupDownloadManager( UpdaterConfiguration.Instance[ appName ] ) ;
				//  add it to our collection 
				_dnldHolders.Add( appName, holder );
			}

			//  create the delegate the Timer will use (on background pool thread) to fire up our helper below:
			TimerCallback timerDelegate = new TimerCallback( this.RestartUpdaterHelper );

			//  create the timer, point it at the RestartUpdaterHelper
			//  use options:  set wait time before firing to TIMEOUT_RESTART_UPDATE, and set it NOT to repeatedly call w/ Timeout.Infinite
			restartTimer = new Timer( timerDelegate, holder, TIMEOUT_RESTART_UPDATE, Timeout.Infinite );

			
			//  put a handle to timer in the holder struct, so RestartUpdaterHelper can Dispose the timer when called
			holder.restartTimer = restartTimer;
			
		}

		
		/// <summary>
		/// This method helps restart the app.  It expects a DnldMgrHolder to be packaged in the object argument; it unpacks this holder
		/// and starts the downloadermanager it contains on the thread it contains
		/// </summary>
		/// <param name="state">an object wrapper around a DnldMgrHolder</param>
		private void RestartUpdaterHelper( object state )
		{
			DnldMgrHolder holder;

			//  extract our holder from the callback's object passed in here as "state"
			holder = (DnldMgrHolder)state;

			//  dispose the timer
			if( null != holder.restartTimer )
			{
				holder.restartTimer.Dispose();
			}

			//  start it
			holder.dnldThread.Start();
			//  log restart
			ApplicationUpdateManager.TraceWrite( 
				"[ApplicationUpdateManager.RestartUpdater]", 
				"RES_RestartedUpdater", 
				holder.dnldMgr.ApplicationName, 
				DateTime.Now.ToString(  Resource.ResourceManager[ "RESX_DateTimeToStringFormat" ],  CultureInfo.CurrentCulture  ) );

		}


		#endregion

	}
}
