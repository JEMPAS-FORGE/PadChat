// Set the framework target explicitly when using the bundled legacy csc.
// This opts into modern .NET 4.8 path handling before any static IO initialises.
[assembly:System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8",FrameworkDisplayName=".NET Framework 4.8")]
