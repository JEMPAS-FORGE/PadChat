# PadChat public package

Target: WoW Forever 1.60.x / Interface 16001, Windows 10/11 x64.
This is a preview release, not a claim of support for every WoW client or device.

The running personal helper and live WoW addon are never changed by this build.
Public sources have independent single-instance events and per-user settings.
Do not run the personal helper and public helper together in actual gameplay.

## Build

Use Python 3.12 x64 with the versions listed in the release's DEPENDENCIES.json.
The current development environment is ../controller-keyboard/voice-env.
Install PyInstaller 6.20.0 and hooks-contrib 2026.8 in an isolated build-tools
folder. Microsoft .NET Framework 4.8's csc.exe compiles the installer/helper.
The speech model folders are whisper-tiny-en and whisper-distil-small-en,
converted CTranslate2 models from SYSTRAN's model repositories.

1. Run prepare-public.py. This copies/adjusts the tested helper implementation.
2. Freeze src/voice_worker.py into frozen/voice-backend, onedir / console,
   excluding av and faster_whisper.audio; collect ctranslate2, faster_whisper,
   sounddevice and onnxruntime. See freeze-backend.ps1 for exact arguments.
3. Put the converted model folders and official model licenses in the configured
   folders. These build inputs are already present in this workspace.
4. Run build-package.py. It uses explicit inputs, adds original addon code,
   dependency/license notices and SHA-256 manifest, creates keyboard-only/source
   ZIPs, then embeds the verified ZIP in PadChat-Setup.exe.
5. Run test-release.py. It executes SHIPPING binaries with Python removed from
   PATH, blank per-user settings, and separate mock game/profile folders. It
   tests actual bundled speech plus installation/update/rollback/removal.

The PCM adapter supports only live float32 arrays and 16-bit WAV test fixtures.
No PyAV / FFmpeg / x264 / x265 binaries are shipped. Those media decoders are
unnecessary for push-to-talk input. faster-whisper's small audio API is replaced
explicitly before import; unchanged MIT recognition code remains bundled.

## Remaining release validation

Before labelling a release stable, run the installer on a second physical PC
without Python, choose a microphone, sign out/in to verify Startup, run WoW on
a fresh account, and test F8/Share, keyboard sending, combat, whisper recipients
and friend invites. Test both native Sony and streamed XInput. The isolated
tests simulate OS integration rather than modifying the developer's Startup or
registry. Sign the installer with a publisher certificate when distributing
widely; this preview is unsigned. Release publication is separate from these local validation checks.

Sources: PyInstaller usage https://pyinstaller.org/en/stable/usage.html ;
Whisper https://github.com/openai/whisper ; Distil-Whisper
https://github.com/huggingface/distil-whisper ; faster-whisper
https://github.com/SYSTRAN/faster-whisper .
