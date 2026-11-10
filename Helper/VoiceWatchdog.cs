using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;

partial class VoicePTT {
 // Deadlines guard a live-but-unresponsive process; HasExited alone cannot.
 // No audio, transcript, clipboard, recipient or timing history is persisted.
 class SpeechDeadlines {
  public const int Limit=120000;
  long loadingEnd,processingEnd;
  public void WorkerStarting(long now){loadingEnd=now+Limit;processingEnd=0;}
  public void WorkerReady(){loadingEnd=0;}
  public void Processing(long now){processingEnd=now+Limit;}
  public void CancelProcessing(){processingEnd=0;}
  public void WorkerStopped(){loadingEnd=processingEnd=0;}
  public string Expired(long now,string state){
   if(loadingEnd!=0&&now>=loadingEnd)return "loading";
   if(state=="processing"&&processingEnd!=0&&now>=processingEnd)return "processing";
   return null;
  }
 }
 class VoiceMessage {public long generation,microphoneGeneration;public string data;}
 static readonly SpeechDeadlines speechDeadlines=new SpeechDeadlines();
 static long workerGeneration;
 static long CurrentWorkerGeneration(){return Interlocked.Read(ref workerGeneration);}
 static bool CurrentWorkerOutput(long received,long current,string kind){
  return kind=="microphones"?received==0:received>0&&received==current;
 }
 static void QueueWorkerOutput(string value,long generation){if(value!=null)messages.Enqueue(new VoiceMessage{generation=generation,data=value});}
 static void QueueMicrophoneOutput(string value){QueueMicrophoneOutput(value,Interlocked.Read(ref microphoneGeneration));}
 static void QueueMicrophoneOutput(string value,long generation){if(value!=null)messages.Enqueue(new VoiceMessage{generation=0,microphoneGeneration=generation,data=value});}
 static bool MessageInteger(Dictionary<string,object> data,string key,int minimum,int maximum){
  object value;if(!data.TryGetValue(key,out value)||!(value is int||value is long))return false;
  long number=Convert.ToInt64(value);return number>=minimum&&number<=maximum;
 }
 static bool MessageString(Dictionary<string,object> data,string key,int maximum){
  object value;return data.TryGetValue(key,out value)&&value is string&&((string)value).Length<=maximum;
 }
 static bool MicrophoneMessageChoice(object value){
  var choice=value as Dictionary<string,object>;
  return choice!=null&&MessageString(choice,"name",512)&&MessageString(choice,"hostapi",128);
 }
 static bool ValidVoiceMessage(Dictionary<string,object> data,string kind){
  if(kind=="ready")return true;
  if(kind=="microphones"){
   object value;if(!data.TryGetValue("devices",out value)||value is string||!(value is System.Collections.IEnumerable))return false;
   int count=0;foreach(object device in (System.Collections.IEnumerable)value)if(++count>512||!MicrophoneMessageChoice(device))return false;
   if(data.TryGetValue("selected",out value)&&value!=null&&!MicrophoneMessageChoice(value))return false;
   return !data.ContainsKey("error")||MessageString(data,"error",32000);
  }
  if(!MessageInteger(data,"id",0,int.MaxValue))return false;
  if(kind=="listening")return true;
  if(kind=="level")return MessageInteger(data,"level",0,100);
  if(kind=="result"||kind=="partial")return MessageString(data,"text",32000);
  if(kind=="error")return MessageString(data,"message",32000)&&(!data.ContainsKey("microphone")||data["microphone"] is bool);
  return false; // Unrecognised informational/status lines never drive state.
 }
 static bool TryDecodeVoiceMessage(VoiceMessage message,out Dictionary<string,object> data){
  data=null;
  if(string.IsNullOrWhiteSpace(message.data)||message.data.Length>(message.generation==0?524288:65536))return false;
  try{
   var decoded=json.Deserialize<Dictionary<string,object>>(message.data);object type;
   if(decoded==null||!decoded.TryGetValue("type",out type)||!(type is string))return false;
   string kind=(string)type;
   if(!CurrentWorkerOutput(message.generation,CurrentWorkerGeneration(),kind)||!ValidVoiceMessage(decoded,kind))return false;
   data=decoded;return true;
  }catch(ArgumentException){return false;}catch(InvalidOperationException){return false;}
 }
 static bool TryReadVoiceMessage(out Dictionary<string,object> data){
  VoiceMessage message;data=null;
  while(messages.TryDequeue(out message)){
   if(message.generation!=0&&message.generation!=CurrentWorkerGeneration())continue;
   if(message.generation==0&&message.microphoneGeneration!=Interlocked.Read(ref microphoneGeneration))continue;
   // Bad backend output must not escape into Tick's cancellation path, discard
   // another valid queued result or change microphone/settings ownership.
   if(TryDecodeVoiceMessage(message,out data))return true;
  }
  return false;
 }
 static void InvalidateSpeechSession(){
  // Invalidate first, even if clipboard cleanup or a broken process pipe throws.
  speechDeadlines.CancelProcessing();sid++;phase="idle";stage=0;released=false;
  probeStage=0;pendingTestWords="";text=preview=line=inviteName="";recognizedForRecovery="";sendAttempted=false;
 }
 static bool CancellableVoicePhase(string state){return state=="recording"||state=="processing"||state=="result"||state=="review";}
 static void EnforceSpeechDeadline(){
  string expired=speechDeadlines.Expired(Now,phase);if(expired==null)return;
  // Run before accepting queued replies: a late old result cannot defeat the
  // timeout or resume a cancelled recording. No automatic recording/send.
  try{Cancel(expired=="loading"?"Speech model loading timed out. Retrying locally; nothing sent.":
   "Speech processing timed out; nothing sent. Restarting locally. Release PTT and try a shorter message.");}
  finally{
   StopWorker();workerFailures++;workerRetryAt=Now+RetryDelay(workerFailures);
   heldAt=0;latched=true;keyboardHeld=false;pressSource="";
   WriteHealth("speech-timeout");
  }
 }
 static void TestSpeechWatchdog(){
  var guard=new SpeechDeadlines();
  guard.WorkerStarting(10);
  if(guard.Expired(120009,"idle")!=null||guard.Expired(120010,"idle")!="loading")throw new Exception("Cold-load deadline boundary failed");
  guard.WorkerReady();if(guard.Expired(900000,"idle")!=null)throw new Exception("Ready worker expired");
  guard.Processing(500);
  if(guard.Expired(120499,"processing")!=null||guard.Expired(120500,"processing")!="processing"
   ||guard.Expired(120500,"review")!=null)throw new Exception("Recognition/review deadline isolation failed");
  guard.CancelProcessing();if(guard.Expired(900000,"processing")!=null)throw new Exception("Cancelled processing expired again");
  guard.WorkerStarting(0);guard.Processing(0);guard.WorkerStopped();
  if(guard.Expired(900000,"processing")!=null)throw new Exception("Stopped worker kept deadline");
  guard.WorkerStarting(800000);if(guard.Expired(800001,"idle")!=null)throw new Exception("Restart reused expired loading deadline");
  foreach(string kind in new[]{"ready","result","partial","level","error"}){
   if(CurrentWorkerOutput(1,2,kind)||CurrentWorkerOutput(0,2,kind)||!CurrentWorkerOutput(2,2,kind))throw new Exception("Obsolete worker output accepted: "+kind);
  }
  if(!CurrentWorkerOutput(0,2,"microphones")||CurrentWorkerOutput(1,2,"microphones"))throw new Exception("Enumeration output source isolation failed");
  foreach(string state in new[]{"recording","processing","result","review"})if(!CancellableVoicePhase(state))throw new Exception("Escape cancel phase missing");
  foreach(string state in new[]{"idle","probe","delivery"})if(CancellableVoicePhase(state))throw new Exception("Escape steals unrelated action");
  long savedGeneration=CurrentWorkerGeneration();int savedSid=sid;
  try{
   Interlocked.Exchange(ref workerGeneration,41);
   QueueWorkerOutput("{not valid JSON}",40); // Obsolete output must be rejected before parsing.
   QueueWorkerOutput("{\"type\":\"ready\"}",40);
   QueueWorkerOutput("{\"type\":\"result\",\"id\":99,\"text\":\"late fixture\"}",40);
   QueueMicrophoneOutput("{\"type\":\"ready\"}");
   QueueMicrophoneOutput("{\"type\":\"microphones\",\"devices\":[]}");
   QueueWorkerOutput("{\"type\":\"ready\"}",41);
   Dictionary<string,object> accepted;
   if(!TryReadVoiceMessage(out accepted)||(string)accepted["type"]!="microphones"
    ||!TryReadVoiceMessage(out accepted)||(string)accepted["type"]!="ready"
    ||TryReadVoiceMessage(out accepted))throw new Exception("Actual queue retained obsolete speech/ready output");
   phase="processing";stage=6;released=true;text="private fixture";preview="draft fixture";
   speechDeadlines.Processing(0);InvalidateSpeechSession();
   if(phase!="idle"||stage!=0||released||sid!=savedSid+1||text!=""||preview!=""
    ||speechDeadlines.Expired(900000,"processing")!=null)throw new Exception("Cancellation did not invalidate state before external IO");
  }finally{VoiceMessage stale;while(messages.TryDequeue(out stale)){}Interlocked.Exchange(ref workerGeneration,savedGeneration);sid=savedSid;}
  Console.WriteLine("PASS: cold-load/final deadlines, cancellation/review isolation, fresh restart deadlines, obsolete worker output and Escape scope.");
 }
 static void TestWorkerShutdown(){
  if(worker!=null)throw new Exception("Shutdown test requires its own isolated child");
  worker=new Process{StartInfo=new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName,"--worker-shutdown-fixture"){
   UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true}};
  try{
   worker.Start();
   if(!worker.StandardOutput.ReadLine().StartsWith("fixture-ready"))throw new Exception("Disposable shutdown fixture did not start");
   using(var witness=Process.GetProcessById(worker.Id)){
    var processHandle=witness.Handle;
    worker.StandardInput.Close(); // A broken/closed control pipe cannot orphan the child.
    StopWorker();
    if(worker!=null||!witness.WaitForExit(2000))throw new Exception("Closed input pipe left speech process running");
   }
  }finally{StopWorker();}
  Console.WriteLine("PASS: production shutdown terminates its owned disposable child after closed input pipe. No microphone/game process used.");
 }
}
