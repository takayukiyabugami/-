# Art Brief Templates

Use these templates as flexible structures. Remove irrelevant lines instead of filling them with noise.

## Shared Sections

- `Visual Goal`: what the player should immediately understand.
- `Gameplay Use`: how the asset affects navigation, combat, status, threat, reward, story, or interaction.
- `Shape Language`: dominant shapes, silhouette, scale, rhythm, and contrast.
- `Palette and Values`: base colors, accents, value grouping, faction or rarity logic.
- `Materials`: surface language, texture density, wear, reflectivity, emissive use.
- `Production Notes`: file types, naming, scale, pivots, atlas or trim use, export expectations.
- `Technical Guardrails`: budget assumptions, LODs, texture size, material slots, rig or shader limits.
- `QA Checklist`: readability, engine import, performance, animation, collision, states, fallback.

## 2D UI

Include:

- Screen or HUD context
- Information hierarchy
- Interaction states: default, hover/focus, pressed, disabled, selected, alert, cooldown
- Input mode: mouse, controller, touch, keyboard, VR pointer
- Layout behavior: aspect ratios, safe area, anchoring, scaling, localization expansion
- Typography direction: size classes, weight, contrast, fallback for long text
- Icon language: silhouette, stroke weight, filled/outline rules, hit target size
- Color semantics: neutral, danger, success, rarity, interactable, locked
- Export specs: sprites, 9-slice panels, atlases, SVG/vector only if pipeline supports it
- Engine notes: Unity Canvas/UIToolkit or Unreal UMG, DPI scaling, sprite atlases, font assets

QA checks:

- Text remains readable at the smallest supported resolution.
- Buttons do not shift when states change.
- Icons remain recognizable without color.
- Critical UI passes contrast and color-blind safety checks.
- No tiny text is baked into images unless localization is impossible.

## Illustration and Key Art

Include:

- Subject and story moment
- Composition: camera, focal point, foreground/midground/background
- Pose or action
- Lighting plan
- Mood and emotional target
- Palette and value structure
- Prop, costume, or environment callouts
- Cropping needs: store capsule, splash, thumbnail, banner, card frame
- Image prompt when needed
- Negative prompt: copied IP, unreadable text, extra limbs, clutter, logo artifacts, inconsistent details

QA checks:

- The focal point reads in thumbnail size.
- The image supports the game's genre and promise.
- Important gameplay objects are not hidden by effects.
- Crops do not cut through faces, hands, weapons, or UI-safe areas.

## Concept Art

Include:

- Design problem
- Exploration axes: silhouette, material, faction, scale, threat level, rarity, biome, era
- Chosen direction and rejected alternatives
- Functional callouts
- Orthographic or three-quarter needs
- Variants: safe, bold, production-ready
- Paintover or model-sheet notes if relevant

QA checks:

- The concept can be modeled, rigged, textured, and animated.
- The silhouette reads from the gameplay camera.
- Details concentrate where players actually look.
- The design has one memorable motif, not ten small ones.

## 3D Model

Include:

- Asset role: hero, gameplay prop, repeated prop, background, cinematic
- Scale and pivot
- Silhouette from gameplay camera
- Topology intent: deformation loops, hard-surface bevels, modular seams, collision proxy
- Material slot plan
- Texture set plan
- LOD or Nanite/HLOD plan
- Rig or animation needs
- Export format: FBX/glTF/USD only if pipeline supports it
- Engine import notes

Starting budget assumptions when no project budget exists:

- Repeated small prop: 500-3,000 triangles, 1 material, 512-1K textures.
- Hero prop or weapon: 5,000-30,000 triangles, 1-3 materials, 1K-2K textures.
- Player or major NPC: 40,000-100,000 triangles on PC/console, 2-5 materials, 2K-4K hero textures.
- Mobile character: 5,000-25,000 triangles, 1-3 materials, 1K-2K textures.
- Modular environment piece: 500-15,000 triangles, trim sheets or tileables preferred.

Label these as starting points. Replace them with project budgets when available.

QA checks:

- Pivot, scale, forward axis, and origin are intentional.
- Normals and tangents import correctly.
- Material slots are not multiplied by convenience.
- LODs preserve silhouette and collision expectations.
- Deformation zones have enough geometry and clean weights.

## Texture and Material

Include:

- Surface purpose: readable material identity, gameplay signal, mood, wear, faction
- Workflow: PBR metallic/roughness, stylized, hand-painted, trim sheet, tileable, atlas, decal
- Map list: base color, normal, roughness, metallic, AO, emissive, height only when justified
- Resolution plan by asset role
- Channel packing plan
- Mipmaps and compression expectations
- Tiling scale and texel density
- Shader features and fallback material

QA checks:

- Base color is not carrying baked lighting unless the style requires it.
- Roughness values separate materials at gameplay distance.
- Normal intensity does not shimmer or break silhouettes.
- Emissive surfaces have a performance and bloom plan.
- Texture memory fits the platform.

## Environment and Background

Include:

- Player route, camera, and sightlines
- Landmark hierarchy
- Modular kit list
- Ground, wall, ceiling, vista, sky, decal, foliage, and prop layers
- Lighting mood and gameplay readability
- Collision and traversal notes
- Occlusion, streaming, LOD/HLOD, foliage density, and shadow strategy
- Set dressing rules: density, repetition control, interactable contrast

QA checks:

- The critical path is readable without UI.
- Cover, hazards, exits, and interactables have distinct visual language.
- Repeated modules hide tiling without adding excessive materials.
- Background detail does not compete with gameplay targets.
- Lighting remains readable after engine import.

## Technical Art Bridge

Use this when translating art intent into engineering-safe requirements:

- `Art Goal`: what must survive technically.
- `Risk`: FPS, memory, rig, shader, animation, collision, lighting, UI scaling.
- `Constraint`: target engine, platform, budget, pipeline, existing assets.
- `Implementation Rule`: the non-negotiable handoff rule.
- `Fallback`: what to cut first if performance or stability fails.
- `Verification`: profiler, frame debugger, rig test, shader compile, import test, screenshot comparison.
