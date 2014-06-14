
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// ConfigSectionHandler.cs
//
// IConfigurationSectionHandler implementation that handles Updater configuration files.
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
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.IO;
using System.Text;

namespace Microsoft.ApplicationBlocks.ApplicationUpdater
{
	/// <summary>
	/// Main configuration section handler
	/// </summary>
	internal  class UpdaterSectionHandler : IConfigurationSectionHandler
	{		
		#region Declarations

		private bool _isValidDocument = true;
		private string _schemaErrors = "";
		private StringBuilder _sb = new StringBuilder( 1000 );
		
		#endregion
		
		/// <summary>
		/// Default constructor
		/// </summary>
		public UpdaterSectionHandler(){}

		
		object IConfigurationSectionHandler.Create( object parent, object configContext, XmlNode section )
		{
			
			XmlSerializer serializer = null ;
			serializer = new XmlSerializer( typeof( UpdaterConfiguration ) );

			//  first pass the node to our validation helper to catch serious errors before we even try deserialization:
			ValidateConfiguration( section );

			//  now try for deserialization--remember, each of the config property classes is decorated with xml deserialization 
			//  attributes which will "automatically" put elements/attributes in class fields
			UpdaterConfiguration config = 
				(UpdaterConfiguration)serializer.Deserialize( 
					new XmlNodeReader( section.SelectSingleNode( @"UpdaterConfiguration" ) ) );

			//  uses default XML serialization to populate the strong-typed classes used to describe configuration data.
			//  see the XML serialization attributes on the configuration classes.
			config.Downloader.Config = section.SelectSingleNode( 
				@"./UpdaterConfiguration/downloader" );
			config.Validator.Config = section.SelectSingleNode( 
				@"./UpdaterConfiguration/validator" );
			return config;
		}


		/// <summary>
		/// This helper method uses the XSD schema to validate the Updater configuration xml file.  It also does some logical checks.
		/// If anything is out of order, it throws an exception here before Updater processing can go any further.
		///   NOTE:  configuration is the single most important aspect of Updater setup, it MUST be correct
		/// </summary>
		/// <param name="section">the xmlnode containing the Updater configuration file</param>
		private void ValidateConfiguration( XmlNode section )
		{                
			// throw if there is no configuration node.
			if( null == section )
			{
				throw new ConfigurationException( "The configuration section passed within the UpdaterSectionHandler class was null; this is an unrecoverable error, there must be a configuration file defined.", section );
			}
                
			//Validate the document using a schema
			XmlValidatingReader vreader = new XmlValidatingReader( new XmlTextReader( new StringReader( section.OuterXml ) ) );
			
			// use "using" to dispose these resources
			//  open stream on Resources; the XSD is set as an "embedded resource" so Resource can open a stream on it
			using( Stream xsdFile = Resource.ResourceManager.GetStream( "Microsoft.ApplicationBlocks.ApplicationUpdater.ConfigSchema.xsd" ) )
			{ 
				using( StreamReader sr = new StreamReader( xsdFile ) )
				{
					vreader.ValidationEventHandler += new ValidationEventHandler( ValidationCallBack );
					vreader.Schemas.Add( XmlSchema.Read( new XmlTextReader( sr ), null ) );
					vreader.ValidationType = ValidationType.Schema;
					// Validate the document
					while (vreader.Read()){}

					if( !_isValidDocument )
					{ 
						_schemaErrors = _sb.ToString();
						throw new ConfigurationException( Resource.ResourceManager[ "RES_EXCEPTION_DocumentNotValidated", _schemaErrors ] );
					}
				}
			}


		}

		
		private void ValidationCallBack( object sender, ValidationEventArgs args )
		{
			//  check what KIND of problem the schema validation reader has;
			//  on FX 1.0, it gives a warning for "<xs:any...skip" sections.  Don't worry about those, only set validation false
			//  for real errors
			if( args.Severity == XmlSeverityType.Error )
			{
				_isValidDocument = false;
				_sb.Append( args.Message + Environment.NewLine );
			}
		}



	}


}
