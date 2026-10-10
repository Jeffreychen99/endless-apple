# Endless Apple — development workspace

An iPhone/macOS client project based on Ethan Moffat's MIT-licensed EndlessClient. This is a compatibility prototype, **not yet a playable client for the current official server**.

## What exists

- Existing C#/MonoGame desktop client, with a portable Mac build target.
- Shared .NET protocol core and one-shot compatibility probe.
- Native UIKit iPhone/iPad connection shell, built with Xcode and the .NET iOS workload.
- Offline protocol tests, asset-audit tests, and an official-client inventory tool.
- A concrete [compatibility report](docs/COMPATIBILITY.md) and [architecture notes](docs/ARCHITECTURE.md).

## Verified status

Current official-server initialization, login, character listing, selection, world-entry completion and own-player appearance parsing are verified with the authorized credential file. The shared native renderer reads all 14 current map graphics layers and draws the identified scenery layers plus character bodies, hair, boots, armor, hats, weapons and shields, using original EGF artwork and no Wine.

The iPhone client now retains the world session and responds to synchronization/keepalive packets. Native Mac headless tests use the same world state and renderer. The existing Mac game UI still needs this integration.

**Walking is now verified:** after relocation to map 15, two bounded live tests confirmed one step and a return through authoritative own-player replies. The iPhone shares this movement handler and uses a player-following camera. Nearby-player walking also has a parser verified against the current executable and offline fixtures; subsequent live runs received and handled that opcode.

NPC appearance and batched movement now render from the current 1,049-entry NPC catalog. Live verification observed seven NPCs and nearby-player movement. Shadow artwork is blended (currently approximate 50% opacity), and four-frame terrain animation runs on the iPhone scene timer. Own-movement replies now use the third field as a visibility mask rather than facing; authoritative lists remove departed entities. This last protocol correction passed offline tests but still needs a live retest.

**Not full gameplay yet:** character/NPC walking animation, combat effects, item drops, map transfer/warps, remaining scenery placement and special appearance rules remain unfinished. Movement is limited to ordinary floor. The Mac game UI still needs integration.

### Headless development (no Device Hub)

```sh
scripts/dotnet-local.sh run --project Apple/Endless.Probe -- \
  --official --login-file ~/Desktop/creds.txt --enter-world \
  --assets .local/official --world-preview artifacts/world-155.png --json
# Repeat graphics work offline, without additional sign-ins:
scripts/dotnet-local.sh run --project Apple/Endless.Probe -- \
  --render-map 155 --assets .local/official --world-preview artifacts/world-155.png
```

The credential file contains username and password on separate lines and stays outside the repository. Reports omit credentials, character IDs and raw authenticated packets. World observation ends after eight seconds. The optional `--walk-test` requires a valid starting floor tile and attempts one step and return, reporting success only after server position updates. Cached maps are shown only if revision and byte size match the welcome response. The iPhone development build includes local maps, identified scenery archives and base character artwork; these assets must not be committed or redistributed without permission.

## Local setup

The checkout is `/Users/jeffrey/Desktop/Programming/endless-apple`, separate from finance-tracker. Source starts at upstream revision `c68891c2b81cb971aa22ce570d976f7066a4d7d0` on local branch `codex/apple-client`.

```sh
cd /Users/jeffrey/Desktop/Programming/endless-apple
# SDKs are already installed here. For a fresh Apple Silicon checkout:
scripts/bootstrap.sh
scripts/test.sh
```

All SDKs, downloads, reference checkouts, generated assets, configuration and reports are ignored by Git. Original licensing and attribution remain in place. Source-code licensing does not grant rights to redistribute official game assets.

## Connection diagnostic

```sh
scripts/dotnet-local.sh run --project Apple/Endless.Probe -- --help
# Makes exactly one live initialization attempt, without account credentials:
scripts/dotnet-local.sh run --project Apple/Endless.Probe -- --official --json
# A compatible local classic server:
scripts/dotnet-local.sh run --project Apple/Endless.Probe -- \
  --host 127.0.0.1 --port 8078 --version 0.0.28 --dialect classic --json
```

Exit codes: 0 = documented initialization accepted; 2 = response did not establish compatibility; 1 = invalid options, transport failure, timeout or malformed input. Even code 0 does not mean login or world entry works. The official profile is pinned to the downloaded 0.4.57 distribution and should only change alongside verified protocol evidence. No automatic retries, credential collection or account creation.

The older public-schema request was rejected. After comparing the current official executable, the corrected request now receives code 2 / 15-byte initialization data. The current response is not a classic challenge hash. Connection acknowledgement and authentication are now verified; world entry and bounded walking are now verified. See [initialization evidence](docs/INIT-0457.md).


## Desktop baseline

```sh
scripts/build-mac.sh
scripts/run-mac.sh
# Optional local .app bundle in artifacts (keep it inside this checkout):
scripts/package-mac.sh
```

The content compiler uses the local x64 .NET SDK and Rosetta because upstream MonoGame 3.8.2 ships Intel-only FreeImage. This is a developer baseline, not a signed/notarized application. The launcher uses an isolated `.local/.endlessclient` configuration and defaults to `127.0.0.1`. It does not attempt a live login on launch. Official Recharged assets are kept separate from the legacy assets restored by upstream NuGet dependencies.

## iPhone shell

The iPhone target uses Xcode 27, an isolated .NET SDK 10.0.401, and iOS workload set 10.0.401.1 (Microsoft.iOS 27.0.10722). The desktop renderer continues using upstream's .NET 8 toolchain. `Apple/Endless.iOS/global.json` pins the modern SDK without changing the root SDK pin.

```sh
# For a fresh checkout; installed in this workspace already:
scripts/bootstrap-apple.sh
# Review and accept Apple's license in Xcode, and finish its first-launch setup.
scripts/build-ios.sh
python3 scripts/run-ios.py
```

The build script uses `/Applications/Xcode.app/Contents/Developer` through `DEVELOPER_DIR`, without changing the system-wide developer directory. It never accepts Apple's license automatically. Override `DEVELOPER_DIR` if Xcode is elsewhere.

Physical-device installation additionally needs signing configuration. The shell displays the official profile, sends a user-triggered initialization request, reports distinct failure/acceptance states, and cancels when its scene becomes inactive. It has a working sign-in screen, character selection, a cached terrain preview, and a separate credential-free connection check. Gameplay is not implemented. See `docs/COMPATIBILITY.md` for the current build and simulator status.

## Inspect supplied game files

```sh
python3 scripts/audit-assets.py .local/official > artifacts/official-assets.json
```

The auditor only reads allowlisted asset types under data/gfx/sfx/mfx and emits relative paths, sizes, signatures and hashes. It excludes account/configuration files and symbolic links. The inventory is not a parser-compatibility claim.

## Next milestone

Resolve the live coordinate/movement mismatch, complete equipment/NPC rendering and map transfer, then verify walking and warps before designing the iPhone HUD. See the ordered gates in `docs/COMPATIBILITY.md`.

The iPhone connection check now observes the official pre-login session for 45 seconds, answers synchronization and keepalive packets, and disconnects. Run the same check on Mac with `scripts/dotnet-local.sh run --project Apple/Endless.Probe -- --official --session-watch --json`. That connection check does not use credentials; sign-in is a separate action.

### iPhone UI font/art prerequisites

Run `scripts/bootstrap-ui-fonts.sh` once to prepare the ignored local fonts. It adapts the existing desktop `sans_11px` bitmap atlas for native iPhone input and downloads the open-licensed Carter One fallback with its OFL notice. Builds remain offline. The classic login/HUD assets are read from the existing `packages/endlessclient.binaries/1.4.0.2/build/net462/client/gfx` package; current status/sprite resources remain in `.local/official`. Neither archive is committed by these changes. Original bitmap action words are preferred over the fallback title font.

Credential-free review arguments: `--login-preview`, `--keyboard-preview`, `--selection-preview`, `--hud-preview`, `--hud-self-test`. These synthetic previews bypass saved-account sign-in. Never add a real credential to a preview.

### Simulator UI regression checks

With an iPhone simulator already booted, run `scripts/test-iphone-ui.sh SIMULATOR-UDID` (find its ID using `xcrun simctl list devices booted`). This builds/installs only the simulator app and runs the standalone Xcode UI tests; it does not touch physical-phone signing or certificates. Existing local asset/font prerequisites still apply.

Tests use synthetic preview state and never submit credentials. They cover character navigation/artwork clearance, original inventory selection/scrolling, stable world/control placement, verified/unknown stats, login input, and actual held attack/release. A separate software-keyboard layout test explicitly skips if the simulator has no visible keyboard; this is not a substitute for physical-phone AutoFill/keyboard review. Xcode result bundles and attachments stay under ignored `artifacts/`.

`--hud-preview --expanded --hud-self-test` additionally checks three viewport sizes, repeat/cancel behavior, and native world-image color/orientation against the PNG path. Its conversion timing is a microbenchmark, not end-to-end frame rate. Review fresh screenshots separately before calling a UI checkpoint complete, as required by `AGENTS.md`.
