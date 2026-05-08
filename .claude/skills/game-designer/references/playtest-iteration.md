# Playtest And Iteration

Use this when the design needs validation, prototype scope, or tuning.

## Prototype Scope

Build the smallest version that proves the design hypothesis.

- One player verb set.
- One failure condition.
- One reward or progression beat.
- One representative level, arena, puzzle, or encounter.
- Debug values exposed as constants or inspector fields when implementation exists.

Avoid in the first prototype:

- Full economy.
- Long story sequence.
- Cosmetic reward systems.
- Large content lists.
- Complex unlock trees.
- Production-quality art unless visual readability is the hypothesis.

## Design Hypothesis

Write the hypothesis as:

```text
If the player can [action/decision] under [pressure/constraint],
then they will feel [target emotion],
because [system feedback/reward/consequence].
```

Examples:

- If the player chooses between healing now or saving a charge for the boss, then they will feel tense agency, because both choices have visible risk.
- If the player sees an enemy wind-up before the hit, then failure will feel fair, because counterplay is readable.

## Observation Checklist

Watch behavior more than opinions.

- Do players understand the goal without explanation.
- Do they retry after failure.
- Do they blame themselves, the rules, or the controls.
- Do they find one dominant strategy.
- Do they notice rewards and state changes.
- Do they pause because they are thinking or because they are confused.
- Do they use the intended counterplay.
- Do they stop from boredom, frustration, fatigue, or completion.

## Success Criteria

Choose 2-4 criteria before testing.

- 80% of players understand the objective within 30 seconds.
- Players can name the cause of death after failure.
- At least two viable strategies appear across testers.
- Players voluntarily retry at least once.
- No more than one major input complaint appears per session.
- The target encounter is cleared after 2-5 attempts by the intended audience.

## Iteration Rules

- If players do not understand, improve signposting and feedback before changing numbers.
- If players understand but cannot execute, adjust timing windows, input buffering, camera, or pressure.
- If players execute but feel bored, add risk, faster decisions, stronger rewards, or more meaningful tradeoffs.
- If one strategy dominates, add cost, counterplay, positional risk, cooldown, or scenario variation.
- If the game feels unfair, check information timing before lowering difficulty.
- Change one major variable per test round when possible.

## Tuning Log Format

```markdown
## Test Goal
- ...

## Observed Behavior
- ...

## Diagnosis
- ...

## Change
- ...

## Next Test
- ...
```
