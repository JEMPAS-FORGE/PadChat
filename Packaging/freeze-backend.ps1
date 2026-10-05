$ErrorActionPreference='Stop'
$packageRoot=$PSScriptRoot
$voicePython=Join-Path (Split-Path (Split-Path $packageRoot)) 'controller-keyboard\voice-env\Scripts\python.exe'
$env:PYTHONPATH=Join-Path $packageRoot 'build-tools'
& $voicePython (Join-Path $packageRoot 'prepare-public.py')
if($LASTEXITCODE -ne 0){throw 'Public source preparation failed'}
& $voicePython -m PyInstaller --noconfirm --onedir --console --noupx --name voice-backend --distpath (Join-Path $packageRoot 'frozen') --workpath (Join-Path $packageRoot 'freeze-build') --specpath $packageRoot --exclude-module av --exclude-module faster_whisper.audio --collect-all ctranslate2 --collect-all faster_whisper --collect-all sounddevice --collect-all onnxruntime (Join-Path $packageRoot 'src\voice_worker.py')
if($LASTEXITCODE -ne 0){throw 'Speech engine build failed'}
