# PadChat 0.2.2 Preview 1

- Installer ignores malformed/unavailable game discovery entries and retains a
  usable Browse window. Invalid previous install metadata can be repaired.
- Ships the minimap options button and in-game voice shortcut, microphone,
  controller modifier, startup and enable/disable choices with the matching helper.
- First-use controller setup appears for controllers without automatic opening;
  native PlayStation touchpad and existing custom bindings remain intact.
- Adds a bounded companion check and microphone-test mode from Voice settings.
  Tests show level/recognised text without submitting chat or invitations.
- Optional review-before-send: press PTT again to submit, Escape/B/Circle cancels.
  Existing release-to-send stays the default; legacy settings migrate.
- Keyboard/options/voice panels fit smaller logical viewports and UI scales;
  controller help uses the detected PlayStation/Xbox button names.
- Opening modifier release no longer toggles keyboard Shift accidentally.

Forever 1.60.1 build 70291 / Interface 16001 only. This preview does not release
the experimental multi-client branch. Live physical input/chat acceptance remains
separate from the automated regression and isolated shipping package checks.
