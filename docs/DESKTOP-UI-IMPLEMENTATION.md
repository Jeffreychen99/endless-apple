# Desktop UI implementation study

Source inspection, October 9, 2026. This repository's desktop source describes the classic client implementation; it is evidence for layout and asset assembly, not proof that current official-server protocol or every updated binary behaves identically.

## Login

`reference/monogame/EndlessClient/ControlSets/LoginPromptControlSet.cs` composes gfx001/2 with separate editable fields. The panel is at (266,291); fields at (402,322) and (402,358), 140 pixels wide. Text has four pixels of left padding, separate edge/interior/cursor artwork, ordered keyboard focus and Enter submission. Connect/Cancel use 91×29 regions of gfx001/15, at (361,389)/(453,389). Button states are separate atlas columns. A pending login task gates duplicate submissions.

Consequences: preserve a dark input interior with breathing room inside a real wood frame, and keep action lettering independent of target size. iPhone uses always-visible fields, Remember me at left and Connect at right. Desktop legacy input limits are not imposed on the current server.

`InitialControlSet.cs` selects splash graphics 41–44 and positions the figure at (229,70). The user's page-entry-only refresh rule governs mobile. The scene and foreground illustration are separate layers; foreground must stay inside the enclosing frame.

## Character selection

`reference/monogame/EndlessClient/UIControls/CharacterInfoPanel.cs` uses the complete 276×123 gfx001/11 image at (334,36+index×124). It does not stretch the decorated center. The name occupies (165,26,89,22); level starts at (29,99). A CharacterControl renders the real appearance; Login/Delete are separate controls at (161,57)/(161,85), with pending-task protection.

This explains the former mobile defect: nine-slicing the complete card distorted its painted portrait/name wells, seam positions and caption. The mobile card now retains one aspect ratio and overlays dynamic content in those original regions. Its portrait uses the existing feet-anchored renderer. Latest mobile preference intentionally replaces the desktop column with one centered card and previous/next arrows. Delete is omitted. Smaller splash artwork sits below.

## Gameplay shell

`reference/monogame/EndlessClient/HUD/Controls/HudBackgroundFrame.cs` assembles several original resources: gfx002/1 main frame, /21 top-left, /22 sidebar, /23 topbar. Topbar starts at (49,7), sidebar at (7,53), with a separate right strip at (629,53), plus explicit gap filling. The desktop effect comes from layered, naturally scaled edges, not one stretched brown background.

Mobile preserves stable world projection and control placement, adds tiled classic honey-wood perimeter strips and a transparent-centered control surround, and retains updated status sprites. Expanded menus overlay the world; opening one must not refit the camera. Further rounded-corner fidelity can reuse dedicated corner resources without stretching scene content.

## Reusable rules

- Distinguish whole decorated panels from blank frame pieces.
- Keep source-coordinate content wells and original lettering proportions.
- Tile grain at edges; preserve corner thickness and believable seams.
- Grow touch targets without growing bitmap letterforms.
- Gate pending actions, and stop held actions on modal input capture.
- Preview images verify layout only; live protocol behavior requires separate bounded evidence.

## Custom Check Connection lettering

Generated with the built-in image-generation tool, using the original gfx001/13 action sheet as a visual reference. This is custom matching lettering, not recovered EO artwork. Saved unchanged as `ios/Endless.iOS/Assets/check-connection-lettering.png`, bundled on an original wood plaque. Graphic files remain ignored under the project asset policy.

Prompt: “Create ONE small game UI lettering asset, transparent background, exact text 'Check Connection'. Match reference classic Endless Online hand-lettered fantasy bitmap menu words (Create Account, View Credits). Tall condensed brush-script letters, strongly leaning right, cream/pale yellow fill, thin black/dark-brown outline and dark offset shadow. Narrow chunky strokes, connected handwritten lowercase, capital C each word. Not modern sans/typeset italic. One horizontal line, tightly bounded modest transparent padding. NO wooden plaque/screws/additional text. Laid over actual original wooden button resource. Rough pixel-era edges and narrow expressive proportions.”

The full generated image is preserved locally; source/reference resources and generated graphics are not added to Git.

## Material follow-up

Classic gfx002/1 supplies the small amber navigation plaque at (60,330,37,20) and the pale HUD surround at (110,454,400,14). gfx002/28 supplies the quiet amber panel interior. The current gfx002/17 equipment sheet supplies the compact sword category glyph (5,31,16,16); dark backing is keyed out for placement on the amber attack plaque. The earlier 8-pixel plank-strip fill and dark header-well button crop were inappropriate for large control surfaces. Login retains the classic gfx001/23 perimeter with a clean amber interior instead of stretching its painted inner bands.


## Original inventory artwork and footprint metadata

The current item table's nonreserved records are 78 bytes. Inventory graphic is the EO short at offset 1; the size char is at offset 74. The size mapping agrees with upstream `EOLib.IO/ItemSize.cs`: width = size / 4 + 1, height = size % 4 + 1 for values 0–7. gfx026 uses resource `inventoryGraphic * 2`. Checked all 1,501 nonreserved installed records: every referenced inventory graphic exists. Reserved short records retain safe unknown-art/1×1 fallbacks.

Item art is drawn at a shared 26-source-pixels-per-cell scale, bounded by its footprint; a small placeholder inside a large footprint must not be stretched into a giant icon. Quantities overlay quietly, and names appear on selection. This is read-only inventory presentation; item action packets require independent verification.

## Native presentation and input review

World buffers are opaque RGBA. The iPhone now wraps that buffer in an sRGB CGImage/UIImage, retaining its data provider, instead of running a PNG compression/decompression cycle per animation frame. PNG remains the export format for offline artifacts. A colored-corner fixture compares native and PNG presentation for orientation/channel fidelity.

Held controls schedule timers in common run-loop modes to remain active during touch tracking. The Xcode test runner exercises actual press/release as well as synthetic layout checks. The HUD is an accessibility modal region so VoiceOver cannot reach login controls behind the active world.

Splash size is calculated after the control stack finishes arranging hidden/visible states. Controller-level layout callbacks alone were too early for the selection transition and allowed Back to cover the illustration.

## Attack anchor correction (October 9 afternoon)

Current executable 0x5ac3ea–0x5ac963 composes melee poses on the same canvas as standing (body standing x34, y19+gender; world-relative origin x43, y19+gender). Wind-up body x30−gender and strike x32+gender yield −13−gender / −11+gender. Boots retain the standing resource for wind-up; strike uses +10 (0x5ac5ae–0x5ac66f). Head x28 becomes x26 on front strike, or x26+2×gender on rear strike; front y12−gender, rear y8. These differ from the former centered body, always-striking boots and −4 head shift. Body, armor and weapon frame selection use one actor pose snapshot; no independent equipment timer is introduced. Offline equipped contact sheets cover both genders, four directions and both attack phases. This is visual alignment evidence, not confirmed combat damage.

Classic gfx002/1 contains the rounded top corners at (0,0,64,54) and (576,0,64,54), the inventory bag glyph at (69,334,20,12), and the character/paperdoll head glyph at (599,374,20,12). Black is keyed transparent in the frame aperture. Original corner cap sizes must survive resizing; cutting a rectangular transparent hole destroys them.

### October 9, 3:40 feedback corrections

- Login scene gfx001/30: extend only rows 200–469 as backdrop. Rows 470–479 are the complete bottom wood band and are rendered once in front of the splash. The prior 472–479 overlay left two stretched wood rows behind the art, making the figures appear to overlap the frame.
- Desktop `InventoryPanel.cs` locates Paperdoll at gfx002/27 `(39,385,88,19)` with its surrounding panel gfx002/44. Mobile trims that title to `(51,388,65,13)` so its opaque padding cannot cover the rim, then uses those original title pixels plus the double-rim region `(380,0,98,35)` of graphic 44, separated so the title is not stretched with the border. The right control column has Paperdoll above a wider held-Attack button; the disabled Use action is omitted.
- Top chrome extends to screen coordinate zero, including time/Wi-Fi/battery. Rounded desktop corners belong at that upper edge; the viewport below the status tracks uses straight side pieces. Do not put a second rounded arch immediately above/below the tracks.
