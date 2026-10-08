from pathlib import Path
import importlib.util,sys,threading,time,types,numpy as np
root=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(root/'Helper'))
from speech_vocabulary import custom_terms,recognition_prompt
assert custom_terms('Friend-Realm, friend-realm, Thunder Bluff')==['Friend-Realm','Thunder Bluff']
assert len(recognition_prompt(','.join(['longword'+str(x) for x in range(32)])).encode('utf-8'))<=125
assert 'Wailing Caverns' in recognition_prompt() and recognition_prompt('Custom-Realm').startswith('World of Warcraft. Custom-Realm')
for bad in ('/invite Friend','x~name','x\nname','é'*31,','.join(['a']*33),'a'*601):
 try:custom_terms(bad)
 except ValueError:pass
 else:raise AssertionError(bad)
# Import the actual worker with a model stub; no downloads or microphone needed.
fw=types.ModuleType('faster_whisper');fw.WhisperModel=lambda *a,**k:object()
saved=sys.modules.get('faster_whisper');sys.modules['faster_whisper']=fw
spec=importlib.util.spec_from_file_location('candidate_worker',root/'Helper/voice_worker.py')
w=importlib.util.module_from_spec(spec);spec.loader.exec_module(w)
if saved is not None:sys.modules['faster_whisper']=saved
else:sys.modules.pop('faster_whisper',None)
events=[];w.emit=lambda kind,**kw:events.append((kind,kw))
entered=threading.Event();release=threading.Event();prompts=[]
class Model:
 def transcribe(self,a,**kw):
  prompts.append(kw['initial_prompt']);entered.set();assert release.wait(3)
  return [types.SimpleNamespace(text='ordinary speech',no_speech_prob=0,avg_logprob=0)],None
jobs=w.RecognitionJobs(Model());jobs.vocabulary='Friend-Realm';jobs.submit('result',7,np.ones(8000,dtype=np.float32)*.1)
assert entered.wait(3);jobs.vocabulary='Different-Realm';jobs.cancel();release.set()
for _ in range(100):
 if not jobs.running:break
 time.sleep(.01)
assert not events and 'Friend-Realm' in prompts[0] and 'Different-Realm' not in prompts[0];jobs.close()
# Drive the real main loop with fake PortAudio failure, asserting close failure
# and callback stalls cancel inference and never submit a final message.
import io
class Jobs:
 def __init__(self,*a):self.condition=threading.Condition();self.running=False;self.pending=None;self.submitted=[];self.cancelled=0;self.vocabulary='';instances.append(self)
 def submit(self,kind,*a):self.submitted.append(kind);return True
 def cancel(self):self.cancelled+=1
 def close(self):pass
class Stream:
 def __init__(self,**kw):self.closed=False
 def start(self):pass
 def stop(self):raise RuntimeError('device removed')
 def close(self):self.closed=True;closed.append(True)
w.RecognitionJobs=Jobs;w.load_model=lambda *a,**k:object();w.input_devices=lambda sd:[{'index':2,'name':'Saved','hostapi':'MME'}];w.read_selection=lambda:{'name':'Saved','hostapi':'MME'};w.sd.InputStream=Stream
instances=[];closed=[];events.clear();oldstdin,oldargv=sys.stdin,sys.argv
try:
 sys.argv=['worker'];sys.stdin=io.StringIO('{"cmd":"start","id":42}\n{"cmd":"stop","id":42}\n{"cmd":"quit"}\n');w.main()
 assert 'result' not in instances[-1].submitted and instances[-1].cancelled>=2 and closed
 assert any(k=='error' and v.get('microphone') for k,v in events) and not any(k=='processing' for k,v in events)
 # Clock advances between loop iterations; start then no frames triggers stall.
 clock=[0];original_clock=w.time.monotonic
 w.time.monotonic=lambda: (clock.__setitem__(0,clock[0]+2) or clock[0])
 events.clear();sys.stdin=io.StringIO('{"cmd":"start","id":43}\n{"cmd":"quit"}\n');w.main()
 assert any(k=='error' and 'stopped delivering' in v.get('message','') for k,v in events)
 assert 'result' not in instances[-1].submitted
finally:
 sys.stdin,sys.argv=oldstdin,oldargv;w.time.monotonic=original_clock
print('Passed bounded vocabulary, actual decoder prompt/snapshot, cancelled in-flight results, PortAudio close failure and capture-stall non-delivery.')
