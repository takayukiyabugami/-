# Intake Checklist

Use this checklist to extract facts from the user prompt and decide whether a question is worth asking.

## Extract First

- Genre and camera: action, RPG, roguelite, puzzle, tactics, simulation, platformer, shooter, rhythm, card, hybrid.
- Platform and input: PC, console, mobile, browser, touch, keyboard/mouse, controller, single-stick, one-button.
- Target player: casual, core, expert, children, party, speedrunner, collector, story-first, competitive.
- Session shape: seconds, 3 minutes, 15 minutes, 45 minutes, run-based, stage-based, daily play.
- Player fantasy: power, survival, mastery, creativity, collection, exploration, expression, competition, relaxation.
- Core verbs: move, aim, dodge, attack, build, choose, collect, combine, negotiate, solve, manage, explore.
- Win/loss: clear condition, fail condition, scoring, ranking, survival, extraction, puzzle completion.
- Constraints: team size, prototype time, engine, asset limits, target complexity, accessibility, content budget.
- Existing problem: boring, too hard, too easy, slow, unclear, unfair, repetitive, shallow, stressful, overloaded.

## Ask Only High-Impact Questions

Ask at most 3 questions total. Ask only if the answer changes the design materially.

Good questions:

- "主な入力はタッチ、キーボード、コントローラーのどれか。"
- "1プレイは何分を想定しているか。"
- "プレイヤーに一番感じさせたい感情は何か。"
- "失敗時にリトライ型か、損失を背負って継続する型か。"

Skip questions when:

- The user says to decide.
- A reasonable default exists.
- The question only changes flavor, not system structure.
- The request is a review and the design artifact already gives enough evidence.

## Default Assumptions

Use these when the user provides no answer:

- Platform: PC/browser with keyboard or controller-friendly controls.
- Target player: general game-literate player.
- Session: 10-20 minutes for a prototype, 3-5 minutes for arcade/mobile loops.
- Priority: prove the core loop before content volume.
- Output: Japanese, concise, testable, and implementation-agnostic.

## Trigger Routing

- Character visual design or image prompt: use `game-character-design`.
- Full playable implementation: use an implementation or game creation workflow.
- Pure story, lore, dialogue, or prose: use a writing workflow.
- Gameplay system design, balance, UX, player psychology, or playtest planning: continue with this skill.
