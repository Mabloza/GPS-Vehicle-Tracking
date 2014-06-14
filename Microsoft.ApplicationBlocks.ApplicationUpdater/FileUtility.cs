
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// FileUtility.cs
//
// Helper class for doing file and directory manipulation.
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
using System.IO;

using Microsoft.ApplicationBlocks.ExceptionManagement;

namespace Microsoft.ApplicationBlocks.ApplicationUpdater
{
	/// <summary>
	/// Provides certain utilities used by configuration processors, such as correcting file paths
	/// </summary>
	internal  class FileUtility

	{

		#region Declarations

		private const string DELIMITER_BACKSLASH				= @"\";
		private const string DELIMITER_FORWARDSLASH				= @"/";

		#endregion



		public static bool IsUNCPath( string path )
		{
			//  FIRST, check if this is a URL or a UNC path; do this by attempting to construct uri object from it
			Uri url = new Uri( path );
					
			if( url.IsUnc )
			{
				//  it is a unc path, return true
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Takes a UNC or URL path, determines which it is (NOT hardened against bad strings, assumes one or the other is present)
		/// and returns the path with correct trailing slash--UNC==back URL==forward
		/// </summary>
		/// <param name="path">URL or UNC</param>
		/// <returns>path with correct terminal slash</returns>
		public static string AppendSlashURLorUNC( string path )
		{					
			if( IsUNCPath( path ) )
			{
				//  it is a unc path, so decorate the end with a back-slash (to correct misconfigurations, defend against trivial errors)
				return AppendTerminalBackSlash( path );
			}
			else
			{
				//  assume URL here
				return AppendTerminalForwardSlash( path );
			}
		}


		/// <summary>
		/// Takes url-friendly paths such as "/foo/bar/file.txt" and converts to UNC-friendly paths by simple substitution, 
		/// --> "\foo\bar\file.txt"
		/// </summary>
		/// <param name="path">the url path</param>
		/// <returns>the UNC path</returns>
		public static string ConvertToUNCPath( string path )
		{
			return path.Replace( DELIMITER_FORWARDSLASH, DELIMITER_BACKSLASH );
		}

		
		/// <summary>
		/// If not present appends terminal backslash to paths
		/// </summary>
		/// <param name="path">path for example "C:\AppUpdaterClient"</param>
		/// <returns>path with trailing backslash--"C:\AppUpdaterClient\"</returns>
		public static string AppendTerminalBackSlash( string path )
		{
			if( path.IndexOf( DELIMITER_BACKSLASH, path.Length - 1 ) == -1 )
			{
				return path + DELIMITER_BACKSLASH;
			}
			else
			{
				return path;
			}
		}
		
		
		/// <summary>
		/// Appends a terminal forward-slash if there is not already one, returns corrected path
		/// </summary>
		/// <param name="path">the path that may be missing a terminal forward-slash</param>
		/// <returns>the corrected path with terminal forward-slash</returns>
		public static string AppendTerminalForwardSlash( string path )
		{
			if( path.IndexOf( DELIMITER_FORWARDSLASH, path.Length - 1 ) == -1 )
			{
				return path + DELIMITER_FORWARDSLASH;
			}
			else
			{
				return path;
			}
		}

		
		/// <summary>
		/// Given a file path such as "C:\foo\file.txt" this extracts the local root directory path, "C:\foo\" 
		/// complete with terminal backslash
		/// </summary>
		/// <param name="path">the full file path</param>
		/// <returns>the local root directory (strips terminal file name)</returns>
		public static string GetRootDirectoryFromFilePath( string path )
		{
			return AppendTerminalBackSlash( Path.GetDirectoryName( path ) );
		}


		/// <summary>
		/// Used to delete a directory and all its contents.  
		/// </summary>
		/// <param name="path">full path to directory</param>
		public static void DeleteDirectory( string path )
		{
			if( Directory.Exists( path ) )
			{
				try
				{
					Directory.Delete( path, true );				
				}
				catch( UnauthorizedAccessException uae )
				{					
					string error = ApplicationUpdateManager.TraceWrite( uae, "[FileUtility.DeleteDirectory]", "RES_EXCEPTION_UnauthorizedAccessDirectory", path, uae.Message );
					ExceptionManager.Publish( uae );
					//  throw, this is serious enough to halt:
					throw uae;
				}
			}
		}

		
		/// <summary>
		/// Used to delete a file  
		/// </summary>
		/// <param name="path">full path to file</param>
		public static void DeleteFile( string path )
		{
			if( File.Exists( path ) )
			{
				try
				{
					File.Delete( path );			
				}
				catch( UnauthorizedAccessException uae )
				{					
					string error = ApplicationUpdateManager.TraceWrite( uae, "[FileUtility.DeleteFile]", "RES_EXCEPTION_UnauthorizedAccessFile", path, uae.Message );
					ExceptionManager.Publish( uae );
					//  throw, this is serious enough to halt:
					throw uae;
				}
			}
		}


		/// <summary>
		/// Copy files from source to destination directories.  Directory.Move not suitable here because
		/// downloader may still have temp dir locked
		/// </summary>
		/// <param name="sourcePath">source path</param>
		/// <param name="destPath">destination path</param>
		public static void CopyDirectory( string sourcePath, string destPath  )
		{
			//  put paths into DirectoryInfo object to validate 
			DirectoryInfo dirInfoSource = new DirectoryInfo( sourcePath );
			DirectoryInfo dirInfoDest = new DirectoryInfo( destPath );

			//  check if destination dir exists, if so delete it
			if( Directory.Exists( dirInfoDest.FullName ) )
			{
				Directory.Delete( dirInfoDest.FullName, true );
			}
			//  make new dir named with new version #
			Directory.CreateDirectory( dirInfoDest.FullName );
			//  do recursive copy of temp dir to new version # dir--
			//  YES we could use one-line "Directory.Move" but _temp dir may be locked by other thread_
			CopyDirRecurse( dirInfoSource.FullName, dirInfoDest.FullName );
		}
		

		/// <summary>
		/// Utility function that recursively copies directories and files.
		/// Again, we could use Directory.Move but we need to preserve the original.
		/// </summary>
		/// <param name="sourcePath"></param>
		/// <param name="destinationPath"></param>
		private static void CopyDirRecurse( string sourcePath, string destinationPath )
		{
			//  ensure terminal backslash
			sourcePath = FileUtility.AppendTerminalBackSlash( sourcePath );
			destinationPath = FileUtility.AppendTerminalBackSlash( destinationPath );

			//  get dir info which may be file or dir info object
			DirectoryInfo dirInfo = new DirectoryInfo( sourcePath );

			foreach( FileSystemInfo fsi in dirInfo.GetFileSystemInfos() )
			{
				if ( fsi is FileInfo )
				{
					//  if file object just copy
					File.Copy( fsi.FullName, destinationPath + fsi.Name );
				}
				else
				{
					//  must be a directory, create destination sub-folder and recurse to copy files
					Directory.CreateDirectory( destinationPath + fsi.Name );
					CopyDirRecurse( fsi.FullName, destinationPath + fsi.Name );
				}
			}
		}
		


	}

}
