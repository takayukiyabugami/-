---
name: story-prose-crafter
description: Plan, draft, and revise emotionally compelling prose with strong sensory pull, scene pressure, character desire, reader appetite, foreshadowing, and markdown project files. Use when the user wants to write a novel, improve a scene, create a story from a thin premise, make food or product prose irresistible, make writing more gripping, or diagnose why a draft feels flat.
---

# Story Prose Crafter

## Overview

Build prose through emotion and structure at the same time. Do not only make sentences prettier; identify desire, pressure, choice, cost, sensory pull, rhythm, and aftertaste.

Default to Japanese output unless the user asks for another language. Use concise editorial judgment, then produce usable prose or project files.

## Workflow

### 1. Classify the Request

- **New project**: Gather the minimum concept, then create a markdown story project.
- **Thin premise**: State assumptions, ask only blocking questions, then propose a sharper premise.
- **Drafting**: Lock POV, scene objective, conflict, emotional turn, and ending beat before prose.
- **Revision**: Diagnose weak points before rewriting.
- **Line polish**: Improve rhythm, imagery, compression, and final sentence impact without changing canon.
- **Desire copy**: Make the reader want an experience, food, place, object, or action through sensory sequence and concrete payoff.
- **Micro prose**: For short requests under 500 words, infer the goal and draft directly unless a missing constraint would break the piece.

### 2. Gather Only Blocking Inputs

Read `references/intake-checklist.md` when starting or expanding a project.

Ask at most three questions at once. If the user asks you to decide, proceed and mark assumptions.

Required before long prose:

- premise or scene situation
- POV character
- immediate desire
- opposition or friction
- target tone
- scene endpoint

For short persuasive or sensory prose, do not ask blocking questions unless the target, audience, or forbidden tone is unclear. Assume a vivid, grounded tone and proceed.

### 3. Create Project Files When Useful

For long-form work, use `scripts/new_story_project.ps1` to create:

- `concept.md`
- `characters.md`
- `outline.md`
- `manuscript.md`
- `revision-notes.md`

Default output root is `outputs/`. If a project already exists, update existing markdown instead of creating a parallel canon.

### 4. Draft Prose

Read `references/prose-style-guide.md` before drafting or polishing.

Read `references/desire-copy-guide.md` when the user's goal is to make someone want to eat, buy, visit, try, remember, or act.

Draft with:

- a visible desire in the first movement
- sensory details tied to pressure, not decoration
- dialogue that changes the situation
- internal thought that reveals contradiction
- paragraph rhythm that accelerates near decisions
- an ending beat that leaves consequence, question, or emotional residue

For desire copy, draft with:

- immediate sensory entry
- texture, temperature, scent, sound, and timing in sequence
- a small delay before payoff
- one concrete action the reader imagines doing next
- a final line that turns want into decision

### 5. Revise Drafts

Read `references/revision-rubric.md` before diagnosing or rewriting.

Return this order:

1. `Diagnosis`
2. `Revision Direction`
3. `Rewritten Draft`
4. `Notes`

Do not praise vaguely. Name the failure mode and fix it in the rewrite.

## Output Rules

- Preserve canon unless the user explicitly asks for structural changes.
- Make every beautiful sentence earn its keep through character, tension, or image.
- Prefer concrete verbs and precise nouns over abstract emotional labels.
- Avoid generic encouragement, empty melancholy, and ornate description without consequence.
- Avoid generic deliciousness claims; prove desire through sensory cause and reader action.
- When uncertain, choose clarity and forward motion over literary display.

## References

- `references/intake-checklist.md`: use for project setup and missing story inputs.
- `references/prose-style-guide.md`: use for drafting, line polish, and emotional rhythm.
- `references/desire-copy-guide.md`: use for appetite, product, place, and experience-driven prose.
- `references/revision-rubric.md`: use for critique, rewrite, and quality control.
