# PadChat 0.2.2-preview.5

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
