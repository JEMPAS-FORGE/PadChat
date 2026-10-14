# Forever compatibility - PadChat 0.2.2 Preview 7

Checked on 15 October 2026: WoW Forever **1.60.1.70374 / Interface 16001**.
Blizzard's US, EU, KR and TW patch endpoints report this build.

The complete Gethe/wow-ui-source Forever comparison from 70338 changes only
version.txt. All 49 audited API/UI files are byte-identical. Chat sending and
destinations, gamepad input and modifiers, user/override bindings, restricted
secure frames, friend invitations and microphone enumeration have no published
UI/API source delta requiring a PadChat code fix. Engine behavior may still
change independently of published UI source.

Fresh published addon and source archives match. Eleven automated suites pass
for opening/closing, draft/focus cleanup, channel/whisper routing and one-shot
direct sending, invitations, saved bindings/modifiers and voice setup, recovery,
and pinned restricted-frame validation. The actual C# production transport
methods are compiled and exercised in an isolated harness. These are source and
automated checks, not live protected gameplay or physical controller testing.

All nine existing release assets retain their verified sizes/SHA-256 digests.
The standalone installer and complete Windows ZIP contain the same installer;
the fresh CurseForge ZIP matches the tested addon. Prior unchanged exact-shipping
installer lifecycle/startup/microphone checks are retained, not rerun. No new
release, larger speech model or other-client support is needed for this build.
Live combat/controller/repeated-session and fresh-PC acceptance remain incomplete.

Source pin: 735496de78ebd30ce816a0637982c556987f258c.
Previous pin: 943764493e6b16d63ded3ab304150d1f05e58b57.

- [Full UI source comparison](https://github.com/Gethe/wow-ui-source/compare/943764493e6b16d63ded3ab304150d1f05e58b57...735496de78ebd30ce816a0637982c556987f258c)
- [Blizzard development notes](https://us.forums.blizzard.com/en/wow/t/2360696)
- [Installer and downloads](https://github.com/JEMPAS-FORGE/PadChat/releases/tag/v0.2.2-preview.7)
- [Installation guide](README.md)

Voice requires Windows 10/11 x64, English and a microphone available to Windows
on the gaming PC. Forever only; separate experimental clients are not promoted.
