# Chess Game

Simple browser chess game with no external dependencies.

## Run

1. Open `index.html` in a browser.
2. Click a piece to see legal moves.
3. Click a highlighted destination to move.

## Architecture

- `domain.js`: pure rule layer (legal move generation, check/checkmate logic, replay, deterministic hash)
- `script.js`: UI layer (DOM rendering, selection UX, simple presentation effects)
- shared parity data source: `../chess-spec/golden-moves.json`

## Parity Test

Run golden parity verification:

```bash
npm run test:golden
```

If Node.js is not installed yet, run static contract checks with PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\verify-golden-static.ps1
```

## Rules Implemented

- Piece movement and captures
- Turn management
- Castling
- En passant
- Pawn promotion (prompt choice: q/r/b/n)
- Check, checkmate, and stalemate detection

## Theme

- Grassland board style
- Original high-line shonen-inspired art style (not based on a specific franchise)
- Custom piece illustrations:
  - Pawn: shield + sword
  - Rook: carriage rider
  - Bishop: horse rider with naginata
  - Knight: ninja
  - Queen: robot rider
  - King: plump king
- Capture animations by piece:
  - Pawn: slash strike
  - Rook: carriage spear thrust
  - Knight: aerial downward blade
  - Bishop: naginata swing
  - Queen: beam saber swing
  - King: belly crush
- Movement animation:
  - Knight: instant move (special style)
  - Other pieces: smooth movement with running trail effect
- Pawn update:
  - Armored warrior with shield, black-steel sword, and visible legs
  - Uses the provided heavy-armored warrior reference (single-pose crop) as pawn base
  - Primary pawn pose is now back-facing (third-person readability)
  - Added production reference sheet with multi-view turnaround and build callouts:
    `assets/pawn-design-sheet-production.png`
  - Move: grounded walk cycle with stable pace and clear silhouette
  - Pawn locomotion avoids run, weapon actions, and sudden acceleration
