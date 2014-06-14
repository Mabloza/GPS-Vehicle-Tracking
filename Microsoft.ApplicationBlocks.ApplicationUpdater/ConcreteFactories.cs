
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// ConcreteFactories.cs
//
// Factory pattern classes for creating objects used in Updater.
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

using Microsoft.ApplicationBlocks.ApplicationUpdater;
using Microsoft.ApplicationBlocks.ApplicationUpdater.Validators;
using Microsoft.ApplicationBlocks.ApplicationUpdater.Downloaders;
using Microsoft.ApplicationBlocks.ApplicationUpdater.Interfaces;

using Microsoft.ApplicationBlocks.ExceptionManagement;

namespace Microsoft.ApplicationBlocks.ApplicationUpdater
{


	internal class DownloaderFactory
	{
		#region Constructors

		//  hide constructors
		static DownloaderFactory(){}

		private DownloaderFactory(){}

		#endregion

		public static IDownloader Create( UpdaterConfiguration configuration )
		{
			IDownloader downloader = null;

			#region Create the IDownloader provider

			//  use the GenericFactory to actually create the instance
			downloader = (IDownloader)GenericFactory.Create( configuration.Downloader.Assembly, configuration.Downloader.Type );

			try
			{
				downloader.Init( configuration.Downloader.Config );
			}
			catch( Exception e )
			{
				string error = ApplicationUpdateManager.TraceWrite( e, "[DownloaderFactory.Create]", "RES_ErrorInitializingDownloader", configuration.Downloader.Type, configuration.Downloader.Assembly );
				ApplicationUpdaterException theException = new ApplicationUpdaterException( error, e );
				ExceptionManager.Publish( theException );
				throw theException;
			}

			#endregion

			return downloader;
		}
	}

	
	internal class ValidatorFactory
	{
		#region Constructors

		//  hide constructors
		static ValidatorFactory(){}

		private ValidatorFactory(){}

		#endregion

		public static IValidator Create( UpdaterConfiguration configuration )
		{
			IValidator validator = null;
						
			#region Create the IValidator provider

			//  use generic Factory to create instance of Validator with config type/asm info			
			validator = (IValidator) GenericFactory.Create( configuration.Validator.Assembly, configuration.Validator.Type );

			try
			{
				validator.Init( configuration.Validator.Config );
			}
			catch( Exception e )
			{
				string error = ApplicationUpdateManager.TraceWrite( e, "[ValidatorFactory.Create]", "RES_ErrorInitializingValidator", configuration.Validator.Type, configuration.Validator.Assembly );
				ApplicationUpdaterException theException = new ApplicationUpdaterException( error, e );
				ExceptionManager.Publish( theException );
				throw theException;
			}

			#endregion

			return validator;
		}
	}



}
