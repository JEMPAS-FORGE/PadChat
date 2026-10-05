# PadChat

Controller chat keyboard and optional **keyboard or controller voice-to-text** for WoW Forever 1.60.1.

## Download and install

Open this project's **Releases** page and select **0.2.0-preview.1**.

- **PadChat-Windows-Voice-0.2.0-preview.1.zip**: complete Windows package. Extract, close WoW, run PadChat-Setup.exe, select the Forever game folder and a Windows host microphone. Installs both the addon and local speech companion. No Python, API key, paid speech account or model download needed.
- **PadChat-0.2.0-preview.1.zip**: addon only. Place the PadChat folder in Interface/AddOns and restart WoW. Installs the controller keyboard; it does not capture microphone audio.

The CurseForge addon download is also addon-only. Voice is optional and requires the Windows package above. See INSTALL.txt for complete instructions and removal.

## Keyboard players: voice-to-text without a controller

With PadChat Voice running and a host microphone selected:

- **Tap F8** to switch between available chat channels.
- **Hold F8** to dictate with a live text preview.
- **Release F8** to send and leave chat.
- **F9** or **/padchat** opens the on-screen keyboard outside combat.

Recognition may mishear words and names. Release sends immediately: test a short message in Say first. For ambiguous player names, use the invitation list.

## Controller players

Create/Share (PlayStation) or Back/View (Xbox-compatible input) performs the same tap/hold voice action with the companion. Touchpad **click** opens the keyboard; finger sliding is not supported. Options + Triangle (Xbox Start + Y), held briefly, is an alternative keyboard shortcut through the companion.

In the keyboard, D-pad or left stick selects keys. X/A confirms, Square/X deletes, Triangle/Y inserts a space, Circle/B closes while keeping the draft. L1/LB changes channel, R1/RB chooses suggestions, L2/LT changes case, R2/RT switches symbols, and Options/Start sends. Confirmation locking helps avoid stick drift selecting another letter.

Chat channels include Say, Yell, General, Party, Raid, Guild and available whisper reply targets. Friend and recent-whisper lists support guarded invitations. The keyboard closes and releases temporary bindings in combat. PadChat does not replace combat bindings or enable native gamepad support.

## Supported setup

- WoW Forever **1.60.1**, Interface **16001**. Retail and older Classic are not supported by this preview.
- Windows **10/11 x64** for the optional companion; local English recognition.
- One native Sony controller or one XInput input. The controller must already reach Windows and work in WoW.
- Microphone must be available on the host Windows PC. Moonlight on iPad does not forward the iPad or controller microphone. Separately configured microphone links work only if exposed as Windows inputs.

Disable ControllerKeyboard / ControllerKeyboardTouchpad before using PadChat. Do not run older WoW voice helpers alongside PadChat Voice. Voice startup at Windows sign-in is optional in Setup.

## Privacy

Speech runs locally using bundled models, without online recognition requests or OpenAI token charges. The microphone opens while push-to-talk is held. Speech buffers and transcripts stay in memory. The companion briefly uses and restores the clipboard for verified chat input. Microphone preferences and health status are stored under %LOCALAPPDATA%/PadChat. WoW stores drafts and learned words in PadChatDB SavedVariables.

## Preview status and verification

This is an **unsigned early preview**, not a stable release certified on every PC. Windows may show an unknown-publisher warning. SHA-256 checksums are included in the release. Automated addon checks, shipping installer fresh/update/rollback/removal checks, and actual bundled speech inference passed in isolated Windows folders. Startup and registration were simulated; a second physical PC, fresh player account and live microphone/gameplay checks remain outstanding. See TEST-REPORT.json.

Please report issues with the game version, controller and connection, microphone type and whether the companion is running. Do not post credentials or private chats.

## Source layout

- PadChat/: in-game Lua/XML addon.
- Helper/: original C# companion and Python speech worker.
- Packaging/: installer, filesystem compatibility layer, build/test scripts and build notes.
- DEPENDENCIES.json: frozen build dependency versions.

Packaging scripts document the original development-folder layout and model inputs; they are reference build tools, not a one-command build from this repository. No personal microphone configuration, recordings, transcripts, account credentials or live-game settings are included.

## License

Original PadChat code is MIT licensed. Bundled speech dependencies and models retain their own licenses and notices in the installed package. Blizzard UI assets are referenced from the installed game and are not shipped. PadChat is not affiliated with Blizzard, Sony, Microsoft, Valve, Moonlight, Sunshine or OpenAI.
