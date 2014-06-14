
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// Metadata.cs
//
// Encapsulates configuration, server, and client information used by Updater.
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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.ApplicationBlocks.ExceptionManagement;

namespace Microsoft.ApplicationBlocks.ApplicationUpdater
{
	#region ClientApplicationInfo
	/// <summary>
	/// Client config file definition
	/// </summary>
	public  class ClientApplicationInfo
	{
		/// <summary>
		/// Uses an XML file as source of data.  Populate a ClientApplicationInfo object and return it.
		/// </summary>
		/// <param name="filePath">The XML file path</param>
		/// <returns></returns>
		public static ClientApplicationInfo Deserialize( string filePath )
		{
			// Cannot use xml-deserialization here 
			ApplicationUpdateManager.TraceWrite( "[ClientApplicationInfo.Deserialize]", "RES_MESSAGE_DeserializingClientApplicationInfo", filePath );

			//  create xml doc
			XmlDocument doc = new XmlDocument();
			XmlNode node = null;
			ClientApplicationInfo clientAppInfo = new ClientApplicationInfo();

			try
			{
				//  load it
				doc.Load( filePath );
				//  select the sub-node we want:
				//  NOTE that we are using a regular app.config, specifically AppStart.exe.config; 
				//  SO we can't just treat it like a regular xml-deserialization because the serializer doesn't like the extra 
				//  nodes.  Therefore just walk the XML to get values for class

				node = doc.SelectSingleNode( "configuration/appStart/ClientApplicationInfo" );
			
				//  set values
				clientAppInfo.AppFolderName			= node.SelectSingleNode( "appFolderName" ).InnerText ;
				clientAppInfo.AppExeName			= node.SelectSingleNode( "appExeName" ).InnerText ;
				clientAppInfo.InstalledVersion		= node.SelectSingleNode( "installedVersion" ).InnerText ;
			}
			catch( Exception e )
			{
			
				string error = ApplicationUpdateManager.TraceWrite( e, "[ClientApplicationInfo.Deserialize]", "RES_EXCEPTION_CouldNotDeserializeClientApplicationInfo", e.Message,  filePath );
				
				ExceptionManager.Publish( e );
				throw e;
				
			}
			//  return it
			return clientAppInfo;

		}

		
		/// <summary>
		/// Saves the client config file--usually AppStart.exe.config--with changes.
		/// 
		/// </summary>
		/// <param name="filePath">local path to config file</param>
		/// <param name="appFolderName">path to client application's folder</param>
		/// <param name="installedVersion">currently installed version (updated)</param>
		public static void Save( string filePath, string appFolderName, string installedVersion  )
		{
			XmlNode node = null;
			XmlDocument doc = new XmlDocument();
			
			//  tell it to preserve whitespace and therefore preserve administrator's vision :)
			doc.PreserveWhitespace = true;

			try
			{
				//  load client xml doc
				doc.Load( filePath );

				//  change the appFolderName--remember, we are now loading this app from the versioned folder--
				//  such that before we looked for it in "1.0.0.0\", now we must look in "2.0.0.0\"
				node = doc.SelectSingleNode( "configuration/appStart/ClientApplicationInfo/appFolderName" );
				node.InnerText = appFolderName;

				//  Change the server version
				node = doc.SelectSingleNode( "configuration/appStart/ClientApplicationInfo/installedVersion" );
				node.InnerText = installedVersion;

				//  Change the last updated date
				node = doc.SelectSingleNode( "configuration/appStart/ClientApplicationInfo/lastUpdated" );
				node.InnerText = XmlConvert.ToString( DateTime.Now );

				ApplicationUpdateManager.TraceWrite( "[ClientApplicationInfo.Save]", "RES_MESSAGE_SavingClientApplicationInfo", filePath );

				doc.Save( filePath );
			}
			catch( UnauthorizedAccessException uae )
			{					
				string error = ApplicationUpdateManager.TraceWrite( uae, "[ClientApplicationInfo.Save]", "RES_EXCEPTION_UnauthorizedAccessFile", filePath, uae.Message );
				ExceptionManager.Publish( uae );
				//  throw, this is serious enough to halt:
				throw uae;
			}
			catch( Exception e )
			{
				//  need to add specific error info for this, it is a well-known error when the appstart (or other "shim" config)
				//  is bad or absent:
				string error = ApplicationUpdateManager.TraceWrite( e, "[ClientApplicationInfo.Save]", "RES_EXCEPTION_ErrorSavingClientApplicationInfo", filePath, appFolderName, installedVersion );
				//  throw new, wrapping up original--it's possible exception has nothing to do with what we anticipate above, 
				//  so give all info possible in roll-up
				throw new Exception( error, e );
			}
		}



		/// <summary>
		/// Default public constructor
		/// </summary>
		public ClientApplicationInfo(){}


		
		/// <summary>
		/// The folder name for the application-- the base folder
		/// </summary>
		[XmlElement( "appFolderName" )]
		public string AppFolderName
		{
			get{ return _appFolderName; }
			set
			{ 
				DirectoryInfo dirInfo = new DirectoryInfo( FileUtility.AppendTerminalBackSlash( value ) );
				_appFolderName = dirInfo.FullName; 			
			}
		} string _appFolderName;

		/// <summary>
		/// The EXE name for the application within the given folder 
		/// </summary>
		[XmlElement( "appExeName" )]
		public string AppExeName
		{
			get{ return _appExeName; }
			set{ _appExeName = value; }
		} string _appExeName;
		
		/// <summary>
		/// The installed version for the application.  
		/// </summary>
		[XmlElement( "installedVersion" )]
		public string InstalledVersion
		{
			get{ return _installedVersion; }
			set{ _installedVersion  = value; }
		} string _installedVersion ;
	}
	#endregion


	#region ServerApplicationInfo
	/// <summary>
	/// Server config file definition
	/// </summary>
	public  class ServerApplicationInfo
	{

		/// <summary>
		/// Deserializes the configuration in an XML file
		/// </summary>
		/// <param name="filePath">The XML file path</param>
		/// <returns>ServerApplicationInfo object</returns>
		public static ServerApplicationInfo Deserialize( string filePath )
		{
			ServerApplicationInfo serverInfo = null;
			XmlSerializer _serializer = new XmlSerializer( typeof( ServerApplicationInfo ) );

			ApplicationUpdateManager.TraceWrite( "[ServerApplicationInfo.Deserialize]", "RES_MESSAGE_DeserializeServerApplicationInfo", filePath );
			
			try
			{
				using( FileStream fs = new FileStream( filePath, FileMode.Open ) )
				{
					serverInfo = (ServerApplicationInfo)_serializer.Deserialize( new XmlTextReader( fs ) );
				}
				//  return it
				return serverInfo;
			}
			catch( Exception e )
			{
				ApplicationUpdateManager.TraceWrite( e, "[ServerApplicationInfo.Deserialize]", "RES_EXCEPTION_CouldNotDeserializeServerApplicationInfo", e.Message, filePath );
				
				ExceptionManager.Publish( e );
				throw e;
			}
		}



		/// <summary>
		/// Holds an instance of post-processor info class 
		/// </summary>
		[XmlElement( "postProcessor" )]
		public PostProcessorConfiguration PostProcessor
		{
			get{ return _postProcessor; }
			set{ _postProcessor = value; }
		} private PostProcessorConfiguration _postProcessor = null;

		/// <summary>
		/// Stores the manifest-level "signature" or keyed hash which is used to validate contents of all nodes below root.
		/// </summary>
		[XmlAttribute( "signature" )]
		public string ManifestSignature
		{
			get{  return _manifestSignature; }
			set{  _manifestSignature = value; }
		}  private string _manifestSignature;

		/// <summary>
		/// The available version on the server
		/// </summary>
		[XmlElement( "availableVersion" )]
		public string AvailableVersion
		{
			get{ return _availableVersion; }
			set{ _availableVersion = value; }
		} string _availableVersion;

		/// <summary>
		/// The URL OR UNC location of files on the server
		/// </summary>
		[XmlElement( "updateLocation" )]
		public string UpdateLocation
		{
			get{ return _updatelocation; }
			set{
				try
				{
					_updatelocation = FileUtility.AppendSlashURLorUNC( value );
				}
				catch( Exception e )
				{
					string error =  ApplicationUpdateManager.TraceWrite( e, "[ServerApplicationInfo.UpdateLocation]", "RES_InvalidServerXmlFileUpdateLocation", value );

					ApplicationUpdaterException theException = new ApplicationUpdaterException( error, e );
					ExceptionManager.Publish( theException );
					throw theException;
				}
			}
		} string _updatelocation = "";
		
		/// <summary>
		/// The files available on the server--makes no assumption about URL- or UNC-ness
		/// </summary>
		[XmlArray( "files" ), XmlArrayItem( "file", typeof(FileConfig) )]
		public FileConfig[] Files
		{
			get{ return _files; }
			set{ _files = value; }
		} FileConfig[] _files;
	}
	#endregion


	#region PostProcessorConfiguration
	/// <summary>
	/// Encapsulates post-processor (IPostProcessing) information (type, assembly) used
	/// to instantiate a post-processor after download and validation; 
	/// post-processors allow higher-level install activities such as deleting old directories, installing queues, 
	/// eventlogs, etc.
	/// </summary>
	public  class PostProcessorConfiguration
	{
		/// <summary>
		/// default constructor--not used
		/// </summary>
		public PostProcessorConfiguration(){}
	
		
		/// <summary>
		/// The fully-qualified assembly
		/// </summary>
		[XmlAttribute( "assembly" )]
		public string Assembly
		{
			get{ return _assembly; }
			set{ _assembly = value; }

		} private string _assembly = "";

		/// <summary>
		/// The type
		/// </summary>
		[XmlAttribute( "type" )]
		public string Type
		{
			get{ return _type; }
			set{ _type = value; }
		} private string _type = "";
		
		/// <summary>
		/// The name, that is the file path
		/// </summary>
		[XmlAttribute( "name" )]
		public string Name
		{
			get{ return _name; }
			set{ _name = value; }
		} private string _name = "";

	}
	#endregion

	
	#region FileConfig
	/// <summary>
	/// A file available on the server
	/// </summary>
	public class FileConfig
	{
		/// <summary>
		/// default constructor--not used
		/// </summary>
		public FileConfig(){}


		/// <summary>
		/// The file name
		/// </summary>
		[XmlAttribute( "name" )]
		public string Name
		{
			get{ return _name; }
			set{
				try
				{
					new FileInfo( value );
				}
				catch( Exception e )
				{
					string error =  ApplicationUpdateManager.TraceWrite( e, "[FileConfig.Name]", "RES_InvalidServerFileName", value );

					ApplicationUpdaterException theException = 
						new ApplicationUpdaterException( error, e);
					ExceptionManager.Publish( theException );
					throw theException;
				}
				_name = value; 
			}
		} string _name;

		/// <summary>
		/// returns the UNC-friendly file path/name
		/// </summary>
		public string UNCName
		{
			get
			{ 
				if( "" == _uncName )
				{
					_uncName = FileUtility.ConvertToUNCPath( _name );
				}
				return _uncName; 
			}
		} private string _uncName = "";

		/// <summary>
		/// The file signature
		/// </summary>
		[XmlAttribute( "signature" )]
		public string Signature
		{
			get{ return _signature; }
			set{ _signature = value; }
		} string _signature = "";

	}
	#endregion
}
