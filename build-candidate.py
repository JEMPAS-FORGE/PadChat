from pathlib import Path

import os
import hashlib

import json

import shutil

import subprocess

import zipfile

ROOT=Path(__file__).resolve().parent

PROJECT=ROOT

CSC=Path('C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe')

OUT=ROOT/'dist';OUT.mkdir(exist_ok=True)

PAYLOAD=ROOT/'Payload'

def compile(target,sources,extra=()):

 args=[str(CSC),'/nologo','/codepage:65001','/target:winexe','/platform:x64','/optimize+',

 '/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll',

 '/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll','/r:Microsoft.CSharp.dll',

 '/win32manifest:'+str(ROOT/'Packaging/app.manifest'),'/out:'+str(target),*extra,

 str(ROOT/'Packaging/Framework.cs'),str(ROOT/'Packaging/LongIO.cs'),*[str(p) for p in sources]]

 subprocess.run(args,check=True)

def main():

 PAYLOAD.mkdir(exist_ok=True)

 shutil.copytree(ROOT/'PadChat',PAYLOAD/'PadChat',dirs_exist_ok=True)

 # Reuse hash-verified shipping CPU backend/models and licenses, not an active

 # personal installation. The changed helper is rebuilt from matching source.

 app=PAYLOAD/'App'

 if not app.exists():
  backend_input=os.environ.get('PADCHAT_BACKEND_INPUT')
  if not backend_input:raise RuntimeError('Set PADCHAT_BACKEND_INPUT to the matching licensed shipping App folder; see BUILD.md')
  shutil.copytree(Path(backend_input),app)
 # Match the shipping CPU-only policy: CTranslate2 imports every adjacent DLL.
 # Optional cuDNN is not needed for int8 CPU inference and may fail to initialise.
 cuda=app/'Backend/_internal/ctranslate2/cudnn64_9.dll'
 if cuda.exists():cuda.unlink()
 compile(app/'PadChatVoice.exe',sorted((ROOT/'Helper').glob('*.cs')))
 (PAYLOAD/'Docs').mkdir(exist_ok=True)
 for p in ('START-HERE.txt','LICENSE.txt','VALIDATION.md','CHANGELOG.md','README.md','COMPATIBILITY.md','BUILD.md','DEPENDENCIES.json'):

  src=ROOT/p

  if src.exists():shutil.copyfile(src,PAYLOAD/'Docs'/p);shutil.copyfile(src,OUT/p)

 if (ROOT/'START-HERE.txt').exists():shutil.copyfile(ROOT/'START-HERE.txt',app/'START-HERE.txt')

 files={p.relative_to(PAYLOAD).as_posix():hashlib.file_digest(p.open('rb'),'sha256').hexdigest() for p in PAYLOAD.rglob('*') if p.is_file() and p.name!='manifest.json'}

 (PAYLOAD/'manifest.json').write_text(json.dumps(files,indent=2))

 archive=ROOT/'payload.zip'

 with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:

  for p in PAYLOAD.rglob('*'):

   if p.is_file():z.write(p,p.relative_to(PAYLOAD).as_posix())

 compile(ROOT/'Uninstall.exe',[ROOT/'Packaging/Setup.cs',ROOT/'Packaging/Discovery.cs'],('/define:UNINSTALL',))

 compile(OUT/'PadChat-Setup.exe',[ROOT/'Packaging/Setup.cs',ROOT/'Packaging/Discovery.cs'],('/resource:'+str(archive)+',payload.zip','/resource:'+str(ROOT/'Uninstall.exe')+',Uninstall.exe'))

 with zipfile.ZipFile(OUT/'PadChat-0.2.2-preview.3.zip','w',zipfile.ZIP_DEFLATED) as z:

  for p in (ROOT/'PadChat').iterdir():

   if p.is_file():z.write(p,'PadChat/'+p.name)

 with zipfile.ZipFile(OUT/'PadChat-source.zip','w',zipfile.ZIP_DEFLATED) as z:

  for folder in ('PadChat','Helper','Packaging','Tests'):

   for p in (ROOT/folder).iterdir():

    if p.is_file():z.write(p,folder+'/'+p.name)

  for p in ('build-candidate.py','run-tests.py','START-HERE.txt','VALIDATION.md','CHANGELOG.md','README.md','COMPATIBILITY.md','BUILD.md','DEPENDENCIES.json'):

   if (ROOT/p).exists():z.write(ROOT/p,p)

 with zipfile.ZipFile(OUT/'PadChat-Windows-Voice-0.2.2-preview.3.zip','w',zipfile.ZIP_DEFLATED,compresslevel=1) as z:

  for name in ('PadChat-Setup.exe','START-HERE.txt','LICENSE.txt','VALIDATION.md'):

   if (OUT/name).exists():z.write(OUT/name,name)

 hashes={p.name:hashlib.file_digest(p.open('rb'),'sha256').hexdigest() for p in OUT.iterdir() if p.is_file() and p.name not in ('SHA256.json','checksums-sha256.txt','PadChat-Setup.exe.sha256')}

 (OUT/'SHA256.json').write_text(json.dumps(hashes,indent=2))

 (OUT/'checksums-sha256.txt').write_text(''.join(v+'  '+k+'\n' for k,v in sorted(hashes.items())))

 (OUT/'PadChat-Setup.exe.sha256').write_text(hashes['PadChat-Setup.exe']+'  PadChat-Setup.exe\n')

 print('Built',OUT)

if __name__=='__main__':main()

