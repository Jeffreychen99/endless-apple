# iPhone gameplay behavior

Target contract for the native iPhone client on the current official Endless Online server. This guide describes intended behavior, not a claim that it is implemented. Read [TODO](../TODO.md) for completion, [shared style](UI-STYLE.md) and [HUD](HUD-STYLE.md) for presentation. Mac is deferred.

## Evidence and design intent

The user requests smooth four-direction isometric movement, X-shaped controls, tap routing, held attack, real NPCs/mobs and world interactions. The saved desktop captures establish the visual presentation of entities, inventories, speech and dialogs. The [short-video observation log](references/VIDEO-NOTES.md) adds a traversal/HUD reference; it does not verify today's official-server timings, collision exceptions, combat rules or packet formats. Treat the rules below as the requested mobile contract unless explicitly labeled observed. Verify mechanics against current-server behavior and recovered client logic before coding guesses.

## Session and world entry

Login → character selection → explicit character Login → loading → playable world. Opt-in remembered credentials stop at character selection. Loading must not briefly show another character's map or accept movement before world state is ready.

Apply the authoritative map, position, identity and available entity snapshot coherently. Missing optional art/data should have a bounded fallback and tracked diagnostic, not silently fabricate stats or declare complete rendering. A failed entry returns to a usable retry/selection state. Never expose raw protocol details in ordinary play.

On disconnect/background/exit, cancel held controls, paths and pending interaction intent. Re-entry must establish a fresh valid world state; no replay of stale attack or movement commands. The live game is not paused merely because a menu is open.

## Movement, facing and camera

- Four logical directions project onto the two diagonal screen axes. D-pad artwork, hit regions, tile picking, facing and animation must share that mapping; verify all four directions against a known map.
- Press/hold starts a continuous sequence of permitted steps. Releasing stops further requests; an accepted in-flight step can finish. Do not accumulate a long movement queue.
- Keep travel, footfall animation and equipment frames coordinated. Consecutive steps should join smoothly without returning to idle each tile. Use confirmed timing and monotonic animation time, not frame-rate-dependent increments or guessed video speed.
- Direction input requests facing even if forward travel is blocked. Send only legal turn/step commands; a blocked turn must not locally move the character through the obstacle.
- Server position and occupancy are authoritative. Prediction, if used, is bounded and reconciled; avoid oscillation, teleport-like corrections and unlimited retry loops. Terrain, doors and dynamic entity occupancy must be distinct checks. Do not assume every server has identical player-collision exceptions.
- Camera follows the rendered player smoothly at a stable scale and projection unless the user drags to inspect the map. One-finger dragging adds a bounded map-space camera offset; attack resets it to centered follow. Pinching and opening drawers retain the offset. Frame changes from opening inventory/chat must never refit the map or shift the player. Preserve genuine map-edge voids rather than invent scenery.

## Tap destinations and interaction intent

One active intent at a time. A new tile tap replaces the old route; manual D-pad input or attack cancels it. Record the destination in map coordinates so the subtle bracket marker follows camera motion correctly. Clear it on arrival, cancellation, transition or unreachable outcome.

Resolve taps in this order: active UI/modal hit regions; a selectable visible entity or interactable footprint; walkable terrain. Use actual sprite/footprint rules rather than selecting a hidden object behind a panel or an arbitrary bounding rectangle. When targets overlap ambiguously, provide a restrained disambiguation rather than triggering several actions.

| Target | Intended result |
| --- | --- |
| Walkable ground | Route to the chosen tile; re-evaluate dynamic obstacles after progress; stop with concise feedback if unreachable |
| Friendly NPC | Approach a valid adjacent/in-range tile, face it, request its actual dialogue/shop/quest interaction |
| Hostile mob | Resolve target/facing feedback; attacking remains an explicit attack action unless a later user decision adds tap-to-attack |
| Chest / billboard / resource | Approach valid range and perform the verified object-specific interaction; never treat the occupied object tile as a walk destination |
| Door / portal | Use its verified entry behavior when approached/stepped on; begin a transition only on confirmed server state |
| Ground item | Select/pick up through the supported request, with confirmed bag/ground updates; do not erase it optimistically |

Interaction intent has a target identity, map and valid range. Cancel it if the target disappears, the map changes, manual controls take over or progress times out. Approach should issue one interaction when eligible, not one every animation frame. Chest opening, harvest, dialogue and pickup are separate behaviors; there is no assumed universal action packet.

## Combat and actor life cycle

- Hold the attack control to repeat permitted basic attacks; release, cancellation, a modal, backgrounding, death or leaving the world stops repetition. Keep movement/attack coordination explicit and avoid queued bursts after latency.
- A locally displayed swing communicates attempted action. Only server outcomes establish hit/miss, damage, HP change, death, rewards or drops. Show compact world-anchored feedback when those events are decoded; no invented damage numbers.
- Render original directional body/equipment attack poses. Separate melee, ranged and special effects when verified rather than reusing a sword swing for everything. Frame timing must be measured from source/live behavior, not inferred from an edited guide clip.
- Maintain separate player and NPC identities, positions, facing, appearance and animation state. Create, update, remove and respawn entities from validated state. Removal clears collision/targeting; new entities must not inherit a removed entity's animation or health.
- Large mobs retain their silhouette and world anchoring. Depth-sort scenery and actors using verified ground anchors/layers, so tall sprites can occlude appropriately without changing their collision footprint to their whole image rectangle.
- Death and recovery follow verified server flows. Do not autonomously spend resources, revive or dismiss a meaningful choice during tests.

## Inventory, equipment and character state

Inventory items use real identity, quantity, appearance and supported footprint metadata. Selection reveals relevant actions without implying unsupported operations. Equipping/unequipping updates bag, paperdoll, world appearance and stats only when confirmed; handle rejection without duplicates or disappearing items. Destructive drop/junk operations require a deliberate choice and suitable confirmation, never test them merely to check button styling.

HP, TP, SP and experience/TNL share one authoritative character state. Unknown differs from zero. Verify whether each decoded field is current, maximum, remaining or cumulative before deriving a fill. Update world and HUD atomically enough to avoid a new level paired with stale progress. Do not infer stamina drain, regeneration or EXP formulas from visual bars.

## Chat, menus and interruptions

Opening a menu keeps the world and controls spatially stable. Inventory begins minimized. Chat text focus prevents gameplay input from interpreting typing/taps as attacks or movement. Closing the keyboard restores input deliberately, without resuming a previous hold.

A modal NPC/shop/chest dialog captures input and stops held actions; it does not pause the online world. Keep context visible and permit dismissal. Normal drawer opening does not silently create a new move/attack intent. World speech balloons and the compact chat log serve different roles; anchor balloons to speakers and bound their lifetime without permanent clutter.

## Verification scenarios

Keep implementation checks in TODO. A screenshot or successful connection alone cannot check these outcomes:

- Enter the chosen character/map; disconnect and re-enter without stale entities, controls or another account's state.
- Hold each direction for several tiles, release midway, turn against walls/NPCs/players; compare footfall and travel cadence under latency.
- Route around static and moving obstacles; replace/cancel a route; verify marker alignment and unreachable handling.
- Open/close every drawer and chat keyboard while stationary and moving; compare identical world coordinates and projection.
- Hold/release attack, change facing, receive real hit/miss/damage, observe removal/respawn; distinguish send success from combat success.
- Enter a supported door/portal; verify map/state replacement and cancelled old intent. Repeated taps must not trigger duplicate entries.
- Approach/interact with NPC, chest, sign and resource through each verified flow; ensure modal input cannot leak into movement/attack.
- Verify inventory/equip updates and actual HP/TP/SP/TNL values against the same live character. Use non-destructive checks first.

Open questions requiring official-server evidence: exact cadence and animation timing, collision exceptions, interaction ranges and object-specific packets, resource cooldowns, portal flows, full combat/death events, and SP/EXP semantics. The source clip does not resolve them.

### Desktop-source animation reuse

Front melee strikes require the weapon frame 17 above the body as well as frame 14 behind it. Back equipment uses attack sprites 3/4. Ranged subtype 1/2 uses body sheet 7, weapon 18/19, armor 21/22 and a standing recovery pose; do not send it through melee selection. These rules are adapted from desktop `CharacterSpriteCalculator` and the property renderers, with current asset-layout adaptations. Visual pose checks do not establish projectile or damage protocol support. Consult [engine reuse](ENGINE-REUSE.md) before extending duplicated systems.

### Missing weapon-pose resources

Old Choppy (1065; weapon graphic 11) has nonempty standing/wind-up resources but empty 1014/1016/1017 strike resources in both gender archives. Keep a nonempty wind-up, falling back to standing only if necessary, when every part of the requested weapon pose is empty. Do not replace an individually empty rear layer when its foreground part is valid. This prevents disappearing equipment; it is an explicit visual fallback until exact throwing/projectile behavior is verified. Original-asset regression: `scripts/dotnet-local.sh run --project engine/apple/Endless.Protocol.Tests -- --weapon-assets .local/official`. This suite reports pose availability, not live combat or projectile parity. Weapons without any mapped standing artwork remain unsupported rather than invented.
