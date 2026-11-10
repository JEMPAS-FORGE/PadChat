# PadChat Preview 13 compatibility

Checked 11 November 2026: **WoW Forever 1.60.1.70888 UI source**, ref
`4cd0ab752a77164f654ccbca9d0c1fe8dafceebe`. PadChat remains
**0.2.2-preview.13 / Interface 16001**. No compatibility code fix was needed.

The complete UI comparison contains 16 changed files. The changed incoming
party-invite event is not consumed by PadChat; its outgoing invite functions,
chat, gamepad, binding, voice and secure-frame contracts remain unchanged.
All 21 exact published-source suites and four native invite-prompt checks
passed. The matching installer/backend retain their 12 passed package checks;
published source, addon, installer and download checksums were reverified.

CurseForge file [9542329](https://www.curseforge.com/wow/addons/padchat/files/9542329)
is Approved as a Beta for Forever 1.60.1. Voice users need the matching
[Windows installer](https://github.com/JEMPAS-FORGE/PadChat/releases/download/v0.2.2-preview.13/PadChat-Setup.exe)
as well as the addon. Select Beta updates in CurseForge or use the direct file.

This is a source audit and automated regression check, **not live gameplay
certification on build 70888**. The new menu and D-pad were previously confirmed
by one user, as were the earlier transparent meter and ready-cue foundation.
New appearance choices, destination display, failed-message recovery, repeated
controller/combat sessions, mixed-DPI/fullscreen and a clean second PC still
need broader live testing.

Windows 10/11 x64; English local CPU speech; Forever 1.60.x only.
Launch `_forever_/Wow.exe` is supported, with version-checked legacy Forever
`WowB.exe` support retained. Published speech models/defaults are unchanged.
No paid speech service is required; speed and accuracy depend on hardware and
speech. This remains an unsigned preview.

The legacy beta patch endpoint still reports 70532; it does not establish the
current launch-client version or regional rollout. The source pin above is
independent of those stale beta values.

Sources: [pinned UI change](https://github.com/Gethe/wow-ui-source/commit/4cd0ab752a77164f654ccbca9d0c1fe8dafceebe),
[official launch hotfixes](https://worldofwarcraft.blizzard.com/en-us/news/24315281/wow-forever-hotfixes-november-5-2026).
