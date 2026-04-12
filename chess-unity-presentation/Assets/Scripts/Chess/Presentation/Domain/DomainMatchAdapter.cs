using System;
using System.Collections.Generic;
using Chess.Domain;
using UnityEngine;

namespace Chess.Presentation
{
    public sealed class PieceViewBinding : MonoBehaviour
    {
        [SerializeField] private int pieceId;
        [SerializeField] private ChessPieceType pieceType;
        [SerializeField] private bool isWhite = true;
        [SerializeField] private BoardSquare square = new BoardSquare(0, 0);

        public PieceId PieceId => new PieceId(pieceId);
        public ChessPieceType PieceType => pieceType;
        public PieceColor SideColor => isWhite ? PieceColor.White : PieceColor.Black;
        public BoardSquare Square
        {
            get => square;
            set => square = value;
        }

        public void Initialize(int id, ChessPieceType type, bool white, BoardSquare initialSquare)
        {
            pieceId = id;
            pieceType = type;
            isWhite = white;
            square = initialSquare;
        }
    }

    public sealed class DomainMatchAdapter : MonoBehaviour, IChessMoveValidator, IChessBoardCommitter, IChessTurnSwitcher
    {
        [SerializeField] private BoardGrid3D boardGrid;
        [SerializeField] private bool bootstrapStandardPosition = true;

        private readonly Dictionary<PieceId, PieceViewBinding> _viewById = new Dictionary<PieceId, PieceViewBinding>(64);
        private readonly Dictionary<int, PieceId> _idByIndex = new Dictionary<int, PieceId>(64);

        private BoardState _board;
        private ChessMatchService _matchService;
        private PieceColor _activeColor = PieceColor.White;
        private ulong _syntheticInputToken = 1000;

        public event Action<MoveResult> OnMoveAccepted;
        public event Action<ChessMove, MoveRejectReason> OnMoveRejected;
        public event Action<MoveResult> OnCaptureResolved;
        public event Action<PieceColor> OnTurnSwitched;

        public PieceColor ActiveColor => _activeColor;

        private void Awake()
        {
            if (boardGrid == null)
            {
                boardGrid = FindObjectOfType<BoardGrid3D>();
            }

            InitializeDomain();
        }

        private void Start()
        {
            IndexViewBindings();
        }

        public void RebuildViewIndex()
        {
            IndexViewBindings();
        }

        public bool TryValidate(in MoveRequest request, out MoveValidationResult validationResult)
        {
            validationResult = default;
            SquareCoord from = ToDomainCoord(request.from);
            SquareCoord to = ToDomainCoord(request.to);
            if (!from.IsOnBoard || !to.IsOnBoard)
            {
                validationResult.isLegal = false;
                validationResult.rejectReason = MoveRejectReason.OutOfBounds.ToString();
                return false;
            }

            if (!_board.TryGetPieceAt(from, out Piece movingPiece))
            {
                validationResult.isLegal = false;
                validationResult.rejectReason = MoveRejectReason.NoPieceAtSource.ToString();
                return false;
            }

            ChessMove move = new ChessMove(from, to);
            bool legal = _board.IsLegalMove(move, out MoveRejectReason rejectReason);
            if (!legal)
            {
                validationResult.isLegal = false;
                validationResult.rejectReason = rejectReason.ToString();
                return false;
            }

            Transform movingTransform = ResolvePieceTransform(movingPiece.Id);
            Transform capturedTransform = ResolveCapturedTransform(from, to, movingPiece);

            validationResult.isLegal = true;
            validationResult.rejectReason = string.Empty;
            validationResult.movingPieceType = ToPresentationType(movingPiece.Type);
            validationResult.movingPiece = movingTransform;
            validationResult.isCapture = capturedTransform != null;
            validationResult.capturedPiece = capturedTransform;
            validationResult.from = request.from;
            validationResult.to = request.to;
            validationResult.worldFrom = movingTransform != null ? movingTransform.position : ResolveWorld(from);
            validationResult.worldTo = ResolveWorld(to);
            validationResult.worldFacing = (validationResult.worldTo - validationResult.worldFrom).normalized;
            validationResult.requiresPromotion = movingPiece.Type == PieceType.Pawn && (to.RankIndex == 0 || to.RankIndex == 7);
            return true;
        }

        public void CommitMove(in MoveValidationResult validationResult, PromotionChoice promotionChoice)
        {
            ChessMove move = BuildMoveFromValidation(validationResult, promotionChoice);
            MoveResult result = _matchService.SubmitMove(move);
            if (!result.Accepted)
            {
                throw new InvalidOperationException($"Commit rejected: {result.RejectReason}");
            }

            UpdateViewMappingAfterMove(result);
        }

        public void SwitchTurn()
        {
            _activeColor = _board.SideToMove;
        }

        public int FillLegalMoveRequests(List<MoveRequest> requests, int sourceId, float receivedAt)
        {
            if (requests == null)
            {
                return 0;
            }

            requests.Clear();
            IReadOnlyList<ChessMove> legalMoves = _board.GenerateLegalMoves();
            for (int i = 0; i < legalMoves.Count; i++)
            {
                ChessMove move = legalMoves[i];
                requests.Add(new MoveRequest(
                    from: ToPresentationCoord(move.From),
                    to: ToPresentationCoord(move.To),
                    inputToken: ++_syntheticInputToken,
                    sourceId: sourceId,
                    receivedAt: receivedAt));
            }

            return requests.Count;
        }

        private void InitializeDomain()
        {
            _board = bootstrapStandardPosition ? BoardState.CreateStandard() : BoardState.CreateEmpty();
            _matchService = new ChessMatchService(_board);
            _matchService.OnMoveAccepted += result => OnMoveAccepted?.Invoke(result);
            _matchService.OnMoveRejected += (move, reason) => OnMoveRejected?.Invoke(move, reason);
            _matchService.OnCaptureResolved += result => OnCaptureResolved?.Invoke(result);
            _matchService.OnTurnSwitched += color =>
            {
                _activeColor = color;
                OnTurnSwitched?.Invoke(color);
            };
        }

        private void IndexViewBindings()
        {
            _viewById.Clear();
            _idByIndex.Clear();

            PieceViewBinding[] bindings = FindObjectsOfType<PieceViewBinding>();
            for (int i = 0; i < bindings.Length; i++)
            {
                PieceViewBinding binding = bindings[i];
                _viewById[binding.PieceId] = binding;
                SquareCoord coord = ToDomainCoord(binding.Square);
                if (coord.IsOnBoard)
                {
                    _idByIndex[coord.ToIndex()] = binding.PieceId;
                }
            }
        }

        private Transform ResolvePieceTransform(PieceId pieceId)
        {
            if (_viewById.TryGetValue(pieceId, out PieceViewBinding binding) && binding != null)
            {
                return binding.transform;
            }

            // Runtime bootstrap can add PieceViewBinding after this adapter Awake.
            // Reindex lazily once so early moves do not fail with null transforms.
            IndexViewBindings();
            if (_viewById.TryGetValue(pieceId, out binding) && binding != null)
            {
                return binding.transform;
            }

            return null;
        }

        private Transform ResolveCapturedTransform(SquareCoord from, SquareCoord to, Piece movingPiece)
        {
            if (_board.TryGetPieceAt(to, out Piece target) && target.Color != movingPiece.Color)
            {
                return ResolvePieceTransform(target.Id);
            }

            if (movingPiece.Type == PieceType.Pawn &&
                _board.EnPassantTarget.HasValue &&
                _board.EnPassantTarget.Value.Equals(to) &&
                !_board.TryGetPieceAt(to, out _))
            {
                SquareCoord captureSquare = new SquareCoord(to.FileIndex, from.RankIndex);
                if (_board.TryGetPieceAt(captureSquare, out Piece capturedPawn))
                {
                    return ResolvePieceTransform(capturedPawn.Id);
                }
            }

            return null;
        }

        private void UpdateViewMappingAfterMove(MoveResult result)
        {
            if (!_viewById.TryGetValue(result.MovingPieceId, out PieceViewBinding movingBinding))
            {
                return;
            }

            SquareCoord from = result.Move.From;
            SquareCoord to = result.Move.To;
            _idByIndex.Remove(from.ToIndex());
            _idByIndex[to.ToIndex()] = result.MovingPieceId;
            movingBinding.Square = ToPresentationCoord(to);

            if (result.IsCapture && !result.CapturedPieceId.IsNone && _viewById.TryGetValue(result.CapturedPieceId, out PieceViewBinding capturedBinding))
            {
                SquareCoord captureSquare = result.CaptureSquare ?? to;
                _idByIndex.Remove(captureSquare.ToIndex());
                capturedBinding.gameObject.SetActive(false);
            }
        }

        private ChessMove BuildMoveFromValidation(in MoveValidationResult validationResult, PromotionChoice choice)
        {
            SquareCoord from = ToDomainCoord(validationResult.from);
            SquareCoord to = ToDomainCoord(validationResult.to);
            if (validationResult.requiresPromotion)
            {
                return new ChessMove(from, to, ToDomainPromotion(choice));
            }

            return new ChessMove(from, to);
        }

        private Vector3 ResolveWorld(SquareCoord coord)
        {
            if (boardGrid != null && boardGrid.TryGetWorldPosition(coord, out Vector3 world))
            {
                return world;
            }

            return Vector3.zero;
        }

        private static SquareCoord ToDomainCoord(BoardSquare square)
        {
            return new SquareCoord(square.file, square.rank);
        }

        private static BoardSquare ToPresentationCoord(SquareCoord coord)
        {
            return new BoardSquare(coord.FileIndex, coord.RankIndex);
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

        private static PieceType ToDomainPromotion(PromotionChoice choice)
        {
            switch (choice)
            {
                case PromotionChoice.Rook:
                    return PieceType.Rook;
                case PromotionChoice.Bishop:
                    return PieceType.Bishop;
                case PromotionChoice.Knight:
                    return PieceType.Knight;
                default:
                    return PieceType.Queen;
            }
        }
    }
}
