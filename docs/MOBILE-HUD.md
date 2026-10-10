> Historical implementation notes, not the current design contract. Read [HUD style](HUD-STYLE.md) and [shared UI style](UI-STYLE.md) for current requirements; [TODO](../TODO.md) for status. Earlier joystick, menu-resizing camera, unavailable-action and data-decoding descriptions below may be superseded.

# Mobile HUD

The native iPhone world view now opens a portrait-first HUD using the wood interior from the official gfx002 archive. No Wine runtime is involved.

- Four top tracks: HP, MP, stamina, experience. HP reads the current actor snapshot; other tracks remain unknown rather than inventing values.
- Fixed movement and action area. Direction buttons use the existing world movement queue. Attack and interaction remain disabled until their protocol actions are implemented.
- Bag, Chat, Skills and Stats tabs with a shared collapsible drawer. Selecting the open tab collapses it; switching tabs opens the selected panel. Controls retain their position.
- Drawer panels currently explain that their data is unavailable. This is a layout implementation, not completed inventory or chat support.
- A leave-world button cancels the session and returns to login.

For credential-free layout checks, launch with `--hud-preview`, optionally `--expanded`. Preview loads bundled map 1, performs no network requests, and does not load account credentials.

Remaining: inventory data decoding and item grid/actions; chat receive/send and keyboard layout; stamina/experience/MP decoding; icon crops and accessibility scaling; landscape-specific layout. Use original art from the local official client, never generated mockup art in the application.

Camera/control checkpoint: world rendering uses a 320-pixel-wide camera with height matched to the visible world panel. Live camera follows interpolated player position. The offline preview now includes an explicitly synthetic character and close camera rather than fitting the entire map. A four-direction isometric drag joystick repeats at 120 ms only while movement is permitted, and cancels on release/removal. Tabs and actions use cropped original gfx002 glyphs.

Updated-art checkpoint: gfx002/58 supplies the actual heart/TP and other status tracks; unknown values remain empty. gfx002/1 frames the viewport, gfx002/51 supplies scalable panel corners, and gfx002/50 supplies inset button frames. Inventory starts minimized. Offline preview now allows joystick navigation with a synthetic character and rerenders when menu size changes. Preview HP is synthetic; it is not account data. Raised-tile animation sheets now crop their 64-pixel frame instead of displaying all frames together.

Direct-crop correction: the attack asset now uses attack-sword-crop.png, cropped and rotated directly from the user screenshot; the generated approximation is excluded from the bundle. Buttons use HudButton's 55/255 black overlay while highlighted. Disabled attack/use actions remain disabled until their protocol is implemented. Top and bottom safe areas now repeat the warmed original wood texture. Tab icons have explicit 32-point image views, and joystick/attack/use hit areas are separated.
