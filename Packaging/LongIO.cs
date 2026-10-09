// Use extended Win32 paths for IO without changing Windows' global policy.
// Keep display paths, process arguments and shortcuts in ordinary syntax.
using System;
using System.IO;
using System.Linq;
using System.Text;
static class LongPath {
 public static string IO(string path){string full=Path.GetFullPath(path);if(full.StartsWith(@"\\?\"))return full;return full.StartsWith(@"\\")?@"\\?\UNC\"+full.Substring(2):@"\\?\"+full;}
 public static string Display(string path){return path.StartsWith(@"\\?\UNC\")?@"\\"+path.Substring(8):path.StartsWith(@"\\?\")?path.Substring(4):path;}
}
static class LongDirectory {
 public static bool Exists(string p){return System.IO.Directory.Exists(LongPath.IO(p));}
 public static DirectoryInfo CreateDirectory(string p){return System.IO.Directory.CreateDirectory(LongPath.IO(p));}
 public static void Delete(string p,bool recursive){System.IO.Directory.Delete(LongPath.IO(p),recursive);}
 public static void Move(string a,string b){System.IO.Directory.Move(LongPath.IO(a),LongPath.IO(b));}
 public static string[] GetFiles(string p){return System.IO.Directory.GetFiles(LongPath.IO(p)).Select(LongPath.Display).ToArray();}
 public static string[] GetFiles(string p,string pattern,SearchOption option){return System.IO.Directory.GetFiles(LongPath.IO(p),pattern,option).Select(LongPath.Display).ToArray();}
 public static string[] GetDirectories(string p){return System.IO.Directory.GetDirectories(LongPath.IO(p)).Select(LongPath.Display).ToArray();}
 public static string[] GetDirectories(string p,string pattern,SearchOption option){return System.IO.Directory.GetDirectories(LongPath.IO(p),pattern,option).Select(LongPath.Display).ToArray();}
}
static class LongFile {
 public static bool Exists(string p){return System.IO.File.Exists(LongPath.IO(p));}
 public static string ReadAllText(string p){return System.IO.File.ReadAllText(LongPath.IO(p));}
 public static void WriteAllText(string p,string text){System.IO.File.WriteAllText(LongPath.IO(p),text);}
 public static void Copy(string a,string b,bool overwrite){System.IO.File.Copy(LongPath.IO(a),LongPath.IO(b),overwrite);}
 public static void Copy(string a,string b){Copy(a,b,false);}
 public static void Delete(string p){System.IO.File.Delete(LongPath.IO(p));}
 public static FileStream OpenRead(string p){return System.IO.File.OpenRead(LongPath.IO(p));}
 public static FileStream Open(string p,FileMode m){return System.IO.File.Open(LongPath.IO(p),m);}
 public static FileStream Create(string p){return System.IO.File.Create(LongPath.IO(p));}
 public static FileAttributes GetAttributes(string p){return System.IO.File.GetAttributes(LongPath.IO(p));}
}
