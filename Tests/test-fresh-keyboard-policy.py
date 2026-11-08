from pathlib import Path
import os,subprocess,tempfile
r=Path(__file__).resolve().parent.parent
with tempfile.TemporaryDirectory(prefix='FreshPolicy-',dir=r/'Tests') as temp:
 d=Path(temp);src=d/'Check.cs';exe=d/'Check.exe'
 src.write_text(r'''
using System;using System.IO;
partial class VoicePTT {
 public static void CheckFreshPolicy(){
  voiceEnabled=true;LoadVoiceShortcut();PublishInputPolicy();
  if(voiceShortcutConfigured||inputPolicy.enabled||voiceShortcut.key!=121)throw new Exception("Fresh companion captured an unchecked default");
  WriteShortcut(ShortcutPath(),new VoiceShortcut{key=119});LoadVoiceShortcut();PublishInputPolicy();
  if(!voiceShortcutConfigured||!inputPolicy.enabled||voiceShortcut.key!=119)throw new Exception("Existing F8 selection lost");
  File.WriteAllText(ShortcutPath(),"bad JSON");LoadVoiceShortcut();PublishInputPolicy();
  if(voiceShortcutConfigured||inputPolicy.enabled)throw new Exception("Corrupt shortcut activated fallback key");
  var saved=new GameVoiceSettings{shortcut=new VoiceShortcut{kind="mouse",key=5},microphone="My mic",startup="KEEP",pad="SHARE",padMod="NONE",enabled=true,vocabulary=""};
  File.WriteAllText(GameSettingsPath(),json.Serialize(saved));LoadGameVoiceSettings();PublishInputPolicy();
  if(!voiceShortcutConfigured||!inputPolicy.enabled||voiceShortcut.key!=5||voiceShortcut.kind!="mouse")throw new Exception("Saved in-game choice not restored");
  WriteShortcut(ShortcutPath(),new VoiceShortcut{key=119});LoadVoiceShortcut();LoadGameVoiceSettings();
  if(voiceShortcut.key!=119)throw new Exception("Valid Windows binding replaced by an older game snapshot");
  Console.WriteLine("PASS: production input policy ignores unchecked/corrupt defaults, preserves saved F8, and restores saved Mouse 4 from game settings. No input/audio injected.");
 }
}
class Check {static void Main(){VoicePTT.CheckFreshPolicy();}}
''',encoding='utf-8')
 args=['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe','/nologo','/codepage:65001','/target:exe','/platform:x64','/main:Check','/out:'+str(exe),'/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:Microsoft.CSharp.dll',str(r/'Packaging/Framework.cs'),*map(str,sorted((r/'Helper').glob('*.cs'))),str(src)]
 build=subprocess.run(args,capture_output=True,text=True);assert build.returncode==0,build.stdout+build.stderr
 profile=d/'profile';profile.mkdir();env=dict(os.environ,PADCHAT_DATA_DIR=str(profile))
 result=subprocess.run([str(exe)],env=env,capture_output=True,text=True,timeout=20)
 assert result.returncode==0,result.stdout+result.stderr
 print(result.stdout)
