# iPhone gameplay HUD

Read [shared UI rules](UI-STYLE.md) first. This is the intended design; [TODO](../TODO.md) records unfinished behavior.

Gameplay state, targeting, transitions and verification are defined in [GAMEPLAY](GAMEPLAY.md). This file owns their visual arrangement. [Video notes](references/VIDEO-NOTES.md) provide limited contextual evidence, not official-server mechanics.

## What the desktop examples establish

Study [chat](references/desktop-hud-chat.png), [stats](references/desktop-hud-stats.png), [settings](references/desktop-hud-settings.png), [player list](references/desktop-hud-player-list.png), and [updated inventory](references/desktop-hud-inventory-updated.png).

Across these examples, a large upper world window sits in a continuous wood surround. Thin status tracks are attached to its top edge. A horizontal divider leads into a compact lower board, flanked by narrow navigation rails. The content changes, but the outer geometry, frame vocabulary and navigation remain recognizable. On mobile, preserve that continuity with a stable viewport, fixed action controls and a drawer; do not copy the desktop's many tiny controls or require its wide proportions.

The newer inventory/player-list captures show the requested heart and other pictorial status elements. Use those for status styling; the older lettered HP/TP/SP/TNL tracks explain hierarchy and placement, not the final sprite choice.

### Content-specific patterns

- **Inventory:** a recessed amber lattice, thin dark cell divisions, and item art at varying footprints. Swords and clothing can span multiple cells; the examples are not a grid of uniformly shrunken app icons. Keep silhouettes readable and derive footprint/layout from verified item metadata. On phone, scroll the item region and separate selection/detail actions from the grid; avoid covering items with permanent verbose labels. Weight is secondary information in its own small inset well. The [shop example](references/desktop-shop-inventory.png) separates Paperdoll/weight/Drop/Junk from item storage; do not add unimplemented/destructive actions merely to fill that space.
- **Chat:** dense but readable lines in one inset log, with compact speaker/channel cues, a narrow scroll gutter and channel selection near the composer. Preserve the connected log rather than using large messenger-style cards. In-world speech balloons remain a separate light neutral treatment with dark text.
- **Stats/player list:** aligned rows and columns with fine shared rules, modest headings and minimal decoration. Mobile can reduce columns or scroll; preserve label/value alignment and hierarchy instead of squeezing the full desktop table to illegibility.
- **Settings:** compact paired label/value rows with inset choices, not a pile of unrelated large switches. Touch targets may be enlarged while the surface styling remains the same.
- **Target feedback:** the [updated inventory/world reference](references/desktop-hud-inventory-updated.png) shows small pale-gold corner/bracket marks around a ground target. Use that visual vocabulary for the requested destination indicator, projected to the isometric tile and quiet enough not to obscure sprites; exact source-sprite identity still needs verification.

### NPC and interaction dialogs

Study [shop](references/desktop-shop-inventory.png), [quest](references/desktop-dialog-quest.png), and [options](references/desktop-dialog-options.png). They share a wood outer frame, narrow dark title strip, recessed content, edge scrollbar and footer actions. The quest view places a small NPC sprite alongside compact text/underlined responses; the shop uses icon-led choices; options use small square checks and aligned prices.

On iPhone, use the same dialog family for NPC speech, chests, billboards and resource prompts as appropriate. Keep title/content/actions structurally separate, allow the content to scroll and keep dismiss/confirm actions reachable above safe areas. Preserve visible world context without moving the camera. An interaction dialog can temporarily capture input, but must stop held movement/attack and must not leave those controls repeating underneath it. A dialogue choice is a real action, not decorative underlined text.

## World and camera

- Portrait-first. The world should fill most of the upper screen at a useful play scale, rather than fitting the entire map into a miniature overview.
- Follow the character smoothly and keep it centered during normal movement. Natural map boundaries/walls may leave edges visible; do not invent off-map scenery.
- Opening, closing or switching inventory/chat/skills/stats must not change camera angle, zoom, world placement or character screen position. Use a stable world projection and reveal panels as overlays. Menu toggles must not trigger a new camera fit.
- Keep the original textured wood viewport frame. Avoid extra opaque backing rectangles around the movement and attack region.

## Status and menus

- Include HP, TP/mana, SP/stamina and TNL/experience at the top. Prefer the updated desktop hearts and other status sprites from gfx002/58 over recreating older plain bars.
- Display the character's actual current values and server updates. Distinguish unknown values from zero; never display synthetic/full values in a real session. Make the meaning of TNL/progress consistent with the decoded data.
- Maintain a dedicated, stable movement/action area and a separate collapsible menu area for Inventory, Chat, Skills, Stats, etc.
- Inventory/menu content starts minimized. Expansion and tab switches must not shift the movement/action controls or world camera. Keep icons large enough to recognize, with minimal visible text and accessible names.
- Inventory uses original inset wood/grid styling and original item art. Names/quantities are real data. Chat needs readable history and a keyboard-aware composer. Neither panel should impersonate a completed action while its protocol is unavailable.

## Movement and targets

- D-pad keys use crisp wood-panel frames with cream/gold pixel arrows, not rounded system-style buttons. Custom matching pixel cosmetics are permitted by the latest user feedback.
- Four-way X-shaped D-pad aligned with the isometric directions, not an upright Cartesian joystick.
- Hold a direction for continuous walking. Coordinate animation, interpolation and repeat cadence: avoid sped-up feet and stop/start motion between consecutive steps. Stop on release/cancel; honor authoritative movement and collisions.
- Direction input also changes facing when the adjacent tile is blocked by terrain, NPCs or players. Turning is independent of advancing and must remain synchronized with the server.
- A normal tile tap routes toward that tile. Show a small, restrained marker on the selected square, projected onto the map. Update or clear it on replacement, arrival, cancellation or an unreachable target.
- Tapping an NPC or interactable tile invokes the appropriate interaction, approaching into range if necessary: dialogue, chest, billboard, berries/resources, etc. Do not route through an occupied target tile or send an unrelated generic action.

## Attack and action controls

- Use an original desktop combat glyph on an amber action plaque for attack. The earlier screenshot-sword requirement is superseded; a decorative sword may be considered separately if it improves the layout. Preserve clear, non-overlapping attack, movement and secondary-action targets.
- Hold attack to repeat at the permitted cadence; release/cancel/backgrounding/leaving the world stops repetition. Avoid accumulating queued attacks.
- Use original attack sprites and transitions. An animation/request alone is not evidence of server damage or a successful hit.
- All controls receive the shared slight darkening when held. Remove the unintended dark brown rectangle under D-pad/attack while retaining intentional textured wood framing.

## Verification

Check minimized/expanded menus and chat keyboard without camera/control jumps; sustained walking in each direction; blocked turning against each obstacle type; tap marker alignment during camera motion; interaction targets; held/released attacks; original frames and item/status accuracy. Inspect small-phone hit regions. Confirm live actions separately from offline visual previews.

Existing diagnostic preview: `--hud-preview`, optionally `--expanded`, uses synthetic state and no credentials. It is a layout aid, not live-game verification.


### Frame assembly

Retain a visible continuous honey-wood perimeter and a separate thin surround for the fixed control area. Tile original edge grain at natural scale; keep the middle transparent so the D-pad/attack region does not acquire an opaque brown backing slab. Desktop implementation uses separate main, top and side resources rather than stretching decorated panel contents. See [source study](DESKTOP-UI-IMPLEMENTATION.md).

Use the desktop main HUD navigation plaque style for controls. The broad controls area uses quiet amber panel material, not repeated outer-frame planks. Retain continuous warm outer framing.


Read-only inventory uses original variable footprints and scrolls within the drawer. Keep enough expanded height for several cell rows on compact phones; the expansion remains an overlay. Stats show verified values in quiet ruled rows, with an explicit unavailable state for undecoded values. The HUD captures accessibility navigation while active so hidden login controls are not reachable behind it.

### October 9, 3:05 PM control refinements

Open drawer buttons stay darkened/selected; tapping the same button closes its drawer and restores the button. Switching tabs transfers selection. The dedicated hide button is removed. Inventory uses the original bag glyph; the former people glyph belongs to Players. Until a verified server-wide roster is available, Players explicitly lists nearby actors only.

Latest layout: D-pad left, original Paperdoll plaque above the quest scroll icon in the center, and tall Attack on the right. Remove the unused Use-item action. Show the actual equipped appearance and known item names; no unsupported equip actions. Reuse classic gfx002/1 rounded corners at the very top of the full screen, not beneath the status safe area. Fill the time/Wi-Fi/battery region and the space above the stat tracks with one broad wood-panel crop. Never repeat an eight-pixel perimeter strip over this large surface. Use straight viewport sides beneath the tracks; do not add another rounded cap there. Check screen curves and status-track overlap in a rendered phone screenshot.

Pinch the world to zoom from 1× to 2.5× around the character. HUD bounds remain fixed; tile picking and destination marks use the camera of the displayed frame. Attack availability must not flicker with movement-pending updates; the protocol still rejects attacks during an in-flight step. Quest button opens an explicit unavailable journal until server data is implemented.

Broad status and home-indicator fills use the quiet original gfx002/28 interior, not the planked credential panel or repeated eight-pixel edge art. Keep the classic gfx002/1 corner cutouts visible next to time/battery; hardware clipping must not erase them. Quest glyph uses the complete scroll/pen crop and a modest tone reduction, preserving its highlights.



Latest revision: omit the decorative sword. Paperdoll shares the ordinary amber navigation plaque, with a conservatively refined version of its original lettering (`Assets/paperdoll-lettering-refined.png`, image-generation edit of classic gfx002/27 crop 51,388,65,13). This is not a new font or an exact untouched desktop asset. Inventory and Quest icons retain dark outlines over transparent backgrounds.

One-finger drag pans the world; two-finger pinch zooms. Neither changes HUD bounds or moves the player. Attack resets the offset and resumes centered follow. Drawers preserve the current camera.
