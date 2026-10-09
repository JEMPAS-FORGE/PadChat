# PadChat 0.2.2-preview.4

- Move keyboard/mouse voice hooks to a dedicated Windows input thread, keeping
  slow speech shutdown, settings files and UI work out of callbacks.
- Bound the pending input queue. After a stall/overflow, cancel an affected
  keyboard recording and require a fresh physical press instead of replaying a
  delayed release. Ignore injected input; preserve saved PTT keys/modifiers.
- Retry failed listener startup/thread exit and expose bounded listener health
  metadata without audio, recognised words, ordinary typing or recipients.
- Fix the combat Invalid frame handle path: regular Options/Voice panels close
  in the normal combat event handler, while secure owners clear their bindings.
  The protected keyboard send button retains its secure combat cleanup.
- Repair the portable source builder's backend input and include the pinned
  combat regression fixture/test instructions.

Previous installer shared-Data-folder and text fixes, controller opening,
in-game mic/PTT options, guided setup, review, speech hints and recovery remain.
No game interface, default binding, microphone selection or speech model change.
Personal GPU tuning is not bundled in this CPU-only public preview.
