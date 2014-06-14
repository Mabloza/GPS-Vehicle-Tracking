
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// AssemblyInfo.cs
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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;


[assembly: FileIOPermission(SecurityAction.RequestMinimum)]
[assembly: SecurityPermissionAttribute(SecurityAction.RequestMinimum, Flags=SecurityPermissionFlag.UnmanagedCode)]
[assembly: SecurityPermissionAttribute(SecurityAction.RequestMinimum, ControlThread = true)]

[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: AssemblyTitle("ApplicationUpdater")]
[assembly: AssemblyDescription("Microsoft Updater Application Block")]
[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyProduct("Bluebricks: UAB")]
[assembly: AssemblyCopyright("(c) 2003 Microsoft Corporation")]	

[assembly: AssemblyVersion( "1.0.0.0" ) ]

[assembly: AssemblyDelaySign(false)]
