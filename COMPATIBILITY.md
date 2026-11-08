# Forever compatibility - Preview 10

9 November 2026: supports the launch `_forever_/Wow.exe` client and legacy
Forever `WowB.exe`, with executable file version restricted to 1.60.x.
The installed launch executable is 1.60.1.70862, interface 16001.
The latest completed API audit is pinned to Gethe/wow-ui-source Forever
9dc84f0d3bbae1768ac3405b3716000bd93eda06 (8 November).
Chat, input, secure-send and invitation contracts did not change in that audit.
Retail and other Classic clients remain unsupported even when named Wow.exe.

Preview 10 fixes launch-folder discovery, stale beta-folder migration, installer
labels/validation, running-game checks and companion foreground recognition.
It also rejects malformed worker/microphone messages without losing the next
valid response. The free offline CPU speech backend and model are unchanged.
Saved microphone, PTT, startup preference and WoW SavedVariables are preserved.
No personal speech replacement rules or experimental other-client support ship.

Production source and shipping installer/helper regression results are recorded
with this release. They do not establish physical controller, combat, repeated
live session or fresh-PC compatibility. No game input, restart or microphone
recording is part of the launch-path regression. Historical live tests apply
only to their recorded versions and environments.
