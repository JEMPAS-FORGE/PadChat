using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
partial class VoicePTT {
 sealed class AppearanceSettings {public int size=16;public string position="TOP";public bool meter=true,sound=false;}
 static AppearanceSettings appearance=new AppearanceSettings();
 static int readyCueSession=-1;
 static Action readySoundAction=()=>System.Media.SystemSounds.Asterisk.Play();
 static string AppearancePath(){return Path.Combine(dataRoot,"voice-appearance.json");}
 static AppearanceSettings ParseAppearance(string wire){
  var m=Regex.Match(wire??"",@"\APCA1;(14|16|20|24);(TOP|CENTER|BOTTOM);([01]);([01])\z");
  if(!m.Success)throw new ArgumentException("Invalid overlay preferences.");
  return new AppearanceSettings{size=int.Parse(m.Groups[1].Value),position=m.Groups[2].Value,meter=m.Groups[3].Value=="1",sound=m.Groups[4].Value=="1"};
 }
 static string AppearanceWire(AppearanceSettings s){return "PCA1;"+s.size+";"+s.position+";"+(s.meter?"1":"0")+";"+(s.sound?"1":"0");}
 static void LoadAppearance(){
  try{var path=AppearancePath();if(!File.Exists(path)||new FileInfo(path).Length>4096)return;
   var s=json.Deserialize<AppearanceSettings>(File.ReadAllText(path));appearance=ParseAppearance(AppearanceWire(s));
  }catch{appearance=new AppearanceSettings();}
 }
 static void ReadGameAppearance(string path){
  if(new FileInfo(path).Length>4*1024*1024)throw new IOException("PadChat settings file is too large.");
  string content;
  using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
  using(var reader=new StreamReader(stream,Encoding.UTF8,true))content=reader.ReadToEnd();
  var matches=Regex.Matches(content,@"\[""appearanceWire""\]\s*=\s*""([^""\r\n]{1,100})""");
  if(matches.Count==0)return;
  if(matches.Count!=1)throw new ArgumentException("Ambiguous overlay preferences.");
  var next=ParseAppearance(matches[0].Groups[1].Value);
  if(AppearanceWire(next)==AppearanceWire(appearance))return;
  AtomicVoiceFile(AppearancePath(),json.Serialize(next));appearance=next;
 }
 static bool VoiceSettingsAlreadyApplied(GameVoiceSettings s){
  if(!gameSettingsManaged||!voiceShortcutConfigured||voiceShortcut==null||voiceShortcut.kind!=s.shortcut.kind||voiceShortcut.key!=s.shortcut.key||voiceShortcut.modifiers!=s.shortcut.modifiers||
   voiceEnabled!=s.enabled||voiceReview!=s.review||voicePad!=s.pad||voicePadMod!=s.padMod||(voiceVocabulary??"")!=(s.vocabulary??""))return false;
  try{var path=GameSettingsPath();if(!File.Exists(path)||new FileInfo(path).Length>8192)return false;
   var old=json.Deserialize<GameVoiceSettings>(File.ReadAllText(path));
   return old!=null&&old.shortcut!=null&&old.shortcut.kind==s.shortcut.kind&&old.shortcut.key==s.shortcut.key&&old.shortcut.modifiers==s.shortcut.modifiers&&
    old.microphone==s.microphone&&old.startup==s.startup&&old.pad==s.pad&&old.padMod==s.padMod&&old.enabled==s.enabled&&old.review==s.review&&(old.vocabulary??"")==(s.vocabulary??"");
  }catch{return false;}
 }
 static void ApplyOverlayFont(){
  if(badge==null||badge.IsDisposed||Math.Abs(badge.label.Font.SizeInPoints-appearance.size)<.01)return;
  var previous=badge.label.Font;badge.label.Font=new Font("Segoe UI",appearance.size);previous.Dispose();
 }
 static void CaptureReadySound(){
  if(readyCueSession==sid)return;readyCueSession=sid;
  if(appearance.sound)try{readySoundAction();}catch{} // Sound failure cannot interrupt speech.
 }
 static string recordingCommand,recordingLabel;
 static string RecordingDestination(){return microphoneTest?"Microphone test - nothing sent":!string.IsNullOrEmpty(recordingLabel)?"To: "+recordingLabel:"To: last selected channel (checked before send)";}
}
