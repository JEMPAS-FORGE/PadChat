# Validation — 0.2.2 Preview 2

Automated checks on 9 October 2026 cover the exact release source and binaries:

- Existing addon navigation/open/close/modifiers, sending preparation/focus cleanup,
  chat/whisper destinations, combat guards, friend invites and options regressions.
- Existing logical panel fitting, controller onboarding, owned companion status,
  test non-delivery and review settings checks.
- Actual Lua conflict warnings and two-step consent, changed bindings/actions,
  dismissed consent, controller primary reservation, optional words/edit cancellation,
  PCV3 wire encoding and guided setup/resume/owned-test completion.
- Actual Python decoder prompt, per-job vocabulary snapshot, cancelled in-flight
  results, simulated PortAudio stop failure and missing callback non-delivery.
- Compiled companion PCV1/2/3 migration, bounded vocabulary, reconnect cancellation/
  fresh-press decisions, microphone matching, shortcut/controller/review diagnostics.
- Frozen backend vocabulary command, ordinary-speech content assertions with four
  vocabulary choices, and synthetic speech recognition without Python
  on PATH, with network-disabled model settings; no microphone opened by speech test.
- Shipping installer discovery and isolated install/update/rollback/uninstall,
  integrity/traversal refusal, preference preservation and startup opt-out checks.
- Source/addon/embedded payload/Windows ZIP installer and published asset consistency.

A long glossary caused repetition on the ordinary-speech fixture and was rejected;
the final prompt is bounded to 125 UTF-8 bytes. Proper nouns can still be misheard
(the synthetic Wailing Caverns example becomes Whaling Caverns). Hints are not
guaranteed corrections.

The build excludes the optional CUDA/cuDNN DLL, matching the prior CPU-only
shipping policy; the frozen package check enforces this exclusion.

PortAudio failures are simulated. Synthetic audio and mocked game APIs do not
certify physical microphone/controller reconnects, protected chat sending or visuals.
No separate clean PC, real Windows startup or live gameplay was tested. The existing
game/addon/helper, user microphone and startup preferences were untouched.
This remains an unsigned Forever-only preview. Experimental 0.3 is excluded.
