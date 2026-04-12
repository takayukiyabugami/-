# Piece Sheet v01: Pawn

## ID
- Piece: `pawn`
- Version: `v01`
- Theme: `mecha-knight`
- Role: Frontline infantry mech (shield-bearing line unit)

## Sheet Layout (4096x3072)
- A1: Front view (neutral)
- A2: Side view (weapon-side)
- A3: Back view (battery module visible)
- B1: 3/4 combat pose (shield forward)
- B2: Pure silhouette (black fill)
- B3: Module breakdown and material IDs

## Silhouette Contract
- Height class: short
- Width class: medium-heavy (shield makes frontal width larger than torso)
- Readability keys:
  - Large rounded-rect shield plate on front arm
  - Compact torso + backpack battery brick
  - Visible two-leg stance with wide planted feet
- Must still identify as Pawn at 64 px icon size.

## Proportion (relative units)
- Total height: 1.00
- Head/sensor block: 0.14
- Torso core: 0.28
- Backpack battery: 0.20
- Hip + legs: 0.30
- Foot base: 0.08
- Shield width: 0.52 of full height

## Module Breakdown
- `PWN-HEAD-01`: compact sensor pod, no crown/antenna cluster
- `PWN-TORSO-01`: thick chest slab with central seam
- `PWN-SHLD-01`: convex riot shield with 2 maintenance bolts visible
- `PWN-BATT-01`: rear battery block with vent slits
- `PWN-LEG-01`: short heavy legs with piston hints
- `PWN-BLADE-01`: short utility sword (secondary, not silhouette-dominant)

## Material Assignment
- `MAT-A` painted armor: shield outer shell, chest outer plates
- `MAT-B` exposed metal: joints, pistons, shield edge wear
- `MAT-C` emissive: visor slit, chest line, shield center line
- `MAT-D` soft/rubber: knee and ankle flex zones

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
- Primary attack line: short upward diagonal slash (low-to-high)
- Move cue: backpack emissive pulse 0.55 intensity
- Impact cue: shield edge flash + brief chest emissive flare

## Browser Export Targets
- `chess-game/assets/piece-pawn-white.svg`
- `chess-game/assets/piece-pawn-black.svg`
- Constraints:
  - <= 35 visible shapes
  - clear silhouette at 64 px and 48 px
  - keep shield contour and backpack block in icon

## Unity Blockout Targets
- `Assets/Art/Pieces/Pawn/piece-pawn-sheet-v01.png`
- Blockout notes:
  - center of mass slightly forward (shield bias)
  - foot contact stable for non-running walk cycles
  - avoid thin parts that break at top camera

## Approval Checklist
- [ ] Silhouette recognized as Pawn in black fill only
- [ ] White/Black faction readable at 64 px
- [ ] Shield + backpack motifs preserved in all views
- [ ] Browser export constraints satisfied
- [ ] Unity blockout notes validated
