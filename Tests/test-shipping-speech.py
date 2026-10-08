"""Run manually with a matching extracted App/Backend folder; no mic is opened."""
from pathlib import Path
import json,os,re,subprocess,sys
backend=Path(sys.argv[1]).resolve()/'voice-backend.exe'
root=Path(__file__).resolve().parent
env=dict(os.environ,HF_HUB_OFFLINE='1',PYTHONHOME='',PYTHONPATH='',PATH=os.environ['SYSTEMROOT']+'\\System32')
def run(*args):
 p=subprocess.run([str(backend),*map(str,args)],capture_output=True,text=True,env=env,check=True,timeout=90)
 return json.loads(p.stdout)
assert run('--check-vocabulary','Friend-Realm')['terms']==['Friend-Realm']
expected=re.findall('[a-z]+','Hello everyone can you invite me to the party I need to finish this quest'.lower())
for hints in ('','Friendly-Realm','Thunder Bluff, Wailing Caverns','Friendly-Realm, Friendly Two, Friendly Three, Friendly Four'):
 result=run('--self-test',root/'ordinary-speech-test.wav',hints)
 assert re.findall('[a-z]+',result['text'].lower())==expected,result
result=run('--self-test',root/'wow-terms-test.wav','Thunder Bluff, Wailing Caverns')
assert 'thunder bluff' in result['text'].lower() and len(result['text'].split())<=18,result
print('Passed frozen engine prompt and ordinary-speech content regressions. Proper names may still be misheard; no real microphone tested.')
