# Endless Apple — running implementation TODO

## Latest feedback — camera dragging and button consistency

- [x] Omit the decorative sword rather than crowd the controls with a vertical version.
- [x] Darken Inventory ink using the same transparent-background glyph extraction as Quests.
- [x] Use the shared amber button for Paperdoll. Conservatively refine its original bitmap lettering with image generation; retain its slant, cream fill, dark outline and pixel-style edges instead of substituting a different title font.
- [x] Drag the world to offset the camera independently of movement. Attack restores centered follow; taps use the displayed camera and dragging does not trigger taps. Pinch zoom and drawers preserve the camera offset.
- [x] Simulator pan/recenter, zoom, held attack and independent fresh screenshot review.

Verification: 446 offline assertions and two asset-tool tests pass; simulator build zero warnings/errors. Three UI tests pass, plus a separate pan/recenter run verifying changed rendered pixels. Second-pass panned and centered screenshots inspected at `artifacts/ui-checkpoints/2026-10-09-pan/`. Physical-phone retest remains pending; no phone install/signing changes.


## Latest feedback — Old Choppy, quest glyph and HUD wood

- [x] Reproduce Old Choppy (item 1065, graphic 11, subtype 15): original strike resources 1014/1016/1017 are empty. Retain the available wind-up weapon when all strike parts are blank; otherwise preserve normal split-layer rendering. This is a missing-art fallback, not verified thrown-weapon/projectile behavior.
- [x] Darken the original quest scroll/pen glyph and include its complete bounds.
- [x] Restore visible classic curved corner pieces beside the system status area. Use quiet original amber panel material for broad top/bottom fills; keep perimeter strips on edges only.
- [x] Reuse the desktop WeaponSpriteType source directly for weapon pose constants. Cache resource visibility and decoded resource lookup; replace renderer front-removal with a cursor and reuse one scene camera.
- [x] Final simulator interaction tests and independent second-pass screenshot review of these revisions.
- [ ] Retest Old Choppy on the physical phone after the next requested install. Some current weapons, including Lasergun, have no visible standing resource in the presently mapped archive; they are excluded explicitly from fallback coverage and need separate format/art investigation.

- [x] Add the original horizontal sword as a small framed ornament in the existing bottom safe-area band; preserve world/control geometry. No image generation needed.

Verification: zero-warning/error simulator build; held attack/release, inventory/viewport stability and zoom/control XCTest checks all pass (3/3). Re-rendered after the second review caught the quest crop/background and wood-material issues. Screenshots: `artifacts/ui-checkpoints/2026-10-09-choppy/`. 439 offline assertions plus two asset-tool tests pass. Optional original-asset regression adds 1,868 pose checks across both genders, all four directions and both phases for weapon graphics with visible standing artwork. Old Choppy contact sheets inspect standing/wind-up/strike. No physical-phone installation or signing changes.


When asked to “continue”, work through the unchecked priorities below, preserving the current official-server target and native iPhone architecture. Update this file with completed work, verification, and concrete blockers. Do not call unfinished features complete. Do not reinstall on the physical phone until Jeffrey asks for the next upload/retest.

## Latest feedback — October 9, 4:01 PM

- [x] Replace repeating top perimeter-strip fill with one broad original wood-panel texture.
- [x] Restore front melee weapon strike part, animated back-item resources, and separate ranged shooting/recovery poses.
- [x] Add bounded 1–2.5× pinch zoom centered on the character, preserving HUD geometry and displayed-camera tile picking.
- [x] Reduce View Credits width by about 12%, retaining a 44pt touch target.
- [x] Separate attack enabled state from movement pending, avoiding the false pressed appearance while walking.
- [x] Center Paperdoll above Quests with original quest glyph, tall Attack to their right. Quest journal data remains unimplemented; show an explicit unavailable state.
- [x] Final interaction, equipped-pose, and separate screenshot review.

Verification: build zero warnings/errors; 427 offline assertions + two asset checks; four XCTest cases passed, including pinch gesture and held attack. Second-pass login/HUD screenshots and equipped melee/shield + bow/quiver contact sheets reviewed. Screenshots: `artifacts/ui-checkpoints/2026-10-09-1600/`. Current ranged visual rules adapted from desktop source; projectile/hit behavior and physical-phone retest remain unverified.

- [ ] Prioritize shared desktop-engine reuse before expanding duplicated gameplay systems; see `docs/ENGINE-REUSE.md`. No engine migration is claimed complete.

## Latest feedback — October 9, 3:40 PM

Supersedes the previous splash-seam and HUD-corner completion claims.

- [x] Remove the painted bottom wood from the stretched login/selection background and draw the complete foreground band once over the splash.
- [x] Use original desktop Paperdoll lettering and double-rim button art; lay it above a larger attack button beside the D-pad. Remove the unused Use-item button.
- [x] Fill the top system-status area with wood; anchor rounded corners at the screen top, not below the safe area/stat bars.
- [x] Final build, UI interaction checks, and separate second-pass screenshots of login, selection and HUD.

Verification: zero-warning/error simulator build; four selected XCTest cases passed (character navigation, held attack/release, drawer selection/stable viewport, progress above credentials). Second visual pass found and fixed Paperdoll title padding covering its rim; final lettering crop rebuilt and re-rendered. Screenshots: `artifacts/ui-checkpoints/2026-10-09-1540/`. No phone install, signing changes, live account actions, or new image generation.

## Latest feedback — October 9, 3:05 PM

- [x] Remove Forget Account and Check Connection controls; retain Remember me removal behavior and connection progress above credentials/roster.
- [x] Place splash pixels underneath the bottom wood on both screens; fix the remaining visible gap.
- [x] Correct equipped melee attack layer alignment and review all directions/frames.
- [x] Keep open HUD tabs visibly pressed; tap again to close.
- [x] Replace hide control with Players, correct Inventory glyph, and add Paperdoll in the control gap. Distinguish nearby players from an unimplemented server-wide online list.
- [x] Reuse original curved desktop HUD corners within the iPhone safe frame.
- [x] Second-pass screenshot and interaction verification, including status/error states.

Verification: simulator build has zero warnings/errors; 375 offline assertions pass. Main iPhone UI suite: five passed, one explicit software-keyboard skip. Compact-phone final layout suite: three passed. Screenshot review caught and corrected both the painted-background wood band covering/layering issue and a mismatched top-frame strip; final Paperdoll uses the original character-head icon rather than duplicating Interact. Equipped pose contact sheets cover both genders, all four directions, standing/wind-up/strike using original graphics.

Remaining limits: Players currently lists nearby world actors, not the server-wide online roster. Paperdoll is read-only; equip/unequip remains pending. Physical-phone keyboard/AutoFill and melee motion re-test remain required; ranged/special attack behavior is not claimed complete. No live attack, physical-phone install, signing change, or new image generation in this pass.

## Follow-up polish — October 9, 12:50 feedback

- [x] Remove the login panel's stretched/discolored inner band; equal-size supporting plaques.
- [x] Center splash by visible pixels, bottom-align under the wood on both screens; enlarge selection artwork.
- [x] Move selection arrows beside the counter, enlarge/lower the card.
- [x] Replace repeated perimeter texture in HUD interiors with quieter original amber material and navigation plaques; use a compact desktop combat icon.
- [x] Conduct a separate second-pass rendered review before reporting completion (required by AGENTS.md).
- [ ] Low priority: consider higher-definition matching title/action lettering across the UI. Existing Check Connection is liked; defer further image generation until cost/usage is understood and the work is prioritized.

## Continuous implementation and second-pass review — October 9 afternoon

- Login/selection: cleaned the panel interior, matched supporting plaque sizes, trimmed splash padding, and bottom-anchored art behind the wood. The actual controls reserve illustration space after stack layout, including on the compact iPhone; a regression test caught and then verified the Back-button overlap fix.
- HUD: original amber navigation-plaque perimeter, quiet panel materials, larger readable icons, and a compact original combat glyph. Expanded inventory now has more room without changing the world projection or fixed controls.
- Inventory: original gfx026 sprites, real quantities and weight, scrolling, selection, and 1–2-column × 1–4-row footprints from current item metadata. All 1,501 nonreserved item records have matching original inventory artwork. Equip/use/drop and incremental mutations remain unimplemented.
- Stats drawer now displays server-derived HP/TP and carrying weight. Stamina and experience are explicitly unavailable; no invented full/empty values.
- Added incoming player facing (21/204) from the original handler. 351 offline protocol/graphics assertions pass, including four directions, unchanged position, invalid/truncated rejection and unknown-actor preservation.
- One authorized bounded headless live session entered the world, received 45 inventory entries, sent a blocked-tile turn and retained the same position/connection. This proves a successful write/local turn and continued session, not another client's observation of that facing. No attacks or item actions were sent.
- World frames now display directly from RGBA rather than encoding/decoding PNG every frame. Native-vs-PNG color/orientation comparison passed. Simulator conversion microbenchmark: ten synthetic 320×480 frames took 626.5 ms through PNG versus 4.2 ms directly; this is not an end-to-end FPS measurement.
- Movement/attack repeat timers now use common run-loop modes so touch tracking cannot suspend them. The actual Xcode touch test held attack for 1.6 seconds, observed repeated requests, and verified the count stayed unchanged after release.
- Final simulator build: zero warnings/errors. Final Xcode suite: four tests passed, one explicitly skipped (software keyboard), zero failures. Added a standalone Xcode UI test runner for the installed simulator app. Compact-phone inventory selection/scrolling, stable viewport/control positions, character navigation and non-overlapping illustration passed. Synthetic username/password entry passed, including the expected masked character count. Software keyboard remained off-screen in this environment: the dedicated software-keyboard check must skip explicitly, and physical-phone keyboard/AutoFill remains required.
- The DEBUG hold self-check initially allowed only 750 ms against a 620 ms timer while multi-size preview rendering was pending. Matching the real-touch test’s 1.6-second bounded window resolved that test-timing failure. Final isolated run passed attack repeat/release/cancel/lifecycle stop, movement cadence/confirmation gating/release, and stable world/control geometry at three sizes across all drawers. Production behavior was independently covered by the passing real-touch XCTest. The first strict selection test also identified the stack-layout race, which was fixed and re-tested successfully.
- Second rendered review inspected final login, selection, inventory and stats on the main iPhone and compact-phone login/selection/inventory. Saved synthetic screenshots in `artifacts/ui-checkpoints/2026-10-09-polish/` (ignored by Git). Inventory captures are deliberately scrolled; clipping at the scroll viewport is expected. Secure-field screenshots hide text; masked length was verified through XCTest, but keyboard/AutoFill remains unverified.
- No physical-phone installation, certificate/signing change, real-account credential storage change, or new image generation in this continuation.

## Latest UI revision — centered selector and framing (October 9, 12:20 feedback)

This supersedes the earlier right-column/Login+Delete layout and earlier checked roster-polish claim.

- [x] Single centered original-proportion character card with previous/next arrows; remove Delete.
- [x] Smaller centered selection splash below the card, inside the frame.
- [x] Login Remember me left, Connect right, extra field/frame padding.
- [x] Custom matching Check Connection bitmap lettering on original wood plaque.
- [x] Restore tiled original wood around the screen and control region, keeping the control center transparent.
- [x] Inspect desktop source and update shared/login/selection/HUD guides: [implementation study](docs/DESKTOP-UI-IMPLEMENTATION.md).
- [x] Inspect login, selection and HUD simulator previews; build passes with zero warnings/errors. Carousel checks pass at three widths; HUD checks pass for stable projection/controls across all drawers, held attack cancellation, and bounded movement repeats. Saved screenshots: artifacts/ui-checkpoints/2026-10-09-carousel. Synthetic preview evidence only, not new live-game verification.
- [ ] Software keyboard/AutoFill and physical-iPhone review remain required; simulator geometry checks do not cover these.

## UI correction pass (2026-10-09, latest user review)

- [x] Replace Georgia/system text in the implemented login, roster and HUD with original bitmap words, desktop pixel body text and an outlined decorative fallback for unrecovered labels. New gameplay dialogs must use the same typography.
- [x] One continuous original login/selection scene, including tiled wood safe areas; no separate logo-banner patch.
- [x] Roster: original crisp Character caption, independent non-overlapping name/action regions, platform/feet sharing one anchor, desktop pixel name/level type. Inspected three synthetic characters including a long name.
- [x] Use original brighter orange/honey classic wood across login, selection and HUD; retain updated status sprites.
- [ ] Recheck small-phone, keyboard, long-name and multiple-character layouts after these changes.


## UI design references

Before UI work, follow [project instructions](AGENTS.md), then read [shared UI style](docs/UI-STYLE.md) and the applicable [HUD](docs/HUD-STYLE.md), [login](docs/LOGIN-STYLE.md), or [character-selection](docs/CHARACTER-SELECT-STYLE.md) guide. These define the intended design; this TODO tracks completion. Save and document reference images in [docs/references](docs/references/README.md). Latest user feedback overrides older guide text and should be reflected back into the guides.

## Current priority: iPhone only

User clarification (2026-10-09): remembered credentials are for the iPhone. Mac client development, builds, and Mac-specific testing are deferred and are not part of this TODO. Continue iPhone gameplay, UI, and saved-login work only.

## Latest phone feedback (2026-10-09) — next implementation priorities

These requests supersede earlier visual/interaction choices where they conflict. Earlier checked items describe initial implementation, not acceptance of the current phone experience. Preserve iPhone auto-login and keep Mac work deferred.

Reference: [desktop character-selection screen](docs/references/desktop-character-selection.png). Saved from the user's attachment, unchanged. Consult this image during implementation; source production artwork from the original desktop resources.

### Additional mobile-port feedback

- [x] Replace plain D-pad arrows/rounded keys with crisp wood-panel keys matching desktop chrome. Custom matching pixel art is now authorized for UI cosmetics; original assets remain preferred.
- [ ] Restore the missing equipped-weapon slash in attack animation; verify original weapon frame selection, offsets and layering in all directions. A slash is visual feedback, not proof of damage.

### Gameplay viewport and controls

- [x] Opening or closing inventory, chat, skills, stats or other HUD panels must not change the camera angle, zoom, character screen position or world placement. Panels should reveal/overlay UI without reframing the map. Verify both opening and closing each panel.
- [x] Hold the attack button for repeated attacks at the permitted attack cadence. Stop immediately on release, touch cancellation, leaving the world or backgrounding; do not keep a queued attack loop running.
- [ ] Smooth sustained walking: the walking animation currently feels too fast, while repeated movement in one direction feels staccato. Coordinate frame timing, interpolation and repeat cadence to maintain continuous movement, preserving server authority and collision handling.
- [x] Remove the unintended dark-brown rectangle beneath the D-pad/attack controls while preserving the intended desktop wood frame and button art.
- [x] Add a subtle indicator on a tapped destination tile, aligned with the isometric map and camera. Clear/update it appropriately when the destination changes, is reached, is canceled or is unreachable.
- [ ] Tapping an NPC or interactable tile should select the appropriate interaction: NPC dialogue, chest opening, billboard reading, berry/resource gathering, etc. Route into interaction range when necessary and dispatch the correct verified action; do not treat every target as ordinary walking.
- [ ] Direction input must turn the character even when movement is blocked by a wall, NPC, player or other barrier. Apply in all four directions: e.g. facing left + blocked up input should face up without changing tile. Keep facing synchronized with the server.

### Login and character-selection presentation

- [x] Remove timed splash-art rotation. Select/change the foreground splash only when the login page is freshly entered (including returning from character selection) or the app is reopened. Keep it stable while the user is on that page.
- [ ] Clean up both screens into a cohesive portrait adaptation of the desktop UI, using actual original textures, frames and button artwork.
- [x] Make the Endless Online logo flush with the background; integrated classic scene replaces the mismatched rectangular banner.
- [ ] Position splash art and controls so buttons/login sections do not cover the focal character artwork.
- [x] Remove the unexplained gameplay-map preview from login and character selection; show the world only after explicit world entry.
- [x] Use matching classic wood plaques for Credits/Connect/Check Connection, original word art where available and the shared outlined title fallback elsewhere. Preserve bitmap lettering proportions.
- [x] Left-aligned supporting login actions. Earlier right-column roster superseded by the centered carousel above.
- [x] Rebuild the card using the original desktop panel, equipped preview, level/name and Login. Latest request removes Delete and shows one centered card with navigation arrows.
- [x] Keep visual references under docs/references and link them here so future implementation/review does not depend on temporary clipboard paths.
- [ ] Verify the revised screens with representative character counts, small-phone dimensions, keyboard shown/hidden and auto-login. Compare against the saved desktop reference before marking the presentation complete.

## Phone feedback (2026-10-08)

- [ ] Desktop-style login flow using actual desktop resources, adapted to portrait.
  - [x] Always-visible username/password panel at the top; no Play Game or Exit Game button.
  - [ ] Create Account and View Credits (Check Connection removed by latest feedback).
  - [x] Original background and foreground splash artwork loaded. Timed rotation is superseded by the page-entry-only requirement above.
  - [x] Separate character-selection screen, wood-framed character previews and original login button artwork. Appearance fields verified against live world entry; layout polish remains.
  - [ ] Verify keyboard, small-screen layout, loading/error states, and back/disconnect behavior.
- [ ] iPhone auto-login (opt-in saved credentials; added 2026-10-09).
  - [x] iPhone: opt-in “Remember me” checkbox; keep the default off until explicitly selected.
  - [x] iPhone: store credentials in device-only, unlocked Keychain storage, never plaintext preferences, repository files, backups, or diagnostics. Save only after server acceptance.
  - [x] iPhone implementation: if opted in, restore the username and authenticate on launch, going directly to character selection after server acceptance. Do not automatically enter the world.
  - [x] Show a cancellable signing-in state; return to login on rejected credentials or network failure, without retry loops.
  - [x] Clear prior roster on account switching; latest feedback removes Forget Account, retaining credential removal by unchecking Remember me.
  - [ ] Verify first launch, remembered login, invalid/changed password, unavailable network, cancellation, and credential removal.
- [x] Replace joystick with an X-shaped, isometric four-way D-pad; hold-to-walk and pressed feedback. Implemented; sustained phone interaction remains in verification.
- [x] Tap a map tile to walk there using bounded pathfinding, dynamic collision checks, and server-confirmed steps. Manual movement cancels the route. Includes no-progress timeout; end-to-end phone route testing remains.
- [ ] Implement actual attack command and original attack animation; fix attack/control overlap at supported sizes.
- [x] Improve collision with walls, players, and NPCs; never move through an occupied destination locally. Authoritative positions and destination checks implemented; verify crowded movement on phone.
- [ ] Build/test these changes and report what is verified; wait for user to request a phone update.

## Outstanding gameplay integration

- [ ] Current stamina and EXP/TNL values and updates in HUD (HP/TP appearance values already decoded).
- [ ] Complete mobs/NPC lifecycle: new appearances, movement, disappearance, combat and respawn.
  - [x] Request appearances for newly visible player/NPC IDs after walking (official 1/218; offline tested).
- [ ] Doors/portals and server-confirmed map transitions, cache validation and scene reset.
- [ ] Chests: open and display server contents; item actions only on explicit user input.
- [ ] Resource interactions, including trees, with appropriate tool checks and original animations.
- [ ] Real inventory and equip/unequip actions with server-confirmed appearance/stat updates.
- [ ] Real chat and mobile HUD panels.
- [ ] Continue full world rendering coverage (combat, effects, ground items, pets, special poses).

## Constraints and testing

- Use current official Endless Online, original desktop graphics, and native rendering. No Wine dependency.
- Use the user's authorized local credentials file for bounded headless tests; never print or commit credentials or authenticated raw payloads.
- Preserve wood textures, large icons, default-minimized menus, camera following the player, and the compact original desktop combat glyph (the screenshot sword requirement is superseded).
- Phone install works; native device build and signing configured locally. App icon uses original desktop crescent/E artwork.
- Gameplay was paused for phone setup; the latest feedback request resumes implementation only, not phone deployment.

## Historical implementation checkpoints (read latest status above first)

- 191 offline protocol/graphics assertions pass, including route detours, unreachable/out-of-bounds targets, inverse camera projection, and player/NPC occupancy.
- X-pad and tap-to-route are wired into the iPhone world view. Routes re-plan after confirmed steps; manual movement cancels routing. Live route acceptance and sustained movement still need verification.
- Attack and movement hit regions are separated. Basic attack now sends the command traced from the desktop Ctrl/space path; live combat acceptance and original animation remain unverified. Do not claim combat is fixed.
- Original gfx001 background and four rotating splash illustrations are bundled. Login and character selection now switch between distinct UI states, with original wood resources. This is an initial implementation: finish original button artwork, Create Account/Credits, real character previews, keyboard handling, and layout checks before marking the login task complete.
- Simulator build passed with zero warnings/errors before the latest panel-layout correction. Simulator inspection caught a stretching/layout issue; panel bounds now use constraints and explicit stretch mode. Recheck visually.
- No physical-phone deployment of this feedback work has been performed.

### Remembered sign-in checkpoint

- Added optional iPhone Keychain storage, one launch-time sign-in attempt, Cancel, and Forget Account. Returning to login retains the saved account; unchecking Remember me or choosing Forget Account removes it. Character selection still requires an explicit choice before world entry.
- Fixed ignored login return results that left the UI saying “Signing in…” after a server response ended the attempt. Credential fields are disabled during a sign-in; transient network failures do not trigger retries.
- Keychain simulator checks passed for missing account, save/read, replacement, removal and repeated removal, using a separate synthetic test service. DEBUG launch argument: `--keychain-self-test`. No real credentials are used by this test.
- Credential copies needed for opt-in saving are cleared after acceptance or failure. The transport clears its own credentials as soon as it sends them. Keychain payload serialization uses generated metadata for trimmed device builds.
- Latest simulator build: zero warnings/errors. Existing 191 offline assertions pass. Added keyboard-aware scroll bounds; interactive keyboard testing and the complete real-server remembered-login failure/cancellation matrix remain unverified.
- Visual check confirms the wood panel stays within its bounds. Oversized/cropped background logo and duplicate text title still need fixing; this is not the finished desktop-style login layout.
- No physical-phone installation performed.

### Native client and protocol checkpoint (2026-10-09)

- 201 offline assertions pass. Added character-selection appearance field/legacy-layout checks, atomic NPC removal checks, and attack wire-format/direction/pre-entry checks.
- Live official-server world entry passed. Selection appearance matches in-world gender, skin, hair style/color and all equipment fields. No account details or raw authenticated packets retained in diagnostics.
- Live bounded walk test confirmed one step and a return to the starting tile; both were server-confirmed. Sustained D-pad and tap-path phone testing remains.
- Character cards now render equipped portraits from original body/equipment art. Original logo, Connect and Credits button images replace generic text; oversized duplicate background logo removed. Create Account flow and matching Check Connection styling remain unfinished.
- Added current-client NPC disappearance (27/220) with whole-packet validation before removal. Dead/removed NPC occupancy no longer persists for this packet type. Damage/death/respawn coverage remains incomplete.
- Basic attack uses official Ctrl/space call chain 0x47ced2 -> 0x4e4588 -> 0x4e45a0 -> 0x4fdc9c: action 35/family 102, direction char. Queued through the single session writer, blocked before entry/during pending movement/when HP is zero, and throttled to 600 ms. Tapping attack cancels the tap route. This does not yet implement animation or prove server combat acceptance.
- Prior Mac work was an unintended scope expansion and is deferred. Its synthetic Keychain check failed; no further Mac work is required for this iPhone TODO. Existing files are retained, not treated as an active deliverable.
- Physical phone has not been updated.

### iPhone-only checkpoint (2026-10-09, continued)

- Auto-login is explicitly labeled “Auto-login on this iPhone.” It restores an opted-in account on launch and stops at character selection. No Mac changes or tests in this continuation.
- Fixed temporary inactive-state interruptions: AutoFill/system overlays no longer cancel the connection; entering the background still disconnects. Keyboard Next moves to password; Go submits sign-in.
- Latest iPhone simulator Keychain self-test passed missing/save/read/replace/remove/repeated-remove using a separate synthetic account. Full real-account relaunch and failure/cancellation UI matrix remains to verify.
- Added original two-frame melee body attack animation (gfx011/3, 24x58 crops, current executable 0x6ea348), with equipment attack frames. Starts after the attack command is written, not as a claim of server-confirmed damage. Ranged/special attacks, damage feedback and exact equipment placement still need visual/live comparison.
- One bounded live attack test entered the official world, sent one basic attack command, and remained connected during observation. No hit/damage confirmation was established.
- Movement confirmation now times out after five seconds instead of leaving controls permanently pending. Tap routes stop after three seconds without progress rather than retrying indefinitely.
- 217 offline assertions pass. Additional installed-asset coverage: 441 maps rendered, 8,563 referenced scenery resources checked, 9,222 total checks passed.
- iPhone simulator build passed with zero warnings/errors after the bitmap-button aspect-ratio correction. Original Connect/Credits bitmap lettering now uses an image rather than stretching across the button background.
- Remaining work is still unchecked above: Create Account flow, final layout verification, full auto-login lifecycle verification, complete combat/NPC lifecycle, SP/EXP, transitions, inventory/equipment, resources/chests, chat, effects/items/pets. Do not mark these complete based on build success or asset coverage.
- No physical-phone installation performed.

- Added server basic-attack broadcasts (21/102), validated atomically before updating actor facing and animation; unsupported skill poses are not treated as basic melee. Offline suite now passes 221 assertions.
- Protocol research continuation: main current packet dispatcher starts at 0x508454; family byte is [ebp+0x10], action [ebp+0x0c]. Attack broadcast handler 0x510060..0x5103cc consumes four-byte records (player short, facing char, attack-kind char). Family 245 is jukebox-related, not proof of combat acceptance. Family 100 action 241 at 0x520fbc parses item cooldowns, not inventory contents. Do not mislabel these initial packets as stats or inventory.

### Read-only inventory integration

- Recovered 241/226 inventory snapshot handler 0x519091 -> 0x5a085c: weight/capacity shorts, followed by 14-byte entries (10-byte descriptor with item-ID short, 4-byte quantity). Added bounded snapshot parser and atomic state replacement.
- Bag now displays names from the installed item catalog, quantities and carrying weight. It remains minimized by default. Item icons, descriptor modifiers, incremental mutations and equip/use actions remain unfinished.
- Unsupported snapshot layouts preserve prior contents and report inventory unavailable without disconnecting the world session.
- Live inventory verification attempted once but timed out before confirmation; do not claim live inventory works yet. No additional retries performed.
- Offline suite passes 227 assertions. iPhone Keychain simulator self-test passed. Full saved-account launch/error/cancellation matrix remains pending. No Mac work or physical-phone deployment performed.

### Requested phone update (2026-10-09)

- User explicitly requested a phone update for progress review. Latest ios-arm64 build succeeded with zero warnings/errors and installed on the paired iPhone using the existing development certificate/profile. No certificates or profiles were created or revoked in this update.
- Added `Update iPhone.command` and `scripts/update-iphone.sh` to repeat build/install/launch using existing signing assets. Connect and unlock the phone before running. These scripts do not manage or renew signing assets.
- This installation is a progress build, not completion of the remaining gameplay TODOs.

### Expanded desktop style references (2026-10-09)

- Saved all ten additional user screenshots unchanged under docs/references, alongside the existing character-selection reference. The reference index links each capture and explains its role.
- Shared/HUD/login/character-selection guides now distinguish observed classic and updated visual families, the user's chosen combination, reusable frame/well/button/dialog patterns, and mobile adaptations. Reference screenshots do not override explicit phone requirements or prove implementation completion.

### Gameplay and cohesive design contract (2026-10-09)

- Added docs/GAMEPLAY.md for session/world entry, movement/facing, target intent, combat/entity lifecycle, transitions, inventory/equipment and interaction verification. Requirements remain distinct from implementation completion.
- Added timestamped notes for the user-linked 1:52 Nostalgia clip; sampled presentation only, not official-server mechanics or measured cadence.
- Refined shared depth/attention/state rules and login/roster alignment. AGENTS.md now requires the gameplay guide for behavioral changes as well as UI work. No gameplay fixes or phone deployment in this documentation checkpoint.

### Stable HUD and input checkpoint (2026-10-09)

- Drawer opening/closing now overlays a fixed world viewport. Simulator regression checks passed Bag/Chat/Skills/Stats open/close at 320×568, 393×852 and the running phone size: world frame, render dimensions and controls remain unchanged. Chat keyboard behavior is still unverified because chat is not connected.
- Held sword attack repeats at 620 ms, above the existing 600 ms session gate; release, drag exit, cancellation, view removal and background/connection cancellation stop the timer. Simulator event checks passed repeat, release, touch cancellation and lifecycle stop. This verifies input dispatch, not live server damage or official timing accuracy.
- Removed the extra framed dark panel below controls; original surrounding wood remains. Inspected minimized and expanded simulator screenshots; controls do not overlap.
- Added a subtle gold destination bracket overlay projected through the displayed camera. Routes clear it on manual movement/attack, cancellation, arrival or failure. This is drawn feedback informed by the reference; it is not claimed to be a recovered original sprite. Live routing remains to verify.
- Removed timed splash rotation; it selects on creation and changes on return to login. Hid the leftover world preview on world-entry setup/return so it cannot leak into login/selection. Full login/character-selection visual redesign is still pending.
- Simulator build: zero warnings/errors. Existing offline suite: 227 assertions passed. Added DEBUG --hud-preview --hud-self-test for the above layout/input regressions without credentials or network. Preview uses synthetic actor/marker data.
- No physical-phone installation. Remaining major work includes sustained walking/blocked facing, actual interactions/transitions, SP/EXP, inventory actions, chat, combat lifecycle and login/selection polish. These remain unchecked.

- Follow-on: recovered independent facing command from the official executable (21/204, one direction char). Blocked movement now sends a turn without walking or waiting for a movement confirmation, suppressing repeated identical facing requests. Four-direction wire fixtures and invalid-input checks added; phone/live-server facing verification remains unchecked.

- Final validation after facing integration: 233 offline assertions passed; iPhone simulator build succeeded with zero warnings/errors. The earlier HUD interaction checks passed on the preceding UI-identical build.

### Movement, slash and desktop roster checkpoint (2026-10-09)

- Movement implementation: held controls wake promptly when confirmation permits, use a 250 ms minimum step interval, and never bypass pending confirmation. Visual interpolation adapts to recent confirmed-step intervals (bounded 200–400 ms), and the walking cycle continues across adjacent steps instead of resetting each tile. This improves cadence without predicting movement; exact desktop timing and live phone feel still need comparison.
- Simulator hold tests passed sustained repeat bounds, pending-confirmation gating, and release cancellation. Prior stable-viewport and held-attack checks still pass at three sizes. Offline suite now has 241 passing assertions, including continuous walk phase/position and directional slash crops. Build has zero warnings/errors.
- D-pad now uses original framed wood-button artwork plus custom cream/gold pixel arrows. Updated AGENTS/shared style to reflect the user's permission for matching custom UI cosmetics. Original source assets remain preferred.
- Added weapon slash overlay from current gfx003/8 (232×522, four directions × nine rows), relocated from the legacy gfx002/40 source. Uses existing upstream weapon metadata mappings for recognized shared melee graphics; no slash for bare hands/ranged/unknown graphics. Inspected a synthetic Small Sword slash in the simulator. Current-only weapon mappings and all live equipment variants remain to verify, so the broad slash TODO stays open.
- Login now integrates the original full background/logo, has coherent matching button styling and square recessed credential fields, with stable lower splash art. Character selection uses original gfx001/11 cards in the user-selected right column, artwork visible on the left, separate portrait/name/Login/Delete regions, and a level label from the current roster's two-byte field at offset 4. Unknown legacy level remains a dash. Delete is explicitly disabled while unsupported.
- Inspected synthetic three-character selection with a long name, login, and HUD/slash simulator screenshots. Added --login-preview, --selection-preview and --attack-preview for credential-free review. Small-phone keyboard/AutoFill and live account switching remain in verification; Create Account/Delete/Password commands remain unfinished.
- Movement TODO remains open for live turning/collision/latency acceptance rather than treating simulator evidence as final phone approval. No phone installation in this checkpoint.

### October 9 typography/material correction and verification

- Located the exact classic art in the existing EndlessClient binary package. Login and selection now use its integrated scene, original Connect/View Credits/Character/Login/Delete/Back lettering and warm wood. Recovered words retain aspect ratio. Native dynamic labels use an outlined Carter One fallback; this is an approximation, not original EO lettering.
- Converted the upstream un-smoothed `sans_11px` bitmap atlas to a local native outline font for credential input, names, levels, HUD values and body text. Font source/conversion and bootstrap are documented; all generated/downloaded fonts remain local, with the fallback's license bundled.
- Roster platform and body feet now share a fixed ground anchor. Frames/name wells/caption/action targets are independent; the caption no longer stretches with the whole card. Disabled Delete remains explicit because its protocol flow is unfinished.
- Rebuilt iPhone simulator successfully, zero warnings/errors. 241 offline assertions passed. HUD simulator regressions passed stable world/control geometry across drawers at three sizes, held movement confirmation gating/release and attack hold/release/cancellation.
- Live official-server bounded test confirmed world entry, one walk step and the return step, with no move pending at completion. Initial inventory snapshot arrived (45 entries); roster appearance matched world appearance. No credentials, names or raw authenticated packets included in diagnostic output. This updates the earlier timed-out inventory check; it does not verify inventory actions, sustained movement or damage.
- Visually inspected login, three-character roster with a long name, and minimized HUD. Native input focus works in `--keyboard-preview`, but the simulator is using hardware keyboard mode; software-keyboard/AutoFill and small-phone input interaction remain unverified. The computer-use tool could not attach to Simulator, so this verification remains unchecked rather than being reported as passed.
- All four latest user reference captures saved locally and indexed. No physical-phone deployment or signing/certificate change performed.
- Broader incomplete work remains: account creation/deletion/password flows; full remembered-login/error matrix; live blocked turning/sustained movement and full combat; SP/EXP; transitions; NPC/resource/chest interactions; inventory actions; chat; remaining rendering coverage.
