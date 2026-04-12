# Chess Unity Presentation

Unity 2022.3 URP-ready chess presentation project.

## Current State

- Pure C# deterministic chess domain is implemented under `Assets/Scripts/Chess/Domain`.
- Presentation FSM / motion / capture orchestration / performance QA monitors are implemented.
- Test suites are placed under:
  - `Assets/Tests/EditMode`
  - `Assets/Tests/PlayMode`

## Missing External Tools

This workspace currently cannot execute:

- Unity Editor / Unity Test Runner
- Node.js / npm

So runtime verification is deferred until those tools are available on your machine.

## Quick Start In Unity

1. Open this folder (`chess-unity-presentation`) with Unity Hub using Unity `2022.3.x`.
2. Create an empty scene.
3. Add one empty GameObject and attach `ChessRuntimeBootstrap`.
4. Enter Play Mode. The script auto-builds:
   - board anchors
   - placeholder pieces
   - domain/presentation wiring
   - optional 200-move auto stress runner

## Stress + QA

- `ChessAutoStressRunner` drives automatic legal moves.
- `PerformanceQaMonitor` records frame checkpoints every 20 moves and emits GO/NO-GO at run end.

## Wiring Contract

Manager order is fixed:

1. `CaptureEventBus`
2. `VfxManager`
3. `AudioManager`
4. `BudgetMonitor`
5. `QualityGovernor`
