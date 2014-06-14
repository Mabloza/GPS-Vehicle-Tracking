
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// ApplicationUpdaterException.cs
//
// Exception class for certain Updater exceptions.
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
using System.Runtime.Serialization;
using Microsoft.ApplicationBlocks.ExceptionManagement;

namespace Microsoft.ApplicationBlocks.ApplicationUpdater
{
	/// <summary>
	/// General exception for the application updater application block
	/// </summary>
	[Serializable]
	public class ApplicationUpdaterException : Microsoft.ApplicationBlocks.ExceptionManagement.BaseApplicationException
	{
		/// <summary>
		/// A default constructor
		/// </summary>
		public ApplicationUpdaterException() { }

		/// <summary>
		/// Creates an exception using a message
		/// </summary>
		/// <param name="message">The message</param>
		public ApplicationUpdaterException( string message ) : base( message ) { }

		/// <summary>
		/// Creates an exception with a message and an inner exception
		/// </summary>
		/// <param name="message">The message</param>
		/// <param name="innerException">The inner exception</param>
		public ApplicationUpdaterException( string message, Exception innerException ) : base( message, innerException ) { }


		/// <summary>
		/// constructor for use by serialization
		/// </summary>
		/// <param name="si"></param>
		/// <param name="context"></param>
		protected ApplicationUpdaterException(SerializationInfo si, StreamingContext context){}
	}
}
