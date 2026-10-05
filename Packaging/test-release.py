"""Release smoke tests use the shipping binaries, not the developer Python runtime."""
from pathlib import Path
import hashlib
import json
import os
import subprocess
import sys
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parent
DIST = ROOT.parent/'releases'/'0.2.0-preview.1'
WORK = ROOT/'release-tests'
WORK.mkdir(exist_ok=True)
def run(args, **kw):
    result=subprocess.run([str(a) for a in args],stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,timeout=600,**kw)
    if result.returncode:raise AssertionError(str(args)+ '\n'+result.stdout+result.stderr)
    return result.stdout

def main():
    data=WORK/'Clean Profile — no Python';data.mkdir(exist_ok=True)
    env=os.environ.copy()
    env.update(PATH=str(Path(os.environ['SYSTEMROOT'])/'System32'),PADCHAT_DATA_DIR=str(data),
               HF_HUB_OFFLINE='1',PYTHONPATH='',PYTHONHOME='')
    backend=ROOT/'payload'/'App'/'Backend'/'voice-backend.exe'
    result=json.loads(run([backend,'--list-inputs'],cwd=data,env=env))
    assert result['selected'] is None, 'Personal microphone selection leaked into package'
    assert result['type']=='microphones'
    transcript=json.loads(run([backend,'--self-test',ROOT.parent.parent/'controller-keyboard'/'voice-test.wav'],cwd=data,env=env))
    assert transcript['type']=='test' and 'party' in transcript['text'].lower(),transcript
    # Both speech models initialise and quit gracefully without opening a mic.
    p=subprocess.Popen([str(backend)],cwd=data,env=env,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
    stdout,stderr=p.communicate('{"cmd":"quit"}\n',timeout=90)
    assert p.returncode==0 and any(json.loads(line).get('type')=='ready' for line in stdout.splitlines()),stderr
    assert not (data/'voice-microphone.json').exists()
    run([ROOT/'payload'/'App'/'PadChatVoice.exe','--self-test'],cwd=data,env=env)
    # Fresh user folder, mixed spaces/Unicode, identical actual payload.
    stage=WORK/('Installer '+next(tempfile._get_candidate_names()))
    output=run([DIST/'PadChat-Setup.exe','--self-test',stage,'embedded','embedded'],cwd=data,env=env)
    print(output.strip())
    # The included source can reproduce all public addon mock tests.
    for name in ('test-padchat.py','test-runtime.py','test-invites.py'):
        text=(ROOT.parent/name).read_text()
        text=text.replace("root/'PadChat/Model.lua'", "root/'packaging/payload/PadChat/Model.lua'")
        text=text.replace("root/'PadChat'", "root/'packaging/payload/PadChat'")
        code=compile(text,str(ROOT.parent/name),'exec')
        namespace={'__file__':str(ROOT.parent/name),'__name__':'__main__'}
        exec(code,namespace)
        if name=='test-runtime.py':
            l=namespace['l']
            l.execute('''
            P.conflictingAddon=nil;combat=false
            C_AddOns={IsAddOnLoaded=function(name) return name=='ControllerKeyboard' end}
            event=P.openBindingOwner.scripts.OnEvent
            event(P.openBindingOwner,'PLAYER_LOGIN')
            assert(P.conflictingAddon)
            local before=P.open
            SlashCmdList.PADCHAT('');assert(P.open==before)
            ''')
    # Explicit payload allowlist excludes personal state, clips and recordings.
    manifest=json.loads((ROOT/'payload'/'manifest.json').read_text())
    banned=('voice-microphone.json','voice-health.json','voice-routing-error.json','.wav','.pyc','__pycache__')
    for name,expected in manifest.items():
        # Compiled modules inside PyInstaller are intentional; no loose user files.
        assert not any(name.endswith(value) for value in banned),name
        assert hashlib.file_digest((ROOT/'payload'/name).open('rb'),'sha256').hexdigest()==expected,name
    assert not any('/av.libs/' in name or name.startswith('App/Backend/_internal/av/') for name in manifest)
    assert not any(name.endswith('cudnn64_9.dll') for name in manifest)
    for path in DIST.glob('*.zip'):
        with zipfile.ZipFile(path) as zip:assert zip.testzip() is None
    report={'result':'passed','platform':sys.platform,'pythonRequiredForEndUser':False,
            'localModelsInitialise':True,'syntheticPhraseRecognised':True,
            'freshMicrophoneSelection':True,'installerTests':output.strip(),
            'osIntegration':'startup and registration simulated in isolated test folders',
            'limitations':['No second physical PC/clean Windows image tested','No real microphone or public chat used in release smoke tests']}
    (DIST/'TEST-REPORT.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('PASS: packaged speech without Python/PATH, fresh mic settings, both models, helper routing, public addon, legacy conflict guard, package hashes and ZIP contents.')

if __name__=='__main__':main()
