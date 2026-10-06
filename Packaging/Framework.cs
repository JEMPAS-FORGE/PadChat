// Set the framework target explicitly when using the bundled legacy csc.
// This opts into modern .NET 4.8 path handling before any static IO initialises.
[assembly:System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8",FrameworkDisplayName=".NET Framework 4.8")]

// Shared by the installer and voice companion. No unrelated foreground app
// qualifies, and older private-server executables are not an advertised target.
static class WoWClients {
 public static readonly string[] Executables={"Wow.exe","WowClassic.exe","WowB.exe","WowT.exe"};
 public static readonly string[] Folders={"_retail_","_classic_","_classic_era_","_anniversary_","_classic_anniversary_","_forever_","_classic_beta_"};
 public static bool IsProcess(string name){
  foreach(var file in Executables)if(string.Equals(System.IO.Path.GetFileNameWithoutExtension(file),name,System.StringComparison.OrdinalIgnoreCase))return true;
  return false;
 }
 public static bool SupportsVersion(string value){
  System.Version version;
  if(!System.Version.TryParse(value,out version))return false;
  return version.Major==12 || version.Major==5&&version.Minor==5 || version.Major==1&&(version.Minor==15||version.Minor==60) || version.Major==2&&version.Minor==5;
 }
 public static string FindExecutable(string game){
  foreach(var file in Executables){string path=System.IO.Path.Combine(game,file);if(System.IO.File.Exists(path))return path;}
  return null;
 }
 public static bool AnyRunning(){
  foreach(var file in Executables){var processes=System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(file));bool found=processes.Length>0;foreach(var p in processes)p.Dispose();if(found)return true;}
  return false;
 }
}
