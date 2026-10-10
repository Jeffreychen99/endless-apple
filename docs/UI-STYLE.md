# Shared iPhone UI style

Design contract distilled from user feedback through October 9, 2026. This specifies the target, not completed functionality. Track implementation and verification in [TODO](../TODO.md).

## Core direction

The app should feel like the original Endless Online desktop client reorganized for an iPhone. Use a portrait-first composition. Preserve the desktop's illustration, textured wood, beveled frames, pixel artwork and lettering; adapt placement, proportions and touch targets. Full desktop HUD parity is not required.

The latest explicit user feedback wins over this document. Shared rules live here; screen-specific rules live in [HUD](HUD-STYLE.md), [login](LOGIN-STYLE.md), and [character selection](CHARACTER-SELECT-STYLE.md). Do not treat existing placeholder UI as the design reference. Mac work is outside the current scope.

## Visual patterns distilled from the reference set

See the [annotated reference library](references/README.md). The consistent identity is warm, handmade fantasy cabinetry surrounding a lively illustrated/pixel world. The UI feels like a wooden board with fitted compartments: materially rich at its edges, orderly and quiet where information must be read.

| Observed pattern | Implication for the iPhone client |
| --- | --- |
| Honey-colored perimeter planks, visible seams and rounded upper world corners | Treat the screen as one enclosing frame, not a collection of unrelated floating cards. Carry texture around joins and preserve the characteristic world aperture. |
| Repeated light rim → dark outline → inset edge → content surface | Build a shared frame hierarchy. Nested wells should feel recessed, with modest bevels rather than soft modern drop shadows. |
| Light outer wood, medium amber panel boards, deep brown text/input wells | Use contrast by surface role. Wood surrounds readable dark wells; do not put strong grain behind every line of text. |
| Cream/slightly yellow lettering with dark edging; decorative italic action words; compact upright data text | Separate display/action lettering from body/data typography. Do not make chat, quantities or tables ornate just because the buttons are. |
| Mostly quiet tan/brown chrome; saturated colors in characters, item sprites and status marks | Let art and state carry the color. Avoid recoloring every control with bright modern accent colors. |
| Small metal screws, seams, ruled divisions and hard-edged bevels | Use original details at believable scale; do not enlarge screws, border thickness or grain with an oversized panel stretch. |
| Large outlined, softly shaded splash illustration; much smaller crisp in-world sprites | Preserve both scales of art. Login is an illustrated scene; gameplay is a pixel scene. Do not flatten them into one generic illustration or icon style. |
| The same shell holds chat, settings, inventory and tables | Reuse frames, wells, scrollbars and button states; change content organization rather than inventing a new visual system per panel. |

### Combining the two visible design families

The classic examples favor brighter honey wood, parchment/landscape backgrounds, screw-ended rectangular menu plaques and inset grid panels. The updated examples show more olive/dark-brown wells, clipped/beveled button ends, stronger outlined splash art and pictorial heart/status tracks. Both use framed boards and cream action lettering.

Chosen direction follows the user's preferences: **warm classic wood and framing, updated status sprites, original current-client art, and a coherent button family within each screen**. Do not mix a dark newer Credits plaque, a generic system Check Connection button and an unrelated classic sign-in button. Shared treatment matters more than copying whichever crop is easiest to find. Screenshot tones are qualitative references, not calibrated color samples; inspect source assets before choosing transformations.

### Shared component vocabulary

- **Outer frame:** textured wood surround and physical seams. Most visible at screen/viewport edges.
- **Panel:** framed medium-wood board grouping related controls. Use one clear primary boundary rather than repeated oversized nesting.
- **Content well:** darker recessed region for text, lists, credentials or portraits. Less texture contrast than the outer frame.
- **Action plaque:** original beveled wood with cream action lettering. Consistent proportions and darkened pressed state. Pair related actions on a common baseline.
- **Icon button:** compact amber inset tile with original dark/gold glyph; enlarge the glyph enough to read while retaining edge space.
- **List/table:** shared well with fine dividers, aligned rows, compact headings and an edge scrollbar. Avoid independent card styling for every row.
- **Modal:** same frame/well system over the still-visible world; title well, content well and anchored footer actions. It must not recenter the camera.

These are reuse boundaries for implementation, not new framework requirements. Shared frames and button rendering should serve all three screen types.

## Composition, depth and attention

The reference set is cohesive because its hierarchy stays consistent across very different content. Preserve these rules when an exact desktop layout cannot fit:

1. **One scene, one enclosing shell.** Login/selection use an illustrated backdrop; gameplay uses the world. The wood enclosure connects them. A new feature should inhabit that scene, not introduce another visual theme.
2. **Three surface depths.** Outer light wood encloses medium panel boards; dark wells receive editable/readable content. Fine rims explain depth. Reserve nested frames for a real grouping (portrait, name, list), not every label.
3. **Decoration belongs at boundaries.** Grain, screws, corner joins and bevels supply character. Text and item regions stay comparatively quiet. Do not apply noisy wood uniformly behind all content or replace the perimeter with a flat color.
4. **Hierarchy through placement and contrast.** Faces, equipped sprites, status marks and selected items are focal. Supporting captions and weight/level values are smaller and quieter. A bright blue system button must not become the strongest element in a warm EO scene.
5. **Consistent axes within asymmetry.** Art can be off-center and irregular; form labels, card edges, action baselines and list columns should align precisely. Asymmetry is planned negative space, not arbitrary offsets.
6. **Density by content type.** Inventory is spatial; chat is linear; stats are tabular; choices are short lists. Share their shell without forcing them into one generic card layout.

### Layer and state rules

- Gameplay order: world and actors → world-anchored markers/balloons → permanent HUD → open menu/dialog → modal input capture. A modal covers the world without altering its projection. Exact occlusion between world sprites belongs to [gameplay](GAMEPLAY.md).
- A button's pressed darkening is transient. A selected tab/item needs a separate persistent inset/highlight treatment; disabled and pending controls must also be distinguishable. Prefer verified desktop state assets; do not claim a screenshot establishes unseen hover/press behavior.
- Keep panel geometry stable through empty, loading, populated and failed states. Put feedback inside the panel it concerns, and retain useful previous data when it is safe to do so.
- Artwork style has two intentional scales: expressive outlined splash figures and crisp small world/item sprites. Both belong together through common colors/materials; neither should be resampled into blurry background decoration.

### Fidelity and adaptation boundary

Preserve source texture scale, border logic, lettering proportions, recognizable icons and item silhouettes. Adapt safe-area padding, content scrolling, touch targets and screen arrangement. Never copy video captions, watermarks, compression artifacts or sample character data as client UI. The [video notes](references/VIDEO-NOTES.md) separate observed presentation from uncertain gameplay mechanics.

## Materials, frames and color

- Use actual desktop wood-panel artwork for buttons, frames and panels. Grain must be visible; a flat brown fill is not an adequate substitute. Carry the material through intended top/bottom chrome and safe-area extensions.
- Aim for the warm golden/honey wood of the user's classic desktop references, with darker inset wells and crisp dark edging. Prefer original matching resources. If a tonal adjustment is needed, preserve the grain and apply it consistently to related components.
- Keep corners, bevels, edge thickness and texture scale consistent. Use edge-preserving slices and tiled interiors where appropriate. Do not stretch an entire decorated panel or button to fill arbitrary bounds.
- Distinguish intentional wood framing from accidental opaque rectangles underneath controls. Do not cover the world or illustration with a backing slab merely to fill unused space.
- A screen must look like one set of desktop controls: consistent material, border treatment, lettering scale, pressed state and spacing.

## Artwork and text

- Source production art from the original desktop resources. Prefer originals over generic system symbols. User update: matching custom pixel art is permitted for D-pad keys and other UI cosmetics where it improves the mobile port; preserve the palette, crisp edges and bevel vocabulary, and record which assets are custom. Latest user feedback permits replacing the attack sword with a suitable original combat icon.
- If the decorative sword is used, prefer the approved screenshot crop over the generated approximation. It is no longer required as the attack button; prioritize coherent original navigation/attack glyphs.
- Keep sprite and bitmap-text aspect ratios intact. Preserve pixel edges with nearest-neighbor sampling where appropriate. Scaling a touch target must not stretch its lettering.
- Prefer sufficiently large original icons for compact gameplay controls; avoid redundant text. Retain accessibility labels even when visible labels are omitted.
- When visible text is needed, use the desktop's original lettering/font where available. For dynamic text without a recovered font, use a consistent readable fallback, document the limitation, and do not present it as an exact match. Georgia/system fonts in current code are not a permanent design decision.
- Preserve intended transparency/color keys. Logos and illustrations must blend into their background without a visible mismatched rectangular patch.

## Composition and interaction

- Plan the artwork region and control region together. Use intentional asymmetry on login. The latest selection design deliberately centers one carousel card and smaller artwork below. Keep important faces, poses and focal artwork clear of buttons.
- Respect phone safe areas and keyboard space. Controls must remain usable on small phones and with longer character/item names. Scroll content when needed rather than shrinking controls until unreadable.
- Use at least 44-point touch targets as the working mobile baseline, with visible separation. This is an implementation baseline, not a requirement to enlarge the source bitmap itself.
- Buttons darken slightly while pressed and restore on release/cancel. Do not change their geometry or move neighboring controls during a press.
- Keep feedback subtle and in the desktop style. Show real state: unavailable stats must not look full, an outgoing request is not confirmed success, and fake preview values must remain confined to explicit previews.
- Keep technical diagnostics out of normal gameplay. User-facing errors should be concise and actionable.

## Resource starting points

Paths below are local source resources, not permission to commit/distribute the client archives. Graphic numbers are the IDs used by `EgfArchive.Bitmap`, not raw Windows resource IDs. Inspect crops before use; this is not an exhaustive atlas.

| Source | Use |
| --- | --- |
| `.local/official/gfx/gfx001.egf` | Login/selection background, splash art, panels and word buttons |
| gfx001 graphics 30–36, 41–44 | Background variants and foreground illustrations |
| gfx001 graphics 2, 11, 13, 14, 19, 23 | Login/character panels, button sheets and frame resources; inspect for intended role |
| `.local/official/gfx/gfx002.egf` | Gameplay HUD resources |
| gfx002 graphics 1, 28, 50, 51, 58 | Viewport frame, wood, buttons, panel frame and updated status tracks |
| `Apple/Endless.iOS/Assets/attack-sword-crop.png` | User-approved exact sword crop |

Record archive/graphic/crop and any transformation near the asset code when adding a resource. Reuse shared rendering/components where the visual rules are shared.

## Review checklist

Compare the actual rendered result with the saved reference and the applicable screen guide. Check texture continuity, bitmap proportions, state changes, readable icons, pressed feedback, safe areas, keyboard layout, and unintended overlap. Use screenshots as review evidence, not as proof that server actions work. Keep any remaining mismatch in the TODO.

## Typography and material correction (October 9 review)

Two distinct roles are required: cream/light-yellow, dark-edged cursive bitmap titles/actions; upright pixel body text for names, levels, credentials, chat and values. Do not substitute Georgia or system sans as the permanent style. Original classic resources are available in `packages/endlessclient.binaries/1.4.0.2/build/net462/client/gfx`: gfx001/15 has Login/Delete, gfx001/11 has Character, gfx001/13 has View Credits. Use lettering without stretching its aspect ratio. Original classic gfx002 provides the requested brighter orange wood; current gfx002/58 remains the status sprite source.

The repository's `EndlessClient/ContentPipeline/BitmapFonts/sans_11px` atlas is an un-smoothed Microsoft Sans Serif bitmap font. `scripts/build-ui-font.py` adapts those existing glyph pixels to local native outlines for UIKit input, maintaining the pixel shapes; this is a desktop-repository source, not a claim of recovering the current official binary's embedded font. Generated font resides in ignored `.local/ui-fonts`. Prefer whole/half pixel multiples and verify readability on device.

Unrecovered/dynamic decorative labels use [Carter One](https://github.com/google/fonts/tree/main/ofl/carterone), cream with a dark outline, as a documented approximation. Recovered Connect/View Credits/Character/Login/Delete/Back sprites always take precedence. Carter One is bundled with its OFL notice; it is not represented as EO's original font.


### Decorated panels versus frame pieces

A complete decorated panel is not a generic nine-slice: preserve its aspect ratio and position dynamic contents in its source-coordinate wells. Build expandable shells from actual blank edge strips with tiled grain. Never stretch a caption, name well or portrait well as part of a frame center. The [desktop implementation study](DESKTOP-UI-IMPLEMENTATION.md) records source evidence and the chosen mobile adaptations.

### Material scale and second-pass review

Do not repeat a narrow perimeter strip across broad panels: it exposes plank joins as a noisy grid. Perimeter wood, quiet amber panel material, and inset navigation plaques have different jobs. Inspect all three in fresh renders, then do a separate second pass against user feedback before reporting completion (AGENTS.md).

When refining an existing title image, preserve its established letter shapes, proportions, slant, cream fill and dark outline. Use a conservative definition cleanup from the original reference, retaining pixel-style edges; do not substitute a new decorative font or turn it into polished HD lettering. Paperdoll uses this treatment on the same amber plaque as adjacent controls.
