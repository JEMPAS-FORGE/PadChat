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
 class VoiceMessage {public long generation;public string data;}
 static readonly SpeechDeadlines speechDeadlines=new SpeechDeadlines();
 static long workerGeneration;
 static long CurrentWorkerGeneration(){return Interlocked.Read(ref workerGeneration);}
 static bool CurrentWorkerOutput(long received,long current,string kind){
  return kind=="microphones"?received==0:received>0&&received==current;
 }
 static void QueueWorkerOutput(string value,long generation){if(value!=null)messages.Enqueue(new VoiceMessage{generation=generation,data=value});}
 static void QueueMicrophoneOutput(string value){QueueWorkerOutput(value,0);}
 static bool TryReadVoiceMessage(out Dictionary<string,object> data){
  VoiceMessage message;data=null;
  while(messages.TryDequeue(out message)){
   if(message.generation!=0&&message.generation!=CurrentWorkerGeneration())continue;
   var decoded=json.Deserialize<Dictionary<string,object>>(message.data);
   if(!CurrentWorkerOutput(message.generation,CurrentWorkerGeneration(),(string)decoded["type"]))continue;
   data=decoded;return true;
  }
  return false;
 }
 static void InvalidateSpeechSession(){
  // Invalidate first, even if clipboard cleanup or a broken process pipe throws.
  speechDeadlines.CancelProcessing();sid++;phase="idle";stage=0;released=false;
  probeStage=0;pendingTestWords="";text=preview=line=inviteName="";
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
   QueueMicrophoneOutput("{\"type\":\"microphones\"}");
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
