# PadChat compatibility

Checked **13 November 2026**. Blizzard's Forever launch patch service now
reports **1.60.1.70938** for US, EU, CN, KR and TW. The installed launch
`_forever_/Wow.exe` independently reports the same version.

The current public PadChat release is **0.2.2-preview.15**, approved on
[CurseForge](https://www.curseforge.com/wow/addons/padchat/files/9565746), with a
matching [Windows installer](https://github.com/JEMPAS-FORGE/PadChat/releases/download/v0.2.2-preview.15/PadChat-Setup.exe)
for the addon and optional voice companion. Its real installer/client detection
code accepts the new launch executable. All **24 published-source regression
suites pass** again. The tagged source, addon ZIP, installer, Windows ZIP,
instructions and checksums were reverified against the public assets.

**The full 70938 in-game API audit is pending.** The available
[Forever UI source](https://github.com/Gethe/wow-ui-source/tree/forever) still
reports **1.60.1.70888**, pinned at
`4cd0ab752a77164f654ccbca9d0c1fe8dafceebe`. All 51 pinned files remain unchanged.
The last fully audited source interface is **16001**; a runtime interface value
from 70938 has not been captured. The current official launch hotfix article
contains no newly published UI/API change identifying a PadChat fix.

No patch-specific PadChat defect has been established by these checks, and no
new binary has been released solely for the client build number. Successful
regression and installer checks do not establish live gameplay compatibility
with 70938. The source comparison will need to be completed when the matching
UI source is available. Broader controller/combat/repeated-session, Windows
reboot and clean second-PC testing also remain incomplete.

Preview 15 retains movement-readiness protection, startup recovery,
version-checked `_forever_/Wow.exe` detection and version-checked legacy
Forever `WowB.exe` support. The matching package retains its 14 passed package
checks; exact unchanged package bytes were verified rather than repackaged.
Speech models and defaults are unchanged. Windows 10/11 x64; Forever 1.60.x
only. The experimental multi-client branch and unpublished features are not
included in this release.

Launch build evidence comes from Blizzard's **wowf** patch product. The legacy
**wow_classic_beta** endpoint still reports 70532 and is not used as the current
launch-client version.

Sources: [Blizzard launch versions](http://us.patch.battle.net:1119/wowf/versions),
[last audited UI source](https://github.com/Gethe/wow-ui-source/commit/4cd0ab752a77164f654ccbca9d0c1fe8dafceebe),
[official launch hotfixes](https://worldofwarcraft.blizzard.com/en-us/news/24315281/wow-forever-hotfixes-november-5-2026).
