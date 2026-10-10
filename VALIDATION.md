# Validation - 0.2.2 Preview 5

New checks on 10 October 2026:
- Two-minute load/final deadline boundaries, readiness, cancellation/review
  isolation and fresh restart state with a controlled clock; actual queue
  rejects obsolete ready/result/malformed output and preserves enumeration.
- Production session invalidation before external IO; Escape phase ownership.
- Production StopWorker with a real disposable child whose control input is
  closed; termination succeeds without a microphone or game process.
- Compiled input-policy routing, keyboard/mouse modifiers, injected/repeated input,
  capture/review cancellation, bounded queue overflow and stale-event rejection.
- Real Windows hook installation and message pump during a 2.2-second caller
  stall, then shutdown/restart. No physical input was injected into the game.
- Previous public combat snippet reproduces Invalid frame handle against the
  pinned Forever RestrictedFrames validator. Corrected snippet and options/voice
  combat cycles pass; live protected gameplay still needs acceptance testing.
- Matching release helper and complete isolated installer checks are rerun.

Unchanged installer metadata/folder and frozen-source proof below are retained
from Preview 3; they are not new fresh-PC tests.

Installer-specific regression evidence:
- Old production validation rejects shared-parent-Data and no-local-Data client fixtures; accepts local Data.
- Corrected production validation accepts all three layouts with a Forever executable file-version fixture, refuses wrong-client/missing executables, and leaves game folders untouched.
- Actual unshown SetupForm title and Browse/game-folder label assertions pass; source uses explicit UTF-8 compilation and ASCII installer punctuation.
- Shipping binary read-only folder validation, discovery and isolated full installer checks are recorded alongside the release.


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

