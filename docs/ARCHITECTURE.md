# Apple client architecture

Keep the existing C#/MonoGame client as a reference and desktop baseline. Current Recharged support must be a separate adapter; do not silently replace the classic packet types with guessed fields.

- `engine/apple/Endless.Protocol`: dependency-free .NET 8 framing, initialization, strict response parsing and cancellation. Shared by the command-line probe and iPhone shell.
- `tools/Endless.Probe`: explicit one-shot network diagnostic. No login feature, automatic retries or network access on launch without an action.
- `engine/apple/Endless.Protocol.Tests`: offline wire-format and stream-fragmentation regression runner.
- `ios/Endless.iOS`: UIKit login shell sharing the protocol layer. Account fields stay disabled; connection checks report reply code, size and elapsed time. rendering and authentication are intentionally pending compatibility evidence. It is not yet a playable port.
- `EndlessClient`: upstream desktop gameplay/rendering, with an optional portable DesktopGL Mac build.

Next extract platform-independent game state and commands from the desktop controllers, then attach touch and keyboard input to the same command layer. Avoid a hosted proxy. Keep credentials in platform secure storage only after authentication is understood; never in profiles, diagnostics, arguments or backups. Treat server data as untrusted and bound every length before allocation.

The first iOS shell uses native text and controls, so dynamic text sizing, safe areas, scrolling and app suspension can be handled before the MonoGame render host is integrated. UIKit does not replace the planned game renderer.

Do not represent a successful INIT response as a playable connection: keep initialization, authentication, character selection and world-entry states distinct.

## iPhone art requirement

User requirement (October 5, 2026): the iPhone game must use the desktop client's original art. The native login shell is temporary. Reuse the desktop sprites, tiles, character/equipment layers, animations and applicable interface artwork; adapt safe areas, touch targets and layout for iPhone. Preserve pixel edges, transparency, frame timing and resource IDs during any texture conversion. Do not replace the game art with generated or substitute illustrations. Keep an explicit mapping from desktop resource IDs to mobile textures, and validate representative maps and character layers against the desktop renderer before integrating gameplay. Asset integration can follow current-server connectivity and authentication.
