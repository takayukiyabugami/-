# Technical Art Safety

Use this reference when art decisions can break frame rate, rigs, shaders, imports, or production handoff.

## Triage Order

1. Reproduce the failure in the smallest scene or asset set.
2. Identify the axis: CPU, GPU, memory, import, animation, shader compile, render order, or content scale.
3. Disable suspect layers one at a time: post effects, lights, shadows, VFX, high-cost materials, animation, collision, streaming.
4. Measure before and after. Use engine profilers, frame debugger tools, console stats, and memory reports.
5. Apply the cheapest reversible fix first.
6. Document the cut line: what quality drops first if the target platform fails.

## FPS and Memory Safety

Common causes:

- Too many materials or draw calls.
- Too many realtime lights or shadow casters.
- High overdraw from transparent UI, particles, foliage, hair, glass, decals, or layered VFX.
- Large textures without mipmaps or with wrong compression.
- High triangle density on assets that occupy few pixels.
- Missing LODs, HLODs, occlusion, impostors, culling, or streaming.
- Expensive post effects, screen-space effects, realtime GI, reflection captures, or planar reflections.
- Heavy animation rigs, cloth, physics, IK, or per-bone update cost.

Fast repairs:

- Reduce material slots and combine surfaces that share shading needs.
- Add LODs or HLODs for repeated props, environments, foliage, and background assets.
- Cap particle counts, lifetime, collisions, lights, and soft-particle usage.
- Replace transparent layers with opaque geometry or masked materials where possible.
- Compress textures, enable mipmaps, reduce non-hero texture resolution, and channel-pack masks.
- Bake lighting where design allows; reduce realtime shadow distance and caster count.
- Move dense detail into normal maps, trim sheets, decals, or baked texture detail.
- Use engine-native instancing, batching, occlusion, and streaming features.

QA gates:

- Test in a representative gameplay camera, not only in an empty asset viewer.
- Check worst-case scenes, not average scenes.
- Capture frame time, draw calls, triangles, texture memory, shader variants, and shadow cost.
- Keep a fallback material and reduced-quality prefab or blueprint for critical assets.

## Rig Safety

Prevent breakage:

- Keep one clear root and stable hierarchy.
- Freeze or apply transforms before skinning according to DCC pipeline rules.
- Avoid negative scale and non-uniform scale in skinned hierarchy.
- Preserve bind pose after animation work starts.
- Use consistent naming for skeleton, bones, sockets, attachment points, and meshes.
- Normalize skin weights and cap influences to the project standard, usually 4 for broad real-time safety.
- Add deformation loops around shoulders, elbows, wrists, hips, knees, ankles, jaw, and eyelids when relevant.
- Use twist bones, helper bones, corrective shapes, or pose-space fixes only when the engine pipeline supports them.
- Separate cloth, hair, accessories, and weapons when they need independent simulation or swapping.

Repair order:

1. Confirm import scale, axis, skeleton, and bind pose.
2. Compare the DCC skeleton against the engine skeleton.
3. Check whether animation was retargeted to the wrong rest pose.
4. Normalize weights and remove stray vertex weights.
5. Rebuild broken constraints in DCC; do not depend on engine import to fix them.
6. Re-export a clean FBX with explicit animation bake settings.
7. In engine, retest idle, locomotion, extreme poses, attachments, ragdoll or physics assets, and facial animation if present.

Red flags:

- Twisted limbs after retargeting.
- Mesh explosion on animation start.
- Missing root motion.
- Different bone count between mesh and animation.
- Zero-length or duplicate bones from export.
- Weighted vertices assigned to helper or hidden bones by mistake.

## Shader and Material Safety

Prevent shader explosion:

- Keep shader feature keywords low and deliberate.
- Avoid multiplying static switches across many materials.
- Avoid per-pixel procedural noise unless it is cheap or baked.
- Avoid dynamic branches, unbounded loops, excessive texture samples, and expensive translucency.
- Keep transparency, refraction, subsurface, tessellation, displacement, and world-position animation rare.
- Provide a fallback material for low quality, mobile, or emergency repair.
- Use material instances and shared master materials instead of unique graphs for every asset.

Repair order:

1. Replace the suspect material with a known-safe material.
2. If the issue disappears, inspect texture samples, keywords, transparency, custom nodes, and feature switches.
3. Strip unused variants or static switches.
4. Bake procedural work to textures.
5. Channel-pack masks and reduce samples.
6. Split gameplay-critical readability from luxury effects.
7. Rebuild the graph from a minimal working material if the original is unstable.

Pink or broken materials:

- Check missing shader references, unsupported render pipeline, compile errors, platform defines, and package dependencies.
- In Unity, confirm URP/HDRP compatibility and shader graph target.
- In Unreal, confirm material domain, feature level, platform shader support, and plugin dependencies.

## Lighting, VFX, and Environment Safety

- Treat realtime shadows as a scarce budget.
- Keep small set dressing props from casting unnecessary shadows.
- Bake static lighting where compatible with the project.
- Use reflection probes or captures with clear update rules.
- Restrict particle lights, collision, depth sorting, and soft particles.
- For foliage, control density, alpha overdraw, wind shader cost, shadow casting, and LOD transitions.
- For large environments, plan streaming, occlusion, HLOD, impostors, and memory budgets from the blockout stage.

## UI Safety

- Avoid baking localized text into images.
- Keep atlases organized by lifetime and screen usage.
- Avoid excessive full-screen translucent overlays and blur layers.
- Test smallest resolution, longest text, controller focus, touch hit targets, and color-blind readability.
- Keep animation polish from causing layout shifts.

## Required Repair Output

When writing a repair plan, include:

- `Symptom`: what is failing.
- `Likely Cause`: best current diagnosis.
- `Fast Safe Fix`: reversible first move.
- `Deeper Fix`: production-quality repair.
- `Verification`: how to prove the fix worked.
- `Fallback`: what to cut if the target still fails.
