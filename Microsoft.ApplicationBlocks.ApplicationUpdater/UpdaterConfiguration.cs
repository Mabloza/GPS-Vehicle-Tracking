
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// UpdaterConfiguration.cs
//
// Encapsulates various configuration file entities for use by Updater.
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
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.ApplicationBlocks.ExceptionManagement;

namespace Microsoft.ApplicationBlocks.ApplicationUpdater
{


	#region PollingType ENUM
	/// <summary>
	/// The polling type for the updater service
	/// </summary>
	public  enum PollingType
	{ 
		/// <summary>
		/// Specify interval in milliseconds
		/// </summary>
		Milliseconds, 

		/// <summary>
		/// Specify interval in seconds
		/// </summary>
		Seconds, 

		/// <summary>
		/// Specify interval in minutes
		/// </summary>
		Minutes, 

		/// <summary>
		/// Specify interval in hours
		/// </summary>
		Hours, 

		/// <summary>
		/// Specify interval in days
		/// </summary>
		Days, 

		/// <summary>
		/// Specify interval in weeks
		/// </summary>
		Weeks, 

		/// <summary>
		/// Specify interval using an extended format.
		/// </summary>
		ExtendedFormat }

	#endregion


	#region UpdaterConfiguration

	/// <summary>
	/// Main configiration class stored on the XML file. The configuration is stored
	/// using Xml serialization so the configuration is the Xml representation for
	/// this class and its related classes.
	/// </summary>
	public  class UpdaterConfiguration
	{
		#region Declarations

		private static UpdaterConfiguration _configuration = null;
		
		#endregion

		#region Constructor

		static UpdaterConfiguration(){}
		/// <summary>
		/// default ctor
		/// </summary>
		public UpdaterConfiguration(){}
		
		#endregion

		#region property

		/// <summary>
		/// Returns the singleton of UpdaterConfiguration created during construction
		/// </summary>
		[XmlIgnore()]
		public static UpdaterConfiguration Instance
		{
			get
			{ 
				if( null == _configuration )
				{
					Init();
				}
				return _configuration; 
			}
		}

		private static void Init()
		{
			#region Get the configuration instance
			try
			{
				if ( null == _configuration )
				{
					_configuration = (UpdaterConfiguration)ConfigurationSettings.GetConfig( "appUpdater" );
				}
			}
			catch( ApplicationUpdaterException aex )
			{
				string error = ApplicationUpdateManager.TraceWrite( aex, "[UpdaterConfiguration.cctor]", "RES_UnableToLoadConfiguration",  AppDomain.CurrentDomain.SetupInformation.ConfigurationFile );
				
				ApplicationUpdaterException theException = new ApplicationUpdaterException( error, aex );

				ExceptionManager.Publish( theException );
				throw theException;	
			}
			catch( ConfigurationException configEx )
			{
				string error = ApplicationUpdateManager.TraceWrite( configEx, "[UpdaterConfiguration.cctor]", "RES_UnableToLoadConfiguration",  AppDomain.CurrentDomain.SetupInformation.ConfigurationFile );

				ConfigurationException plusConfEx = new ConfigurationException( error, configEx );

				ExceptionManager.Publish( plusConfEx );
				throw plusConfEx;
			}
			catch( Exception exception )
			{
				//  for general exception, just publish and throw no more is reasonable to add
				ExceptionManager.Publish( exception );
				throw exception;
			}
			if( _configuration == null )
			{
				string error = ApplicationUpdateManager.TraceWrite( "[UpdaterConfiguration.cctor]", "RES_UnableToLoadConfiguration",  AppDomain.CurrentDomain.SetupInformation.ConfigurationFile );

				ApplicationUpdaterException theException = new ApplicationUpdaterException( error );
				ExceptionManager.Publish( theException );
				throw theException;
			}
			#endregion
		}

		
		#endregion

		/// <summary>
		/// process the logListener element of config so we can store log location for Trace output
		/// </summary>
		[XmlElement( "logListener" )]
		public LoggingListener Logging
		{
			get{ return _logging; }
			set{ _logging = value; } 
		} private LoggingListener _logging;

		/// <summary>
		/// The interval between server polling
		/// </summary>
		[XmlElement( "polling" )]
		public PollingConfiguration Polling
		{
			get{ return _polling; }
			set
			{
				_polling = value; 
				//  FIRST validate polling object; if polling time is < 60 seconds, throw exception
				int pollInterval = 0;
				if( _polling.Type != PollingType.ExtendedFormat )
				{
					pollInterval = int.Parse( _polling.Value, CultureInfo.CurrentCulture );
				}
				if( _polling.Type == PollingType.Seconds && pollInterval < 60 )
				{
					string error = ApplicationUpdateManager.TraceWrite( "[PollingConfiguration.ctor]", "RES_EXCEPTION_PollingIntervalTooShort" );
				
					//  DON'T set time too short, update polling < 60 seconds not allowable.
					throw new ApplicationUpdaterException( error );
				}
			}
		} PollingConfiguration _polling;

		/// <summary>
		/// The downloader provider. The class configured must implement IDownloader
		/// </summary>
		[XmlElement( "downloader" )]
		public DownloaderConfiguration Downloader
		{
			get{ return _downloader; }
			set{ _downloader = value; }
		} DownloaderConfiguration _downloader;

		/// <summary>
		/// The validator provider. The class configured must implement IValidator
		/// </summary>
		[XmlElement( "validator" )]
		public ValidatorConfiguration Validator
		{
			get{ return _validator; }
			set{ _validator = value; }
		} ValidatorConfiguration _validator;

		/// <summary>
		/// The list of applications defined on the XML file
		/// </summary>
		[XmlElement( "application" )]
		public ApplicationConfiguration[] Applications
		{
			get{ return _applications; }
			set{ _applications = value; }
		} ApplicationConfiguration[] _applications;	


		/// <summary>
		/// indexer iterates array of applications, returns one matching the name
		/// </summary>
		public ApplicationConfiguration this[ string name ]
		{
			get
			{
				foreach( ApplicationConfiguration application in _applications )
				{
					if( name == application.Name )
					{
						return application;
					}
				}
				return null;
			}
		}
	}
	#endregion


	#region LoggingListener

	/// <summary>
	/// The LoggingListener class encapsulates the "logListener" element of config file, and puts the "logPath" attribute in a file path string.
	/// </summary>
	public class LoggingListener
	{
		/// <summary>
		/// The LOCAL file path to a log file which will be written during operation of the Updater
		/// </summary>
		[XmlAttribute( "logPath" )]
		public string LogPath
		{
			get{ return _value; }
			set
			{ 
				_value = value; 
			}
		} string _value;
	}

	#endregion
	

	#region PollingConfiguration
	/// <summary>
	/// The polling configuration
	/// </summary>
	public  class PollingConfiguration
	{
		/// <summary>
		/// The PollingType for the updater service
		/// </summary>
		[XmlAttribute( "type" )]
		public PollingType Type
		{
			get{ return _type; }
			set{ _type = value; }
		} PollingType _type;

		/// <summary>
		/// The value for the type specified.
		/// </summary>
		[XmlAttribute( "value" )]
		public string Value
		{
			get{ return _value; }
			set
			{ 
				_value = value; 
			}
		} string _value;
	}
	#endregion

	
	#region ClientConfiguration
	/// <summary>
	/// Defines the client configuration for a given application
	/// </summary>
	public  class ClientConfiguration
	{


		/// <summary>
		/// The AppStart.exe.config, or the config of the application that directly consumes Updater, OR the config of the
		/// Service EXE that controls Updater.  NOTE:  this is the so-called "Controller" config.
		/// </summary>
		[XmlElement( "xmlFile" )]
		public string XmlFile
		{
			get{ return _xmlFile; }
			set{ 
				//  check that the dir/file exist:
				if( !File.Exists( value ) )
				{
					string error = ApplicationUpdateManager.TraceWrite( "[ClientConfiguration.XmlFile]", "RES_ClientXmlFileMustExist", AppDomain.CurrentDomain.SetupInformation.ConfigurationFile, value );
					ConfigurationException configEx = new ConfigurationException( error );
					ExceptionManager.Publish( configEx );
					//  throw, we can't work without this.
					throw configEx;
				}

				//  set the path
				_xmlFile = value; 
				
			}
		} string _xmlFile;

		/// <summary>
		/// The client base directory--MUST be a file path not URL
		/// </summary>
		[XmlElement( "baseDir" )]
		public string BaseDir
		{
			get{ return _baseDir; }
			set{
				try
				{
					new DirectoryInfo( value );
				}
				catch( Exception e )
				{
					string error = ApplicationUpdateManager.TraceWrite( e, "[ClientConfiguration.BaseDir]", "RES_InvalidPathOnBaseDir", AppDomain.CurrentDomain.SetupInformation.ConfigurationFile, value );

					ApplicationUpdaterException theException = new ApplicationUpdaterException( error, e );
					ExceptionManager.Publish( theException );
					throw theException;
				}
				if( !Path.IsPathRooted( value ) )
				{
					string error = ApplicationUpdateManager.TraceWrite( "[ClientConfiguration.BaseDir]", "RES_InvalidPathOnBaseDirMustBeRooted", AppDomain.CurrentDomain.SetupInformation.ConfigurationFile, value );

					ApplicationUpdaterException theException = 
						new ApplicationUpdaterException( error );
					ExceptionManager.Publish( theException );
					throw theException;
				}
				//  check for existence of this dir
				if( !Directory.Exists( value ) )
				{
					string error = ApplicationUpdateManager.TraceWrite( "[ClientConfiguration.BaseDir]", "RES_EXCEPTION_BaseDirMustExist", AppDomain.CurrentDomain.SetupInformation.ConfigurationFile, value );

					ApplicationUpdaterException theException = 
						new ApplicationUpdaterException( error );
					ExceptionManager.Publish( theException );
					throw theException;				
				}
				
				//  assign the value
				_baseDir = FileUtility.AppendTerminalBackSlash( value ); 
			}
		} string _baseDir;
		

		/// <summary>
		/// The temp dir used to download files before validated--MUST be a file path not URL
		/// </summary>
		[XmlElement( "tempDir" )]
		public string TempDir
		{
			get{ return _tempDir; }
			set{ 
				//  this dir doesn't exist until we make it, so can't check for existence here
				try
				{
					new DirectoryInfo( value );
				}
				catch( Exception e )
				{
					string error = ApplicationUpdateManager.TraceWrite( e, "[ClientConfiguration.TempDir]", "RES_InvalidPathOnTempDir", AppDomain.CurrentDomain.SetupInformation.ConfigurationFile, value );

					ApplicationUpdaterException theException = new ApplicationUpdaterException( error, e );
					ExceptionManager.Publish( theException );
					throw theException;
				}
				if( Path.IsPathRooted( value ) )
				{
					_tempDir = value; 
				}
				else
				{
					_tempDir = Path.Combine( _baseDir, value );
				}
				//  make sure it has terminal slash
				_tempDir = FileUtility.AppendTerminalBackSlash( _tempDir );				

			}
		} string _tempDir;
	}
	#endregion

	
	#region ServerConfiguration
	/// <summary>
	/// The server configuration for a given application
	/// </summary>
	public  class ServerConfiguration
	{
		/// <summary>
		/// The Server Manifest location from which we download.  MAY be a URL OR A UNC, so make no assumptions
		/// </summary>
		[XmlElement( "xmlFile" )]
		public string ServerManifestFile
		{
			get{ return _xmlFile; }
			//  JUST set path exactly as found in manifest, do not try to "fix" it--UNC vs URL a headache, 
			//  IMPORTANT:  IT IS UP TO DEV to get download path to Manifest correct in config
			set{ _xmlFile = value; }
		} string _xmlFile;

		/// <summary>
		/// Destination of the Server Manifest file on the local machine--MUST be a file path, NOT URL
		/// </summary>
		[XmlElement( "xmlFileDest" )]
		public string ServerManifestFileDestination
		{
			get{ return _xmlFileDest; }
			set{ _xmlFileDest = value; }
		} string _xmlFileDest;

		/// <summary>
		/// Maximum time to wait for the metadata file to download
		/// </summary>
		[XmlElement( "maxWaitXmlFile" )]
		public int MaxWaitXmlFile
		{
			get{ return _maxWaitXmlFile; }
			set{ _maxWaitXmlFile = value; }
		} int _maxWaitXmlFile;
		
	}
	#endregion

	
	#region ApplicationConfiguration
	/// <summary>
	/// The application configuration
	/// </summary>
	public  class ApplicationConfiguration
	{
		/// <summary>
		/// The application name
		/// </summary>
		[XmlAttribute("name")]
		public string Name
		{
			get{ return _name; }
			set{ _name = value; }
		} string _name;

		/// <summary>
		/// Client configuration
		/// </summary>
		[XmlElement("client")]
		public ClientConfiguration Client
		{
			get{ return _client; }
			set{ _client = value; }
		} ClientConfiguration _client;

		/// <summary>
		/// Tells Validator whether to bother with validation or not--
		/// BE VERY CAUTIOUS WHEN USING "FALSE" AS AN OPTION, IT LEAVES YOUR APPLICATION
		/// MUCH OPEN TO ATTACK--should really only be used during testing.  
		/// Even internal deployment with this option "false" is dangerous.
		/// </summary>
		[XmlAttribute( "useValidation")]
		public bool UseValidation
		{
			get{ return _useValidation; }
			set{ _useValidation = value; }
		} bool _useValidation;

		/// <summary>
		/// Server configuration
		/// </summary>
		[XmlElement("server")]
		public ServerConfiguration Server
		{
			get{ return _server; }
			set{ _server = value; }
		} ServerConfiguration _server;
		
	}
	#endregion

	
	#region ValidatorConfiguration
	/// <summary>
	/// The validator configuration class
	/// </summary>
	public  class ValidatorConfiguration
	{
		/// <summary>
		/// The type name for the validator. The type configured must implement 
		/// the IValidator interface.
		/// </summary>
		[XmlAttribute( "type" )]
		public string Type
		{
			get
			{ 
				return _type; 
			}
			set{ _type = value; }
		} string _type;

		/// <summary>
		/// The assembly where the Type is defined.
		/// </summary>
		[XmlAttribute( "assembly" )]
		public string Assembly
		{
			get
			{ 
				return _assembly; 
			}
			set{ _assembly = value.Trim(); }
		} string _assembly;

		/// <summary>
		/// Configuration for the Validator
		/// </summary>
		[XmlIgnore]
		public XmlNode Config
		{
			get{ return _config; }
			set{ _config = value; }
		} XmlNode _config;
	}
	#endregion

	
	#region DownloaderConfiguration
	/// <summary>
	/// The downloader configuration
	/// </summary>
	public  class DownloaderConfiguration
	{
		/// <summary>
		/// The type name for the downloader. The type configured must implement 
		/// the IDownload interface.
		/// </summary>
		[XmlAttribute( "type" )]
		public string Type
		{
			get{ return _type; }
			set{ _type = value; }
		} string _type;

		/// <summary>
		/// The assembly where the Type is defined.
		/// </summary>
		[XmlAttribute( "assembly" )]
		public string Assembly
		{
			get{ return _assembly;  }
			set{ _assembly = value.Trim(); }
		} string _assembly;

		/// <summary>
		/// Configuration for the downloader provider
		/// </summary>
		[XmlIgnore]
		public XmlNode Config
		{
			get{ return _config; }
			set{ _config = value; }
		} XmlNode _config;
	}
	#endregion
}
