# Preview 14 validation scope

Run `run-tests.py` for source and isolated startup lifecycle checks, then `test-package.py` against the matching built payload/installer. The release gate records the final results in RELEASE-VALIDATION.json.

New checks use fake game/helper processes and isolated named synchronization objects. They do not record a microphone, type into a game, alter a running personal helper, or restart WoW. Startup/update fixtures test shortcut migration and preserve user preferences.

Limitations: no real Windows reboot, clean second PC, or broad physical-controller/mixed-monitor/live-combat acceptance of the new public launcher. Existing live confirmation applies to the user's separate repaired setup. Recognition defaults and bundled CPU models are unchanged.
