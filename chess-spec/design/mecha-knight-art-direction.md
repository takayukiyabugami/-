# Mecha Knight Art Direction

## Vision
Create a stronger "battle mech x knight" identity than the current browser pass while preserving instant piece readability.

## Style Pillars
- Readability first: every piece must be recognized from silhouette at small size.
- Military hierarchy: each role looks like a unit class, not just a costume swap.
- Hard-surface realism: painted metal, edge wear, warning marks, and emissive seams.
- Shared universe: Browser 2D and Unity 3D use the same motif library and color tokens.

## Faction Language
- White side:
  - Primary armor: `#E8E2D4` (ivory alloy)
  - Secondary metal: `#B68B47` (brass)
  - Emissive: `#6EEBFF` (cyan)
- Black side:
  - Primary armor: `#3C4249` (gunmetal)
  - Secondary metal: `#1D2025` (black steel)
  - Emissive: `#D64545` (crimson)

## Piece Identity (Silhouette Contract)
- Pawn:
  - Short, forward-heavy body
  - Large shield front plate
  - Backpack battery brick
- Rook:
  - Rectangular fortress mass
  - Turret or cannon rail profile
  - Flat top silhouette
- Knight:
  - Aggressive forward lean
  - Long leg ratio and lance shape
  - Distinct assault posture
- Bishop:
  - Tall and narrow profile
  - Banner-lance or antenna mast
  - Ritual-command look
- Queen:
  - Largest non-king silhouette
  - Layered mantle fins and multi-weapon profile
  - Dominant upper-body shape
- King:
  - Thick central torso and guarded core
  - Crown-like antenna cluster
  - Defensive, anchored stance

## Material Spec
- Base layers: painted metal + dark primer + exposed metal edge.
- Detail frequency:
  - Hero pieces (King/Queen/Knight): high panel density.
  - Utility pieces (Pawn/Rook/Bishop): medium density with clear large forms.
- Weathering:
  - Edge wear on high-contact corners only.
  - No full-surface grunge that kills readability.

## Motion and VFX Hooks
- Each piece must define one signature attack line direction.
- Emissive strips pulse on move start and impact.
- Capture effects must inherit side emissive color.

## Complexity Budgets
- Browser SVG target:
  - 1 piece icon: <= 35 visible sub-shapes at 100% zoom.
  - Keep major silhouette readable at 64 px.
- Unity blockout target:
  - Piece mesh silhouette clear in unlit mode.
  - Keep animation-safe center of mass near base center.

## Do / Do Not
- Do: prioritize silhouette, role identity, and faction contrast.
- Do: reuse module motifs (shield type, vent type, antenna family).
- Do Not: over-detail internal lines that disappear in game camera.
- Do Not: make white and black differ only by tint.
