# Desktop UI reference library

User-supplied screenshots saved unchanged on October 9, 2026. The filenames “classic” and “updated” distinguish visible design families, not verified version numbers. These images establish composition, material and component patterns; they do not establish current protocol behavior or authorize copying sample account/chat data into the app.

Read the [shared guide](../UI-STYLE.md), then the relevant screen guide. Inspect full-size images for the feature being changed. Screenshot compression, capture crops, black off-map voids and white triangular missing corners are not UI assets to reproduce. Source production assets from the original desktop archives.

## Login and character selection

| Reference | What to study |
| --- | --- |
| [Classic login](desktop-login-classic.png) | Logo anchored upper-left; compact lower-left action rail; large right-hand illustrated figures; muted landscape; enclosing honey-wood frame and screw details |
| [Updated login](desktop-login-updated.png) | Integrated crescent logo; restrained gray/olive landscape; strongly outlined foreground pair; recessed labeled credential fields; newer clipped-end wood buttons |
| [Character selection](desktop-character-selection.png) | Illustration left, repeated character cards right; portrait well and lower-left level; name well and stacked actions; aligned supporting buttons beneath cards |

The user explicitly wants always-visible credentials, no Play Game/Exit, stable splash per page visit, and no controls obscuring focal art. Those requests override the desktop screenshots' literal button placement/contents.

## Gameplay panels

| Reference | What to study |
| --- | --- |
| [Chat](desktop-hud-chat.png) | Large upper world aperture; permanent icon rails; inset text log; compact speaker/channel marks, scroll gutter and bottom channel tabs |
| [Stats](desktop-hud-stats.png) | Compact ruled table, aligned labels and values, broad sword well, shared outer frame |
| [Shop and inventory](desktop-shop-inventory.png) | Multi-cell item silhouettes in an inset lattice; separate item actions/weight; framed modal with header, icon-led option rows, inset list and footer Cancel |
| [Player list](desktop-hud-player-list.png) | Tabular column headers and aligned rows within the same panel shell; updated pictorial status row visible at top |
| [Settings](desktop-hud-settings.png) | Paired label/value rows and small inset dropdown affordances; the same navigation rails and board geometry despite different content |
| [Updated inventory](desktop-hud-inventory-updated.png) | Hearts and other compact status sprites; warm classic-like shell retained; item silhouettes with varying dimensions; bracket-like target marker and label in the world |

## Dialogues and options

| Reference | What to study |
| --- | --- |
| [Options dialog](desktop-dialog-options.png) | Narrow dark title well, dark readable content well, square checkboxes, aligned prices, slim scrollbar, paired footer actions |
| [Quest dialog](desktop-dialog-quest.png) | Small NPC sprite beside conversational text, underlined choices, dark inset content, shared Cancel/OK footer and wood border |

Use [HUD guidance](../HUD-STYLE.md) for mobile interaction panels; these are overlays on the world, not replacement scenes.

## Local storage and Git

Reference screenshots are intentionally ignored by Git. They remain on this development machine for inspection; a fresh clone needs the user-supplied images restored to the filenames in this index. This index and all style/gameplay guides stay tracked. Required small runtime artwork (the approved sword crop and iPhone app icon) is explicitly exempted from the media ignore rules.

## Asset provenance

All eleven PNGs in this library are user-provided reference captures, including the previously saved character-selection image. Preserve originals. Record any future crops separately and describe their use. Never copy the sample names, messages, item quantities or account-field text into fixtures or live UI.

The separately approved attack sword crop is at `../../Apple/Endless.iOS/Assets/attack-sword-crop.png`. Reuse it; do not regenerate a lookalike.

## Gameplay video research

[Timestamped video notes](VIDEO-NOTES.md) document the user-linked 1:52 Nostalgia guide, the sampled frames and limitations. Use [GAMEPLAY](../GAMEPLAY.md) for the mobile behavior contract; video observations do not establish current official-server rules.

### October 9 typography review

User captures: [banner seam](login-seam-feedback.png), [title headers](desktop-title-headers.png), [Paperdoll](desktop-paperdoll-title.png), [pixel name](desktop-pixel-name.png). The first documents a defect; the other three are typography targets, not new UI content.

## October 9 malformed mobile card

[User-reported stretched card](iphone-card-distortion-2026-10-09.png) documents a defect, not the target style. Do not stretch decorated panels through their name/portrait wells. Latest user preference replaces the right-hand column with a centered carousel and removes Delete. See [source study](../DESKTOP-UI-IMPLEMENTATION.md).

## October 9 material and alignment review

The user supplied [login color defect](login-panel-color-2026-10-09.png), [floating splash base](splash-floating-base-2026-10-09.png), [repeated HUD texture defect](hud-repeated-texture-2026-10-09.png), and [desktop amber control target](desktop-amber-controls-2026-10-09.png). The first three are defects to avoid; the last establishes the quiet panel fill and beveled navigation tiles.

- `desktop-rounded-hud-corner.png`: user-supplied October 9, 3:04 PM crop showing the original curved wood/world aperture; production source located in classic gfx002/1.

### October 9, 3:40 follow-up

- `desktop-paperdoll-button.png`: requested original double-rim Paperdoll plaque.
- `feedback-splash-frame-seam.png`: defect reference, figures crossing the painted bottom-band boundary.
- `feedback-hud-status-wood.png`: defect reference; rounded corners belong at the screen top and the entire system-status region should be wood.
