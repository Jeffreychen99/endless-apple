# Short gameplay reference notes

Reviewed October 9, 2026. Presentation evidence only; the current official server remains the product target.

## User-supplied clip

[Endless Online Nostalgia — Reaper Boss Guide](https://www.youtube.com/watch?v=UPjlciRclzg), channel Endless Nostalgia. YouTube displayed **1:52**. The title/description identify Nostalgia, not a verified recording of the current official client/server. Method: brief playback and sampled visible frames at 0:00, approximately 0:14, 0:30, 1:00, 1:30, 1:40 and 1:50. This was not a frame-by-frame timing analysis or uninterrupted full viewing. No video downloaded; timestamps link to the original.

| Sample | Directly visible | Useful implication |
| --- | --- | --- |
| [0:00](https://www.youtube.com/watch?v=UPjlciRclzg&t=0s) | Large dark enemy sprites around a smaller purple-clad figure; compact overhead health bar and MISS text; grass, rocks and openings share the isometric scene | Preserve relative actor scale and compact world-space feedback; this frame cannot establish who caused a hit or its rules |
| [0:14](https://www.youtube.com/watch?v=UPjlciRclzg&t=14s), [0:30](https://www.youtube.com/watch?v=UPjlciRclzg&t=30s) | Outdoor path/trees/sign; lower inventory lattice and side icon rails remain visible | Inventory can coexist with the world rather than replacing it; outdoor silhouettes and paths need visual clarity |
| [1:00](https://www.youtube.com/watch?v=UPjlciRclzg&t=60s) | Rail-lined ground with character above the same lower inventory shell | Different map scenery should not introduce another HUD theme |
| [1:30](https://www.youtube.com/watch?v=UPjlciRclzg&t=90s), [1:40](https://www.youtube.com/watch?v=UPjlciRclzg&t=100s) | Gray rocky/cave-like terrain, water/void edges and creatures; unchanged inventory structure | Maintain recognizable UI framing across environments; do not fill genuine map voids with invented tiles |
| [1:50](https://www.youtube.com/watch?v=UPjlciRclzg&t=110s) | Creator's end card | Not a client screen or styling reference |

The clip has guide captions, watermark, cropping and a decorative side treatment that differs from the user's desktop references. Do not port those into the app. Inventory coexistence across samples is visible; camera behavior during menu opening, exact movement smoothness, playback/editing speed, attack cadence, NPC dialogue, chest use, equipment mutations and portal semantics were **not verified**. No claims about current official-server functionality follow from this clip.

## Resulting design decisions

Use the user's eleven saved desktop captures as the primary material/layout reference. Use this clip for contextual world/HUD coexistence and scale only. Warm classic frames plus updated official status sprites remain the chosen style; do not copy a whole variant-client skin. The [gameplay guide](../GAMEPLAY.md) records mobile interaction requirements and official-server verification separately.

Future video research should record title, channel, duration under five minutes, exact observed timestamps and server/version uncertainty. Prefer a verified current-official clip for unresolved behavior. Do not record timing estimates as constants from edited or sped-up footage.

October 9, 4:01 follow-up: reopened the linked 1:52 clip and sampled playback at 0:24 (outdoor path/inventory). This sample did not resolve equipped attack layers, and the browser session expired before further combat inspection. Animation fixes in this pass are grounded in the local desktop sprite-calculator/property-renderer source and current asset contact sheets, not a claim of verified video attack timing.
