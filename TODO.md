# Endless Apple TODO

Current scope: iPhone on the official Endless Online server. Work through the priorities below when asked to continue. Remove completed tasks; Git history holds implementation and verification history. Keep concrete unresolved limitations here, without checkpoint logs.

Follow [AGENTS.md](AGENTS.md), the [shared UI rules](docs/UI-STYLE.md), and the applicable [login](docs/LOGIN-STYLE.md), [character selection](docs/CHARACTER-SELECT-STYLE.md), [HUD](docs/HUD-STYLE.md), and [gameplay](docs/GAMEPLAY.md) guides. Consult [engine reuse](docs/ENGINE-REUSE.md) before expanding gameplay systems. UI changes require an independent second rendered review.

## Combat and movement

- [ ] Retest Old Choppy on the physical iPhone in all directions: the current fallback retains wind-up art when strike resources are empty. Check body, clothing, weapon and back-item alignment together.
- [ ] Investigate weapons with missing standing art in the mapped archive, including Lasergun; do not treat the Old Choppy fallback as coverage for them.
- [ ] Implement monster combat feedback: health bars, damage numbers, death fade-out, and visible item/gold drops from server events. Review a short Endless Online combat video for presentation; verify actual damage separately from attack writes/animations.
- [ ] Verify ranged/special attack behavior, including projectiles.
- [ ] Verify sustained D-pad walking and tap routes on phone for smooth cadence, crowded collision, cancellation and server corrections.
- [ ] Verify blocked-direction turning is observed by the server/other clients in all directions; local turning and a successful write are already covered.

## Gameplay integration

- [ ] Decode and display current stamina and EXP/TNL updates.
- [ ] Complete mob/NPC movement, combat, disappearance and respawn lifecycle.
- [ ] Fix nonworking door/map-portal interactions and implement server-confirmed transitions, map cache validation and scene reset.
- [ ] Repair malformed door visuals: inspect original door art, open/closed frames, offsets and layering against desktop references.
- [ ] Route tapped NPCs/interactables into interaction range and dispatch the appropriate verified action: dialogue, chest, billboard or resource.
- [ ] Tapping an NPC opens its dialogue textbox and supports server-confirmed quest acceptance; use the [NPC/quest video](https://www.youtube.com/watch?v=i1hUiv0Xas4) as a presentation reference.
- [ ] Tapping another player opens a desktop-style options menu containing Paperdoll and Trade only; implement remote paperdoll inspection and trade request/accept/cancel/offer/confirmation flow with server-confirmed state. Reference: [player-options video](https://www.youtube.com/watch?v=MZIMvDcrDvE).
- [ ] Rebuild self/other-player Paperdoll to match the [desktop screenshot](docs/references/desktop-paperdoll-layout-2026-10-10.png): silhouette-aligned equipment slots, original item art, recessed character-info fields, wood framing and pixel text, adapted for readable phone layout.
- [ ] Display server chest contents; perform item actions only on explicit input.
- [ ] Implement resource harvesting, including trees, tool requirements and original animations.
- [ ] Complete inventory mutations and equip/unequip/use/drop actions with server-confirmed appearance/stat changes; Paperdoll is currently read-only.
- [ ] Implement real chat, quest journal data and the server-wide online roster; Players currently shows nearby actors.
- [ ] Extend rendering coverage for effects, ground items, pets and special poses.

## Account flow and phone verification

- [ ] Complete Create Account and verify View Credits behavior.
- [ ] Verify remembered-login lifecycle: first launch, successful relaunch to selection, changed/invalid password, unavailable network, cancellation and credential removal by unchecking Remember me.
- [ ] Verify software keyboard/AutoFill, loading/error states and back/disconnect behavior on phone; the simulator software-keyboard check was unavailable.
- [ ] Review login and selection with small screens, long names and different character counts alongside keyboard/auto-login states. Preserve centered carousel, readable typography and splash art beneath the wood frame.
- [ ] After the next requested phone update, retest HUD selected states, independent movement/attack touches, pan/recenter, zoom and stable camera geometry when drawers open.

Physical-phone installation requires a new user request. Simulator checks are part of implementation. Keep credentials and authenticated captures out of source, diagnostics and screenshots.

## Deferred

- [ ] Low priority: refine remaining title lettering in the established pixel style when prioritized; reuse original assets before generating more artwork.
- [ ] If Mac development resumes, extract shared UIKit presentation from the direct iPhone-source dependency. Mac UI work and a Swift rewrite are not active tasks.
