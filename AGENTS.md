# Endless Apple project instructions

## Scope and continuity

- Current active product is the native iPhone client for the current official Endless Online server. Mac client work is deferred unless the user explicitly resumes it. No Wine dependency.
- Read `TODO.md` before continuing implementation. Work from its current priorities and keep its status accurate. Remove completed tasks and superseded feedback instead of accumulating checkpoint logs; use Git history for completed work and verification history. Keep concrete unverified behavior and blockers in TODO.md.
- Do not install/update the physical phone without a user request for that update. Simulator checks are part of normal implementation.

## Required UI design references

Before creating, editing, or reviewing any UI, layout, styling, artwork, camera/HUD behavior, or touch interaction:

1. Read `docs/UI-STYLE.md` for shared visual and interaction rules.
2. Read each applicable screen guide:
   - Gameplay, controls, camera, menus: `docs/HUD-STYLE.md` and `docs/GAMEPLAY.md`
   - Login, credentials, splash artwork: `docs/LOGIN-STYLE.md`
   - Character cards and selection: `docs/CHARACTER-SELECT-STYLE.md`
3. Inspect the relevant saved reference images linked from those guides. Read the actual affected UI, not only the named control; preserve adjacent controls, keyboard behavior, safe areas, and collapsed/expanded states.
4. Before reporting a major checkpoint complete, do a separate second-pass review after implementation and the first verification. Inspect fresh rendered screenshots of every changed screen against the latest user feedback and desktop references; check adjacent controls, texture repetition, color seams, artwork centering/edge alignment, typography, spacing, and touch-target overlap. Fix findings and re-render affected screens. Compilation and a first glance are not sufficient. Record unresolved cases honestly in TODO.md.
5. Before delivery, verify the changed screens against the guides. Use simulator/visual inspection when the change is visual. Do not claim visual verification from compilation alone. Record material unverified cases in `TODO.md`.

These guides describe the intended design, not the current implementation. Existing code and historical screenshots may contain known defects and must not override the guides. Use the user's newest explicit feedback over older rules. If feedback establishes a reusable rule, update the shared guide or relevant screen guide and link the concrete work from `TODO.md`; avoid contradictory copies of the same rule.

## Desktop engine reuse

- Read `docs/ENGINE-REUSE.md` before expanding gameplay/rendering systems. Inspect and reuse/adapt the desktop implementation first. Separate iPhone UI/platform concerns from reusable game rules; document concrete current-server or dependency incompatibilities when direct reuse is not possible. Do not equate the open-source desktop client with the current official client source, or claim a MonoGame iOS migration without a working proof.

## Gameplay changes

Before changing movement, combat, world entry, entity lifecycle, inventory/equipment, interactions or session behavior, read `docs/GAMEPLAY.md` even when no UI file changes. Consult its evidence notes before treating a reference video as proof of official-server mechanics.

## Assets and account data

- Reuse original desktop artwork, textures, sprite sheets and button lettering. Follow the shared guide's sourcing and scaling rules; prefer original assets. The user now permits custom matching pixel art for D-pad keys and other UI cosmetics; follow docs/UI-STYLE.md and identify custom artwork honestly.
- Keep reference images under `docs/references`, with their source/purpose documented. Do not commit proprietary client archives, build output, credentials, or authenticated packet captures as references.
- iPhone auto-login is opt-in and uses device-only Keychain storage. Never place credentials in code, preferences, logs, screenshots, or test fixtures. Use synthetic accounts for storage tests.
- Keep observed protocol behavior separate from guesses. Use bounded tests, validate packets before changing state, and never claim gameplay compatibility from a successful build or connection alone.
