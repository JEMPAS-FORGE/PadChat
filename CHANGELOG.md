# PadChat 0.2.2 Preview 8 - voice recovery and audit fixes

- Fixes cancelled chat handoffs blocking later dictation. Explicit cancellation
  revokes old send tokens; a new connection check cannot replay an old message.
- Rejects opening/PTT shortcut conflicts whichever control is changed first.
  Recovers from malformed saved drafts, word counts and binding values.
- Preserves pre-existing installer staging/recovery files on refusal and prevents
  concurrent installer/uninstaller transactions.
- Discards obsolete microphone replies after newer settings or Voice Off, and
  bounds device discovery to 30 seconds. Refresh and Restart stop the owned old
  enumeration process; the saved input is never silently switched.
- Rolls back file and startup-setting changes if applying voice settings fails.
- Allows Windows binding/microphone configuration and Exit while voice is Off
  or waiting for PTT to be released, without turning voice back on.
- Restores an empty text clipboard after a verified send or cancelled handoff,
  while preserving clipboard contents changed by the user during delivery.
- Positions the non-activating dictation overlay on the game window/monitor,
  including negative monitor coordinates, resize and display changes.

- Detects a sustained mismatch between the saved PTT button and input events,
  including a hook removed while its message thread stays alive. Reattaches
  input without reloading the speech model. Release PTT and press again.
- Adds controller-selectable Restart voice + reload UI in Voice settings,
  with expiring confirmation, combat and draft guards. Saved settings are kept;
  unsaved option edits and unfinished dictation are discarded.
- Adds Restart voice to the Windows tray menu. It restarts the speech worker
  and listener without closing WoW or changing the saved microphone or binding.
- Versioned in-game requests expire and are durably consumed once. Recovery
  invalidates old sessions and never starts recording or sends automatically.
- Keeps the same free offline speech engine/model. Update BOTH parts with the
  matching installer; CurseForge installs the addon only.

Automated addon, transport, native lost-hook, microphone lifecycle, settings
rollback and installer checks cover these changes. These do not establish repeated physical input or fresh-PC gameplay
reliability. The original recurring interruption cause remains unconfirmed;
this adds a tested recovery path, not a guarantee against every failure.

# PadChat 0.2.2 Preview 7 - voice chat while moving

- Added a checked, one-shot voice handoff to the addon. Ordinary voice chat no
  longer opens WoW's normal edit box or relies on a synthetic Enter to send.
- Keeps channel/whisper destination, draft/focus, expiry and duplicate-send
  safeguards. On an uncertain result, it never automatically retries.
- Voice users must update BOTH the addon and Windows companion. Run the new
  PadChat-Setup.exe with WoW closed; existing preferences are preserved.
- Existing controller/keyboard opening, PTT binding, mic selection, setup,
  installer repairs and input-listener recovery are retained.

The reported Mouse 5 + W/A/D case was confirmed in game on the author's local
build. The packaged public adaptation has automated transport and addon checks.
Combat, physical controllers and a second clean PC still need live acceptance.
This remains an unsigned preview. Speech is free/offline; its portable CPU
engine is unchanged. No bigger model, capture-tail experiment or personal word
replacement is bundled. Checked Forever 1.60.1.70338, interface 16001.

# PadChat 0.2.2-preview.7

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
