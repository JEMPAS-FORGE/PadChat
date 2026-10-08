using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

partial class VoicePTT {
 static int probeStage;static long probeDeadline;
 static string probeClipboard,probeOwned,probeToken,pendingTestWords="";
 static bool probeResult,microphoneTest,reviewWaitingRelease;
 static bool ParseVoiceProbe(string value,out string token,out bool test) {
  token=null;test=false;
  var m=Regex.Match(value??"",@"\APCVQ1;([0-9.]{1,40});(NORMAL|TEST)\z");
  if(!m.Success)return false;token=m.Groups[1].Value;test=m.Groups[2].Value=="TEST";return true;
 }
 static string VoiceStatusRecord(string token,string state,int micLevel,string words) {
  return "PCVS1;"+token+";"+state+";"+Math.Max(0,Math.Min(100,micLevel))+";"+Uri.EscapeDataString(Clean(words,300));
 }
 static void RestoreProbeClipboard() {
  if(probeOwned!=null&&Clip()==probeOwned) {
   if(string.IsNullOrEmpty(probeClipboard))Clipboard.Clear();else Clipboard.SetText(probeClipboard);
  }
  probeOwned=null;
 }
 static void BeginVoiceProbe(bool result) {
  probeResult=result;probeClipboard=Clip();probeOwned=null;probeStage=1;
  phase="probe";Bridge(0x71);probeDeadline=Now+140;
 }
 static void AdvanceVoiceProbe(bool held) {
  if(probeStage==0||Now<probeDeadline)return;
  if(!SameGame()){probeStage=0;RestoreProbeClipboard();phase="idle";return;}
  if(probeStage==1){Keys(0x11,0x43);probeStage=2;probeDeadline=Now+120;return;}
  if(probeStage==2) {
   string value=Clip();bool test;string token;
   if(!ParseVoiceProbe(value,out token,out test)) {
    probeStage=0;phase="idle";Status("Voice check unavailable. Close other text boxes and update both PadChat parts.",5000);return;
   }
   probeOwned=value;probeToken=token;microphoneTest=test;
   if(probeResult&&!test){RestoreProbeClipboard();probeStage=0;phase="idle";Bridge(0x7A);Status("Microphone test finished; nothing sent.",4000);return;}
   string state=probeResult?"test":!voiceEnabled?"disabled":ready?"ready":"loading";
   string record=VoiceStatusRecord(token,state,level,probeResult?pendingTestWords:"");
   Clipboard.SetText(record);probeOwned=record;Keys(0x11,0x56);
   probeStage=3;probeDeadline=Now+140;return;
  }
  if(probeStage==3) {
   probeStage=0;RestoreProbeClipboard();Bridge(0x7A);
   if(probeResult){phase="idle";Status("Microphone test — nothing sent\nPeak/last level: "+level+"%\n"+Clean(pendingTestWords,300),6500);pendingTestWords="";return;}
   if(!voiceEnabled||!ready||!held){phase="idle";if(!ready)Status("Companion connected; speech model is loading. Try again shortly.",3500);return;}
   BeginRecording();
  }
 }
 static bool RequiresReview(bool test,bool enabled){return !test&&enabled;}
 static void TestVoiceDiagnostics() {
  string token;bool test;
  if(!ParseVoiceProbe("PCVQ1;12.34;TEST",out token,out test)||!test||token!="12.34")throw new Exception("Mic test query failed");
  if(!ParseVoiceProbe("PCVQ1;12;NORMAL",out token,out test)||test)throw new Exception("Normal query failed");
  foreach(string value in new[]{"/s hello","PCVQ1;12;SEND","PCVQ1;12;TEST\n","PCVQ1;bad;TEST"})
   if(ParseVoiceProbe(value,out token,out test))throw new Exception("Unsafe status query accepted");
  if(!VoiceStatusRecord("1","test",180,"a\n|b").StartsWith("PCVS1;1;test;100;")||RequiresReview(true,true)||!RequiresReview(false,true)||RequiresReview(false,false))throw new Exception("Diagnostics/review isolation failed");
  Console.WriteLine("PASS: bounded status protocol, microphone-test delivery isolation and optional review decisions.");
 }
}
