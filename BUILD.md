# Build the Forever preview

Use Windows x64, Python 3.12 and .NET Framework 4.8 csc.exe. No personal game
installation is modified. Set PADCHAT_BACKEND_INPUT to an extracted App folder
from the hash-verified matching public installer (its --self-test accepts an
isolated NEW test root followed by embedded embedded and extracts shipping-payload.zip).
This input supplies the frozen CPU speech backend, models, SDL and required
license/model notices; source builds reuse them unchanged. Preserve all notices.
The full backend dependency inventory is DEPENDENCIES.json. Python speech sources
are in Helper; model weights/binaries are distributed in the installer, not Git.

Run python build-candidate.py. It compiles exact C# sources, embeds the checked
manifest/payload and writes dist/. Packaging/DiscoveryStateTest.cs is a separate
developer console test; the build explicitly excludes it from the GUI installer.
For addon tests install lupa, then python run-tests.py. To run installer tests use
PadChat-Setup.exe --discovery-test and --self-test NEW_ISOLATED_ROOT embedded embedded.
Never reuse an existing game/user folder as a mock test root. Extract the test's
shipping-payload.zip and run App/PadChatVoice.exe --self-test with a fresh
PADCHAT_DATA_DIR. Actual Windows startup and physical devices need separate testing.
No experimental multi-client code is included.
