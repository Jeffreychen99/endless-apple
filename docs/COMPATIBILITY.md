# Compatibility investigation — 2026-10-01

## Current status — 2026-10-06

Official login, character listing and selection are verified headlessly. The welcome response identifies map 155 (557 bytes); its locally supplied map matches the server revision and size. Shared native code parses current map terrain and decodes original EGF bitmap resources, including 16-bit RGB555/565. A terrain PNG was produced after live selection. The iPhone preview uses the same implementation. Scenery mapping, actors, movement, map transfer and final world entry remain incomplete. See APPLE-README.md for credential-file and offline-render commands.

## Historical investigation (superseded by current status below)

The corrected client now receives the official server’s code-2, 15-byte initialization response. **Session establishment, authentication and official-server gameplay are not implemented or verified**. Do not enter real account credentials into the legacy client expecting Recharged compatibility.

One credential-free initialization request was sent to `game.endless-online.com:8078` at `2026-10-01T18:37:07.86927Z`, advertising version `0.4.57` and the public Recharged schema's protocol marker `113`. It returned an INIT reply with code `3`, length `51` bytes. In the public schema code 3 is a rejection. The extended response was not decoded, so the reason is unknown; it does not establish that the user's account or address is banned. No login, account creation or movement was sent. A second, explicitly initiated diagnostic request at `2026-10-02T00:26:18.765678Z` used the same profile and installation identity after rejection-text parsing was added. It also returned code 3 / 51 bytes; its printable payload contained a video URL rather than actionable protocol guidance. That URL was not followed. There is no automatic retry loop, and no further live requests are planned without new evidence.

This is a real TCP response, not a successful authenticated session. The earlier classic challenge-hash check was inapplicable to the current Recharged response; see the newer binary findings below.

## Pinned inputs

| Input | Revision / source |
|---|---|
| EndlessClient | `c68891c2b81cb971aa22ce570d976f7066a4d7d0` (2025-12-20) |
| Recharged XML reference | `3b1d5f0e0608dfab6a5f4b673aadad2f2131d0a5` (2025-06-26) |
| eolib-dotnet reference | `a34fb85a413dc1a1f732e92cf513ca233eac39e7` (2026-09-30) |
| Official distribution | `https://www.endless-online.com/client/0_04/EndlessOnline0457c.zip` |
| Actual server configuration | Official distribution's `config/setup.ini` |

References are in the ignored `reference/` folder. Official files are in ignored `.local/official/`. Neither is redistributed or committed. Public source links: https://github.com/ethanmoffat/EndlessClient, https://github.com/sorokya/eo-protocol-recharged, https://github.com/ethanmoffat/eolib-dotnet.

## Concrete gaps

- Legacy initialization marker is 112; the public Recharged schema uses 113.
- Legacy encryption parameters are one byte each; the Recharged schema uses two-byte encoded values plus an extra increment field. The remaining packet transform must be established before sending authenticated traffic.
- Recharged's schema describes password encryption but does not provide enough information here to claim current authentication support. Do not send classic login packets to the official server.
- The current asset audit found 43 EGF graphics files, 441 EMF maps, 16 EPF tables, 45 scene files, 65 MIDI files, and 437 WAV files in the audited directories. Classic pub-file loaders do not implement the current EPF tables.
- A successful connection to EOSERV or another classic private server would not resolve these gaps.

## Next acceptance gates

1. Implement and verify the encoded Connection/Accept acknowledgement and session packet transform using the current client evidence in INIT-0457.md. Initialization request formatting is now corrected. Continue investigating authentication without sending credentials until its format is established.
2. Add captured, sanitized packet fixtures for the exact server version. Never commit passwords, session secrets, or raw authenticated traffic.
3. Complete initialization and keepalive; then authenticate using credentials entered locally by the user.
4. List characters, select one, transfer/validate current map and pub data, render world state.
5. Walk, change maps, receive other entities, and remain connected for 30 minutes.
6. Repeat on a physical iPhone and exercise suspension, cellular transitions and reconnection.

## Operator inquiry draft (not sent)

I'm developing an iPhone/macOS client for the current official Endless Online server. Are third-party clients permitted, and is a current protocol specification or supported integration available? The public Recharged initialization schema is older than 0.4.57. Can you provide the current handshake, packet encoding and authentication requirements, current EPF/EMF formats, and guidance on distributing the required graphics/audio? I can provide a minimal credential-free reproduction and would welcome a test-server endpoint.

## Build and validation status

- Mac DesktopGL Release build: succeeded, zero warnings/errors. The packaged app remained running for a bounded eight-second startup check and was then terminated by the test harness. This verifies startup only, not visual correctness or gameplay.
- Protocol regression runner: 39 assertions passed, including bounded rejection text, cancellation and fragmented/coalesced TCP frames.
- Asset auditor: two tests passed, including exclusion of configuration files and symlinks.
- iOS and Mac bundle property lists: valid.
- Updated 2026-10-04: Xcode 27.0 (27A266a), .NET SDK 10.0.401 and iOS workload 27.0.10722 / set 10.0.401.1 are installed. Apple's license gate is cleared. Full native iPhone simulator packaging succeeds with zero warnings/errors; installation and launch succeed on the iPhone 18 Pro / iOS 27.0 simulator. Physical-device testing remains outstanding.
- Mac visual inspection through app-control hung and was interrupted. Do not confuse that tool failure with a verified in-game rendering result. MIDI initialization warns that background music is unavailable.

## Apple toolchain update — 2026-10-04

The iOS target now uses .NET 10 and UIKit scenes. The first compiler pass found UIFontTextStyle/NSString mismatches; these were fixed, and the managed compilation target passes cleanly. New scripts isolate the Apple SDK from the original Mac renderer's .NET 8 SDK. `scripts/build-ios.sh` checks Apple's native-tool readiness before attempting the full app build and does not accept any license automatically.

The Mac Release client was rebuilt with zero warnings/errors and its local .app bundle regenerated. Official-server compatibility is unchanged: no additional live server requests were made during this toolchain work.

The iPhone portrait connection screen was visually verified after correcting a UIKit width constraint: the two-argument dimension overload treated -48 as a multiplier rather than a constant. The corrected constraint explicitly uses multiplier 1 and constant -48. Screenshot: `artifacts/ios-preview.png`. `python3 scripts/run-ios.py` installs and launches the app with bounded subprocess timeouts; it supports this Xcode installation's DeviceHub interface. No additional official-server probes were sent.

## Mobile login milestone — 2026-10-04

At the user's request, the iPhone login screen now displays disabled username/password fields and sign-in, with an independent credential-free connection check. A single explicitly initiated check from the iOS 27 simulator returned reply code 3, 51 bytes, in 416 ms. The screen correctly showed “Server responded · connection rejected” and the same printable video URL observed by the CLI. Screenshot: `artifacts/ios-login-response.png`. No credentials, login packet or follow-up packet was sent. This is the third live initialization check recorded in this investigation, and the first from the mobile app. It proves mobile transport and response handling, not successful initialization or authentication.

The development launch argument `--check-server-once` initiates one check when the view appears; ordinary launches do not contact the server automatically. The visible button remains subject to a one-minute in-process cooldown and each request has a 12-second cancellation timeout.

## Corrected current initialization — 2026-10-05

Static inspection of official 0.4.57c revealed a missing three-byte field in our request. The initial field is randomized, and the actual challenge follows marker 113. See [address-level evidence](INIT-0457.md). A single CLI check with this correction returned reply code 2 and 15 bytes at `2026-10-05T07:41:26Z`, using the existing installation ID and official profile. The initial diagnostic called it a challenge mismatch, but further inspection showed the current client stores that field as session data without comparing the classic hash. The parser now reports `init-received`, explicitly short of an established session.

Current validation: 64 offline protocol assertions, two asset tests, iOS native build with zero warnings/errors. No credentials or login packets have been sent. Connection/Accept and a single pre-login synchronization reply are implemented. Persistent keepalive and authentication remain unfinished.

The updated iPhone simulator app was then checked once: code 2, 15 bytes, 359 ms, with “initialization received” visually verified in `artifacts/ios-init-received.png`. These two corrected-layout attempts bring the recorded total to five initialization checks. Both used existing installation identities; neither sent credentials or a Connection/Accept packet.

### October 5: iPhone pre-login synchronization

The iPhone simulator completed initialization and Connection/Accept against the current official server, received decoded Login/Syn4 (family 3, action 244; three bytes), and transmitted the official-format `k` reply. The screen reported 742 ms and was visually checked in `artifacts/ios-synchronization.png`. The check disconnects afterward; server acceptance of the synchronization reply, persistent sessions, authentication and gameplay remain unverified. No credentials were used. iOS build: zero warnings/errors; 64 offline protocol assertions and two asset tests passed.

### October 5: sustained official-server observation

At 13:47 UTC (06:47 Pacific), the CLI kept one official-server connection open for the 45-second observation window, answered 47 Login/Syn4 requests and one Connection/Ping, and received ongoing traffic. Sanitized result: `artifacts/session-watch.json`. This supports the current framing and keepalive implementation; account authentication and gameplay remain unimplemented. The iPhone check now uses this flow with progress updates.

### Offline session regression coverage

The network exchange is now exercised through a fragmented duplex stream fixture, with no sockets or credentials. Checks cover initialization rejection, acknowledgement, synchronization, keepalive sequence updates, EOF after replies, unknown packets, and invalid keepalive parameters. Oversized four-byte EO numbers now produce a handled protocol error rather than an unhandled overflow. Total: 64 protocol assertions plus two asset tests. Authentication is still not enabled: the old XML Login/Request definition has not been matched to the current executable's credential submission path.

### Local official-client runtime investigation

Downloaded Gcenx Wine 11.18 into `.local/wine-runtime`; its SHA-256 matched the GitHub release digest (`aa0ea4c82e636ae7bca2076387cb0a5affa26509ad13f119ecd0d62bd7ba6f82`). Wine reports its version and launches `Endless.exe` after prefix initialization. The separate `.local/official-lab/config/setup.ini` points the primary host and both fallback hosts to `127.0.0.1`, port 18078. No credentials have been submitted.

Current blocker: native UI automation does not expose the running Wine/Endless process as an application, so the login UI cannot yet be inspected or operated through that tool. The Wine log also reports `GL_INVALID_FRAMEBUFFER_OPERATION`; this does not by itself establish whether the game window is usable. Need a human observation of whether the official client window is visible before selecting the next runtime fix. Logs: `artifacts/wine-client-retry.log`. Current authentication packet encoding remains unverified.

### Loopback login lab

`scripts/login-lab.py` binds only 127.0.0.1:18078, accepts at most five connections and exits within ten minutes. It responds to current INIT, validates Connection/Accept, and sends the known synchronization request. It logs packet identifiers, lengths and sequence numbers only; it does not persist arbitrary payloads or forward traffic. A fragmented in-memory exchange verified the initialization/acknowledgement/synchronization path. The official Wine UI cannot be operated through the available native UI tool, so a user click on Play Game is needed to validate the real client against this lab. Use dummy credentials only.


### Native sign-in test after loopback verification

The official Windows client matched the locally recovered dummy credential encoder exactly (45 payload bytes, action 35 / family 2). Native iOS now offers an explicit, single-attempt sign-in test with a 30-second overall timeout, synchronization and keepalive handling, no automatic retries, and no credential/payload logging. It reports only response opcode and length and closes the test connection. This does not yet confirm authentication or support character selection/world entry. Real-account verification is pending user entry in the simulator. Offline validation: 74 protocol assertions and two asset tests.


### Initialization crash fix

The simulator crash at 23:41:36 occurred in Handshake.Parse before acknowledgement or credentials were sent: the first frame was not an unencoded initialization reply. InvalidDataException was missing from the UIKit event-handler exception filters (it does not derive from IOException). Both handlers now catch it explicitly; sign-in also converts an invalid initial reply to a recoverable result and clears credential buffers. The triggering packet contents were not retained, so the server-side cause is unknown. Regression tests cover this response and verify no credentials are transmitted.


### Native world rendering — October 7

Added original-art hats/weapons/shields and hat color-key sampling at (1,32), full-height hat rendering, current NPC catalogs (normal records 85 bytes, reserved records 3 bytes), NPC appearances (13/220), and batched NPC movement (21/220). NPCs share scenery/player depth ordering. A bounded live test confirmed world entry, three players, seven NPCs, NPC/player movement, and a step with return. No authenticated packet bodies or credentials were retained.

Map blend artwork now uses full sprite dimensions and approximate 50% opacity instead of opaque cropped shapes. Terrain resources with four horizontal 64-pixel frames animate at 250 ms intervals; original-art offline checks verified distinct frames and wraparound. The iPhone build including this timer completed with zero warnings/errors.

Further executable inspection identified the own-movement third byte as a visibility mask (0x50cb7a, 0x50ccc6), not direction. The client now consumes the two delimited short-ID lists, removes entities only when their mask bit is set, preserves the own player, and derives facing from the requested or confirmed move. Malformed visibility batches do not partially mutate state. 162 offline protocol/graphics assertions passed after this correction.

The final live retest and iPhone rebuild after the visibility correction were not executed: automatic approval review hit its usage limit. Earlier iPhone build and live results above precede that correction. Full rendering remains incomplete: walking/attack animations, effects, drops, warps/map downloads, special equipment and exact scenery placement still need work. Mac GUI integration also remains outstanding.
