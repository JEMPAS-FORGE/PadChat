# PadChat 0.2.2 Preview 2

- Expanded local WoW recognition vocabulary with optional user-entered word/player
  hints in Voice settings. Bounded hints never rewrite transcripts or add commands.
- Warns about existing WoW keyboard/controller actions before saving PTT bindings;
  requires explicit acceptance, with cancellation/change invalidating acceptance.
- Guided setup selects a microphone/PTT, saves/reloads, then verifies recognition
  in the existing non-delivery test. Progress resumes after reload.
- Interrupted microphone capture and controller source changes discard affected
  speech. The helper refreshes audio enumeration, retains the saved input and
  requires release plus a fresh PTT press; it never silently chooses another mic.
- Existing settings, review mode, F8/Share tap/hold controls, installer repair,
  minimap button and controller keyboard remain available.

Forever 1.60.1.70291 / interface 16001 only. Unsigned Windows x64 preview.
No separate fresh-PC test was possible. Physical reconnects, microphone/controller
PTT, live protected chat, visual layout and Windows startup remain live-test gaps.
