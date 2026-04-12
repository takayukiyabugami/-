using System;
using System.Collections.Generic;
using Chess.Domain;
using UnityEngine;

namespace Chess.Presentation
{
    public sealed class ChessRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private bool autoBuildOnStart = true;
        [SerializeField] private bool attachStressRunner = true;
        [SerializeField] private float squareSpacing = 1.1f;
        [SerializeField] private Vector3 boardOrigin = new Vector3(-3.85f, 0f, -3.85f);

        private void Start()
        {
            if (autoBuildOnStart)
            {
                BuildIfMissing();
            }
        }

        [ContextMenu("Build Runtime Chess Rig")]
        public void BuildIfMissing()
        {
            if (FindObjectOfType<ChessTurnController>() != null)
            {
                return;
            }

            GameObject root = new GameObject("ChessRuntimeRoot");

            BoardGrid3D boardGrid = root.AddComponent<BoardGrid3D>();
            Transform[] anchors = BuildAnchors(root.transform);
            boardGrid.ConfigureAnchors(anchors, 0f);

            DomainMatchAdapter adapter = root.AddComponent<DomainMatchAdapter>();
            SetField(adapter, "boardGrid", boardGrid);

            SimulationInputGateway input = root.AddComponent<SimulationInputGateway>();
            ChessPieceMover mover = root.AddComponent<ChessPieceMover>();
            DefaultPromotionUI promotion = root.AddComponent<DefaultPromotionUI>();
            ChessTurnController controller = root.AddComponent<ChessTurnController>();
            ChessTurnDebugOverlay overlay = root.AddComponent<ChessTurnDebugOverlay>();

            CaptureEventBus bus = root.AddComponent<CaptureEventBus>();
            QualityGovernor quality = root.AddComponent<QualityGovernor>();
            BudgetMonitor budget = root.AddComponent<BudgetMonitor>();
            VfxManager vfx = root.AddComponent<VfxManager>();
            AudioManager audio = root.AddComponent<AudioManager>();
            CaptureOrchestratorInstaller installer = root.AddComponent<CaptureOrchestratorInstaller>();
            PerformanceQaMonitor perf = root.AddComponent<PerformanceQaMonitor>();

            SetField(controller, "inputGatewayBehaviour", input as MonoBehaviour);
            SetField(controller, "moveValidatorBehaviour", adapter as MonoBehaviour);
            SetField(controller, "movePresentationBehaviour", mover as MonoBehaviour);
            SetField(controller, "boardCommitterBehaviour", adapter as MonoBehaviour);
            SetField(controller, "turnSwitcherBehaviour", adapter as MonoBehaviour);
            SetField(controller, "promotionUiBehaviour", promotion as MonoBehaviour);

            SetField(overlay, "controller", controller);
            SetField(bus, "turnController", controller);
            SetField(bus, "pieceMover", mover);
            SetField(budget, "eventBus", bus);
            SetField(budget, "qualityGovernor", quality);
            SetField(vfx, "eventBus", bus);
            SetField(vfx, "budgetMonitor", budget);
            SetField(vfx, "qualityGovernor", quality);
            SetField(audio, "eventBus", bus);
            SetField(audio, "budgetMonitor", budget);
            SetField(installer, "eventBus", bus);
            SetField(installer, "vfxManager", vfx);
            SetField(installer, "audioManager", audio);
            SetField(installer, "budgetMonitor", budget);
            SetField(installer, "qualityGovernor", quality);
            SetField(perf, "eventBus", bus);

            BuildPieceViews(root.transform, boardGrid);
            adapter.RebuildViewIndex();

            if (attachStressRunner)
            {
                ChessAutoStressRunner stress = root.AddComponent<ChessAutoStressRunner>();
                SetField(stress, "turnController", controller);
                SetField(stress, "domainAdapter", adapter);
                SetField(stress, "inputGateway", input);
                SetField(stress, "performanceQaMonitor", perf);
            }
        }

        private Transform[] BuildAnchors(Transform parent)
        {
            Transform anchorRoot = new GameObject("BoardAnchors").transform;
            anchorRoot.SetParent(parent, false);
            Transform[] anchors = new Transform[64];

            int index = 0;
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    GameObject anchor = new GameObject($"Anchor_{file}_{rank}");
                    anchor.transform.SetParent(anchorRoot, false);
                    anchor.transform.position = boardOrigin + new Vector3(file * squareSpacing, 0f, rank * squareSpacing);
                    anchors[index++] = anchor.transform;
                }
            }

            return anchors;
        }

        private void BuildPieceViews(Transform parent, BoardGrid3D boardGrid)
        {
            BoardState standard = BoardState.CreateStandard();
            Transform piecesRoot = new GameObject("PieceViews").transform;
            piecesRoot.SetParent(parent, false);

            foreach ((SquareCoord square, Piece piece) in standard.EnumeratePieces())
            {
                GameObject node = CreatePrimitiveForPiece(piece.Type);
                node.name = $"{piece.Color}_{piece.Type}_{piece.Id.Value}";
                node.transform.SetParent(piecesRoot, false);
                node.transform.position = boardGrid.GetWorldPosition(square);
                node.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);

                PieceViewBinding binding = node.AddComponent<PieceViewBinding>();
                binding.Initialize(
                    id: piece.Id.Value,
                    type: ToPresentationType(piece.Type),
                    white: piece.Color == PieceColor.White,
                    initialSquare: new BoardSquare(square.FileIndex, square.RankIndex));

                Renderer renderer = node.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = piece.Color == PieceColor.White
                        ? new Color(0.95f, 0.95f, 0.95f)
                        : new Color(0.2f, 0.2f, 0.2f);
                }
            }
        }

        private static GameObject CreatePrimitiveForPiece(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn:
                    return GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                case PieceType.Knight:
                    return GameObject.CreatePrimitive(PrimitiveType.Capsule);
                case PieceType.Bishop:
                    return GameObject.CreatePrimitive(PrimitiveType.Sphere);
                case PieceType.Rook:
                    return GameObject.CreatePrimitive(PrimitiveType.Cube);
                case PieceType.Queen:
                    return GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                case PieceType.King:
                    return GameObject.CreatePrimitive(PrimitiveType.Capsule);
                default:
                    return GameObject.CreatePrimitive(PrimitiveType.Cube);
            }
        }

        private static ChessPieceType ToPresentationType(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn:
                    return ChessPieceType.Pawn;
                case PieceType.Knight:
                    return ChessPieceType.Knight;
                case PieceType.Bishop:
                    return ChessPieceType.Bishop;
                case PieceType.Rook:
                    return ChessPieceType.Rook;
                case PieceType.Queen:
                    return ChessPieceType.Queen;
                case PieceType.King:
                    return ChessPieceType.King;
                default:
                    return ChessPieceType.Pawn;
            }
        }

        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null)
            {
                return;
            }

            System.Reflection.FieldInfo field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}
