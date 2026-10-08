# Validation — 0.2.2 Preview 1

Passed on 9 October 2026 against the exact Forever candidate and shipping files:

- Addon model, runtime, options, invites and voice-options regressions.
- First-use controller setup, saved modifiers and release, reconnects, opening/
  closing, draft preservation, chat preparation/focus cleanup, whisper/channel
  destinations, combat guards and friend invites.
- Panel fitting at 640x360, 800x450, 1280x720 and 1920x1080 logical viewports;
  bounded status ownership, microphone-test non-delivery and review settings.
- Shipping companion shortcut/controller/settings-migration self-tests, bounded
  diagnostics protocol and review decisions; clean microphone enumeration.
- Actual shipping installer discovery tests and isolated fresh install/update/
  rollback/uninstall tests, preference preservation, startup opt-out, keyboard-
  only installation, wrong-client rejection, traversal and integrity refusal.
- Actual installer-state reader refuses four malformed metadata cases without
  modifying those files. The ordinary E:\Padchat path parses, but is not a valid
  client folder unless it actually contains the Forever game.
- Bundled recognition of a synthetic speech sample with Python removed from PATH
  and offline settings; embedded payload hashes, source/addon bytes and matching
  direct installer inside the Windows ZIP.

Installer OS registration is simulated in isolated tests. Synthetic speech does
not open a real microphone. Source/mocked/isolated checks do not certify protected
chat sending, physical PTT/microphone/controller operation or actual game visuals.
Those require live acceptance in Forever 1.60.1.70291, interface 16001. A separate
clean PC and real Windows sign-in/startup remain unverified. No running game,
installed addon, personal microphone preferences or startup setting was changed.
This remains an unsigned preview; the experimental multi-client branch is excluded.
