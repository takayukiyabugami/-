# Piece Sheet v01: Knight

## ID
- Piece: `knight`
- Version: `v01`
- Theme: `mecha-knight`
- Role: High-mobility assault mech (lancer class)

## Sheet Layout (4096x3072)
- A1: Front view (neutral)
- A2: Side view (attack stance profile)
- A3: Back view (thruster/fin pack visible)
- B1: 3/4 combat pose (lunge start)
- B2: Pure silhouette (black fill)
- B3: Module breakdown and material IDs

## Silhouette Contract
- Height class: medium-tall
- Width class: narrow torso + long weapon reach
- Readability keys:
  - Strong forward lean (torso axis tilted)
  - Long lance profile that extends beyond body width
  - Longer leg ratio than Pawn/Rook/Bishop
- Must remain recognizable as Knight at 64 px icon size.

## Proportion (relative units)
- Total height: 1.12 (vs Pawn 1.00)
- Head/sensor: 0.12
- Torso: 0.24
- Back fins/thruster: 0.18
- Hip + legs: 0.42
- Foot base: 0.08
- Lance length: 1.05 of body height

## Module Breakdown
- `KNT-HEAD-01`: narrow visor head with slanted forehead plate
- `KNT-TORSO-01`: wedge chest armor (forward thrust shape)
- `KNT-LANCE-01`: mono-edge assault lance with energy seam
- `KNT-BACK-01`: directional fin + micro-thruster pack
- `KNT-LEG-01`: long articulated legs with reinforced knee caps
- `KNT-GAUNT-01`: compact off-hand stabilizer arm

## Material Assignment
- `MAT-A` painted armor: chest wedge, shoulder shells, shin covers
- `MAT-B` exposed metal: weapon spine, hip joints, ankle links
- `MAT-C` emissive: visor line, lance seam, back fin slit
- `MAT-D` soft/rubber: inner thigh and elbow flex zones

## Faction Paint Map
- White side:
  - Armor primary `#E8E2D4`
  - Secondary trim `#B68B47`
  - Emissive `#6EEBFF`
- Black side:
  - Armor primary `#3C4249`
  - Secondary trim `#1D2025`
  - Emissive `#D64545`

## Motion / Combat Signature
- Primary attack line: aerial downward thrust (high-to-low)
- Move cue: back fin emissive pulse and short afterimage streak
- Impact cue: lance seam over-bright flash at hit frame

## Browser Export Targets
- `chess-game/assets/piece-knight-white.svg`
- `chess-game/assets/piece-knight-black.svg`
- Constraints:
  - <= 35 visible shapes
  - lance + forward lean readable at 64 px
  - avoid tiny sub-lines that collapse below 48 px

## Unity Blockout Targets
- `Assets/Art/Pieces/Knight/piece-knight-sheet-v01.png`
- Blockout notes:
  - keep lean angle readable from top-isometric camera
  - maintain stable foot contact in idle despite forward bias
  - lance tip should not intersect ground during move cycle

## Approval Checklist
- [ ] Silhouette recognized as Knight in black fill only
- [ ] White/Black faction readable at 64 px
- [ ] Forward-lean + lance motif preserved in all views
- [ ] Browser export constraints satisfied
- [ ] Unity blockout notes validated
