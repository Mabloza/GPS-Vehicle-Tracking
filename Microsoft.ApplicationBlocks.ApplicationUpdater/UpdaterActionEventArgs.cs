
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// UpdaterActionEventArgs.cs
//
// Event arguments definition, provides type-specific event arguments for all Updater events.
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
using System.Xml;

namespace Microsoft.ApplicationBlocks.ApplicationUpdater
{
	/// <summary>
	/// The Updater-specific derivation of System.EventArgs
	/// Encapsulates information of interest to clients of the Updater events, so they can act on the information.
	/// When Updater raises events, it creates this EventArg and populates it with the ServerApplicationInfo 
	/// containing relevant information not known to the client--namely, server manifest information.
	/// </summary>
	public class UpdaterActionEventArgs : EventArgs
	{
		#region constructors
		
		/// <summary>
		/// constructor accepting just ServerApplicationInfo 
		/// </summary>
		/// <param name="serverInformation">server information</param>
		public UpdaterActionEventArgs( ServerApplicationInfo serverInformation ) : this( serverInformation, "" ){}
		
		/// <summary>
		/// Constructor that accepts only application name
		/// </summary>
		/// <param name="appName">name of the application being Updated</param>
		public UpdaterActionEventArgs( string appName ) : this( null, appName ){}
		
		/// <summary>
		/// full constructor takes both items of information passed by this EventArgs derivative.
		/// </summary>
		/// <param name="serverInformation">server information</param>
		/// <param name="appName">name of the application being Updated</param>
		public UpdaterActionEventArgs( ServerApplicationInfo serverInformation , string appName )
		{ 
			//	node may be null depending on raiser.
			_serverInformation = serverInformation;
			//  string app name
			_appName = appName;
		}
		

		#endregion
		
		#region declarations

		private ServerApplicationInfo	_serverInformation	= null;
		private string					_appName			= "";
		
		#endregion

		#region Properties
		
		/// <summary>
		/// the encapsulation of server information from the Server Manifest File.
		/// </summary>
		public ServerApplicationInfo ServerInformation
		{
			get{ return _serverInformation ; }
		}

		/// <summary>
		/// The application name for the application being updated.  Is stored in the Updater configuration file and may not necessarily be the real application name.
		/// </summary>
		public string ApplicationName
		{
			get{ return _appName ; }
		}

		
		#endregion
	}
}