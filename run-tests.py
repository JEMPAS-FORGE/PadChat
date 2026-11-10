from pathlib import Path
import os,subprocess,json,sys
r=Path(__file__).resolve().parent
env=dict(os.environ,PYTHONUTF8='1')
names=['test-padchat.py','test-runtime.py','test-options.py','test-invites.py','test-voice-options.py','test-voice-setup.py','test-speech-recovery.py','test-support-setup.py','test-direct-addon.py','test-direct-delivery.py','test-input-recovery.py','test-audit-lua.py','test-audit-native.py','test-audit-installer.py','test-worker-protocol.py','test-forever-launch.py','test-unused-default.py','test-fresh-keyboard-policy.py','test-options-experience.py','test-native-experience.py','test-experience.py']
results=[]
for name in names:
 p=subprocess.run([sys.executable,str(r/'Tests'/name)],env=env,capture_output=True,text=True,timeout=150)
 results.append({'name':name,'passed':p.returncode==0,'output':p.stdout,'error':p.stderr})
 (r/'SOURCE-TEST-RESULTS.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
 print(name, 'PASS' if not p.returncode else 'FAIL: '+(p.stderr or p.stdout)[-350:],flush=True)
print('Passed',sum(x['passed'] for x in results),'/',len(results))
sys.exit(0 if all(x['passed'] for x in results) else 1)
