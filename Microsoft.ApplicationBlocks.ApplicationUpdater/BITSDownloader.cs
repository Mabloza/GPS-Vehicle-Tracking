
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// BITDownloader.cs
//
// BITS implementation of IDownloader.
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
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Xml;

using Microsoft.ApplicationBlocks.ExceptionManagement;
using Microsoft.ApplicationBlocks.ApplicationUpdater.Interfaces;

namespace Microsoft.ApplicationBlocks.ApplicationUpdater.Downloaders
{
	/// <summary>
	/// The BITS downloader provider for AppUpdater.  BITS is a service available on Windows 2000 and above.  It uses a robust, 
	/// asynchronous HTTP download mechanism that can recover gracefully from disconnects, and utilizes only spare bandwidth.
	/// </summary>
	internal  class BITSDownloader : IDownloader
	{
		#region Declarations

		private const int			TIME_WAIT_SYNCH_DOWNLOAD			= 2000;
		private const int			RETRY_TIMES_SYNCH_DNLD				= 5;
		private const int			BITS_SET_NO_PROGRESS_TIMEOUT		= 5;
		private const int			BITS_SET_MINIMUM_RETRY_DELAY		= 0;
		private readonly int		CULTURE_ID_FOR_COM					= Int32.Parse( Resource.ResourceManager[ "RES_CultureIDToGetCOMErrorStringsFormatted" ], CultureInfo.CurrentCulture );
		private HybridDictionary	_jobs								= null;

		#endregion
		
		/// <summary>
		/// Default constructor
		/// </summary>
		public BITSDownloader()
		{
			//  create the internally-used list of jobs.  We will use this in dispose() to be sure that all
			//  jobs are cleared and don't hang around in BITS' queue
			_jobs = new HybridDictionary( 10 );
		}


		/// <summary>
		/// Initializes the BITSDownloader (not used)
		/// </summary>
		/// <param name="config">xml node which might contain config information used by downloader to set up</param>
		void IDownloader.Init( XmlNode config ) { }


		/// <summary>
		/// Synchronous downloading method using BITS
		/// </summary>
		/// <param name="sourceFile">Source file to download</param>
		/// <param name="destFile">Target file on the local system</param>
		/// <param name="maxTimeWait">Maximum time to wait for a download</param>
		void IDownloader.Download( string sourceFile, string destFile, TimeSpan maxTimeWait )
		{
			IBackgroundCopyManager	backGroundCopyManager = null;
			IBackgroundCopyJob		backGroundCopyJob = null;
			Guid					jobID = Guid.Empty;
			bool					isCompleted = false;
			bool					isSuccessful = false;
			string					cumulativeErrMessage = "";
			BG_JOB_STATE			state;

			//  to defend against config errors, check to see if the path given is UNC;
			//  if so, throw immediately there's a misconfiguration.  Paths to BITS must be HTTP/HTTPS
			ThrowIfSourceIsUNC( sourceFile );

			try
			{
				//  use utility function to create manager, job, get back jobid etc.; uses 'out' semantics
				CreateCopyJob( 
					out backGroundCopyManager, 
					out backGroundCopyJob, 
					ref jobID, 
					"RES_BITSJobName", 
					"RES_BITSDescription" );

				// Add the file to the Job List
				backGroundCopyJob.AddFile( sourceFile, destFile );

				// Start the Back Ground Copy Job.
				backGroundCopyJob.Resume();

				//  set endtime to current tickcount + allowable # milliseconds to wait for job
				int endTime = Environment.TickCount + (int)maxTimeWait.TotalMilliseconds;

				#region __While Loop Waits On Single Download__

				while ( !isCompleted )
				{
					backGroundCopyJob.GetState( out state );
					switch( state )
					{
						case BG_JOB_STATE.BG_JOB_STATE_ERROR:	
						{
							//  use utility to:
							//  a)  get error info 
							//  b)  report it
							//  c)  cancel and remove copy job
							HandleDownloadErrorCancelJob( backGroundCopyJob, ref cumulativeErrMessage );
							
							//  complete loop, but DON'T say it's successful
							isCompleted = true;
							break;
						}
						case BG_JOB_STATE.BG_JOB_STATE_TRANSIENT_ERROR:
						{							
							//  NOTE:  during debugging + test, transient errors resulted in complete job failure about 90%
							//  of the time.  Therefore we treat transients just like full errors, and CANCEL the job
							//  use utility to manage error etc.
							HandleDownloadErrorCancelJob( backGroundCopyJob, ref cumulativeErrMessage );

							//  stop the loop, set completed to true but not successful
							isCompleted = true;
							break;
						}
						case BG_JOB_STATE.BG_JOB_STATE_TRANSFERRED:
						{
							//  notify BITS we're happy, remove from queue and transfer ownership to us:
							backGroundCopyJob.Complete(); 
							//  remove job from our collection, we won't need to Cancel() in our Dispose()
							RemoveCopyJobEntry( jobID );
							isCompleted = true;
							isSuccessful = true;
							break;
						}
						default:
							break;
					}

					if( endTime < Environment.TickCount )
					{
						HandleDownloadErrorCancelJob( backGroundCopyJob, ref cumulativeErrMessage );
						break;
					}

					//  Avoid 100% CPU utilisation with too tight a loop, let download happen.
					Thread.Sleep( TIME_WAIT_SYNCH_DOWNLOAD );
				}

				#endregion

				if( !isSuccessful )
				{
					//  create message + error, package it, publish 
					string error = ApplicationUpdateManager.TraceWrite( 
						"[BITSDownloader.Download]", 
						"RES_MESSAGE_ManifestFileNotDownloaded", sourceFile,  cumulativeErrMessage );
					Exception ex = new Exception( error + Environment.NewLine + cumulativeErrMessage );

					throw ex;
				}
			}
			catch( ThreadInterruptedException tie )
			{
				//  if interrupted, clean up job
				HandleDownloadErrorCancelJob( backGroundCopyJob, ref cumulativeErrMessage );
				ApplicationUpdateManager.TraceWrite( tie, "[BITSDownloader.Download]", "RES_TIEInBITS", sourceFile );
				throw tie;
			}
			catch( Exception e )
			{
				//  if exception, clean up job
				HandleDownloadErrorCancelJob( backGroundCopyJob, ref cumulativeErrMessage );
				//  then log error 
				string error = ApplicationUpdateManager.TraceWrite( 
					e,
					"[BITSDownloader.Download]", 
					"RES_MESSAGE_ManifestFileNotDownloaded", sourceFile,  cumulativeErrMessage );
				Exception ex = new Exception( error, e );
				ExceptionManager.Publish( ex );
				//  throw; allow consuming class to figure out what to do
				throw ex;
			}
			finally
			{
				if( null != backGroundCopyJob ) 
				{
					Marshal.ReleaseComObject( backGroundCopyJob );
				}
				if( null !=  backGroundCopyManager )
				{
					Marshal.ReleaseComObject( backGroundCopyManager );
				}
			}
		}


		/// <summary>
		/// Supports multiple simultaneous downloads asynchronously.  Starts the job immediately and returns a GUID while the download continues.
		/// </summary>
		/// <param name="sourceFile">a string array of source files, which MUST BE URL's they cannot be UNC paths</param>
		/// <param name="destFile">a string array of destination paths which MUST BE UNC's they cannot be URL paths</param>
		/// <returns>a GUID which is a job id for future reference</returns>
		Guid IDownloader.BeginDownload( string[] sourceFile, string[] destFile )
		{
			IBackgroundCopyManager	backGroundCopyManager	= null;
			IBackgroundCopyJob		backGroundCopyJob		= null;
			Guid					jobID					= Guid.Empty;

			try
			{
				//  use utility function to create the job. 
				CreateCopyJob( 
								out backGroundCopyManager, 
								out backGroundCopyJob, 
								ref jobID, 
								"RES_BITSFilesDownloadJobName", 
								"RES_BITSFilesDownloadDescription" );

				// Add the file to the Job List
				for( int i = 0; i < sourceFile.Length; i++ )
				{
					//  to defend against config errors, check to see if the path given is UNC;
					//  if so, throw immediately there's a misconfiguration.  Paths to BITS must be HTTP or HTTPS
					ThrowIfSourceIsUNC( sourceFile[ i ] );

					//  add this file to the job
					backGroundCopyJob.AddFile( sourceFile[i], destFile[i] );
				}

				// Start the Back Ground Copy Job.
				backGroundCopyJob.Resume();

				return jobID;
			}
			finally
			{
				if( null != backGroundCopyJob ) 
				{
					Marshal.ReleaseComObject( backGroundCopyJob );
				}
				if( null != backGroundCopyManager )
				{
					Marshal.ReleaseComObject( backGroundCopyManager );
				}
			}
		}


		/// <summary>
		/// returns a job status enum for a particular job identified by its GUID
		/// </summary>
		/// <param name="jobId">a guid for the job requested</param>
		/// <returns>a JobStatus describing the state of the job</returns>
		JobStatus IDownloader.GetJobStatus( Guid jobId )
		{
			IBackgroundCopyManager	backGroundCopyManager = null;
			IBackgroundCopyJob		backGroundCopyJob = null;
			BG_JOB_STATE			state;
			string					errMessage = "";
			string					jobName = "";
			string					jobDesc = "";
			string					error = "";

			try
			{
				backGroundCopyManager = (IBackgroundCopyManager) new BackgroundCopyManager();
				backGroundCopyManager.GetJob( ref jobId, out backGroundCopyJob );						
				
				//  get job name
				backGroundCopyJob.GetDisplayName( out jobName );
				//  get job desc
				backGroundCopyJob.GetDescription( out jobDesc );
				//  get job state enum value
				backGroundCopyJob.GetState( out state );

				switch(state)
				{
					case BG_JOB_STATE.BG_JOB_STATE_ERROR:
					{
						//  use utility method to handle error:
						HandleDownloadErrorCancelJob( backGroundCopyJob, ref errMessage );
						
						//  return status as error
						return JobStatus.Error;
					}
					case BG_JOB_STATE.BG_JOB_STATE_TRANSIENT_ERROR:
					{
						//    NOTE:  if transient, just treat as full error.  During testing about 90% of transients
						//				resulted in full failure.  Cleanup.
						//  use utility method to handle error:
						HandleDownloadErrorCancelJob( backGroundCopyJob, ref errMessage );
						
						//  return status as error
						return JobStatus.Error;
					}
					case BG_JOB_STATE.BG_JOB_STATE_TRANSFERRED:
					{
						//  tell BITS to transfer to us and stop thinking about the job
						backGroundCopyJob.Complete(); 
						// remove job from collection to be Dispose()ed
						RemoveCopyJobEntry( jobId );
						return JobStatus.Ready;
					}
					case BG_JOB_STATE.BG_JOB_STATE_CANCELLED:
					{
						//  use utility method to handle error:
						HandleDownloadErrorCancelJob( backGroundCopyJob, ref errMessage );
                        
						//  return status as cancelled 
						return JobStatus.Cancelled;
					}
					default:
						return JobStatus.Downloading;
				}
			}
			catch( ThreadInterruptedException tie )
			{
				//  if interrupted, clean up job
				HandleDownloadErrorCancelJob( backGroundCopyJob, ref errMessage );
				ApplicationUpdateManager.TraceWrite( tie, "[BITSDownloader.Download]", "RES_TIEInBITS", "N/A" );
				throw tie;
			}
			catch( Exception e )
			{
				//  use utility method to handle error:
				HandleDownloadErrorCancelJob( backGroundCopyJob, ref errMessage );
				//  bad to catch all exceptions, but OK because we adorn it with necessary additional info then pass it up as innerException
				error = ApplicationUpdateManager.TraceWrite( e, "[BITSDownloader.GetJobStatus]", "RES_EXCEPTION_BITSOtherError", jobId, jobName, jobDesc );
				//  publish
				Exception newE = new Exception( error, e );
				ExceptionManager.Publish( newE );						
				//  rethrow; 
				throw newE;
			}
			finally
			{
				if( backGroundCopyManager != null )
				{
					Marshal.ReleaseComObject( backGroundCopyManager );
				}
				if( backGroundCopyJob != null )
				{
					Marshal.ReleaseComObject( backGroundCopyJob );
				}
			}
		}

		/// <summary>
		/// Removes a copy job from the internal lookup collection
		/// </summary>
		/// <param name="jobID">GUID identifies of a job (job id)</param>
		private void RemoveCopyJobEntry( Guid jobID )
		{
			//  lock our collection of running jobs; remove it from the job collection
			lock( _jobs.SyncRoot )
			{
				_jobs.Remove( jobID );
			}	
		}

		/// <summary>
		/// Internal copy-job factory method.  Used to coordinate all aspects of a job set-up, 
		/// which includes creating a copy manager, creating a job within it, setting download
		/// parameters, and adding the job to our tracking collection for cleanup later
		/// </summary>
		/// <param name="copyManager">null reference to copy manager</param>
		/// <param name="copyJob">null reference to copy job</param>
		/// <param name="jobID">null reference to job id guid</param>
		/// <param name="jobNameKey">the key used to look up the job name in the resource file</param>
		/// <param name="jobDescriptionKey">the key used to look up the job description in the resource file</param>
		private void CreateCopyJob( 
									out IBackgroundCopyManager copyManager, 
									out IBackgroundCopyJob copyJob, 
									ref Guid jobID, 
									string jobNameKey, 
									string jobDescriptionKey )
		{
			
			string jobName = Resource.ResourceManager[ jobNameKey ];
			string jobDesc = Resource.ResourceManager[ jobDescriptionKey ];

			//  wrap in try-finally so we can clean COM objects if unexpected error
			try
			{
				//  create the manager
				copyManager = (IBackgroundCopyManager) new BackgroundCopyManager();
							
				// create the job, set its description, use "out" semantics to get jobid and the actual job object
				copyManager.CreateJob( 
					jobName,
					BG_JOB_TYPE.BG_JOB_TYPE_DOWNLOAD,
					out jobID,
					out copyJob );

				//  set useful description
				copyJob.SetDescription( jobDesc );

				//  ***
				//      SET UP BITS JOB SETTINGS--TIMEOUTS/RETRY ETC           
				//      SEE THE FOLLOWING REFERENCES:
				//  **  http://msdn.microsoft.com/library/default.asp?url=/library/en-us/bits/bits/ibackgroundcopyjob_setminimumretrydelay.asp?frame=true
				//  **  http://msdn.microsoft.com/library/default.asp?url=/library/en-us/bits/bits/ibackgroundcopyjob_setnoprogresstimeout.asp?frame=true
				//  **  http://msdn.microsoft.com/library/default.asp?url=/library/en-us/bits/bits/bg_job_priority.asp
				//  ***
				
				//  in constant set to 0; this makes BITS retry as soon as possible after an error
				copyJob.SetMinimumRetryDelay( (uint)BITS_SET_MINIMUM_RETRY_DELAY );
				//  in constant set to 5 seconds; BITS will set job to Error status if exceeded
				copyJob.SetNoProgressTimeout( (uint)BITS_SET_NO_PROGRESS_TIMEOUT );
				//  make this job the highest (but still background) priority--
				copyJob.SetPriority( BG_JOB_PRIORITY.BG_JOB_PRIORITY_HIGH );
				//  ***


				//  lock our internal collection of jobs, and add this job--we use this collection in Dispose()
				//  to tell BITS to Cancel() jobs--and remove them from the queue
				//  if we did not do this, BITS would continue for (by default) two weeks to download what we asked!
				lock( _jobs.SyncRoot )
				{
					_jobs.Add( jobID, jobName );
				}
			}
			catch( Exception e )
			{
				//  bad to catch all exceptions, but OK because we adorn it with necessary additional info then pass it up as innerException
				string error = ApplicationUpdateManager.TraceWrite( e, "[BITSDownloader.CreateCopyJob]", "RES_EXCEPTION_BITSOtherError", jobID, jobName, jobDesc );
				//  publish
				Exception newE = new Exception( error, e );
				ExceptionManager.Publish( newE );						
				//  rethrow; 
				throw newE;
			}
		}
		
		/// <summary>
		/// Centralizes all chores related to stopping and cancelling a copy job, and getting back
		/// from BITS the errors incurred during the job.
		/// </summary>
		/// <param name="copyJob">reference to the copy job object (not job id)</param>
		/// <param name="errMessage">a cumulative error message passed by reference so
		/// that additions can be made</param>
		private void HandleDownloadErrorCancelJob( 
													IBackgroundCopyJob copyJob, 
													ref string errMessage )
		{
			string					singleError = "";
			string					jobDesc = "";
			string					jobName = "";
			Guid					jobID = Guid.Empty;
			IBackgroundCopyError	copyError = null;
			
			try
			{
				//  check if job is null; don't try to clean up null references!
				if( null != copyJob )
				{
					//  get information about this job for reporting the error
					copyJob.GetDescription( out jobDesc );
					copyJob.GetDisplayName( out jobName );
					copyJob.GetId( out jobID );

					//  find out what the error was			
					copyJob.GetError( out copyError );

					// use the culture id specified in RESX to tell COM which culture to return err message in:
					copyError.GetErrorDescription( (uint)CULTURE_ID_FOR_COM, out singleError );

					//  add error to our "stack" of errors:
					errMessage += singleError + Environment.NewLine;

					//  notify BITS that we consider this job a loss, forget about it:
					copyJob.Cancel();

					//  remove job from collection
					RemoveCopyJobEntry( jobID );

					//  log error, but don't throw here; let dnldmgr take care of error
					//  NOTE that errMessage is used cumulatively for full track of problem
					errMessage = ApplicationUpdateManager.TraceWrite( "[BITSDownloader]", "RES_EXCEPTION_BITSBackgroundCopyError", jobID, jobName, jobDesc, errMessage );
						
					ExceptionManager.Publish( new Exception( errMessage ) );
				}
			}
			finally
			{
				if( null != copyError )
				{
					Marshal.ReleaseComObject( copyError );
					copyError = null;
				}
			}
		}
		
		/// <summary>
		/// Checks the download source location; BITS can only accept HTTP/HTTPS, so if
		/// we are given a UNC path as source, we need to stop right away--misconfiguration
		/// </summary>
		/// <param name="sourceFile">the path to the update file's source</param>
		private void ThrowIfSourceIsUNC( string sourceFile )
		{
			if( FileUtility.IsUNCPath( sourceFile ) )
			{
				string error = ApplicationUpdateManager.TraceWrite( "[BITSDownloader.BeginDownload]", "RES_EXCEPTION_UNCSourcePathToBITS", sourceFile );
				ArgumentException ae = new ArgumentException( error, "sourceFile" );				
				throw ae;
			}
		}
		
			

		#region IDisposable Implementation

		//  take care of IDisposable too   
		/// <summary>
		/// Allows graceful cleanup of hard resources
		/// </summary>
		public void Dispose() 
		{
			Dispose(true);
			GC.SuppressFinalize(this); 
		}

		
		/// <summary>
		/// used by externally visible overload.
		/// </summary>
		/// <param name="isDisposing">whether or not to clean up managed + unmanaged/large (true) or just unmanaged(false)</param>
		protected virtual void Dispose(bool isDisposing) 
		{				
			uint BG_JOB_ENUM_ALL_USERS  = 0x0001;
			uint numJobs;
			uint fetched;
			Guid jobID;
			IBackgroundCopyManager mgr = null;
			IEnumBackgroundCopyJobs jobs = null;
			IBackgroundCopyJob job = null;
			if (isDisposing) 
			{
				try
				{
					mgr = (IBackgroundCopyManager)( new BackgroundCopyManager() );

					mgr.EnumJobs( BG_JOB_ENUM_ALL_USERS, out jobs );

					jobs.GetCount( out numJobs );

					//  lock the jobs collection for duration of this operation
					lock( _jobs.SyncRoot)
					{
						for( int i = 0; i < numJobs; i++ )
						{
							//  use jobs interface to walk through getting each job
							jobs.Next( (uint)1, out job, out fetched );
						
							//  get jobid guid
							job.GetId( out jobID );

							//  check if the job is in OUR collection; if so cancel it.  we obviously don't want to get
							//  jobs from other Updater threads/processes, or other BITS jobs on the machine!
							if( _jobs.Contains( jobID ) )
							{
								//  take ownership just in case, and cancel() it
								job.TakeOwnership();
								job.Cancel();	
								// remove from our collection
                                _jobs.Remove( jobID );					
							}
						}
					}
				}
				finally
				{
					if( null != mgr )
					{
						Marshal.ReleaseComObject( mgr );
						mgr = null;
					}
					if( null != jobs )
					{
						Marshal.ReleaseComObject( jobs );
						jobs = null;
					}
					if( null != job )
					{
						Marshal.ReleaseComObject( job );
						job = null;
					}
				}
			}
		}

		
		/// <summary>
		/// Destructor/Finalizer
		/// </summary>
		~BITSDownloader()
		{
			// Simply call Dispose(false).
			Dispose (false);
		}

		#endregion

	}
}
