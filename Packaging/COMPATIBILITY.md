# PadChat client compatibility — 0.3.0 preview

The shared addon and Windows companion target these **current official clients**.
This does not mean every historical WoW release or private server.

| Client family | API snapshot reviewed | Interface | Verification |
| --- | --- | --- | --- |
| Retail | 12.1.0.69933 | 120100 | Blizzard UI source + API-profile tests; needs live gameplay |
| Classic progression (Mists) | 5.5.4.70032 | 50504 | Blizzard UI source + API-profile tests; needs live gameplay |
| Classic Era / Hardcore | 1.15.9.70003 | 11509 | Shared Era client API + API-profile tests; needs live gameplay |
| Anniversary | 2.5.6.69795 | 20506 | Blizzard UI source + API-profile tests; needs live gameplay |
| Forever | 1.60.1.70235 | 16001 | Existing 0.2.1 controls tested by the user; 0.3 changes need live regression |

These are audited source snapshots; regions may run different builds. TOC
metadata declares these audited interfaces. PTR/beta and future patches need
another check; an out-of-date checkbox is not proof of compatibility. Original
1.12/2.4/3.3 clients and private servers are not supported by this preview.

API selection uses capabilities: modern/legacy chat hooks and friend lists,
localised General channels, optional gamepad events and missing controller APIs.
Blizzard secret values and chat lockdown are respected; unavailable friend
details are not inspected or stored. Combat restrictions remain in place.
The existing secure hardware-click send path is present in all five audited
Blizzard SecureTemplates.lua snapshots. Real chat delivery still needs checking
inside each client; mocks cannot certify protected execution rules.

Voice recognises Wow/WowClassic/WowB/WowT foreground processes, requires the
addon handshake, and retains release-to-send and foreground checks. It runs on
Windows 10/11 x64; it does not add macOS/Linux voice support. Install into each
client folder separately. Microphone and voice startup preferences are shared;
each client retains its own PadChat SavedVariables. Updating one folder does
not rewrite another game's addon. Rerun Setup for each folder when updating.
Uninstall removes PadChat from all recorded client folders, preserving settings,
SavedVariables and other addons.

Live acceptance checklist for **each** client: open/close with mouse, F9 and
controller; type/send once in Say; draft preservation; tap/hold F8 and Share;
channel cycling and explicit whisper recipient; friend invite; combat entry/
exit; restart WoW and Windows startup. Also check same-realm invitations on
Era/Hardcore. Do not mark new client support stable until these checks pass.

Evidence: compat-audit/*/audit.json and COMPAT-TEST-REPORT.json.
Blizzard extracted UI source mirror: https://github.com/Gethe/wow-ui-source
