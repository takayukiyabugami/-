---
name: game-artist
description: Create production-ready game art direction and asset briefs for 2D UI, illustration, concept art, 3D modeling, textures/materials, environments/backgrounds, and technical art checks for Unity or Unreal. Use when Codex is asked to design, specify, critique, or troubleshoot game visuals, art pipelines, asset handoff, FPS-safe art budgets, rig-safe model specs, shader/material sanity, or art/engineering bridge work.
---

# Game Artist

## Overview

Create game art briefs that a production artist, technical artist, or engineer can use without guessing. Cover visual intent and technical survival: readability, asset budgets, import rules, rig safety, shader sanity, engine handoff, and QA.

Default to Japanese output unless the user asks for another language. Keep the skill files in English to avoid encoding drift.

## Workflow

1. Read `references/intake-checklist.md`.
2. Classify the request as one or more of:
   - `2D UI`
   - `illustration`
   - `concept art`
   - `3D model`
   - `texture/material`
   - `environment/background`
   - `technical art pass`
3. Ask at most 3 missing high-impact questions. If the request is thin, state assumptions and proceed.
4. Read `references/art-brief-templates.md` before producing any art brief, asset spec, or prompt.
5. Read `references/technical-art-safety.md` when the request touches FPS, optimization, rigs, animation import, materials, shaders, VFX, lighting, texture memory, asset repair, or engine stability.
6. Read `references/unity-unreal-handoff.md` when the output must be implemented in Unity or Unreal, or when the engine is not specified but likely relevant.
7. Produce the requested brief, critique, or repair plan. Tie every visual decision to player readability, production use, or engine constraints.

## Output Contract

Include these sections unless the user asks for a narrower output:

- `Assumptions`
- `Art Direction`
- `Asset Brief`
- `Technical Guardrails`
- `Engine Handoff`
- `Failure Risks`
- `QA Checklist`
- `Next Step`

For every produced asset direction, include:

- What it should look like
- What it is used for in-game
- Implementation notes
- Likely breakpoints
- Check items before delivery

For reviews or troubleshooting, lead with the strongest risks and fastest safe fixes, then give deeper repairs.

## Art Rules

- Make the design original. Do not copy protected characters, franchise identities, studio styles, logos, or living artists' styles.
- Prefer readable silhouette, clear value grouping, constrained palettes, and functional detail over noisy decoration.
- Connect visual choices to gameplay: targeting, navigation, threat, faction, rarity, item function, player status, range, interactability, or mood.
- For UI, specify hierarchy, states, affordances, input mode, contrast, localization space, and responsive behavior.
- For 3D, specify scale, silhouette, topology intent, deformation zones, texture sets, material slots, LODs, collisions, and export handoff.
- For environments, separate gameplay landmarks, modular kit pieces, set dressing, lighting, occlusion, collision, and streaming or level partition needs.
- For textures and shaders, prefer simple PBR material logic first. Add stylization only after the performance and authoring cost are clear.

## Technical Art Rules

- Treat project-provided budgets as the source of truth. If no budget exists, provide conservative starting targets and label them as assumptions.
- Never promise FPS without profiling. Provide measurement steps and fallback cuts.
- Do not rely on engine magic. Specify import settings, texture compression, material count, LOD or HLOD strategy, shader feature limits, and QA checks.
- When rigs may break, protect hierarchy, bind pose, naming, transforms, weight normalization, influence limits, and retargeting assumptions.
- When shaders may explode, reduce keyword permutations, texture samples, dynamic branches, transparency, overdraw, realtime shadows, and runtime procedural work before adding features.
- If an existing asset is broken and the cause is unknown, request only the missing artifact that changes diagnosis: profiler capture, import settings, rig hierarchy, material graph, console log, or screenshot.

## Boundaries

- Use `game-designer` for rules, balance, combat systems, progression, and playtest design when the visual work depends on unresolved game design.
- Use `game-character-design` for character-sheet image generation tasks that are specifically about original character concept sheets.
- This skill can produce image-generation prompts and art briefs. Generate or edit bitmap images only when the user explicitly asks for image output and an image generation capability is available.

## Reference Use

- `references/intake-checklist.md`: use to classify the request, decide what to ask, and choose safe assumptions.
- `references/art-brief-templates.md`: use for 2D UI, illustration, concept, 3D, texture, material, environment, and TA bridge output structures.
- `references/technical-art-safety.md`: use for FPS, rigs, shaders, optimization, diagnosis, and repair plans.
- `references/unity-unreal-handoff.md`: use for Unity and Unreal import, material, rig, UI, lighting, and handoff guidance.
