using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Collections.Generic;

partial class VoicePTT {
 // A live hook thread is not proof Windows is still calling its hooks. Compare
 // only the saved PTT key against the drained physical event state. Polling is
 // a fault detector, NEVER permission to capture audio or send a message.
 class InputContinuity {
  long mismatch=-1,retryAfter; string scope="";
  public bool Observe(long now,string currentScope,bool eligible,bool physical,bool observed) {
   if(!eligible||scope!=currentScope||physical==observed){mismatch=-1;scope=currentScope;return false;}
   if(now<retryAfter){mismatch=-1;return false;}
   if(mismatch<0){mismatch=now;return false;}
   if(now-mismatch<200)return false;
   mismatch=-1;retryAfter=now+5000;return true;
  }
 }
 static readonly InputContinuity inputContinuity=new InputContinuity();
 static bool recoveryAwaitRelease;
 static int inputRecoveries,voiceRestarts;
 static void ResetRecoveryState() {
  InvalidateSpeechSession(); // Invalidate before ANY process/clipboard IO.
  keyboardHeld=false;heldAt=0;latched=true;pressSource=recordSource="";
  microphoneTest=false;reviewWaitingRelease=false;recoveryAwaitRelease=true;
 }
 static void ClearRecoveredInput() {
  VoiceInput value;while(inputQueue.TryDequeue(out value))System.Threading.Interlocked.Decrement(ref inputQueued);
  keyboardPhysicalDown=GetAsyncKeyState(voiceShortcut.key)<0;
 }
 static void RepairInputListener() {
  ResetRecoveryState();
  RestoreDirectVoiceClipboard();
  try{RestoreProbeClipboard();}catch{probeOwned=null;}
  try{WriteWorker("cancel");}catch{}
  if(SameGame())try{Bridge(0x74);}catch{}
  // Keep the speech model warm. Only the owned input thread/hooks are replaced.
  StopInputListener();StartInputListener();ClearRecoveredInput();inputRecoveries++;
 }
 static bool RecoveryReleaseGate(bool physical,bool controller) {
  if(!recoveryAwaitRelease)return false;
  keyboardHeld=false;heldAt=0;latched=true;pressSource="";
  if(!physical&&!controller){recoveryAwaitRelease=false;keyboardPhysicalDown=false;latched=false;}
  return true; // Even the releasing tick cannot record, cycle or send.
 }
 static bool MaintainInputContinuity(bool controllerChord) {
  bool physical=GetAsyncKeyState(voiceShortcut.key)<0;
  var window=GetForegroundWindow();var policy=inputPolicy;
  bool eligible=voiceEnabled&&!recoveryAwaitRelease&&!shortcutArmed
   &&(microphoneWindow==null||microphoneWindow.IsDisposed)&&(shortcutWindow==null||shortcutWindow.IsDisposed)
   &&IsGame(window)&&inputThread!=null&&inputThread.IsAlive;
  string scope=window.ToInt64()+":"+(policy==null?0:policy.generation);
  if(inputContinuity.Observe(Now,scope,eligible,physical,keyboardPhysicalDown)) {
   try{RepairInputListener();inputRetryAt=0;WriteHealth(ready?"ready":"loading");
    Status("Voice input refreshed; nothing sent. Release PTT, then press again.",6000);}
   catch(Exception ex){inputRetryAt=Now+5000;WorkerError("Input recovery: "+ex.Message);WriteHealth("input-error");}
  }
  return RecoveryReleaseGate(physical,controllerChord);
 }
 static void RestartVoiceNow() {
  ResetRecoveryState();voiceRestarts++;
  RestoreDirectVoiceClipboard();
  try{RestoreProbeClipboard();}catch{probeOwned=null;}
  if(SameGame())try{Bridge(0x74);}catch{}
  StopMicrophoneEnumeration(true);
  try {
   StopWorker();StopInputListener();StartInputListener();ClearRecoveredInput();inputRetryAt=0;
   workerRetryAt=0;workerFailures=0;
   if(voiceEnabled)StartWorker();
   WriteHealth(voiceEnabled?"loading":"disabled");
   Status(voiceEnabled?"Voice restarting locally; unfinished dictation discarded.\nWait for the model, release PTT, then press again.":"Voice input refreshed. Voice remains disabled in your saved settings.",6500);
  }catch(Exception ex){workerRetryAt=Now+5000;inputRetryAt=Now+5000;WorkerError("Voice restart: "+ex.Message);WriteHealth("restart-error");Status("Voice restart failed. Run PadChat Setup to repair.\n"+ex.Message,8000);}
 }
 static long UnixSeconds(){return (long)(DateTime.UtcNow-new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalSeconds;}
 static bool ValidRecoveryRequest(string wire,long now) {
  var match=Regex.Match(wire??"",@"\APCVR1;([0-9]{10});([1-9][0-9]{0,8})\z");
  long timestamp;return match.Success&&long.TryParse(match.Groups[1].Value,out timestamp)&&timestamp>=now-120&&timestamp<=now+10;
 }
 static string RecoveryRequestDigest(string path,string wire) {
  using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToUpperInvariant()+"\n"+wire))).Replace("-","");
 }
 static string ReadRecoveryRequest(string path) {
  if(new FileInfo(path).Length>4*1024*1024)throw new IOException("PadChat settings file is too large.");
  string contents;using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
  using(var reader=new StreamReader(stream,Encoding.UTF8,true))contents=reader.ReadToEnd();
  var fields=Regex.Matches(contents,@"\[""voiceRecoveryWire""\]\s*=");
  var matches=Regex.Matches(contents,@"\[""voiceRecoveryWire""\]\s*=\s*""(PCVR1;[0-9;]{1,30})""");
  if(fields.Count==0)return null;
  if(fields.Count!=1||matches.Count!=1)throw new IOException("Invalid or duplicate voice restart request.");
  return matches[0].Groups[1].Value;
 }
 static bool ClaimRecoveryRequest(string path,string wire,long now) {
  if(!ValidRecoveryRequest(wire,now))return false;
  string digest=RecoveryRequestDigest(path,wire),receipt=Path.Combine(dataRoot,"voice-recovery-receipts.json");
  var consumed=new Dictionary<string,long>();
  if(File.Exists(receipt)) {
   if(new FileInfo(receipt).Length>16384)throw new IOException("Voice restart receipt is too large.");
   consumed=json.Deserialize<Dictionary<string,long>>(File.ReadAllText(receipt));
   if(consumed==null||consumed.Count>128)throw new IOException("Voice restart receipt is invalid.");
  }
  var expired=new List<string>();foreach(var item in consumed)if(item.Value<now-120)expired.Add(item.Key);
  foreach(string key in expired)consumed.Remove(key);
  if(consumed.ContainsKey(digest))return false;
  if(consumed.Count>=128)throw new IOException("Too many voice restart requests. Wait two minutes.");
  consumed.Add(digest,now);
  // Persist consumption BEFORE recovery. Retain all requests within the TTL,
  // including across account switches and companion restarts. No private paths.
  AtomicVoiceFile(receipt,json.Serialize(consumed));return true;
 }
 static bool ConsumeGameRecovery(string path) {
  string wire=ReadRecoveryRequest(path);
  if(wire==null||!ClaimRecoveryRequest(path,wire,UnixSeconds()))return false;
  RestartVoiceNow();return true;
 }
 static void TestInputRecovery() {
  var guard=new InputContinuity();
  if(guard.Observe(0,"a",true,true,false)||guard.Observe(1,"a",true,true,false)||guard.Observe(200,"a",true,true,false)
   ||!guard.Observe(201,"a",true,true,false))throw new Exception("Missing press debounce failed");
  if(guard.Observe(202,"a",true,true,false)||guard.Observe(5201,"a",true,true,false)||!guard.Observe(5401,"a",true,true,false))throw new Exception("Recovery cooldown failed");
  guard=new InputContinuity();guard.Observe(0,"a",true,false,true);guard.Observe(1,"a",true,false,true);
  if(guard.Observe(201,"b",true,false,true)||guard.Observe(401,"b",false,false,true)||guard.Observe(601,"b",true,false,false))throw new Exception("Focus/configuration/matching reset failed");
  guard.Observe(602,"b",true,false,true);if(!guard.Observe(802,"b",true,false,true))throw new Exception("Missing release detection failed");
  int savedSid=sid;phase="processing";stage=26;released=true;text="obsolete words";microphoneTest=true;
  ResetRecoveryState();
  if(sid!=savedSid+1||phase!="idle"||stage!=0||released||text!=""||microphoneTest||!recoveryAwaitRelease)throw new Exception("Recovery retained a sendable session");
  if(!RecoveryReleaseGate(true,false)||!RecoveryReleaseGate(false,true)||!recoveryAwaitRelease
   ||!RecoveryReleaseGate(false,false)||recoveryAwaitRelease||RecoveryReleaseGate(false,false))throw new Exception("Fresh release/press gate failed");
  long now=1800000000;
  foreach(string invalid in new[]{null,"PCVR1;1800000000;0","PCVR1;1800000000;1\n","PCVR1;1799999879;1","PCVR1;1800000011;1","PCVR1;1800000000;1;/run"})
   if(ValidRecoveryRequest(invalid,now))throw new Exception("Unsafe/stale restart request accepted");
  string root=Path.Combine(Environment.GetEnvironmentVariable("PADCHAT_TEST_ROOT")??Path.GetTempPath(),"PadChat-recovery-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  string receipt=Path.Combine(dataRoot,"voice-recovery-receipts.json");
  if(File.Exists(receipt))throw new Exception("Run recovery tests in an isolated PADCHAT_DATA_DIR");
  try {
   string path=Path.Combine(root,"PadChat.lua"),wire="PCVR1;1800000000;1";
   File.WriteAllText(path,"PadChatDB = { [\"draft\"] = \"private\", [\"voiceRecoveryWire\"] = \""+wire+"\", }");
   if(ReadRecoveryRequest(path)!=wire||!ClaimRecoveryRequest(path,wire,now)||ClaimRecoveryRequest(path,wire,now))throw new Exception("SavedVariables restart consumption/replay failed");
   if(!ClaimRecoveryRequest(path,"PCVR1;1800000000;2",now)||ClaimRecoveryRequest(path,wire,now))throw new Exception("Interleaved request replay allowed");
   if(File.ReadAllText(receipt).Contains(root)||new FileInfo(receipt).Length>16384)throw new Exception("Recovery receipt leaked path");
   File.WriteAllText(path,"{ [\"draft\"] = \""+wire+"\" }");if(ReadRecoveryRequest(path)!=null)throw new Exception("Draft mistaken for restart");
   File.WriteAllText(path,"{ [\"voiceRecoveryWire\"] = \""+wire+"\", [\"voiceRecoveryWire\"] = \""+wire+"\" }");
   bool refused=false;try{ReadRecoveryRequest(path);}catch(IOException){refused=true;}if(!refused)throw new Exception("Duplicate restart accepted");
  }finally{Directory.Delete(root,true);if(File.Exists(receipt))File.Delete(receipt);}
  Console.WriteLine("PASS: stalled press/release detection, debounce/cooldown/focus/config isolation, discarded speech, fresh release gate, bounded in-game restart and durable replay refusal. No audio or game input.");
 }
}
