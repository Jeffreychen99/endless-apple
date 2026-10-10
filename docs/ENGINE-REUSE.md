# Reuse the desktop engine before duplicating behavior

Assessment: October 9, 2026. This is a source/dependency assessment, not a completed migration or proven MonoGame iOS build.

## What is available

The checkout is Ethan Moffat's MIT-licensed C# EndlessClient, an open-source implementation of Endless Online. It is not the source of the current official Windows executable. `README.md` identifies the upstream project. The downloaded official client is an executable plus assets under ignored `.local/official`.

Desktop `EndlessClient.csproj` targets .NET 8 and MonoGame on Windows/Linux/macOS. It depends on EOLib, resource managers, content pipeline, input/UI libraries and platform hosting. EOLib and EOLib.IO target netstandard2.0, making shared-library reuse worth investigating. Their target framework alone does not prove all dependencies/platform behavior work on iPhone.

The iPhone target currently references only `Apple/Endless.Protocol` and `Apple/Endless.World`, with UIKit for controls and a separate CPU sprite renderer. Thus source sitting in the repository does not currently execute in the iPhone build. This split created avoidable duplication of rendering rules.

## What genuinely differs

The current official profile is 0.4.57. Existing initialization evidence (`INIT-0457.md`) records rejection of the older public-schema request and successful current-protocol adaptation. Current item/map formats and graphics strides also differ from legacy assumptions. Keep this verified adapter until an alternative proves equivalent. Desktop source is a primary implementation reference, but not automatic proof of current-server wire compatibility.

Mobile-specific work remains necessary: touch input, portrait UI, safe areas, iOS lifecycle, device Keychain and keyboard handling. It does not justify inventing a second set of weapon-layer or equipment-pose rules.

## Preferred boundaries

| Area | Approach |
| --- | --- |
| Sprite selection, layer order, animation state | Extract/adapt desktop pure rules into a shared library; preserve current asset strides behind adapters. Start here. |
| Map ordering, collision and path logic | Compare desktop implementation with current format/state; share compatible algorithms and fixtures. |
| Inventory, quests, NPC interaction models | Reuse compatible domain rules; adapt server messages rather than blindly transplanting classic handlers. |
| Packet dialect/current item/map decoding | Retain verified current adapter; add compatibility evidence before replacement. |
| GPU drawing/platform window | Evaluate a small MonoGame iOS host before deciding whether to migrate rendering. |
| Login/HUD/control arrangement | Keep iPhone-specific composition using shared state/art; desktop screen dimensions/input are not reusable as-is. |

## Concrete next work

1. Isolate desktop `CharacterSpriteCalculator`, `CharacterPropertyRendererBuilder` and property-renderer pose rules from texture loading. Return neutral sprite commands to both rendering backends. Cover melee, bow, shield/quiver, walking, sitting and casting with original-asset pose fixtures.
2. Build a minimal iOS MonoGame renderer using synthetic actors and local original assets, with no login. Verify device/simulator build, AOT, content loading, lifecycle, touch/keyboard coexistence and performance. Do not replace the app until this works and the benefit is measured.
3. Compare shared GPU host versus the existing UIKit shell plus shared rules. Choose based on demonstrated compatibility, not assumed portability.
4. Migrate one subsystem at a time with retained regression coverage. Leave current official login/world-entry behavior intact until equivalents pass.

For each gameplay TODO: find desktop code first, record whether it can be reused directly, extracted or adapted, and only write a separate implementation when a concrete incompatibility requires it. The October 9 missing foreground weapon part and absent quiver attack frames were preventable source-reuse misses.

## Implemented first boundary

Apple.World now compiles the original desktop `WeaponSpriteType.cs` directly as a linked source, using its swing/foreground/shooting constants. The wider sprite calculator still needs extraction; this small reuse does not imply engine migration. Current assets include completely blank Old Choppy strike frames. The mobile asset-aware fallback retains a nonempty wind-up (or standing pose) only when all weapon parts are empty; valid split frames keep desktop order. Exact thrown/projectile behavior remains unverified. Resource-visibility checks are cached, and world depth traversal uses a cursor instead of repeated list removal.

Touch camera panning is a mobile input adaptation. `CameraPan` uses the inverse of the existing 64×32 `WorldCamera` projection and leaves actor/server coordinates untouched; it does not introduce a second projection or alter desktop render rules. The offset is snapshotted before the render worker, and picking uses the camera that produced the displayed frame.
