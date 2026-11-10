from pathlib import Path
import os,re,subprocess,json,hashlib,shutil
here=Path(__file__).resolve().parent.parent;source=here/'Helper'
csc=Path(os.environ['WINDIR'])/'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
refs=['/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:Microsoft.CSharp.dll']
def build(exe,main,paths,gui=False):
 r=subprocess.run([str(csc),'/nologo','/target:'+('winexe' if gui else 'exe'),'/platform:x64','/codepage:65001','/main:'+main,'/out:'+str(exe),*refs,*map(str,paths)],capture_output=True,text=True)
 assert r.returncode==0,r.stdout+r.stderr
def run(exe,*args,env=None):
 r=subprocess.run([str(exe),*args],capture_output=True,text=True,timeout=40,env=env);assert r.returncode==0,r.stdout+r.stderr;return r.stdout
paths=list(source.glob('*.cs'))+[here/'Packaging/Framework.cs'];exe=here/'VoicePTT.options-complete.exe';build(exe,'VoicePTT',paths,True)
env=os.environ.copy()
for directory,key in [('TestProfile','PADCHAT_DATA_DIR'),('TestTemp','TEMP')]:
 p=here/directory;p.mkdir(exist_ok=True);env[key]=str(p)
env['TMP']=env['TEMP']
(here/'NATIVE-SELF-TEST.txt').write_text(run(exe,'--self-test',env=env),encoding='utf-8')
fixture=here/'SurfaceFixture.exe';surface=here/'Tests/OverlayMeterFixture.cs'
build(fixture,'OverlayMeterFixture',paths+[surface]);(here/'SURFACE-TEST.txt').write_text(run(fixture,str(here),env=env),encoding='utf-8')
testdir=here/'TestSource';testdir.mkdir(exist_ok=True);testpaths=[]
for p in paths:
 s=p.read_text(encoding='utf-8-sig')
 if p.name=='VoicePTT.cs':
  s,n=re.subn(r'static void Keys\(params ushort\[\] keys\) \{.*?\n \}',r'static void Keys(params ushort[] keys){}',s,count=1,flags=re.S);assert n==1
  s,n=re.subn(r'static bool SameGame\(\)\{[^\n]+\}','static bool SameGame(){return fixtureSameGame;}',s,count=1);assert n==1
  s,n=re.subn(r'static void Status\(string message,int ms\)\{[^\n]+\}','static void Status(string message,int ms){fixtureStatus=message;}',s,count=1);assert n==1
  s,n=re.subn(r'static void WriteWorker\(string cmd\)\{[^\n]+\}','static void WriteWorker(string cmd){fixtureCommand=cmd;}',s,count=1);assert n==1
 target=testdir/p.name;target.write_text(s,encoding='utf-8');testpaths.append(target)
testexe=here/'ReadyCueFixture.exe';build(testexe,'ReadyCueFixture',testpaths+[here/'Tests/ReadyCueFixture.cs'])
result=run(testexe,str(here),env=env);print(result);(here/'READY-CUE-TEST.txt').write_text(result,encoding='utf-8')
(here/'NATIVE-REPORT.json').write_text(json.dumps({'nativeSelfTest':True,'surfaceAndMeterTest':True,'readinessRoutingTest':True,'sha256':hashlib.sha256(exe.read_bytes()).hexdigest(),'liveAcceptance':False,'speechBackendChanged':False},indent=2),encoding='utf-8')
print('Full helper, rendering regression and ready-cue lifecycle checks passed. Not installed.')
