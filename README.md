# Endless Apple

An iPhone-first Endless Online client built with C#/.NET and UIKit. Mac Catalyst is experimental and deferred. This is an independent project; it is not the official Endless Online client.

## Layout

- `ios/Endless.iOS`: iPhone UI, controls and application lifecycle.
- `engine/apple/Endless.Protocol` and `engine/apple/Endless.World`: current-server protocol, state and rendering.
- `engine/apple/Endless.Protocol.Tests` and `ios/Endless.UITests`: offline and simulator checks.
- `mac/Endless.Mac`: experimental Catalyst host; currently shares iPhone UI source.
- `docs`, `scripts`, `TODO.md`: design references, tooling and current implementation status.

The engine is currently C#/.NET; this layout does not imply a Swift rewrite. iPhone and Catalyst reference the same engine projects. Catalyst still temporarily compiles iPhone UI source and is not yet an independent Mac interface.

## Development

Read [AGENTS.md](AGENTS.md), [TODO.md](TODO.md), and [Apple setup notes](APPLE-README.md). Use `scripts/test.sh` for offline checks and `scripts/build-ios.sh` for a simulator build. Xcode and local .NET SDKs are required; bootstrap scripts prepare those SDKs and UI fonts. No credentials are needed for offline tests.

Game archives, maps, fonts generated during setup, package caches, screenshots and build outputs are local and ignored. App builds require original assets under `.local/official` and classic UI archives under `packages/endlessclient.binaries/1.4.0.2/build/net462/client/gfx`; see setup notes. A fresh clone does not include those assets.

## Desktop reference and attribution

The full [Ethan Moffat EndlessClient](https://github.com/ethanmoffat/EndlessClient) MonoGame source is excluded from this repository. The existing local checkout is retained under ignored `reference/monogame/`. Legacy MonoGame build/run scripts require that optional source and are not Apple build prerequisites.

The Apple build retains a small sprite enum and bitmap-font source extracted from that client, with the upstream MIT notice in [LICENSE](LICENSE). See [source attribution](engine/apple/SourceAssets/README.md). Original game artwork is not covered by that source-code license.
