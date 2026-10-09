# PadChat 0.2.2 Preview 3

- Fixed installer rejection of valid Forever client folders when shared Data is in the parent directory. Discovery and validation no longer require a client-local Data folder.
- Retained WowB.exe and Forever 1.60.x file-version checks, linked-folder refusal, running-game guard, payload integrity and rollback protections.
- Fixed garbled installer title and Browse/status labels; build explicitly uses UTF-8 sources.
- Added a read-only --validate-game PATH diagnostic with a specific missing/unreadable-executable message.
- Installer completion instructions now describe controller opening and identify F9 as optional.

All Preview 2 addon/voice features are retained. Existing settings are preserved.
