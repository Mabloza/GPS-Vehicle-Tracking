
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// DownloaderManager.cs
//
// Manages and coordinates validators and downloaders.
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml;

using Microsoft.ApplicationBlocks.ExceptionManagement;
using Microsoft.ApplicationBlocks.ApplicationUpdater.Interfaces;

namespace Microsoft.ApplicationBlocks.ApplicationUpdater
{


	internal class DownloadJobStatusEntry
	{
		
		#region Declarations

		private string			_appName				= "";
		private Guid			_jobId					= Guid.Empty;
		private JobStatus		_jobStatus				= JobStatus.Downloading;

		#endregion

		#region Constructors

		public DownloadJobStatusEntry( string appName, Guid jobId, JobStatus jobStatusValue )
		{
			this._appName = appName;
			this._jobId = jobId;
			this._jobStatus = jobStatusValue;
		}

		
		#endregion

		#region Properties

		public string ApplicationName
		{
			get
			{
				return _appName;
			}
			set
			{
				_appName = value;
			}
		}

		public Guid JobId
		{
			get
			{
				return _jobId;
			}
			set
			{
				_jobId = value;
			}
		}

		public JobStatus Status
		{
			get
			{
				return _jobStatus;
			}
			set
			{
				_jobStatus = value;
			}
		}

		
		#endregion
	}




	/// <summary>
	/// 1)  Manages the Downloader and Validator, 
	/// 2)  coordinates their activities, 
	/// 3)  copies the files/directories created
	/// 4)  raises events at each step
	/// </summary>
	internal class DownloaderManager
	{
		#region Declarations

		private ApplicationConfiguration	_application						= null;
		private ServerApplicationInfo		_server								= null;
		private IDownloader					_downloader							= null;
		private IValidator					_validator							= null;
		private DownloadJobStatusEntry		_dnldJob							= null;
		private ManualResetEvent			_mustStopUpdating					= null;
		private Thread						_ippThread							= null;
		private BadExitCallback				_badExitCbk							= null;
		private DateTime					_lastUpdate							= DateTime.Now;
		private int							_tickCountAtStart					= Environment.TickCount;
		private const int					MILLISECOND_WAIT_TIMEOUT			= 500;
		private const int					MILLISECOND_WAIT_POLL_LOOP			= 2 * 1000;
		private const int					MILLISECOND_WAIT_TIMEOUT_IPP		= 500;
		//  guids for tagging end-of-download messages:
		private readonly Guid				_guidEndDnld0						= new Guid( new byte[] { 0x37, 0x2A, 0x4D, 0x69, 0x63, 0x68, 0x61, 0x65, 0x6C, 0x53, 0x74, 0x75, 0x61, 0x72, 0x74, 0x2A } );
		private readonly Guid				_guidEndDnld1						= new Guid( new byte[] { 0x62, 0xc2, 0x5F, 0x47, 0x72, 0x61, 0x65, 0x6D, 0x65, 0x4D, 0x61, 0x6C, 0x63, 0x6F, 0x6D, 0x5F } );
		private readonly Guid				_guidEndDnld2						= new Guid( new byte[] { 0x4c, 0x91, 0x15, 0x20, 0x45, 0x64, 0x4A, 0x65, 0x7A, 0x69, 0x65, 0x72, 0x73, 0x6B, 0x69, 0x20 } );
		private readonly Guid				_guidEndDnld3						= new Guid( new byte[] { 0xd0, 0x1c, 0xc3, 0xf5, 0x5F, 0x4A, 0x61, 0x79, 0x20, 0x53, 0x61, 0x77, 0x79, 0x65, 0x72, 0x5F } );
		private readonly Guid				_guidEndDnld4						= new Guid( new byte[] { 0x41, 0xf9, 0x8a, 0xa0, 0x8e, 0x20, 0x4A, 0x61, 0x6D, 0x69, 0x65, 0x43, 0x6F, 0x6F, 0x6C, 0x20 } );
		

		#endregion

		#region Constructors

		/// <summary>
		/// Only constructor for DownloaderManager.  All parameters required for proper function.
		/// </summary>
		/// <param name="downloader">instance of IDownloader implementation</param>
		/// <param name="validator">instance of IValidator implementation</param>
		/// <param name="downloadJob">encapsulation of Job information in DownloadJobStatusEntry instance, such as app name job status job id</param>
		/// <param name="mustStop">an MRE used by Updater to tell this object it must clean up and leave or be Interrupt()ed</param>
		/// <param name="badExitCbk">delegate instance this class will call on on unrecoverable error, such as an unanticpated exception that compromises too badly to contine</param>
		public DownloaderManager
			( 
			IDownloader downloader, 
			IValidator validator, 
			DownloadJobStatusEntry downloadJob, 
			ManualResetEvent mustStop, 
			BadExitCallback badExitCbk 
			)
		{
			_downloader = downloader;
			_validator = validator;
			_dnldJob = downloadJob;
			_mustStopUpdating = mustStop;

			//  using our app's name, look up the application object (ApplicationConfiguration) in array of applications--
			//  since WE are working on one, but there may be many
			_application = UpdaterConfiguration.Instance[ _dnldJob.ApplicationName ];

			//  set out internal delegate instance of BadExitCallback.
			//  this is the function pointer we'll use to notify UpdaterManager of really poor exits (fatal exceptions etc.) 
			_badExitCbk = badExitCbk;
		}
		

		#endregion

		#region Event Definitions
		
		//    DOWNLOAD EVENTS  
		internal event UpdaterActionEventHandler ServerManifestDownloaded;		
		private void OnServerManifestDownloaded(   )
		{
			if( null != ServerManifestDownloaded )
			{
				ServerManifestDownloaded(  this, new UpdaterActionEventArgs( _server, _application.Name )  );
			}
		}


		
		internal event UpdaterActionEventHandler UpdateAvailable;		
		private void OnUpdateAvailable(  )
		{
			if( null != UpdateAvailable )
			{
				UpdateAvailable(  this, new UpdaterActionEventArgs( _server, _application.Name )  );
			}
		}


		internal event UpdaterActionEventHandler DownloadStarted;
		private void OnDownloadStarted(  )
		{
			if( null != DownloadStarted )
			{
				DownloadStarted( this, new UpdaterActionEventArgs( _server, _application.Name ) );
			}
		}


		internal event UpdaterActionEventHandler DownloadCompleted;
		private void OnDownloadCompleted(  )
		{
			if( null != DownloadCompleted )
			{
				DownloadCompleted( this, new UpdaterActionEventArgs( _application.Name ) );
			}
		}




		//    VALIDATE EVENTS  

		
		//
		//**    COMMENT FOR ALL FOLLOWING EVENTS:    *
		// The update manager always sinks these events and forwards them through its own same-named events, providing  **
		// a facade so we don't have to expose the events publicly and complicate the API with many internal classes.  ***
		// ***
		//

		//    ManifestValidated EVENT    
		/// <summary>
		///   THIS EVENT:  signals the server manifest validated successfully.
		/// </summary>
		internal event UpdaterActionEventHandler ManifestValidated;		
		private void OnManifestValidated(  )
		{
			if( null != ManifestValidated )
			{
				ManifestValidated(  this, new UpdaterActionEventArgs( _server, _application.Name )  );
			}
		}


		//    ManifestValidationFailed EVENT    
		/// <summary>
		///   THIS EVENT:  signals the server manifest FAILED to validate.
		/// </summary>
		internal event UpdaterActionEventHandler ManifestValidationFailed;
		private void OnManifestValidationFailed(  )
		{
			if( null != ManifestValidationFailed )
			{
				ManifestValidationFailed( this, new UpdaterActionEventArgs( _server, _application.Name ) );
			}
		}


		//    FilesValidated EVENT    
		/// <summary>
		///   THIS EVENT:  signals the downloaded files validated successfully.
		/// </summary>
		internal event UpdaterActionEventHandler FilesValidated;
		private void OnFilesValidated(  )
		{
			if( null != FilesValidated )
			{
				FilesValidated( this, new UpdaterActionEventArgs( _server, _application.Name ) );
			}
		}


		//    FilesValidationFailed EVENT    
		/// <summary>
		///   THIS EVENT:  signals the downloaded files FAILED to validate successfully.
		/// </summary>
		internal event UpdaterActionEventHandler FilesValidationFailed;
		private void OnFilesValidationFailed(  )
		{
			if( null != FilesValidationFailed )
			{
				FilesValidationFailed( this, new UpdaterActionEventArgs( _server, _application.Name ) );
			}
		}



		#endregion

		#region Properties for MustStopUpdating and ApplicationName

		public ManualResetEvent MustStopUpdating
		{
			get{ return _mustStopUpdating; }
		}

		
		public string ApplicationName
		{
			get{ return _dnldJob.ApplicationName; }
		}

		
		#endregion

		///**
		///***    MAIN METHOD                        
		///**
		/// <summary>
		/// This is the main method.
		/// </summary>
		/// <remarks>This method does the following tasks:<list>
		/// <item>1. Get the server metadata information</item>
		/// <item>2. Compare server version with client versoins</item>
		/// <item>3. Start the download process if some local files are old.</item>
		/// </list></remarks>
		public void RunDownloader()
		{
			bool isManifestDownloadSuccess = false;
			bool isEventSignaled = false;

			//  wrap whole loop in try--we need to catch ThreadInterruptedException and cleanup downloader
			try
			{
				#region While-Loop with Polling Wait and Inner Try-Catch 
				while( true )
				{
					ApplicationUpdateManager.TraceWrite( "[DownloaderManager.RunDownloader]", "RES_MESSAGE_CheckingOnUpdates", _application.Name );

					//  set the job status to Ready, we want to start fresh--
					//  on previous passes it may have been set to Validating, but if we are back here in loop we have
					//  completed one full polling pass
					_dnldJob.Status = JobStatus.Ready;

					//  set the member tickcount so we have timestamp for loop's start
					_tickCountAtStart = Environment.TickCount;
					
					//  get the Manifest from server
					isManifestDownloadSuccess = IsServerManifestDownloaded();

					if( isManifestDownloadSuccess )
					{
						BeginFileDownloads();
					}	
					
					//  now go to the main polling loop, which waits the full pollingInterval.  
					//  during the wait loop it checks download job status
					isEventSignaled = IsPollerIntervalElapsed();

					//  check if we've been signaled
					if ( isEventSignaled )
					{
						break;
					}


				}  //  WHILE
				#endregion

			}  //  TRY

				//  if ThreadInterruptedException, we are being told to stop.  Publish, clean up, and leave (see finally)
			catch( ThreadInterruptedException tie )
			{			
				ApplicationUpdateManager.TraceWrite( tie, "[DownloaderManager.RunDownloader]", "RES_EXCEPTION_ThreadInterruptedException", Thread.CurrentThread.Name );
				ExceptionManager.Publish( tie );
			
				//  OK to swallow this one, we KNOW who did it to us--the ApplicationUpdateManager--and we
				//  just go to Finally now; clean up and unwind stack.  Then AUManager (AU==gold) will Join() us gracefully.
			}
			catch( Exception e )
			{						
				ApplicationUpdateManager.TraceWrite( e, "[DownloaderManager.RunDownloader]" );
				
				ExceptionManager.Publish( e );
				//  SWALLOW this exception, because we want this application to keep trying to update;
				//  error may be transient...try restarting via BadExitCallback

				//  AND LASTLY:  use the "BadExit" callback to notify UpdaterManager something rotten has happened; 
				//  it's up to Updater to figure out what to do but in current implementation Updater simply waits a while then restarts this update
				_badExitCbk( _application.Name );
			}
			finally
			{			
				//  let downloader clean up
				_downloader.Dispose();
				//  let validator clean up
				_validator.Dispose();
				//  delete temp files + manifest
				RemoveManifestAndTempFiles();
				//  stop the IPP thread
				StopPostProcessorThread();

			}  //  FINALLY

		}

		

		#region ____* Download Code *____


		/// <summary>
		/// Download the metadata from the server using the synchronous method
		/// from the IDownloader interface.
		/// </summary>
		/// <returns>true if the manifest downloaded and exists as a file at the correct path</returns>
		private bool IsServerManifestDownloaded()
		{
			bool isManifestValid = false;
			bool isManifestPresent = false;

			ApplicationUpdateManager.TraceWrite( 
				"[DownloaderManager.IsServerManifestDownloaded]", 
				"RES_MESSAGE_DownloadManifestStarted", 
				_application.Name, 
				DateTime.Now.ToString(  Resource.ResourceManager[ "RESX_DateTimeToStringFormat" ], CultureInfo.CurrentCulture ) );
				
			ApplicationUpdateManager.TraceWrite( 
				"[DownloaderManager.IsServerManifestDownloaded]", 
				"RES_MESSAGE_SourceFileName", 
				_application.Server.ServerManifestFile );
				
			ApplicationUpdateManager.TraceWrite( 
				"[DownloaderManager.IsServerManifestDownloaded]", 
				"RES_MESSAGE_DestFileName", 
				_application.Server.ServerManifestFileDestination );


			//  wrap; we don't want to fall through if synch download of manifest fails, we want to wait for next poll time
			try
			{
				//Get the server metadata file (server manifest, commonly called "ServerConfig.xml")
				_downloader.Download(
					_application.Server.ServerManifestFile, 
					_application.Server.ServerManifestFileDestination, 
					TimeSpan.FromMilliseconds( _application.Server.MaxWaitXmlFile ) );

				// **
				// ***  HERE WE POPULATE OUR _server OBJECT TO ENCAPSULATE MANIFEST INFO  ***
				// **
				//  load the _server (ServerApplicationInfo) object w/ manifest data--THIS IS THE ONLY PLACE WE NEED POPULATE THIS
				_server = ServerApplicationInfo.Deserialize( _application.Server.ServerManifestFileDestination );
			}
			catch( Exception e )
			{
				//  if exception during manifest download, log it but swallow here;
				//  we don't want to drop out of main loop and end up restarting, we want to wait full interval
				string message = ApplicationUpdateManager.TraceWrite( e, "[DownloaderManager.IsServerManifestDownloaded]", "RES_EXCEPTION_MetadataCantBeDownloadedOrNotValid", _application.Name );
				Exception ex = new Exception( message, e );
				ExceptionManager.Publish( ex );

				//  return false to alert that manifest download NOT successful
				return false;
			}


			//  **    FIRE MANIFEST DOWNLOADED EVENT    **
			ApplicationUpdateManager.TraceWrite( "[DownloaderManager.IsServerManifestDownloaded]", "RES_MESSAGE_DownloadManifestCompleted", _application.Server.ServerManifestFileDestination );
			this.OnServerManifestDownloaded(  );

			//  **
			//  **    VALIDATE MANIFEST FILE    **
			//  **
			isManifestValid = ValidateManifestFile();

			//  check if it's still present
			isManifestPresent = File.Exists( _application.Server.ServerManifestFileDestination );
			
			
			return ( isManifestPresent && isManifestValid );
		}
		
		
		private void BeginFileDownloads()
		{
			bool isUpdateNeeded = false;
			bool isMustStopSignaled = false;

			try 
			{
				//Compare metadata server file
				isUpdateNeeded = IsClientUpdateNeeded();

				if( isUpdateNeeded )
				{
					//  fire event to notify "UpdateAvailable" 
					//  NOTE:  IF APPLICATION BLOCKS THIS THREAD AND CALLS StopUpdater(), that means this update is being
					//  aborted.  Handle gracefully.
					OnUpdateAvailable(  );

					//  !!!!  CHECK if the MRE "MustStopUpdating" is signaled, exit if so; we've been told to stop politely, 
					//  don't make us Interrupt()
					isMustStopSignaled = _mustStopUpdating.WaitOne( MILLISECOND_WAIT_TIMEOUT, false );
					if( isMustStopSignaled )
					{
						return;
					}
						
					ApplicationUpdateManager.TraceWrite( "[DownloaderManager.BeginFileDownloads]", "RES_MESSAGE_FilesMustBeUpdated", _application.Name );
					
					//  **
					//  			downloader, "DOWNLOAD ALL THESE FILES"--ASYNCH		  
					//  **
					//  Get Job GUID from downloader
					//  put job GUID in our jobstatus object
					_dnldJob.JobId = DownloadApplicationFiles(  );
					//  set job status to downloading
					_dnldJob.Status = JobStatus.Downloading;
					

					//  **
					//    FIRE THE DOWNLOADSTARTED EVENT                                  
					//  **
					OnDownloadStarted(  );

						
					ApplicationUpdateManager.TraceWrite( "[DownloaderManager.BeginFileDownloads]", "RES_MESSAGE_DnldJobStatusUpdated", _dnldJob.JobId, _application.Name );
				}
				else
				{ 										
					ApplicationUpdateManager.TraceWrite( "[DownloaderManager.BeginFileDownloads]", "RES_MESSAGE_NoNewVersionOnServer", _application.Name );
					
					//  we have compared versions.  We have found we have most up-to-date.  Now delete ServerManifest locally
					RemoveManifestAndTempFiles();

					//  set job status to Validating to "dequeue" it
					_dnldJob.Status = JobStatus.Validating;
				}
				
			}
				//  do not swallow TIE's, throw them
			catch( ThreadInterruptedException tie )
			{
				throw tie;
			}
			catch( Exception e )
			{
				ApplicationUpdateManager.TraceWrite( e, "[DownloaderManager.BeginFileDownloads]", "RES_GeneralUpdaterError", _application.Name );
				ExceptionManager.Publish( new Exception( Resource.ResourceManager[ "RES_GeneralUpdaterError", _application.Name ], e ) );
			}
		}

		
		/// <summary>
		///  Determine the files to be downloaded and call the IDownloader to perform the job
		/// </summary>
		/// <returns>the job id guid</returns>
		private Guid DownloadApplicationFiles()
		{
			string fullSourcePath = "";
			string fullDestinationPath = "";

			//Get the location of files on server
			string sourceLocation = _server.UpdateLocation;

			//  ensure terminal slash
			sourceLocation = FileUtility.AppendSlashURLorUNC( sourceLocation );

			//Get the base dir where to copy files
			string baseDir = _application.Client.TempDir;
			
			ApplicationUpdateManager.TraceWrite( "[DownloaderManager.DownloadApplicationFiles]", "RES_MESSAGE_DownloadingAppFiles", _application.Name, _application.Client.TempDir );
			
			//Prepare remote and local URLs

			ArrayList sourceFiles = new ArrayList();
			ArrayList targetFiles = new ArrayList();
			try
			{
				foreach( FileConfig file in _server.Files )
				{
					//  set full source/destination paths complete with filename:
					fullSourcePath = sourceLocation + file.Name;
					fullDestinationPath = baseDir + file.UNCName;

					ApplicationUpdateManager.TraceWrite( 
						"[DownloaderManager.DownloadApplicationFiles]", "RES_MESSAGE_SettingUpFileForDownload" , 
						fullSourcePath , fullDestinationPath );
					
					//  add the source to our source arraylist
					sourceFiles.Add( fullSourcePath );

					//  add the destination to our destination arraylist
					targetFiles.Add( fullDestinationPath );

					//  ensure that the local target directory exists
					//  use utility function to extract root path from given file path--
					//  thus "C:\foo\file.txt" returns "C:\foo\"
					if( !Directory.Exists( FileUtility.GetRootDirectoryFromFilePath( fullDestinationPath ) ) )
						Directory.CreateDirectory( FileUtility.GetRootDirectoryFromFilePath( fullDestinationPath ) );
				}
			}// TRY
			catch( ThreadInterruptedException tie )
			{
				//  rethrow don't swallow tie, need to clean up
				throw tie;
			}
			catch( Exception e )
			{
				string error = ApplicationUpdateManager.TraceWrite( 
									e, 
									"[DownloaderManager.DownloadApplicationFiles]", 
									"RES_EXCEPTION_SettingUpDownloadDirectories", fullSourcePath, fullDestinationPath );

				//  wrap exception in our exception and publish; then throw the raw exception up
				ExceptionManager.Publish( 
					new ApplicationUpdaterException( Resource.ResourceManager[ "RES_EXCEPTION_SettingUpDownloadDirectories" ], e ) );
				throw e;			
			}

			try
			{
				//  send this "job", both source and destination arraylists (as arrays) to the downloader
				return _downloader.BeginDownload( 
					(string[])sourceFiles.ToArray( typeof( string ) ),
					(string[])targetFiles.ToArray( typeof( string ) ) );
			}
				//  don't swallow tie
				//  thread must interrupt gracefully.
			catch( ThreadInterruptedException tie )
			{
				throw tie;
			}
			catch( Exception e )
			{
				ApplicationUpdateManager.TraceWrite( 
									e, 
									"[DownloaderManager.DownloadApplicationFiles]", 
									"RES_EXCEPTION_UsingDownloader", 
									_downloader.GetType().FullName, 
									_downloader.GetType().AssemblyQualifiedName );

				throw e;
			}
		}
		
			
		/// <summary>
		/// This figures out how many milliseconds to wait--or, if using ExtendedFormat, queries the helper to find if time has elapsed.  If time 
		/// has elapsed, it returns (true if the MRE is set, signalling that ApplicationUpdaterManager has signalled to stop)
		/// allowing a new download cycle to begin.  Otherwise, it loops internally, polling the IDownloader for job status.
		/// 
		/// </summary>
		/// <returns>true if we were interrupted (The MustStopUpdating event is signaled)</returns>
		private bool IsPollerIntervalElapsed()
		{
			double mSecToWaitTotal = 0;
			double mSecWaitedSoFar = 0;
			double pollingInterval = 0;
			bool isMustStopSignaled = false;
			bool isWaitTimeElapsed = false;

			//  check if polling type is extended-format; if so don't try parsing an int from it
			if( PollingType.ExtendedFormat != UpdaterConfiguration.Instance.Polling.Type )
			{
				pollingInterval = double.Parse(  UpdaterConfiguration.Instance.Polling.Value ,  CultureInfo.CurrentCulture );
			}

			mSecToWaitTotal = GetPollingIntervalMilliseconds( pollingInterval, UpdaterConfiguration.Instance.Polling.Type );

			//    NOTE:  slightly tricky loop here.
			//  it must satisfy three requirements:
			//		1)  we must remain responsive to UpdaterManager in this loop while polling our IDownloader for job status
			//		2)  we must wait a certain time to poll for a new download
			//		3)  we must poll the downloader for completed jobs, and go to validation when job available
			//	so, we:
			//		a)  cache the tickcount when we started all this (in RunDownloader) for reference
			//		b)  check if UpdateManager has told us to stop (isMustStopSignaled)
			//		c)  poll the downloader
			//		d)  if we're using extended format wait, check if THAT has elapsed
			//		e)  finally get the total loop time by subtracting current tickcount from the cached one;
			//			if we have run out of time or isMustStop is signaled, we leave the loop.  
			//		f)  if there was a major error in downloader, exit
			//		g)  if time waited seems very long (> 2* poll time) then download is taking too long, 
			//			assume failure and exit...UNLESS we are using extended time, in which case we just wait
			//	

			while(  ( !isWaitTimeElapsed ) || ( JobStatus.Downloading == _dnldJob.Status ) )
			{					
				//  query downloader for job status; it will put status in _dnldJob.Status
				CheckDownloadCompleteOrError();

				//  if using Extended Format poll type--check if that helper class tells us we've waited long enough.
				if( PollingType.ExtendedFormat == UpdaterConfiguration.Instance.Polling.Type )
				{
					if( ExtendedFormatHelper.IsExtendedExpired( UpdaterConfiguration.Instance.Polling.Value, _lastUpdate, DateTime.Now ) )
					{
						_lastUpdate = DateTime.Now;
						isWaitTimeElapsed = true;
					}
				}  // IF

				//  calculate the total waited time based on TickCount since started this method
				//  ADD the poll wait time, since we will pass it again before leaving this loop
				mSecWaitedSoFar = ( ( Environment.TickCount - _tickCountAtStart ) + MILLISECOND_WAIT_POLL_LOOP );

				//  check if allowable time has elapsed, if so set flag isWaitTimeElapsed so main can continue
				if( mSecWaitedSoFar > mSecToWaitTotal )
				{
					isWaitTimeElapsed = true;
				}

				//  NOTE:  JUST IN CASE, we do sanity check here:  if we have been waiting for a download for > 2
				//  polling intervals, then FORCE EXIT, something must be wrong
				//  if using extended format, don't do this check
				if( mSecWaitedSoFar > ( 2 * mSecToWaitTotal ) && !( PollingType.ExtendedFormat == UpdaterConfiguration.Instance.Polling.Type ) )
				{
					//  if we've waited this long, the job is hung and we need to recycle--throw
					string error = ApplicationUpdateManager.TraceWrite( "[DownloaderManager.IsPollerIntervalElapsed]", "RES_EXCEPTION_WaitedMoreThanTwicePollPeriod", mSecWaitedSoFar, _dnldJob.ApplicationName );
					Exception ex = new Exception( error );
					throw ex;
				}

				//  do a fixed wait on MustStopUpdating, 5 seconds acc. to constant in declarations...
				isMustStopSignaled = _mustStopUpdating.WaitOne( MILLISECOND_WAIT_POLL_LOOP, false );

				//  if MustStop has been signalled, we must leave the loop regardless
				if( isMustStopSignaled )
				{
					break;
				}
			}			
		
			//  finally return isEventSignaled to inform caller of whether we were told to stop during the wait
			return isMustStopSignaled ;
		}

		
		/// <summary>
		/// Checks job status; if job is NOT downloading, returns immediately--these status 
		/// settings indicate the job is "in process"
		/// Then queries IDownloader instance for job status.  If job is "Ready", set job status
		/// to "validating" and initiates validation.  For all other status, sets job status 
		/// to returned status and returns from function.
		/// </summary>
		private void CheckDownloadCompleteOrError()
		{
			//  check if the job status is anything BUT downloading.  If it's error, cancelled, validating, or ready, we don't
			//  need to query downloader.  In fact doing so will cause an exception in downloader, since it is not downloading it anymore.
			if( JobStatus.Downloading != _dnldJob.Status )
			{
				return;
			}

			//  wrapped in try-catch because it might take a while to poll, and if we're interrupted we need to handle properly
			//  also crucial to log failures of downloader here...
			try
			{
				switch( _downloader.GetJobStatus( _dnldJob.JobId ) )
				{
						//  check if job is ready from downloader; if so, set status to ready in table and signal Validator
					case JobStatus.Ready:
					{
						//  if job ready, annotate in the DownloadJobStatusEntry status field, make it "Validating"
						_dnldJob.Status = JobStatus.Validating;

						//*  TELL THE VALIDATOR TO GET WORKING ON THIS DOWNLOAD    
						ValidateFilesAndCleanup();
							
						//*  NOTIFY LISTENERS THAT DOWNLOAD IS COMPLETE               
						this.OnDownloadCompleted();

						//*  WRITE TO TRACE DNLD COMPLETE		
						ApplicationUpdateManager.TraceWrite( "[DownloaderManager.CheckDownloadCompleteOrError]", "RES_MESSAGE_DownloadComplete", _guidEndDnld0, _guidEndDnld1, _guidEndDnld2, _guidEndDnld3, _guidEndDnld4);
				
						break;
					}
						//  check if job errored; if so, set error to true and leave.
					case JobStatus.Error:
					{
						_dnldJob.Status = JobStatus.Error;
						break;
					}
					case JobStatus.Cancelled:
					{
						_dnldJob.Status = JobStatus.Cancelled;
						break;
					}
					case JobStatus.Downloading:
					{
						_dnldJob.Status = JobStatus.Downloading;
						break;
					}
					default:
					{
						break;
					}
				}

			}
				//  don't swallow TIE's, rethrow
			catch( ThreadInterruptedException tie )
			{
				throw tie;
			}
			catch( Exception e )
			{											
				string error = ApplicationUpdateManager.TraceWrite( e, "[DownloaderManager.CheckDownloadCompleteOrError]", "RES_ErrorDownloadingJob", _application.Name, e.Message);
				ExceptionManager.Publish( new ApplicationUpdaterException( error ,e ) );
			}
		}

		
		private double GetPollingIntervalMilliseconds( double pollingInterval, PollingType pollType )
		{
			//Polling interval
			switch( pollType )
			{
				case PollingType.Milliseconds:
					return  (double)TimeSpan.FromSeconds( pollingInterval ).TotalMilliseconds;
					
				case PollingType.Seconds:
					return  (double)TimeSpan.FromSeconds( pollingInterval ).TotalMilliseconds;
					
				case PollingType.Minutes:
					return  (double)TimeSpan.FromMinutes( pollingInterval ).TotalMilliseconds;
					
				case PollingType.Hours:
					return  (double)TimeSpan.FromHours( pollingInterval ).TotalMilliseconds;
					
				case PollingType.Days:
					return  (double)TimeSpan.FromDays( pollingInterval ).TotalMilliseconds;
					
				case PollingType.Weeks:
					return  (double)TimeSpan.FromDays( pollingInterval * 7 ).TotalMilliseconds;
					
				case PollingType.ExtendedFormat:
					//  set wait to MaxValue
					return  (double)int.MaxValue;
				default:
					return -1;
			}  

		}

		
		/// <summary>
		/// This method compares the recently downloaded file with the client version. 
		/// If the server version is greater compared with the client version, the 
		/// return value is <c>true</c>.
		/// </summary>
		/// <returns>true if client version older than server</returns>
		private bool IsClientUpdateNeeded()
		{			
			ClientApplicationInfo clientInfo = null;

			//  get client info from its config file
			clientInfo = ClientApplicationInfo.Deserialize( _application.Client.XmlFile );

			ApplicationUpdateManager.TraceWrite( "[DownloaderManager.ClientMustBeUpdated]", "RES_MESSAGE_ClientServerVersionCheck", _application.Name, clientInfo.InstalledVersion, _server.AvailableVersion );

			// get both versions, return comparison
			Version clientVersion = null;
			clientVersion = new Version( clientInfo.InstalledVersion );
			
			Version serverVersion = null;
			serverVersion = new Version( _server.AvailableVersion );
			
			return ( serverVersion > clientVersion );
		}

		
		#endregion

		#region ____* Validation and PostProcessor Code *____
		

		/// <summary>
		/// Given a particular application name, cycles through each downloaded file and verifies using IValidator object;
		/// also first validates Manifest;
		/// if even one file fails, it deletes them all;
		/// when complete and if successful, it updates the clienConfig.xml file with new version info based on download.
		/// </summary>
		private void ValidateFilesAndCleanup()
		{
			bool isFileValid = false;
			
			
			//  set job status to Validating.  This is its default state until it cycles back through main Run loop.
			_dnldJob.Status = JobStatus.Validating;
						
			#region File Validation			
			//  **
			//  **    VALIDATE ALL FILES        **
			//  **

			isFileValid = ValidateFiles();

			//  NOW that we've validated each file, check if they all passed...loop above BREAK's if one invalid found
			if( isFileValid )
			{
				
				//  Copy new files from temp dir to version-named dir
				FileUtility.CopyDirectory( 
					_application.Client.TempDir, 
					Path.Combine( _application.Client.BaseDir, _server.AvailableVersion )
					);

				//  delete manifest + temp files
				RemoveManifestAndTempFiles();				

				//  **
				//  **    UPDATE CLIENT FILE        **
				//  **
				
				ApplicationUpdateManager.TraceWrite( "[DownloaderManager.ValidateFilesAndCleanup]", "RES_MESSAGE_UpdatingLocalConfigFile", _application.Client.XmlFile );

				//  serialize Clientappinfo to xml file
				ClientApplicationInfo.Save( 
					_application.Client.XmlFile, 
					Path.Combine( _application.Client.BaseDir, _server.AvailableVersion ), 
					_server.AvailableVersion );
				//  **


				//  **
				//  **    RUN POST-PROCESSOR        **
				//  **
				RunPostProcessor();
				//  **


				//  **
				//          all files valid, RAISE EVENT to notify clients (if using validation)
				//  **
				if( _application.UseValidation )
				{
					this.OnFilesValidated(  );
				}
			}
			#endregion
			
		}

		
		/// <summary>
		/// Using manifest information in node @"<postProcessor type='' assembly=''></postProcessor>", 
		/// this instantiates the object.  The object MUST implement IPostProcessor interface.  That interface
		/// has a single method "void Run()"
		/// The object is used to do post-update actions such as run custom installers, make event logs, clean up 
		/// old installs, etc.
		/// 
		///   NOTE:  
		/// The PostProcessor runs from a thread _spawned by this thread_, and under _THIS_ security context.  
		/// Therefore if AppUpdater 
		/// is running as a high-privilege user, the PostProcessor can do quite a bit...BE CAREFUL.
		/// ALSO note that _we are using a separate thread_, so PostProcessor does NOT block this thread.
		/// </summary>
		private void RunPostProcessor()
		{
			IPostProcessor ipp = null;
			object postProcessor = null;

			//  FIRST, check if PostProcessor config node is null
			if ( null == _server.PostProcessor )
			{
				return;
			}

			//  we must run IPP _from the new version directory_, not the temp dir...temp dir is gone/going away; 
			//  use Path.Combine serially to ensure correct pathing.
			string ippPath = Path.Combine( Path.Combine( _application.Client.BaseDir , _server.AvailableVersion ) , _server.PostProcessor.Name );

			//  check if file actually exists:
			if( !File.Exists( ippPath ) )
			{
				return;
			}

			//  swallow all post-processor errors but log them
			try
			{
				//  instantiate IPP using GenericFactory
				postProcessor = GenericFactory.Create( ippPath, typeof( IPostProcessor ) );
				//  check if it's right interface so we can fail very quietly here
				if ( postProcessor is IPostProcessor )
				{
					ipp = postProcessor as IPostProcessor;
					//***    THREADING    ***
					//  here we create our member thread, and tell it to Run() IPP
					//  this way we are not blocked here, IPP may be very long-running if it 
					//  does un-installers, works with files, etc...
					//    NOTE  
					//  1)  we are not attempting to clean up this thread, catch exceptions, etc. for it; Run() must do that
					//  2)  IF the IPP needs to Dispose(), it must do so internally when it unwinds its stack--
					//		so when Run() is complete, call Dispose() internally
					//	 3)  the IPP should be aware that the parent app might shut down ungracefully--SO it should sink the 
					//		ProcessExit event and do cleanup
					//	 4)  in case it is Abort()'ed or Interrupt()'ed, it should catch both those exceptions and clean up.
					//	 
					_ippThread = new Thread( new ThreadStart( ipp.Run ) );
					_ippThread.Name = "IPostProcessor_Thread_" + _application.Name;
					_ippThread.Start();
				}
				else
				{					
					ApplicationUpdateManager.TraceWrite( "[DownloaderManager.RunPostProcessor]", "RES_MESSAGE_IPPWouldNotCast", ippPath, postProcessor.GetType() );
				}
			}
			catch( ThreadInterruptedException tie )
			{						
				ApplicationUpdateManager.TraceWrite( tie, "[DownloaderManager.RunPostProcessor]", "RES_EXCEPTION_ThreadInterruptedException", Thread.CurrentThread.Name );
				throw tie;
			}
			catch( Exception e )
			{				
				ApplicationUpdateManager.TraceWrite( e, "[DownloaderManager.RunPostProcessor]", "RES_EXCEPTION_RunningIPP", _server.PostProcessor.Name, _server.PostProcessor.Type, _server.PostProcessor.Assembly );
				ExceptionManager.Publish( e );				
			}
			return;
		}

		
		private bool ValidateManifestFile()
		{
			bool isManifestValid = false;
			XmlDocument xml = null;

			xml = new XmlDocument();
			
			
			//  **
			//  **    VALIDATE MANIFEST FILE    **
			//  **

			//  check if validation attribute in app node--"useValidation"--is false; if so, SKIP VALIDATION
			//  WARNING:  THIS IS A VERY DANGEROUS SETTING, USE IT ONLY FOR TESTING!
			if( _application.UseValidation )
			{
				//  FIRST, validate the Server Manifest File--we must first trust that it is valid
				ApplicationUpdateManager.TraceWrite( "[DownloaderManager.ValidateManifestFile]", "RES_MESSAGE_ValidatingManifest", _application.Server.ServerManifestFileDestination );
			
				//  load the server manifest from the known _application config path (must be known since we wrote it there)
				xml.Load( _application.Server.ServerManifestFileDestination );

				//  test core xml node of file
				isManifestValid = _validator.Validate( xml.SelectSingleNode( "ServerApplicationInfo" ), _server.ManifestSignature ) ;
			}
			else
			{
				//  useValidation==false, so skip and just "pretend" we've validated:
				isManifestValid = true;
			}

			//  check if file was valid, if so proceed otherwise delete manifest+tempDir
			if( !isManifestValid )
			{
				
				ApplicationUpdateManager.TraceWrite( "[DownloaderManager.ValidateManifestFile]", "RES_MESSAGE_ManifestFailedValidation", _application.Server.ServerManifestFileDestination );
				
				//  remove manifest + temp files
				RemoveManifestAndTempFiles();

				//  raise the manifest-failed event to notify clients of abject FAILURE
				this.OnManifestValidationFailed();
				//  return false, manifest invalid
				return false;
			}
			else
			{
				if( _application.UseValidation )
				{
					//  raise manifest-validated to notify clients of success
					this.OnManifestValidated();
				}

				//  return true, manifest was valid
				return true;
			}
		}


		/// <summary>
		/// If validation is enabled, which it SHOULD be except for debugging purposes, this walks through all
		/// files in the server config Files collection gleaned from the Manifest, and uses our instance of 
		/// IValidator to check each file against its signature.
		/// </summary>
		/// <returns>false if any file fails validation</returns>
		private bool ValidateFiles()
		{
			bool isFileValid = false;

			//  check if validation attribute in app node--"useValidation"--is false; if so, SKIP VALIDATION
			//  WARNING:  THIS IS A VERY DANGEROUS SETTING, USE IT ONLY FOR TESTING!
			if( _application.UseValidation )
			{
				//  notify validating of files commenced:
				ApplicationUpdateManager.TraceWrite( 
					"[DownloaderManager.ValidateFilesAndCleanup]", "RES_MESSAGE_ValidatingFiles", _application.Name );

				//  CYCLE through ALL FILES, checking validation on each;
				// if even one invalid, delete all
				//Iterate through file collection, Validate each file
				foreach( FileConfig file in _server.Files )
				{
					FileInfo fInfo = new FileInfo( Path.Combine( _application.Client.TempDir, file.Name ) );

					//  find out if given file passes validation
					isFileValid = _validator.Validate( fInfo.FullName, file.Signature );

					if ( !isFileValid )
					{
						ApplicationUpdateManager.TraceWrite( "[DownloaderManager.ValidateFiles]", "RES_MESSAGE_FileFailedValidation", file.Name );
						
						//  delete the file--
						RemoveManifestAndTempFiles();

						//  notify event clients of failed validation:
						this.OnFilesValidationFailed(  );
						//  since one file invalid, we don't trust whole download--return false
						return false;
					}
					else
					{
						ApplicationUpdateManager.TraceWrite( "[DownloaderManager.ValidateFiles]", "RES_MESSAGE_FileValidated", fInfo.FullName );
						isFileValid = true;
					}
				} //  FOREACH
			}
			else
			{
				//  simply return true, since we are skipping validation
				return true;
			}

			//  fall-through case, we went through all files and each time set isFileValid to true
			return isFileValid;
		}

		
		
		#endregion

		#region ____*  Cleanup Code for Files and PostProcessor  *____
		
		/// <summary>
		/// Deletes the Manifest and the Temp directories, including all contents/files
		/// </summary>
		private void RemoveManifestAndTempFiles()
		{
			if( null != _application )
			{
				//  COULD HAVE USED File.Delete(), but having in separate class means later we can queue deletes and/or
				//  implement retry mechanism to make delete more robust when files get locked etc.
				FileUtility.DeleteFile( _application.Server.ServerManifestFileDestination );
				//  delete the dir
				FileUtility.DeleteDirectory( _application.Client.TempDir );
			}
		
		}


		/// <summary>
		/// Wraps the stopping of the IPP thread; this thread ventures outside and does work through IPP.Run(), may take a while; 
		/// we may need to reign it in, so stopping + error collection code here.
		/// Common errors in IPP thread:  TheadInterruptedException + TAE, or frank Exception if those not properly handled.
		/// </summary>
		private void StopPostProcessorThread()
		{
			string threadName = "";
			try
			{
				//  join/interrupt the IPOstProcessor thread; it should catch internally and know to Dispose() NOW!
				if( null != _ippThread )
				{
					//  cache name here
					threadName = _ippThread.Name;

                //  NOTE: if you need to do very long-running work, such as large installs or extensive file manipulation, it
                //  is HIGHLY RECOMMENDED you merely use the IPP Run() method __to spawn a separate process__, 
                //  and in turn do all that long-running work in _that_ process, returning our IPP thread as soon as new Process launches.
                //  Then you're not at the mercy of Updater's lifetime to complete potentially sensitive work!

					//  give it a chance, if it's completed it should Join() otherwise Interrupt() it
					//  Interrupt will only wake it if it's WaitSleepJoin; it will not help if the thread is in unmanaged code
					if( !_ippThread.Join( MILLISECOND_WAIT_TIMEOUT_IPP ) )
					{
						_ippThread.Interrupt();
					}
					//  give another chance to clean up, then abort to prevent having it hang around
					if( !_ippThread.Join( MILLISECOND_WAIT_TIMEOUT_IPP ) )
					{
						_ippThread.Abort();
					}
				}
			}
			catch( Exception e )
			{
				ApplicationUpdateManager.TraceWrite( e, "[DownloaderManager.StopPostProcessorThread]", "RES_EXCEPTION_StoppingIPP", threadName, e.Message );
				ExceptionManager.Publish( e );
				//  SWALLOW this exception, because we WANT other applications to continue downloading.  Just exit quietly after logging the problem.
				//  downloads should not stop because of IPP errors.
			}
		}


		#endregion
	}
}
