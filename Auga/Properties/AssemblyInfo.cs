﻿using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if API
[assembly: AssemblyTitle("AugaAPI")]
#else
[assembly: AssemblyTitle("Auga")]
#endif
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Auga")]
[assembly: AssemblyCopyright("Copyright © Randy Knapp 2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("bcc7dd31-d943-4684-800e-176a97dddb8f")]

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
#if API
[assembly: AssemblyVersion("1.5")]
[assembly: AssemblyFileVersion("1.5")]
#else
[assembly: AssemblyVersion("1.3.8.0")]
[assembly: AssemblyFileVersion("1.3.8.0")]
#endif
