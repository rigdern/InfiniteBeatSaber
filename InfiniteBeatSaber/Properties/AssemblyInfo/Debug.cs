﻿// These values are only used by development builds. The values for release builds are provided by
// 'build-tools\genAssemblyInfo.ps1'.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("InfiniteBeatSaber")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("InfiniteBeatSaber")]
[assembly: AssemblyCopyright("Copyright © Adam Comella 2023")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Enable the `Eval` assembly to see our `internal` types. This makes the REPL
// experience more convenient.
[assembly: InternalsVisibleTo("Eval")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("577c9955-0b0c-413e-8262-e1e9da673964")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.2.3.4")]
[assembly: AssemblyFileVersion("1.2.3.4")]
