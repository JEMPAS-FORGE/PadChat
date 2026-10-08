# Build the Forever preview

Use Windows x64, Python 3.12 and .NET Framework 4.8 csc.exe. No personal game
installation is modified. Set PADCHAT_BACKEND_INPUT to an extracted App folder
from the hash-verified matching public installer (its --self-test accepts an
isolated NEW test root followed by embedded embedded and extracts shipping-payload.zip).
This input supplies the frozen CPU speech backend, models, SDL and required
license/model notices; source builds reuse matching frozen binaries. To change Python, rebuild with the PyInstaller command below; preserve all notices.
The full backend dependency inventory is DEPENDENCIES.json. Python speech sources
are in Helper; model weights/binaries are distributed in the installer, not Git.

Run python build-candidate.py. It compiles exact C# sources, embeds the checked
manifest/payload and writes dist/. Packaging/DiscoveryStateTest.cs is a separate
developer console test; the build explicitly excludes it from the GUI installer.
For tests install lupa, numpy and sounddevice, then python run-tests.py. To run installer tests use
PadChat-Setup.exe --discovery-test and --self-test NEW_ISOLATED_ROOT embedded embedded.
Never reuse an existing game/user folder as a mock test root. Extract the test's
shipping-payload.zip and run App/PadChatVoice.exe --self-test with a fresh
PADCHAT_DATA_DIR. Also run python Tests/test-shipping-speech.py MATCHING_APP_BACKEND_FOLDER. Actual Windows startup and physical devices need separate testing.
To rebuild changed Python in an environment matching DEPENDENCIES.json plus
PyInstaller 6.20, run from this source folder:

python -m PyInstaller --noconfirm --onedir --console --noupx --name voice-backend --distpath frozen --workpath freeze-build --specpath . --exclude-module av --exclude-module faster_whisper.audio --collect-all ctranslate2 --collect-all faster_whisper --collect-all sounddevice --collect-all onnxruntime Helper/voice_worker.py

Copy both whisper model folders and all matching license/model notices from the
verified extracted App/Backend into frozen/voice-backend. Copy that complete
folder into a NEW App/Backend input, retaining matching SDL3.dll/SDL3-LICENSE.txt.
Set PADCHAT_BACKEND_INPUT to that NEW App input. build-candidate.py recompiles C#
and excludes the optional cuDNN DLL, matching the CPU-only shipping policy.
Never overlay a new frozen runtime on an old _internal directory. Require fresh
isolated installer/backend self-tests and source/package checks before distributing.
No experimental multi-client code is included.
