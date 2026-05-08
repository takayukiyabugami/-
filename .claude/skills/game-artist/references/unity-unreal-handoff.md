# Unity and Unreal Handoff

Use this reference when the asset must survive engine import and implementation.

## Shared Handoff Package

Include:

- Source file path and DCC version.
- Export file path and format.
- Engine target and render path.
- Scale unit, forward axis, up axis, pivot, and origin.
- Preview image from DCC and engine.
- Texture sets with resolution, compression intent, color space, and channel packing.
- Material slot list and purpose.
- LODs, collision meshes, sockets, markers, and naming rules.
- Animation clips, frame ranges, root motion policy, and retargeting notes.
- Known risks and fallback cuts.

Naming rules:

- Use predictable prefixes: `SM_`, `SK_`, `M_`, `MI_`, `T_`, `UI_`, `VFX_`, `AN_`, or the project's own convention.
- Avoid spaces, localized characters, and ambiguous suffixes in exported asset names.
- Name texture maps by material and channel, not by temporary art notes.

## Unity Notes

Meshes:

- Confirm scale factor, import units, normals, tangents, readable mesh setting, compression, and blend shape import.
- Use `ModelImporter` settings consistently across similar assets.
- For skinned meshes, choose Humanoid only when the rig fits Unity's avatar requirements. Use Generic for creatures, props, and custom skeletons.
- Validate Avatar mapping, T-pose/A-pose conversion, root motion, animation compression, and clip looping.
- Use `LODGroup` for real-time meshes that need distance degradation.

Materials and shaders:

- Confirm URP, HDRP, or Built-in compatibility before assigning shaders.
- Prefer shared materials and material variants over one-off duplicated materials.
- Limit Shader Graph keywords and variant counts.
- Use GPU instancing and SRP Batcher-compatible layouts when possible.
- Provide low-cost fallback materials for mobile or emergency quality cuts.

Textures:

- Set correct color space: sRGB for base color, linear for masks, normals, roughness, metallic, AO.
- Use mipmaps for 3D textures unless there is a UI-specific reason not to.
- Use platform compression settings, max size overrides, and texture arrays or atlases where appropriate.
- Use Sprite Atlas, 9-slice sprites, and correct pixels-per-unit for UI or 2D assets.

UI:

- For Canvas UI, specify anchors, pivots, safe areas, layout groups, and scaling mode.
- For UI Toolkit, specify USS class roles, sprite/vector asset needs, and responsive breakpoints.
- Use TextMeshPro font assets for localized text where applicable.
- Test controller focus order and touch hit target sizes.

Scenes and environments:

- Use static batching, GPU instancing, occlusion culling, light probes, reflection probes, baked lighting, and addressable streaming when appropriate.
- Separate collision, render mesh, and navigation concerns.
- Keep shadow casters deliberate.

## Unreal Notes

Meshes:

- Confirm FBX import scale, normals, tangents, smoothing groups, collision import, sockets, and LODs.
- Use Skeletal Mesh only when deformation or animation needs it. Use Static Mesh for rigid props.
- Validate skeleton compatibility, IK rigs, retarget poses, physics assets, sockets, and root motion.
- Use Nanite for dense static geometry when the project and platform support it. Do not assume Nanite for skinned meshes, masked foliage, or unsupported targets.
- Use HLOD, World Partition, culling, and impostors for large environments.

Materials and shaders:

- Prefer master materials with Material Instances.
- Keep static switch parameters controlled; they create shader permutations.
- Verify material domain, blend mode, shading model, feature level, and platform support.
- Avoid expensive translucent, refraction, pixel depth offset, and world position offset unless budgeted.
- Use Material Quality Switch or platform quality variants for fallback.

Textures:

- Set texture groups correctly: World, Character, UI, NormalMap, Masks, etc.
- Use sRGB only for color textures.
- Use virtual texturing only when the project already supports it and streaming behavior is understood.
- Channel-pack masks to reduce samples when it does not harm authoring.

UI:

- For UMG, specify DPI scaling, anchors, focus navigation, controller support, and localization space.
- Keep UI textures in the correct texture group and compression mode.
- Avoid full-screen translucent or blur-heavy widgets unless measured.

Lighting and environments:

- Decide early between Lumen, baked lighting, static lighting, or hybrid lighting based on platform.
- Control shadow casters, reflection captures, virtual shadow maps, foliage density, and post-process volumes.
- For gameplay levels, separate blockout readability from set dressing density.

## Engine-Specific Failure Checks

Unity:

- Pink material: render pipeline mismatch, missing shader, bad Shader Graph target, missing package.
- Broken rig: Avatar mapping, wrong rig type, changed hierarchy, bad bind pose, animation compression.
- FPS drop: overdraw, realtime lights, shadows, material variants, missing LODs, particle cost.

Unreal:

- Shader compile spike: too many material permutations, static switches, unique master materials.
- Retarget failure: wrong skeleton, retarget pose mismatch, root bone mismatch, missing IK setup.
- FPS drop: translucent materials, VFX, virtual shadow maps, Lumen settings, foliage, missing HLOD.

## Acceptance Checklist

- Imports without warnings that affect runtime behavior.
- Looks correct in an engine test scene, not only in DCC.
- Meets stated FPS and memory targets or has documented fallbacks.
- Has correct scale, pivot, collision, materials, textures, and LODs.
- Rig and animations pass idle, movement, extreme pose, attachment, and retarget tests.
- Shader has a low-cost fallback and controlled permutations.
- Handoff includes enough notes for engineering to integrate without reverse engineering the asset.
