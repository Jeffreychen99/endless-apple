# iPhone character-selection screen

Read [shared UI rules](UI-STYLE.md) first. Match the desktop reference closely, adapting arrangement for portrait while preserving the structure inside each character card.

## Primary reference

![User-supplied desktop character-selection reference](references/desktop-character-selection.png)

Saved unchanged from the user's October 9, 2026 attachment. This is a visual/layout reference, not account data to reproduce. Do not use its character names or levels as live placeholders. Extract production panels/buttons/art from the desktop resources rather than treating the whole screenshot as the UI.

## Patterns to preserve from the reference

The illustration is large and free-form on the left; the repeated character cards form an orderly right-hand column. The visual contrast is deliberate: expressive artwork beside a compact roster. The same logo/background language links this screen to [classic login](references/desktop-login-classic.png), making the transition feel like another view of the same place.

Each card is a shallow rectangular wood enclosure, with thin nested gold/dark borders rather than a soft floating shadow. Its portrait well is almost square and occupies roughly the left two-fifths. The equipped character is small enough to sit fully within it, and the level is a quiet lower-left detail. The right side aligns a small Character caption, recessed name well and stacked Login/Delete controls. The card's outline groups all of these into one unit; no extra badge or separate giant name heading is needed.

Three repeated cards establish a rhythm of equal widths and modest vertical gaps. Create/Password align beneath the card column, not centered across the entire illustrated screen. Preserve those alignments on mobile even if the column becomes wider or scrolls. Do not preserve desktop pixel dimensions at the expense of readable labels and touch targets.

The reference's portrait wells, name wells and action plaques are instances of the shared component vocabulary, not three unrelated styles. Its warm grain and fine borders should carry into login and gameplay chrome; scene-specific layout provides variety without changing materials.

## Page composition

- Separate screen/state after accepted sign-in. Retain the original background, integrated logo, splash illustration and textured wood visual language.
- Latest user preference supersedes the right-column design: show one centered card near the top, with left/right arrow boxes beside the counter below it, cycling characters. Put enlarged centered splash artwork below, clear of controls, with its bottom edge covered slightly by the foreground frame. Shift the card down modestly to reduce the empty gap.
- Do not show username/password entry or an unexplained gameplay-map image here.
- Returning to login should refresh its splash selection. Do not run an illustration rotation timer on the selection page.

## Character cards

- Each character has its own original wood-framed card.
- Left: a clearly bounded inset preview box showing the actual character and equipped appearance at a consistent scale. Include the character's real level within this box, near the lower edge as in the reference.
- Right: Character/name area above one Login button. Preserve the desktop's hierarchy, alignment, spacing, inset name well and original button lettering as closely as possible.
- Do not replace the card with one huge name button, omit the inset box, or use a generic avatar. Do not crop off equipment or distort the sprite.
- Use server-derived identity, level and appearance. If a field is not decoded, record that gap rather than inventing a value.
- Login is explicit. While selection/world entry is pending, prevent duplicate submissions and show a concise loading state.
- Delete is deliberately unsupported and omitted from this screen. Do not restore it based on the desktop reference.

## Supporting actions and states

- Preserve the reference's supporting Create/Password actions where implemented, using the same wood/button styling. Track missing account-management functionality in TODO rather than adding fake successful actions.
- Provide a clear return/back action, with the iPhone auto-login/Remember me policy from [login](LOGIN-STYLE.md).
- Handle empty rosters, multiple characters, long names, selection errors and disconnects without mixing another account's cards into the page.

## Consistent roster rhythm

Anchor all cards to the same column edges, portrait baseline and name/action axis. Full equipped figures use consistent ground anchoring; a taller weapon should not arbitrarily enlarge one card or shrink only that character. Reserve room for equipment and long names, keeping level readable without laying text over the feet. Empty/loading/error states inhabit the roster region; they must not jump the logo or illustration around.

Reuse login's scene materials and action treatment. On portrait phones, preserve the card's internal portrait-left/actions-right relationship before preserving the desktop's exact overall two-column width. Reachable controls and clear artwork matter more than a literal screenshot scaled down.

## Verification

Compare rendered cards directly with the reference: inset portrait box, level position, name well, right-hand Login, consistent wood and original lettering. Verify portrait proportions, small screens, scroll behavior, full equipment visibility, loading/error states, and explicit character entry. Screenshot fidelity does not establish that account-management commands work.

### Implementation alignment anchors

Use classic gfx001/11 as one uniformly scaled 276×123 panel. Its caption, portrait well and name well are already painted into the source; never nine-slice through those details. Dynamic name/level and equipped portrait use the matching source-coordinate regions. Login retains original lettering and a minimum 44-point touch target. Adjacent arrows have separate 44-point targets; pending world entry disables navigation and Login.

The desktop column and Delete action described in the historical reference are observations, not the current mobile layout. See [desktop implementation study](DESKTOP-UI-IMPLEMENTATION.md).


The Back action also belongs to the reserved controls region. Its full touch area must end above visible illustration bounds. Recompute that boundary after the stack's own layout when switching states or character cards, and check it on a compact phone.

Status and errors sit above the roster. Splash pixels extend beneath the foreground bottom wood, not a fixed inset above it.
