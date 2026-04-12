# Chess Shared Spec

This folder defines the shared rule and replay contract for both implementations:

- `chess-unity-presentation` (C# domain + Unity presentation)
- `chess-game` (browser UI + JavaScript domain)

## Contract Snapshot

- Board indexing:
  - `index = (rank - 1) * 8 + (file - 'a')`
  - `a1 = 0`, `h8 = 63`
- Turn model: strict alternating `White -> Black`
- Rule scope:
  - normal moves
  - capture
  - castling
  - en passant
  - promotion (explicit choice required)
  - self-check rejection
- Replay format:
  - `ReplayLog(version, initialState, moves[])`
  - deterministic final hash required for same move list

## Golden Tests

`golden-moves.json` is the source of truth for cross-platform parity.

- Unity side can consume the cases in EditMode tests.
- Browser side can consume the same cases in Node tests.
- Every case must assert:
  - accepted/rejected
  - reject reason for rejected moves
  - resulting side to move when accepted
  - deterministic board hash for replay validation
