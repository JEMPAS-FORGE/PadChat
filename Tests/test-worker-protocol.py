"""Exercise actual shipping/candidate queue readers; no model or microphone."""
from pathlib import Path
import json,os,subprocess,tempfile
root=Path(__file__).resolve().parent.parent
csc=Path(os.environ['WINDIR'])/'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
harness=r'''
using System;using System.Collections.Generic;using System.Threading;
partial class VoicePTT {
 public static int AuditWorkerMessages(bool expectFixed) {
  Interlocked.Exchange(ref workerGeneration,17);Interlocked.Exchange(ref microphoneGeneration,29);
  string[] malformed={"not JSON","null","[]","{}","{\"type\":null}","{\"type\":3}",
   "{\"type\":\"result\",\"id\":7,\"text\":null}","{\"type\":\"result\",\"id\":7,\"text\":{}}",
   "{\"type\":\"level\",\"id\":7,\"level\":\"bad\"}","{\"type\":\"level\",\"id\":7,\"level\":101}",
   "{\"type\":\"error\",\"id\":7,\"message\":null}","{\"type\":\"result\",\"id\":1.5,\"text\":\"bad\"}",
   "{\"type\":\"result\",\"id\":2147483648,\"text\":\"bad\"}","{\"type\":\"other\"}",new string('x',65537)};
  int disrupted=0,readerExceptions=0,invalidAccepted=0;
  foreach(string bad in malformed){
   VoiceMessage discard;while(messages.TryDequeue(out discard)){}
   phase="processing";sid=7;released=true;QueueWorkerOutput(bad,17);
   QueueWorkerOutput("{\"type\":\"result\",\"id\":7,\"text\":\"valid result\"}",17);
   bool read=false;Dictionary<string,object> item=null;
   try{read=TryReadVoiceMessage(out item);}catch{readerExceptions++;}
   bool good=read&&item["type"] as string=="result"&&item.ContainsKey("text")&&item["text"] as string=="valid result";
   if(read&&!good)invalidAccepted++;
   if(!good)disrupted++;
   if(expectFixed&&(!good||phase!="processing"||sid!=7||!released))throw new Exception("Malformed worker output interrupted a valid queued result");
  }
  if(!expectFixed&&disrupted==0)throw new Exception("Shipping bug did not reproduce");
  VoiceMessage stale;while(messages.TryDequeue(out stale)){}
  if(expectFixed){
   QueueWorkerOutput("{\"type\":\"result\",\"id\":7,\"text\":\"obsolete\"}",16);
   QueueWorkerOutput("{\"type\":\"microphones\",\"devices\":[]}",17);
   QueueMicrophoneOutput("{\"type\":\"microphones\",\"devices\":[]}",28);
   foreach(string bad in new[]{"{}","{\"type\":\"microphones\",\"devices\":null}","{\"type\":\"microphones\",\"devices\":[{}]}","{\"type\":\"microphones\",\"devices\":[],\"selected\":[]}"})QueueMicrophoneOutput(bad,29);
   QueueMicrophoneOutput("{\"type\":\"microphones\",\"devices\":[{\"name\":\"Test mic\",\"hostapi\":\"MME\"}],\"selected\":null}",29);
   QueueWorkerOutput("{\"type\":\"ready\"}",17);
   QueueWorkerOutput("{\"type\":\"level\",\"id\":7,\"level\":0}",17);
   QueueWorkerOutput("{\"type\":\"level\",\"id\":7,\"level\":100}",17);
   QueueWorkerOutput("{\"type\":\"partial\",\"id\":7,\"text\":\"\"}",17);
   QueueWorkerOutput("{\"type\":\"error\",\"id\":7,\"message\":\"disconnect\",\"microphone\":true}",17);
   foreach(string kind in new[]{"microphones","ready","level","level","partial","error"}){
    Dictionary<string,object> item;if(!TryReadVoiceMessage(out item)||(string)item["type"]!=kind)throw new Exception("Valid/current output lost: "+kind);
   }
   Dictionary<string,object> extra;if(TryReadVoiceMessage(out extra))throw new Exception("Unexpected queued output");
  }
  Console.WriteLine((expectFixed?"FIXED":"SHIPPING")+": "+readerExceptions+" queue-reader exceptions, "+invalidAccepted+" malformed messages accepted, "+(malformed.Length-disrupted)+"/"+malformed.Length+" next valid results returned cleanly.");return 0;
 }
}
class ProtocolAudit {static int Main(string[] args){try{return VoicePTT.AuditWorkerMessages(args.Length>0&&args[0]=="fixed");}catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}}}
'''
results=[]
with tempfile.TemporaryDirectory(prefix='Protocol-',dir=root/'Tests') as temp:
    tmp=Path(temp);fixture=tmp/'Audit.cs';fixture.write_text(harness,encoding='utf-8')
    for label in ('shipping','fixed'):
        sources=sorted((root/'Helper').glob('*.cs'))
        if label=='shipping':
            sources=[p for p in sources if p.name!='VoiceWatchdog.cs']+[root/'Tests/Fixtures/VoiceWatchdog-preview8.cs']
        exe=tmp/(label+'.exe')
        args=[str(csc),'/nologo','/target:exe','/platform:x64','/codepage:65001','/main:ProtocolAudit','/out:'+str(exe),
              '/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:Microsoft.CSharp.dll',
              str(root/'Packaging/Framework.cs'),*map(str,sources),str(fixture)]
        build=subprocess.run(args,capture_output=True,text=True);assert build.returncode==0,build.stdout+build.stderr
        run=subprocess.run([str(exe),label],capture_output=True,text=True,timeout=10)
        assert run.returncode==0,run.stdout+run.stderr
        results.append({'source':label,'passed':True,'output':run.stdout.strip()});print(run.stdout.strip())
(root/'WORKER-PROTOCOL-TEST.json').write_text(json.dumps({'passed':True,'actualProductionQueueReader':True,'results':results,'noGameOrAudioUse':True},indent=2)+'\n',encoding='utf-8')
