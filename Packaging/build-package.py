"""Assemble explicit public allowlists; no personal settings or recordings."""
from pathlib import Path
import hashlib
import importlib.metadata as md
import json
import shutil
import subprocess
import sys
import zipfile

ROOT = Path(__file__).resolve().parent
PERSONAL = ROOT.parent.parent / 'controller-keyboard'
PAYLOAD = ROOT / 'payload'
VERSION = '0.2.1-preview.1'
DIST = ROOT.parent / 'releases' / VERSION
CSC = Path(r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe')

def run(*args):
    subprocess.run([str(a) for a in args], check=True)

def compile_cs(target, *sources, extra=()):
    run(CSC, '/nologo', '/target:winexe', '/platform:x64', '/optimize+',
        '/r:System.Windows.Forms.dll', '/r:System.Drawing.dll',
        '/r:System.Web.Extensions.dll', '/r:System.IO.Compression.dll',
        '/r:System.IO.Compression.FileSystem.dll', '/r:Microsoft.CSharp.dll',
        '/win32manifest:' + str(ROOT/'app.manifest'),
        '/out:' + str(target), *extra, ROOT/'Framework.cs', ROOT/'LongIO.cs', *sources)

def license_inventory(target):
    target.mkdir(parents=True, exist_ok=True)
    inventory=[]
    # Include installed dependency license files; excess notices are harmless,
    # and keep every transitive dependency's license available offline.
    for dist in md.distributions():
        if dist.metadata['Name'].lower() in ('pip','av'):
            continue
        item={'name':dist.metadata['Name'], 'version':dist.version,
              'license':dist.metadata.get('License-Expression') or dist.metadata.get('License',''),
              'source':dist.metadata.get('Home-page') or dist.metadata.get_all('Project-URL',[])}
        inventory.append(item)
        dest=target / (dist.metadata['Name']+'-'+dist.version)
        dest.mkdir(exist_ok=True)
        for file in dist.files or []:
            if any(s in file.name.lower() for s in ('license','licence','copying','notice')):
                source=Path(dist.locate_file(file))
                if source.is_file():
                    # Flat names avoid repeating long site-package paths under
                    # user profiles and Windows' legacy MAX_PATH boundary.
                    ident=hashlib.sha256(str(file).encode()).hexdigest()[:8]
                    out=dest / (ident+'-'+source.name)
                    shutil.copyfile(source,out)
    # Interpreter license retained separately from package metadata.
    interpreter=Path(sys.base_prefix)/'LICENSE.txt'
    if interpreter.exists():shutil.copyfile(interpreter,target/'Python-LICENSE.txt')
    for file in (ROOT/'model-licenses').glob('*'):
        shutil.copyfile(file,target/file.name)
    (target/'DEPENDENCIES.json').write_text(json.dumps(inventory,indent=2),encoding='utf-8')

def main():
    DIST.mkdir(parents=True,exist_ok=True)
    if PAYLOAD.exists():
        # Only this resolved, known generated payload directory is replaced.
        assert PAYLOAD.parent == ROOT and PAYLOAD.name == 'payload'
        shutil.rmtree(PAYLOAD)
    PAYLOAD.mkdir()
    addon=PAYLOAD/'PadChat'
    shutil.copytree(ROOT.parent/'PadChat',addon)
    toc=addon/'PadChat.toc'
    toc.write_text(toc.read_text().replace('## Version: 0.1.0','## Version: '+VERSION),encoding='utf-8')
    # Conflict guards are in the tested canonical addon source. Shipping those
    # exact sources avoids accumulating duplicated guards during packaging.
    app=PAYLOAD/'App';app.mkdir()
    shutil.copytree(ROOT/'frozen'/'voice-backend',app/'Backend')
    # CTranslate2's Windows wheel loads every adjacent DLL at import time.
    # CPU inference does not need cuDNN, and loading it breaks on some PCs.
    # Remove the GPU-only DLL from this deliberately CPU-only distribution.
    cudnn=app/'Backend'/'_internal'/'ctranslate2'/'cudnn64_9.dll'
    if cudnn.exists():cudnn.unlink()
    for name in ('whisper-tiny-en','whisper-distil-small-en'):
        dest=app/'Backend'/name;dest.mkdir(exist_ok=True)
        for file in (PERSONAL/name).iterdir():
            if file.suffix in ('.bin','.json','.txt') and file.is_file():shutil.copyfile(file,dest/file.name)
    compile_cs(app/'PadChatVoice.exe',*(ROOT/'src'/n for n in ('VoicePTT.cs','VoiceKeyboard.cs','VoiceMicrophone.cs','VoiceController.cs')))
    native=ROOT/'native'/'SDL3-3.2.28'
    shutil.copyfile(native/'SDL3.dll',app/'SDL3.dll')
    shutil.copyfile(native/'LICENSE.txt',app/'SDL3-LICENSE.txt')
    shutil.copyfile(ROOT/'START-HERE.txt',app/'START-HERE.txt')
    docs=PAYLOAD/'Docs';docs.mkdir()
    shutil.copyfile(ROOT/'START-HERE.txt',docs/'START-HERE.txt')
    shutil.copyfile(ROOT/'LICENSE.txt',docs/'LICENSE.txt')
    license_inventory(docs/'Licenses')
    shutil.copyfile(native/'LICENSE.txt',docs/'Licenses'/'SDL3-LICENSE.txt')
    # PyInstaller's own redistribution notices from isolated build tools.
    for d in (ROOT/'build-tools').glob('*.dist-info'):
        for f in d.rglob('*'):
            if f.is_file() and any(x in str(f).lower() for x in ('license','copying','notice')):
                dest=docs/'Licenses'/'Build-tools'/d.name/f.relative_to(d)
                dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(f,dest)
    files={f.relative_to(PAYLOAD).as_posix():hashlib.file_digest(f.open('rb'),'sha256').hexdigest() for f in PAYLOAD.rglob('*') if f.is_file()}
    (PAYLOAD/'manifest.json').write_text(json.dumps(files,indent=2),encoding='utf-8')
    archive=ROOT/'payload.zip'
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as zip:
        for f in PAYLOAD.rglob('*'):
            if f.is_file():zip.write(f,f.relative_to(PAYLOAD).as_posix())
    uninstall=ROOT/'Uninstall.exe'
    compile_cs(uninstall,ROOT/'Setup.cs',extra=('/define:UNINSTALL',))
    compile_cs(DIST/'PadChat-Setup.exe',ROOT/'Setup.cs',extra=('/resource:'+str(archive)+',payload.zip','/resource:'+str(uninstall)+',Uninstall.exe'))
    with zipfile.ZipFile(DIST/'PadChat-keyboard-only.zip','w',zipfile.ZIP_DEFLATED) as zip:
        for f in addon.iterdir():
            if f.is_file():zip.write(f,'PadChat/'+f.name)
        zip.write(ROOT/'START-HERE.txt','START-HERE.txt');zip.write(ROOT/'LICENSE.txt','LICENSE.txt')
    for name in ('START-HERE.txt','LICENSE.txt'):shutil.copyfile(ROOT/name,DIST/name)
    # Small source distribution for both MIT code and native dependency source pointers.
    with zipfile.ZipFile(DIST/'PadChat-source.zip','w',zipfile.ZIP_DEFLATED) as zip:
        for f in (ROOT/'src').iterdir():
            if f.is_file():zip.write(f,'Helper/'+f.name)
        for f in addon.iterdir():
            if f.is_file():zip.write(f,'PadChat/'+f.name)
        for f in ('Setup.cs','Framework.cs','LongIO.cs','prepare-public.py','build-package.py','freeze-backend.ps1','test-release.py','BUILD.md','app.manifest','START-HERE.txt','LICENSE.txt'):
            zip.write(ROOT/f,'Packaging/'+f)
        zip.write(docs/'Licenses'/'DEPENDENCIES.json','DEPENDENCIES.json')
        zip.write(native/'LICENSE.txt','Packaging/SDL3-LICENSE.txt')
    hashes={f.name:hashlib.file_digest(f.open('rb'),'sha256').hexdigest() for f in DIST.iterdir() if f.is_file() and f.name!='SHA256.json'}
    (DIST/'SHA256.json').write_text(json.dumps(hashes,indent=2),encoding='utf-8')
    print('Built',DIST)

if __name__=='__main__':main()
