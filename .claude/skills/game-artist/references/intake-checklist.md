# Intake Checklist

Use this reference to gather only the information that changes the art direction or technical plan. Do not interrogate the user for every field.

## Classify the Request

- `2D UI`: HUDs, menus, icons, buttons, inventory, reticles, diegetic UI, UX visuals, UI style guides.
- `illustration`: key art, splash art, card art, item art, marketing-adjacent game illustration.
- `concept art`: visual exploration, mood frames, shape language, faction style, props, characters, vehicles, locations.
- `3D model`: characters, props, weapons, vehicles, modular pieces, hero assets, destructibles.
- `texture/material`: PBR sets, stylized materials, trim sheets, atlases, decals, shader-driven surfaces.
- `environment/background`: levels, biomes, arenas, skyboxes, vista, modular environment kits, background plates.
- `technical art pass`: FPS risk, rig safety, shader/material complexity, import repair, texture memory, LODs, lighting, VFX cost.

## High-Impact Fields

Ask about these only when missing information would materially change the output:

- Game genre and camera: FPS, TPS, top-down, side view, isometric, VR, mobile portrait, etc.
- Engine and render path: Unity URP/HDRP/Built-in, Unreal deferred/forward/mobile, unknown.
- Target platform: mobile, Switch-like handheld, PC, console, VR, web.
- Asset role: hero asset, repeated prop, background dressing, gameplay-critical object, UI core element.
- Visual style: realistic, stylized, anime, painterly, low poly, tactical, horror, cozy, sci-fi, fantasy.
- Deliverable: written brief, prompt, critique, asset spec, repair plan, engine handoff checklist.
- Constraints: FPS target, texture memory, polygon budget, file format, existing rig, existing shader, team pipeline.
- References: user-provided images, games, mood words, palette, production constraints. Extract abstract traits, not protected identity.

## Default Assumptions for Thin Requests

State these when using them:

- Output language: Japanese.
- Design originality: original asset direction, no copied IP or named artist style.
- Engine: Unity/Unreal-neutral real-time game handoff unless the user specifies one.
- Performance target: stable 60 FPS on the stated platform; if no platform is stated, assume mid-range PC/console and provide scale-down options.
- UI: 16:9 1080p baseline with responsive safe-area notes.
- Materials: PBR metallic/roughness workflow unless stylized flat shading is requested.
- Textures: use mipmaps, engine-native compression, and channel packing where appropriate.
- 3D: real-time topology, limited material slots, LOD plan, clean pivot, sane scale, and engine-friendly naming.
- Rigs: one clear root, clean bind pose, normalized weights, controlled bone influences, no destructive hierarchy changes after animation starts.

## Question Policy

- Ask at most 3 questions.
- Prefer questions about platform, engine, and asset role before style details.
- If the user asks for speed or says to decide, do not block. Make assumptions and proceed.
- If diagnosing a broken asset, ask for the single missing artifact that separates likely causes.

## Red Flags

Escalate into a technical art pass when the prompt mentions:

- FPS drops, stutter, memory spikes, shader compile delays, long loading, broken batching.
- Broken skeletons, twisted limbs, bad retargeting, lost animation, bad bind pose, mesh exploding.
- Shader errors, pink materials, too many variants, transparent sorting, overdraw, runtime crashes.
- Huge textures, too many materials, high draw calls, missing LODs, heavy realtime lights, excessive particles.
