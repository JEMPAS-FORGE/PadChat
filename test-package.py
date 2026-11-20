from pathlib import Path

import hashlib

import json

import os

import subprocess

import tempfile

import zipfile
import sys

ROOT=Path(__file__).resolve().parent

PROJECT=ROOT.parent.parent

OUT=ROOT/'dist'

APP=ROOT/'Payload/App'

PROFILE=ROOT/'IsolatedPackageProfile';PROFILE.mkdir(exist_ok=True)

env=dict(os.environ,PADCHAT_DATA_DIR=str(PROFILE),PADCHAT_TEST_ROOT=str(PROFILE),HF_HUB_OFFLINE='1',PYTHONHOME='',PYTHONPATH='',PATH=os.environ['SYSTEMROOT']+'\\System32')

results=[]

def run(name,args,timeout=600):

 p=subprocess.run([str(a) for a in args],capture_output=True,text=True,env=env,cwd=PROFILE,timeout=timeout)

 item={'name':name,'passed':p.returncode==0,'exitCode':p.returncode,'output':p.stdout,'error':p.stderr}

 results.append(item);(ROOT/'PACKAGE-TEST-REPORT.json').write_text(json.dumps(results,indent=2))

 print(name,':',p.returncode,p.stdout.strip(),p.stderr.strip(),flush=True)

 assert p.returncode==0,name

 return p.stdout

assert not (APP/'Backend/_internal/ctranslate2/cudnn64_9.dll').exists(),'CPU-only runtime must exclude cuDNN'

run('shipping companion self-test',[APP/'PadChatVoice.exe','--self-test'])
run('shipping launcher lifecycle policy',[APP/'PadChatLauncher.exe','--self-test'])
run('shipping companion native input thread stall/restart',[APP/'PadChatVoice.exe','--input-thread-test'])

run('shipping installer discovery',[OUT/'PadChat-Setup.exe','--discovery-test'])

stage=ROOT/('InstallerTest-'+next(tempfile._get_candidate_names()))

run('shipping installer full isolated install/update/rollback/uninstall',[OUT/'PadChat-Setup.exe','--self-test',stage,'embedded','embedded'])

inputs=json.loads(run('clean microphone enumeration',[APP/'Backend/voice-backend.exe','--list-inputs']))

assert inputs['selected'] is None and not (PROFILE/'voice-microphone.json').exists()

v=json.loads(run('frozen vocabulary custom hints',[APP/'Backend/voice-backend.exe','--check-vocabulary','Friend-Realm, Thunder Bluff']))

assert v['terms']==['Friend-Realm','Thunder Bluff'] and v['prompt'].startswith('World of Warcraft. Friend-Realm')

import re

def words(text):return re.findall(r"[a-z']+",text.lower())

sample=ROOT/'Tests/ordinary-speech-test.wav'

expected=words('Hello everyone can you invite me to the party I need to finish this quest')

for hints in ('','Friendly-Realm','Thunder Bluff, Wailing Caverns','Friendly-Realm, Friendly Two, Friendly Three, Friendly Four'):

 result=json.loads(run('bundled speech content: '+(hints or 'default'),[APP/'Backend/voice-backend.exe','--self-test',sample,hints]))

 assert words(result['text'])==expected,'Ordinary speech regression: '+result['text']

result=json.loads(run('bundled WoW vocabulary sample',[APP/'Backend/voice-backend.exe','--self-test',ROOT/'Tests/wow-terms-test.wav','Thunder Bluff, Wailing Caverns']))

assert 'thunder bluff' in result['text'].lower() and len(words(result['text']))<=18

# The installer test embeds/extracts the real payload. Compare every original

# byte of that payload, installer-in-bundle and source/addon artifacts.

with zipfile.ZipFile(stage/'shipping-payload.zip') as z:

 for name,digest in json.loads(z.read('manifest.json')).items():

  assert hashlib.sha256(z.read(name)).hexdigest()==digest,name

 for p in (ROOT/'PadChat').iterdir():

  if p.is_file():assert z.read('PadChat/'+p.name)==p.read_bytes(),p.name

 assert z.read('App/PadChatVoice.exe')==(APP/'PadChatVoice.exe').read_bytes()
 assert z.read('App/PadChatLauncher.exe')==(APP/'PadChatLauncher.exe').read_bytes()

with zipfile.ZipFile(OUT/'PadChat-Windows-Voice-0.2.2-preview.17.zip') as z:

 assert hashlib.sha256(z.read('PadChat-Setup.exe')).hexdigest()==hashlib.file_digest((OUT/'PadChat-Setup.exe').open('rb'),'sha256').hexdigest()

with zipfile.ZipFile(OUT/'PadChat-0.2.2-preview.17.zip') as addon,zipfile.ZipFile(OUT/'PadChat-source.zip') as source:

 for name in addon.namelist():assert addon.read(name)==source.read(name),name

results.append({'name':'embedded payload/source/addon/installer bundle consistency','passed':True})

# Exercise the Lua code actually shipped in the addon ZIP, not only its sources.
shipping=ROOT/'ShippingAddonTest';shipping.mkdir(exist_ok=True)
with zipfile.ZipFile(OUT/'PadChat-0.2.2-preview.17.zip') as archive:
 for name in archive.namelist():
  target=shipping/name;assert target.resolve().is_relative_to(shipping.resolve())
  target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(archive.read(name))
lua_env=dict(os.environ,PADCHAT_ADDON_TEST_ROOT=str(shipping/'PadChat'))
check=subprocess.run([sys.executable,str(ROOT/'Tests/test-probe-movement.py')],env=lua_env,capture_output=True,text=True,timeout=60)
results.append({'name':'shipping addon readiness while moving','passed':check.returncode==0,'output':check.stdout,'error':check.stderr})
assert check.returncode==0,check.stdout+check.stderr

(ROOT/'PACKAGE-TEST-REPORT.json').write_text(json.dumps(results,indent=2))

print('Package tests and consistency passed. No live game/profile touched.')

