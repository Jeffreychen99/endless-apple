# iPhone login screen

Read [shared UI rules](UI-STYLE.md) first. Build a coherent portrait adaptation of the original desktop login scene, not a generic centered form laid over an illustration.

## Reference composition and visual character

Compare [classic login](references/desktop-login-classic.png) and [updated login](references/desktop-login-updated.png). Both put the logo toward the upper-left and use a muted, low-contrast landscape behind much larger foreground figures. Strong character outlines, bright hair/clothing and overlapping silhouettes make the illustration the emotional focus. Open background space gives branding and controls somewhere to sit.

The classic scene has a honey-wood perimeter, parchment-like landscape and a short, evenly spaced action stack at lower-left; the figures occupy the right. The updated crop uses a gray/olive background, prominent crescent logo, and a labeled wood credential panel with dark input wells. Its rectangular art is part of a complete scene, not a separate banner pasted onto another background.

For portrait, preserve the relationship of quiet space, identity and illustration rather than scaling the full desktop screenshot. Place the always-visible credential panel near the top in a reserved region; keep supporting actions compact and deliberately aligned off-center, with the splash anchored into the remaining illustration region. Shift/crop the background and illustration as separate original layers. Do not shrink foreground art until it becomes a thumbnail or cover its focal faces with an oversized form.

The desktop reference does show controls overlapping parts of the splash; the user's explicit request for clear focal art takes precedence. Likewise Play Game/Exit in the screenshot are not requirements. Do not reproduce screenshot account-field text, version labels, or incidental capture clipping.

Use warm framing per the shared guide. Whether sourcing older rectangular plaques or newer clipped-end plaques, choose one coherent action treatment for this screen, including View Credits. Credentials should look like fitted dark wells within the same wood panel, not unrelated rounded system fields.

## Composition and art

- Use the original desktop background, Endless Online logo, wood frames, foreground splash art and button resources.
- Integrate the logo with the background: no mismatched rectangular patch or duplicate logo. Inspect the composition as a whole rather than hiding the seam with another panel.
- Use deliberate asymmetry. Reserve an area for the illustrated character and a separate area for the form/actions. Keep focal artwork clear of controls; adapt to safe areas and keyboard space.
- Choose foreground splash art when the login page is freshly entered, including return from character selection or app reopening. Keep it stable for that page visit. No timer-driven rotation every few seconds; keyboard changes, status updates and redraws do not refresh it.
- No gameplay-map preview on this screen. World rendering belongs after explicit world entry.

## Form and controls

- Keep username/password inputs visible near the top without a Play Game popup trigger. No Play Game or Exit Game button.
- Provide the sign-in action and View Credits; Create Account remains pending. Omit Check Connection and Forget Account. Use the same original wood/button styling for all; the Credits control must not look like a different UI system.
- Preserve original bitmap lettering proportions and a readable dynamic-font fallback where original lettering is unavailable. Use common spacing, frames and pressed behavior from the shared guide.
- Username Next moves to password; password Go submits. Support keyboard/AutoFill without tearing down sign-in or hiding the usable form.
- Connect owns connection/sign-in progress. Put concise pixel status and error text above the credential panel, and above the roster on selection, so keyboard entry does not cover feedback. Retain deliberate cancel/retry.

## iPhone auto-login

- Offer an opt-in “Remember me” control. Default off. Save only after accepted authentication, in device-only Keychain storage.
- On a subsequent app launch, opted-in credentials make one automatic sign-in attempt, leading to character selection. Never automatically choose a character or enter the world.
- Show a cancellable signing-in state. On failure, return to the usable login form without an automatic retry loop.
- Clear the previous character list when switching accounts. Returning to login may retain the opted-in account; unchecking Remember me removes it. There is no separate Forget Account button.
- A temporary system overlay must not cancel sign-in. Background handling must preserve the consent/storage policy. Never expose the password in normal labels, diagnostics or screenshots.

## Cohesion across states

Align logo, field labels and supporting plaque edges to a small set of deliberate anchors. Reserve the form region before cropping splash art; do not solve keyboard visibility by shrinking or randomly relocating every element. Pending/error text belongs within the existing form region. Keep the illustration selected for this visit stable during these changes.

Login and character selection should feel like connected views of one illustrated scene: reuse the outer material, logo treatment, form/name wells and action lettering scale. Selection replaces the credential task with a roster; it should not introduce a new color palette or a gameplay preview.

## Verification

Review initial entry, stable splash while typing, refresh after returning from selection, consistent action buttons, small-phone keyboard/AutoFill, accepted sign-in, failed sign-in, cancellation, remembered-account launch, unchecking Remember me and account switching. Keep the illustration readable in each state. Auto-login test completion is tracked in TODO, not implied by this guide.

### Seam and typography review

Use consecutive rows of one original scene for the portrait background. Keep the integrated logo at its own aspect ratio, extend wood at the safe area by tiling, and avoid a separate banner. Inputs use the shared desktop pixel typeface. Original Connect and View Credits lettering and matching wood plaques take priority over system/Georgia labels.


### Latest form spacing and illustration feedback

Remember me sits at the left of the form footer; original Connect sits at the right. Keep padding between both input wells and the surrounding wood. Splash art extends beneath the bottom frame: its bottom pixels must be occluded by the foreground wood, with no background gap or protrusion over the frame. See [desktop implementation study](DESKTOP-UI-IMPLEMENTATION.md).

### Bottom anchoring and matching plaques

Trim transparent padding from splash bounds for visual centering. Bottom-align visible artwork flush with, or slightly beneath, the foreground wooden frame; no floating cut-off base. View Credits keeps its original plaque proportions. Avoid stretching painted inner highlights from the panel behind fields.


Fit visible splash bounds below the final laid-out action stack on short phones, not a fixed percentage that can cover faces. The username and both placeholders use EO pixel typography; password content remains a native secure field. Never replace secure entry with an ordinary text label to make captures easier. A simulator accessibility keyboard node does not prove a visible software keyboard; check its intersection with the screen and preserve an explicit verification gap when unavailable.

The background contains painted bottom wood: exclude that band from any vertically stretched background slice. Render the complete band once above the splash, with one continuous top edge across the screen. Layer order alone cannot fix a duplicate band at a different height.

View Credits is approximately 12% narrower than the previous 220pt plaque (193pt), retaining a 44pt touch target.
