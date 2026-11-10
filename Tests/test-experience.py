from pathlib import Path
import os,re,subprocess,json
root=Path(__file__).resolve().parent.parent
source=root/'TestSource';out=root/'ExperienceTestSource';out.mkdir(exist_ok=True)
paths=[]
for p in source.glob('*.cs'):
    s=p.read_text(encoding='utf-8-sig')
    if p.name=='VoicePTT.cs':
        s,n=re.subn(r'static string Clip\(\)\{[^\n]+\}', 'static string Clip(){return fixtureClipboard;}',s,count=1);assert n==1
    s=s.replace('Clipboard.SetText(','FixtureSetClipboard(').replace('Clipboard.Clear()','FixtureClearClipboard()')
    target=out/p.name;target.write_text(s,encoding='utf-8');paths.append(target)
exe=root/'ExperienceFixture.exe'
csc=Path(os.environ['WINDIR'])/'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
r=subprocess.run([str(csc),'/nologo','/target:exe','/platform:x64','/codepage:65001','/main:ExperienceFixture','/out:'+str(exe),'/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:Microsoft.CSharp.dll',*map(str,paths),str(root/'Tests/ReadyCueFixture.cs'),str(root/'Tests/ExperienceFixture.cs')],capture_output=True,text=True)
assert r.returncode==0,r.stdout+r.stderr
env=os.environ.copy();env['PADCHAT_DATA_DIR']=str(root/'TestProfile');env['TEMP']=env['TMP']=str(root/'TestTemp')
r=subprocess.run([str(exe),str(root)],capture_output=True,text=True,env=env,timeout=40)
assert r.returncode==0,r.stdout+r.stderr
(root/'EXPERIENCE-TEST.txt').write_text(r.stdout,encoding='utf-8');print(r.stdout)
(root/'EXPERIENCE-REPORT.json').write_text(json.dumps({'passed':True,'inputAndClipboardStubbed':True,'audioRecordedOrPlayed':False,'liveGameVerified':False},indent=2)+'\n')
