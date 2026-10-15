# Forever compatibility - Preview 8

Checked 15 October 2026: Forever 1.60.1.70374, interface 16001,
Gethe/wow-ui-source Forever ref 735496de78ebd30ce816a0637982c556987f258c.
The prior build comparison changes only version.txt; the 49 audited chat,
secure-frame, gamepad, binding, invitation and microphone API/UI files match.

Preview 8 includes voice recovery and the eleven reproduced audit fixes.
It retains the guarded Preview 7 send protocol, with explicit cancellation
and abandoned-token cleanup, and the same free offline CPU speech backend/model.
The personal GPU installation is maintained separately. No experimental 0.3
or other-client support is promoted.

Automated Lua and compiled transport checks do not establish protected live
gameplay compatibility. The new native test removes only its own disposable
hooks while keeping the listener thread alive and verifies replacement. No
physical input, recording or game control is performed by that test. Repeated
physical-controller, new recovery UI in-game, combat, and fresh-PC acceptance
remain incomplete. The recurring personal interruption cause is unconfirmed.
