# PadChat compatibility

Preview 15 supports WoW Forever 1.60.x / Interface 16001 on Windows 10/11 x64. Checked 12 November 2026 against source and installed client 1.60.1.70888, Gethe/wow-ui-source Forever ref 4cd0ab752a77164f654ccbca9d0c1fe8dafceebe. All 51 pinned API/UI files match the prior audit. The legacy beta endpoints still report 70532; they are not used as the current launch version.

This release protects the voice readiness query against repeated movement-key text. It retains Preview 14 startup recovery and version-checked `_forever_/Wow.exe` detection, plus only version-checked older Forever WowB.exe support. Other clients and the experimental multi-client branch are not promoted.

24 source suites and 14 matching package checks pass. The reported movement case is reproduced in fixtures, but post-update live acceptance is pending. Wider physical controller/combat, repeated sessions, Windows reboot and clean second-PC testing remain incomplete. Speech models and preferences are unchanged.
