# Design Sheet Production Spec

## Scope
This document defines the minimum deliverables for one production-ready piece design sheet.

## Canvas and Layout
- Master size: `4096 x 3072` px (4:3).
- Working color space: sRGB.
- Export preview: `2048 x 1536` px PNG.

## Mandatory Views per Piece
- Front view
- Side view
- Back view
- 3/4 view (combat pose)
- Pure black silhouette card
- Shape breakdown card (major modules only)

## Required Callouts
- Unit class label and short role sentence.
- Height ratio vs Pawn baseline.
- Faction palette swatches (white and black).
- Material IDs:
  - `MAT-A` painted armor
  - `MAT-B` exposed metal
  - `MAT-C` emissive
  - `MAT-D` rubber/soft parts
- Signature weapon and attack arc direction.

## Per-Piece Additions
- Pawn: shield silhouette and leg readability check.
- Rook: turret/castle profile check from top camera.
- Knight: lunge pose and lance reach line.
- Bishop: banner/antenna readability check.
- Queen: layered silhouette readability at 64 px.
- King: crown antenna and core armor emphasis.

## Export Contract
- Browser icon export:
  - `piece-<type>-white.svg`
  - `piece-<type>-black.svg`
  - artboard `1024 x 1024`
- Unity concept export:
  - `piece-<type>-sheet.png`
  - optional mask maps for look-dev notes

## Naming Convention
- Piece types: `pawn`, `rook`, `knight`, `bishop`, `queen`, `king`.
- Version suffix: `-v01`, `-v02`, ...
- Example: `piece-knight-sheet-v02.png`

## Approval Gates
1. Silhouette gate: all 6 pieces recognizable in black fill only.
2. Faction gate: white/black remain readable at 64 px.
3. Style gate: same module language across all pieces.
4. Implementation gate: Browser and Unity exports generated with matching motifs.

## Current v01 Sheets
- `sheets/piece-pawn-sheet-v01.md`
- `sheets/piece-knight-sheet-v01.md`
