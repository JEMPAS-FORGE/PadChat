# PadChat 0.2.2-preview.6

Preview 6 addresses Sarcasmics' voice setup feedback. The in-game test names
the saved keyboard/mouse and controller PTT, with held modifiers spelled out.
Saved choices are displayed separately from unsaved edits. A Previous setup
step button preserves settings. Connection failures explicitly say the mic
has not been tested; focused text/binding capture and open Windows companion
configuration windows get specific guidance. A status result is labelled as
a last check rather than continuous readiness.

These changes improve setup and diagnosis; they do not establish the cause of
Sarcasmics' controller/Sonar problem or prove their PC is fixed. The existing
recording-test level percentage is retained. No new continuously running mic
meter, waveform, microphone auto-selection or protocol/send behaviour is added.

## Retained Preview 5 changes

- Recover when a speech worker is alive but stuck loading or finishing a message.
  A two-minute deadline discards the affected message, restarts the owned worker
  with existing retry backoff, and requires a fresh PTT press. Nothing resumes
  or sends automatically; saved microphone/bindings/models are unchanged.
- Reject ready, result, preview, level and error replies from an obsolete worker
  generation, including replies already queued before a restart. Preserve separate
  microphone-enumeration messages when restarting speech.
- Escape cancels recording, processing, pending results or review in foreground
  WoW. Binding capture keeps its own Escape handling; idle and delivery actions
  are not intercepted. Circle/B still cancels review.
- Invalidate the session and clear in-memory speech before external cancellation
  cleanup, preventing a broken process pipe from retaining sendable old state.
- Terminate the owned speech process even when its quit pipe is already closed.
- Correct the garbled General-unavailable channel notice and document Escape
  cancellation in the in-game voice panel.

Preview 4 input-thread/combat fixes and Preview 3 installer folder/text fixes remain.
CPU-only speech backend and models are unchanged. No private GPU tuning or
experimental multi-client support is bundled.

Headless speech startup: the frozen CPU backend now excludes optional Colorama.
An exact hidden installer preflight reproduced a Colorama invalid-console-handle
crash before enumeration. tqdm has a supported no-Colorama fallback; JSON speech
does not need console colouring. Speech source/model settings remain unchanged.
This is not confirmation of the cause on the reporter's PC. Five consecutive production hidden preflights and the matching shipping
installer/synthetic speech checks pass. Physical devices and the reporter's PC
remain unverified.
