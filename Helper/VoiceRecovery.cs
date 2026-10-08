using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
partial class VoicePTT {
 static string voiceVocabulary="";
 static bool micRecoveryPending,controllerPreviouslySeen;
 static bool ValidVocabulary(string value) {
  if(value==null)value="";
  if(Encoding.UTF8.GetByteCount(value)>600||Regex.IsMatch(value,@"[\p{Cc}/;|""\\]"))return false;
  int count=0;
  foreach(string part in value.Split(',')) {
   string term=part.Trim();if(term.Length==0)continue;
   if(++count>32||Encoding.UTF8.GetByteCount(term)>60||!Regex.IsMatch(term,@"\A[\p{L}\p{N} .'-]+\z"))return false;
  }
  return true;
 }
 static void WriteVocabulary() {
  if(worker!=null&&!worker.HasExited){worker.StandardInput.WriteLine(json.Serialize(new{cmd="vocabulary",text=voiceVocabulary}));worker.StandardInput.Flush();}
 }
 static void RecoverMicrophone() {
  // One fresh backend after a physical recording fails. It re-enumerates inputs
  // but never reopens a microphone or resumes speech without another PTT press.
  StopWorker();ready=false;workerRetryAt=Now+1500;micRecoveryPending=true;
  heldAt=0;latched=true;keyboardHeld=false;pressSource="";
  WriteHealth("microphone-disconnected");
  Status("Microphone interrupted; nothing sent.\nReconnecting the SAME saved input. Release PTT, reconnect the headset, then press again.\nIf it remains missing, use /padchat voice.",8000);
 }
 static bool ReconnectCancels(string previous,string current,string recordingSource,string state){return previous!=current&&recordingSource=="controller"&&state!="idle";}
 static bool ReconnectMustRelease(bool held){return held;}
 static void ControllerRecoveryNotice(string previous,string current,bool chord) {
  if(previous==current||!IsGame(GetForegroundWindow())||phase!="idle"||pressSource=="keyboard")return;
  if(current=="none"||current=="ambiguous")Status(current=="ambiguous"?"Multiple controllers detected. Use one controller for voice.":"Controller disconnected. Keyboard PTT remains available.",3500);
  else if(controllerPreviouslySeen||previous!="none")Status("Controller reconnected. "+(chord?"Release PTT first, then press again.":"Press PTT to start a new message."),3500);
  if(current!="none"&&current!="ambiguous")controllerPreviouslySeen=true;
 }
 static void TestVoiceRecovery() {
  if(!ValidVocabulary("Thunder Bluff, Shadow Bolt, Friendly-Realm")||!ValidVocabulary("")||ValidVocabulary("/invite Friend")||ValidVocabulary("a\nname")||ValidVocabulary(new string('a',601)))throw new Exception("Vocabulary bounds failed");
  if(!ReconnectCancels("SDL:1","none","controller","processing")||ReconnectCancels("SDL:1","none","keyboard","recording")||ReconnectCancels("SDL:1","SDL:1","controller","review")||!ReconnectMustRelease(true)||ReconnectMustRelease(false))throw new Exception("Reconnect isolation failed");
  var parsed=ParseGameSettings("PCV3;keyboard;119;0;Test%20Mic;1;KEEP;SHARE;NONE;1;Thunder%20Bluff%2C%20Friend-Realm");
  if(parsed.vocabulary!="Thunder Bluff, Friend-Realm"||!parsed.review||ParseGameSettings("PCV2;keyboard;119;0;;1;KEEP;SHARE;NONE;0").vocabulary!="")throw new Exception("PCV3 migration failed");
  foreach(string hints in new[]{"bad%0Aname","%2Finvite%20Friend","bad%7Ename","%FF",Uri.EscapeDataString(new string('é',31)),Uri.EscapeDataString(string.Join(",",new string[33]).Replace(",","a,")+"a")}) {
   bool refused=false;try{ParseGameSettings("PCV3;keyboard;119;0;;1;KEEP;SHARE;NONE;0;"+hints);}catch(ArgumentException){refused=true;}if(!refused)throw new Exception("Unsafe PCV3 hints accepted: "+hints);
  }
  Console.WriteLine("PASS: vocabulary bounds, controller reconnect cancellation and fresh-press policy.");
 }
}
