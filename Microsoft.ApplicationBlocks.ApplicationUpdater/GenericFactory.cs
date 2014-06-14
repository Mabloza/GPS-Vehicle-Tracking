
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// GenericFactory.cs
//
// Helper class that acts as the base factory for other Factory Pattern classes.
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
using System.Reflection;

using Microsoft.ApplicationBlocks.ExceptionManagement;

namespace Microsoft.ApplicationBlocks.ApplicationUpdater
{
	/// <summary>
	/// Acts as the basic implementation for the multiple Factory classes used in the Updater.
	/// We need to create instances based on config info for the flexible object types...
	/// Have Factories for those, and since there's much common code for doing Reflection-based activation 
	/// keep that code in one central place.
	/// </summary>
	internal sealed class GenericFactory
	{

		#region Declarations

		private const string COMMA_DELIMITER	 = ",";

		#endregion

		#region Constructors

		static GenericFactory(){}
		

		private GenericFactory(){}


		#endregion

		#region Private Helper Methods


		/// <summary>
		/// Utility method that splits a full type name (assembly + type) into the constituent five parts, trims those parts, and throws an error if there are not exactly five members.
		/// </summary>
		/// <param name="fullType">the assembly + type names, fully qualified</param>
		/// <param name="typeName">the type name, full</param>
		/// <param name="assemblyName">they fully-qualified assembly name including name, version, culture, and public key token</param>
		private static void SplitType( string fullType, out string typeName, out string assemblyName )
		{
			string[] parts = fullType.Split( COMMA_DELIMITER.ToCharArray() );

			// ms--most common source of errors is bad configuration, especially of the assembly+type definitions in config files.
			//  Assert() here so we can be alerted during debugging.
			Debug.Assert( 5 == parts.Length, "in GenericFactory.SplitType, passed fullType = " + fullType + " and it would not split 5 ways." );

			if ( 5 != parts.Length )
			{
				//  pad parts to satisfy error message--not best, but makes clearer errors:
				string[] errorParts = new string[] {"<undefined>","<undefined>","<undefined>","<undefined>","<undefined>"};
				for( int i = 0; i < 5; i++ )
				{
					if( i < parts.Length )
					{
						errorParts[i] = parts[i];
					}
				}
				string error = ApplicationUpdateManager.TraceWrite( "[GenericFactory.SplitType]", "RES_EXCEPTION_BadTypeArgumentInFactory", errorParts );

				//  publish this exception
				ArgumentException ae = new ArgumentException( error, "fullType" );
				ExceptionManager.Publish( ae );
				throw ae;
			}
			else
			{
				//  package type name:
				typeName = parts[0].Trim();
				//  package fully-qualified assembly name separated by commas
				assemblyName = String.Concat(   parts[1].Trim() + COMMA_DELIMITER,
												parts[2].Trim() + COMMA_DELIMITER,
												parts[3].Trim() + COMMA_DELIMITER,
												parts[4].Trim() );
				//  return
				return;
			}
		}


		#endregion

		#region Create Overloads

		//**    FULL EXAMPLE OF WHAT A CORRECT CONFIG FILE TYPE+ASM DEFINITION LOOKS LIKE    **
		//  REMEMBER that PublicKeyToken can be 'd69d63db1380c14d' (for example, your public key will be different ) too if asm is strong-named
		//<validator  
		//	 type="Microsoft.ApplicationBlocks.ApplicationUpdater.Validators.RSAValidator" 
		//	 assembly="Microsoft.ApplicationBlocks.ApplicationUpdater,Version=1.0.0.0,Culture=neutral,PublicKeyToken=null">

		/// <summary>
		/// Returns an object instantiated by the Activator, using fully-qualified combined assembly-type  supplied.
		/// Assembly parameter example: "Microsoft.ApplicationBlocks.ApplicationUpdater,Version=1.0.0.0,Culture=neutral,PublicKeyToken=null"
		/// Type parameter example: "Microsoft.ApplicationBlocks.ApplicationUpdater.Validators.RSAValidator"
		/// </summary>
		/// <param name="fullTypeName">fully-qualified assembly name and type name</param>
		/// <returns>instance of requested assembly/type typed as System.Object</returns>
		public static object Create( string fullTypeName )
		{
			string assemblyName;
			string typeName;
			//  use helper to split
			SplitType( fullTypeName, out typeName, out assemblyName );
			//  just call main overload
			return Create( assemblyName, typeName, null );
		}


		/// <summary>
		/// Uses a file path to load an assembly.  Then instantiates the requested type by searching interfaces.		
		/// Returns an object instantiated by the Activator, using fully-qualified combined assembly-type  supplied.
		/// Assembly parameter example: "Microsoft.ApplicationBlocks.ApplicationUpdater,Version=1.0.0.0,Culture=neutral,PublicKeyToken=null"
		/// Type parameter example: "Microsoft.ApplicationBlocks.ApplicationUpdater.Validators.RSAValidator"
		/// </summary>
		/// <param name="filePath">full path to assembly</param>
		/// <param name="interfaceToActivate">the Type representing the interface to activate</param>
		/// <returns></returns>
		public static object Create( string filePath, Type interfaceToActivate )
		{
			Assembly asm = null;
			Type typeInstance = null;
			Type[] types = null;
			Object obj = null;

			try
			{
				asm = Assembly.LoadFrom( filePath );
				types = asm.GetTypes();
				//  walk through all types in assembly, find the one that IS IPP and use its info
				foreach( Type type in types )
				{
					//  search for interface by string name in this type
					typeInstance = type.GetInterface( interfaceToActivate.FullName, false );
					//  if we find the interface, return the type that implements it
					if( null != typeInstance )
					{
						//  we found the first instance of the given interface
						//  THERE MAY BE MORE but this is a convenience method.
						typeInstance = type;
						break;
					}
				
				}
				obj = asm.CreateInstance( typeInstance.FullName );
			}
			catch( Exception e )
			{
				string error = ApplicationUpdateManager.TraceWrite( "[GenericFactory.Create]", "RES_EXCEPTION_CantCreateInstanceFromFilePath", filePath, interfaceToActivate );

				TypeLoadException tle = new TypeLoadException( error, e );
				ExceptionManager.Publish ( tle );
				throw tle;
			}

			return obj;
		}

		/// <summary>
		/// Returns an object instantiated by the Activator, using fully-qualified assembly + type  supplied.
		/// Assembly parameter example: "Microsoft.ApplicationBlocks.ApplicationUpdater,Version=1.0.0.0,Culture=neutral,PublicKeyToken=null"
		/// Type parameter example: "Microsoft.ApplicationBlocks.ApplicationUpdater.Validators.RSAValidator"
		/// </summary>
		/// <param name="assemblyName">fully-qualified assembly name</param>
		/// <param name="typeName">the type name</param>
		/// <returns>instance of requested assembly/type typed as System.Object</returns>
		public static object Create( string assemblyName, string typeName )
		{
			string aName;
			string tName;

			//  use helper to split
			SplitType( typeName + "," + assemblyName , out tName, out aName );
			
			//  just call main overload
			return Create( aName, tName, null );
		}


		/// <summary>
		/// Returns an object instantiated by the Activator, using fully-qualified asm/type supplied.
		/// Permits construction arguments to be supplied which it passes to the object's constructor on instantiation.
		/// </summary>
		/// <param name="assemblyName">fully-qualified assembly name</param>
		/// <param name="typeName">the type name</param>
		/// <param name="constructorArguments">constructor arguments for type to be created</param>
		/// <returns>instance of requested assembly/type typed as System.Object</returns>
		public static object Create( string assemblyName, string typeName, object[] constructorArguments )
		{
			Assembly assemblyInstance			= null;
			Type typeInstance					= null;

			try 
			{
				//  use full asm name to get assembly instance
				assemblyInstance = Assembly.Load( assemblyName.Trim() );
			}
			catch ( Exception e )
			{
				string error = ApplicationUpdateManager.TraceWrite( "[GenericFactory.Create]", "RES_EXCEPTION_CantLoadAssembly", assemblyName, typeName );

				TypeLoadException tle = new TypeLoadException( error, e );
				ExceptionManager.Publish ( tle );
				throw tle;
			}
			
			try
			{
				//  use type name to get type from asm; note we WANT case specificity 
				typeInstance = assemblyInstance.GetType( typeName.Trim(), true, false );

				//  now attempt to actually create an instance, passing constructor args if available
				if( constructorArguments != null )
				{
					return Activator.CreateInstance( typeInstance, constructorArguments );
				}
				else
				{
					return Activator.CreateInstance( typeInstance );
				}
			}
			catch( Exception e )
			{	
				string error = ApplicationUpdateManager.TraceWrite( "[GenericFactory.Create]", "RES_EXCEPTION_CantCreateInstanceUsingActivate", assemblyName, typeName );

				TypeLoadException tle = new TypeLoadException( error, e );
				ExceptionManager.Publish ( tle );
				throw tle;
			}
		}
		
		
		#endregion
	}
}
