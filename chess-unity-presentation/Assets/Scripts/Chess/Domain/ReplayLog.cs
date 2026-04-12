using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Chess.Domain
{
    /// <summary>
    /// Replay root object: ReplayLog(version, initialState, moves[]).
    /// </summary>
    public sealed class ReplayLog
    {
        /// <summary>
        /// Replay schema version.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Initial board snapshot.
        /// </summary>
        public BoardSnapshot InitialState { get; set; } = new BoardSnapshot();

        /// <summary>
        /// Move list in execution order.
        /// </summary>
        public List<ReplayMoveEntry> Moves { get; set; } = new List<ReplayMoveEntry>();
    }

    /// <summary>
    /// Serializable board snapshot.
    /// </summary>
    public sealed class BoardSnapshot
    {
        /// <summary>
        /// Side to move.
        /// </summary>
        public PieceColor SideToMove { get; set; }

        /// <summary>
        /// White king-side castling right.
        /// </summary>
        public bool WhiteCanCastleKingSide { get; set; }

        /// <summary>
        /// White queen-side castling right.
        /// </summary>
        public bool WhiteCanCastleQueenSide { get; set; }

        /// <summary>
        /// Black king-side castling right.
        /// </summary>
        public bool BlackCanCastleKingSide { get; set; }

        /// <summary>
        /// Black queen-side castling right.
        /// </summary>
        public bool BlackCanCastleQueenSide { get; set; }

        /// <summary>
        /// En passant target in algebraic notation or null.
        /// </summary>
        public string EnPassantTarget { get; set; }

        /// <summary>
        /// Half-move clock.
        /// </summary>
        public int HalfMoveClock { get; set; }

        /// <summary>
        /// Full move number.
        /// </summary>
        public int FullMoveNumber { get; set; } = 1;

        /// <summary>
        /// Piece list.
        /// </summary>
        public List<BoardSnapshotPiece> Pieces { get; set; } = new List<BoardSnapshotPiece>();
    }

    /// <summary>
    /// One piece in board snapshot.
    /// </summary>
    public sealed class BoardSnapshotPiece
    {
        /// <summary>
        /// Piece id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Piece type.
        /// </summary>
        public PieceType Type { get; set; }

        /// <summary>
        /// Piece color.
        /// </summary>
        public PieceColor Color { get; set; }

        /// <summary>
        /// Square coordinate in algebraic form.
        /// </summary>
        public string Square { get; set; } = string.Empty;
    }

    /// <summary>
    /// One replay move entry.
    /// </summary>
    public sealed class ReplayMoveEntry
    {
        /// <summary>
        /// Move in long algebraic form (e2e4, a7a8q).
        /// </summary>
        public string Move { get; set; } = string.Empty;
    }

    /// <summary>
    /// Replay serialization and materialization helpers.
    /// </summary>
    public static class ReplayCodec
    {
        /// <summary>
        /// Converts replay object to JSON.
        /// </summary>
        public static string ToJson(ReplayLog replayLog)
        {
            if (replayLog == null)
            {
                throw new ArgumentNullException(nameof(replayLog));
            }

            StringBuilder sb = new StringBuilder(4096);
            sb.Append('{');
            AppendProp(sb, "version", replayLog.Version.ToString(CultureInfo.InvariantCulture), raw: true);
            sb.Append(',');
            sb.Append("\"initialState\":");
            AppendInitialState(sb, replayLog.InitialState);
            sb.Append(',');
            sb.Append("\"moves\":[");
            for (int i = 0; i < replayLog.Moves.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append("{\"move\":");
                AppendString(sb, replayLog.Moves[i].Move);
                sb.Append('}');
            }

            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>
        /// Parses replay JSON.
        /// </summary>
        public static ReplayLog FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON cannot be empty.", nameof(json));
            }

            object parsed = MiniJson.Deserialize(json);
            Dictionary<string, object> root = RequireObject(parsed, "root");
            ReplayLog replay = new ReplayLog
            {
                Version = ToInt(GetRequired(root, "version"), "version"),
                InitialState = ParseInitialState(GetRequired(root, "initialState")),
                Moves = ParseMoves(GetRequired(root, "moves")),
            };
            return replay;
        }

        /// <summary>
        /// Creates a board snapshot from board state.
        /// </summary>
        public static BoardSnapshot SnapshotFromBoard(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            BoardSnapshot snapshot = new BoardSnapshot
            {
                SideToMove = board.SideToMove,
                WhiteCanCastleKingSide = board.WhiteCanCastleKingSide,
                WhiteCanCastleQueenSide = board.WhiteCanCastleQueenSide,
                BlackCanCastleKingSide = board.BlackCanCastleKingSide,
                BlackCanCastleQueenSide = board.BlackCanCastleQueenSide,
                EnPassantTarget = board.EnPassantTarget.HasValue ? board.EnPassantTarget.Value.ToAlgebraic() : null,
                HalfMoveClock = board.HalfMoveClock,
                FullMoveNumber = board.FullMoveNumber,
            };

            foreach ((SquareCoord square, Piece piece) in board.EnumeratePieces())
            {
                snapshot.Pieces.Add(new BoardSnapshotPiece
                {
                    Id = piece.Id.Value,
                    Type = piece.Type,
                    Color = piece.Color,
                    Square = square.ToAlgebraic(),
                });
            }

            return snapshot;
        }

        /// <summary>
        /// Builds board state from snapshot.
        /// </summary>
        public static BoardState BoardFromSnapshot(BoardSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            SquareCoord? enPassant = null;
            if (!string.IsNullOrWhiteSpace(snapshot.EnPassantTarget))
            {
                if (!SquareCoord.TryParseAlgebraic(snapshot.EnPassantTarget, out SquareCoord parsed))
                {
                    throw new FormatException("Invalid en passant coordinate.");
                }

                enPassant = parsed;
            }

            BoardState board = BoardState.CreateEmpty(
                sideToMove: snapshot.SideToMove,
                whiteKingSide: snapshot.WhiteCanCastleKingSide,
                whiteQueenSide: snapshot.WhiteCanCastleQueenSide,
                blackKingSide: snapshot.BlackCanCastleKingSide,
                blackQueenSide: snapshot.BlackCanCastleQueenSide,
                enPassant: enPassant,
                halfMoveClock: snapshot.HalfMoveClock,
                fullMoveNumber: snapshot.FullMoveNumber);

            for (int i = 0; i < snapshot.Pieces.Count; i++)
            {
                BoardSnapshotPiece piece = snapshot.Pieces[i];
                if (!SquareCoord.TryParseAlgebraic(piece.Square, out SquareCoord square))
                {
                    throw new FormatException($"Invalid square in snapshot: {piece.Square}");
                }

                board.SetPieceAt(square, new Piece(new PieceId(piece.Id), piece.Type, piece.Color));
            }

            return board;
        }

        private static void AppendInitialState(StringBuilder sb, BoardSnapshot snapshot)
        {
            sb.Append('{');
            AppendProp(sb, "sideToMove", snapshot.SideToMove.ToString(), raw: false);
            sb.Append(',');
            AppendProp(sb, "whiteCanCastleKingSide", snapshot.WhiteCanCastleKingSide ? "true" : "false", raw: true);
            sb.Append(',');
            AppendProp(sb, "whiteCanCastleQueenSide", snapshot.WhiteCanCastleQueenSide ? "true" : "false", raw: true);
            sb.Append(',');
            AppendProp(sb, "blackCanCastleKingSide", snapshot.BlackCanCastleKingSide ? "true" : "false", raw: true);
            sb.Append(',');
            AppendProp(sb, "blackCanCastleQueenSide", snapshot.BlackCanCastleQueenSide ? "true" : "false", raw: true);
            sb.Append(',');
            sb.Append("\"enPassantTarget\":");
            if (string.IsNullOrEmpty(snapshot.EnPassantTarget))
            {
                sb.Append("null");
            }
            else
            {
                AppendString(sb, snapshot.EnPassantTarget);
            }

            sb.Append(',');
            AppendProp(sb, "halfMoveClock", snapshot.HalfMoveClock.ToString(CultureInfo.InvariantCulture), raw: true);
            sb.Append(',');
            AppendProp(sb, "fullMoveNumber", snapshot.FullMoveNumber.ToString(CultureInfo.InvariantCulture), raw: true);
            sb.Append(',');
            sb.Append("\"pieces\":[");
            for (int i = 0; i < snapshot.Pieces.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                BoardSnapshotPiece piece = snapshot.Pieces[i];
                sb.Append('{');
                AppendProp(sb, "id", piece.Id.ToString(CultureInfo.InvariantCulture), raw: true);
                sb.Append(',');
                AppendProp(sb, "type", piece.Type.ToString(), raw: false);
                sb.Append(',');
                AppendProp(sb, "color", piece.Color.ToString(), raw: false);
                sb.Append(',');
                AppendProp(sb, "square", piece.Square, raw: false);
                sb.Append('}');
            }

            sb.Append("]}");
        }

        private static void AppendProp(StringBuilder sb, string key, string value, bool raw)
        {
            AppendString(sb, key);
            sb.Append(':');
            if (raw)
            {
                sb.Append(value);
            }
            else
            {
                AppendString(sb, value);
            }
        }

        private static void AppendString(StringBuilder sb, string value)
        {
            sb.Append('"');
            if (value != null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    switch (c)
                    {
                        case '\\':
                            sb.Append("\\\\");
                            break;
                        case '"':
                            sb.Append("\\\"");
                            break;
                        case '\n':
                            sb.Append("\\n");
                            break;
                        case '\r':
                            sb.Append("\\r");
                            break;
                        case '\t':
                            sb.Append("\\t");
                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                }
            }

            sb.Append('"');
        }

        private static BoardSnapshot ParseInitialState(object value)
        {
            Dictionary<string, object> obj = RequireObject(value, "initialState");
            BoardSnapshot snapshot = new BoardSnapshot
            {
                SideToMove = ParsePieceColor(GetRequiredString(obj, "sideToMove"), "sideToMove"),
                WhiteCanCastleKingSide = ToBool(GetRequired(obj, "whiteCanCastleKingSide"), "whiteCanCastleKingSide"),
                WhiteCanCastleQueenSide = ToBool(GetRequired(obj, "whiteCanCastleQueenSide"), "whiteCanCastleQueenSide"),
                BlackCanCastleKingSide = ToBool(GetRequired(obj, "blackCanCastleKingSide"), "blackCanCastleKingSide"),
                BlackCanCastleQueenSide = ToBool(GetRequired(obj, "blackCanCastleQueenSide"), "blackCanCastleQueenSide"),
                EnPassantTarget = GetOptionalString(obj, "enPassantTarget"),
                HalfMoveClock = ToInt(GetRequired(obj, "halfMoveClock"), "halfMoveClock"),
                FullMoveNumber = ToInt(GetRequired(obj, "fullMoveNumber"), "fullMoveNumber"),
            };

            IList list = RequireArray(GetRequired(obj, "pieces"), "pieces");
            for (int i = 0; i < list.Count; i++)
            {
                Dictionary<string, object> pieceObj = RequireObject(list[i], $"pieces[{i}]");
                snapshot.Pieces.Add(new BoardSnapshotPiece
                {
                    Id = ToInt(GetRequired(pieceObj, "id"), $"pieces[{i}].id"),
                    Type = ParsePieceType(GetRequiredString(pieceObj, "type"), $"pieces[{i}].type"),
                    Color = ParsePieceColor(GetRequiredString(pieceObj, "color"), $"pieces[{i}].color"),
                    Square = GetRequiredString(pieceObj, "square"),
                });
            }

            return snapshot;
        }

        private static List<ReplayMoveEntry> ParseMoves(object value)
        {
            IList list = RequireArray(value, "moves");
            List<ReplayMoveEntry> moves = new List<ReplayMoveEntry>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                Dictionary<string, object> obj = RequireObject(list[i], $"moves[{i}]");
                moves.Add(new ReplayMoveEntry
                {
                    Move = GetRequiredString(obj, "move"),
                });
            }

            return moves;
        }

        private static Dictionary<string, object> RequireObject(object value, string name)
        {
            if (value is Dictionary<string, object> dict)
            {
                return dict;
            }

            throw new FormatException($"Expected object for {name}.");
        }

        private static IList RequireArray(object value, string name)
        {
            if (value is IList list)
            {
                return list;
            }

            throw new FormatException($"Expected array for {name}.");
        }

        private static object GetRequired(Dictionary<string, object> obj, string key)
        {
            if (!obj.TryGetValue(key, out object value))
            {
                throw new FormatException($"Missing key: {key}");
            }

            return value;
        }

        private static string GetRequiredString(Dictionary<string, object> obj, string key)
        {
            object value = GetRequired(obj, key);
            if (value is string text)
            {
                return text;
            }

            throw new FormatException($"Key {key} must be string.");
        }

        private static string GetOptionalString(Dictionary<string, object> obj, string key)
        {
            if (!obj.TryGetValue(key, out object value) || value == null)
            {
                return null;
            }

            if (value is string text)
            {
                return text;
            }

            throw new FormatException($"Key {key} must be string or null.");
        }

        private static int ToInt(object value, string name)
        {
            if (value is long l)
            {
                return checked((int)l);
            }

            if (value is int i)
            {
                return i;
            }

            if (value is double d)
            {
                return checked((int)d);
            }

            if (value is string s && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return parsed;
            }

            throw new FormatException($"Value {name} must be integer.");
        }

        private static bool ToBool(object value, string name)
        {
            if (value is bool b)
            {
                return b;
            }

            if (value is string s && bool.TryParse(s, out bool parsed))
            {
                return parsed;
            }

            throw new FormatException($"Value {name} must be boolean.");
        }

        private static PieceType ParsePieceType(string text, string name)
        {
            if (Enum.TryParse(text, true, out PieceType value))
            {
                return value;
            }

            throw new FormatException($"Invalid piece type for {name}: {text}");
        }

        private static PieceColor ParsePieceColor(string text, string name)
        {
            if (Enum.TryParse(text, true, out PieceColor value))
            {
                return value;
            }

            throw new FormatException($"Invalid piece color for {name}: {text}");
        }
    }

    /// <summary>
    /// Deterministic replay runner.
    /// </summary>
    public static class ReplayRunner
    {
        /// <summary>
        /// Replays all moves and returns final board state.
        /// </summary>
        public static BoardState Run(ReplayLog replayLog, out int failedPlyIndex, out MoveResult failedResult)
        {
            if (replayLog == null)
            {
                throw new ArgumentNullException(nameof(replayLog));
            }

            BoardState board = ReplayCodec.BoardFromSnapshot(replayLog.InitialState);
            failedPlyIndex = -1;
            failedResult = default;

            for (int i = 0; i < replayLog.Moves.Count; i++)
            {
                ReplayMoveEntry entry = replayLog.Moves[i];
                ChessMove move = ChessMove.ParseLongAlgebraic(entry.Move);
                MoveResult result = board.ApplyMove(move);
                if (!result.Accepted)
                {
                    failedPlyIndex = i;
                    failedResult = result;
                    return board;
                }
            }

            return board;
        }

        /// <summary>
        /// Builds replay object from initial board and move list.
        /// </summary>
        public static ReplayLog Build(BoardState initial, IReadOnlyList<ChessMove> moves, int version = 1)
        {
            if (initial == null)
            {
                throw new ArgumentNullException(nameof(initial));
            }

            if (moves == null)
            {
                throw new ArgumentNullException(nameof(moves));
            }

            ReplayLog replayLog = new ReplayLog
            {
                Version = version,
                InitialState = ReplayCodec.SnapshotFromBoard(initial),
            };

            for (int i = 0; i < moves.Count; i++)
            {
                replayLog.Moves.Add(new ReplayMoveEntry
                {
                    Move = moves[i].ToLongAlgebraic(),
                });
            }

            return replayLog;
        }
    }

    internal static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (json == null)
            {
                return null;
            }

            return Parser.Parse(json);
        }

        private sealed class Parser : IDisposable
        {
            private readonly string _json;
            private int _index;

            private Parser(string json)
            {
                _json = json;
            }

            public static object Parse(string json)
            {
                using (Parser parser = new Parser(json))
                {
                    return parser.ParseValue();
                }
            }

            public void Dispose()
            {
            }

            private enum Token
            {
                None,
                CurlyOpen,
                CurlyClose,
                SquaredOpen,
                SquaredClose,
                Colon,
                Comma,
                String,
                Number,
                True,
                False,
                Null
            }

            private Dictionary<string, object> ParseObject()
            {
                Dictionary<string, object> table = new Dictionary<string, object>();
                NextToken();
                while (true)
                {
                    Token token = LookAhead();
                    switch (token)
                    {
                        case Token.Comma:
                            NextToken();
                            break;
                        case Token.CurlyClose:
                            NextToken();
                            return table;
                        default:
                            string key = ParseString();
                            if (NextToken() != Token.Colon)
                            {
                                throw new FormatException("Expected ':' in object.");
                            }

                            table[key] = ParseValue();
                            break;
                    }
                }
            }

            private List<object> ParseArray()
            {
                List<object> array = new List<object>();
                NextToken();
                bool parsing = true;
                while (parsing)
                {
                    Token token = LookAhead();
                    switch (token)
                    {
                        case Token.Comma:
                            NextToken();
                            break;
                        case Token.SquaredClose:
                            NextToken();
                            parsing = false;
                            break;
                        default:
                            array.Add(ParseValue());
                            break;
                    }
                }

                return array;
            }

            private object ParseValue()
            {
                Token token = LookAhead();
                switch (token)
                {
                    case Token.String:
                        return ParseString();
                    case Token.Number:
                        return ParseNumber();
                    case Token.CurlyOpen:
                        return ParseObject();
                    case Token.SquaredOpen:
                        return ParseArray();
                    case Token.True:
                        NextToken();
                        return true;
                    case Token.False:
                        NextToken();
                        return false;
                    case Token.Null:
                        NextToken();
                        return null;
                    default:
                        throw new FormatException("Invalid JSON token.");
                }
            }

            private string ParseString()
            {
                StringBuilder sb = new StringBuilder();
                if (NextToken() != Token.String)
                {
                    throw new FormatException("Expected string token.");
                }

                bool parsing = true;
                while (parsing)
                {
                    if (_index >= _json.Length)
                    {
                        break;
                    }

                    char c = _json[_index++];
                    switch (c)
                    {
                        case '"':
                            parsing = false;
                            break;
                        case '\\':
                            if (_index >= _json.Length)
                            {
                                parsing = false;
                                break;
                            }

                            c = _json[_index++];
                            switch (c)
                            {
                                case '"':
                                case '\\':
                                case '/':
                                    sb.Append(c);
                                    break;
                                case 'b':
                                    sb.Append('\b');
                                    break;
                                case 'f':
                                    sb.Append('\f');
                                    break;
                                case 'n':
                                    sb.Append('\n');
                                    break;
                                case 'r':
                                    sb.Append('\r');
                                    break;
                                case 't':
                                    sb.Append('\t');
                                    break;
                                case 'u':
                                    if (_index + 4 > _json.Length)
                                    {
                                        throw new FormatException("Invalid unicode escape.");
                                    }

                                    string hex = _json.Substring(_index, 4);
                                    sb.Append((char)Convert.ToInt32(hex, 16));
                                    _index += 4;
                                    break;
                            }

                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                }

                return sb.ToString();
            }

            private object ParseNumber()
            {
                int lastIndex = GetLastIndexOfNumber(_index);
                int length = (lastIndex - _index) + 1;
                string number = _json.Substring(_index, length);
                _index = lastIndex + 1;

                if (number.IndexOf('.') != -1 || number.IndexOf('e') != -1 || number.IndexOf('E') != -1)
                {
                    if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedDouble))
                    {
                        return parsedDouble;
                    }
                }
                else if (long.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedLong))
                {
                    return parsedLong;
                }

                throw new FormatException($"Invalid number token: {number}");
            }

            private int GetLastIndexOfNumber(int index)
            {
                int lastIndex;
                for (lastIndex = index; lastIndex < _json.Length; lastIndex++)
                {
                    if ("0123456789+-.eE".IndexOf(_json[lastIndex]) == -1)
                    {
                        break;
                    }
                }

                return lastIndex - 1;
            }

            private void EatWhitespace()
            {
                while (_index < _json.Length && char.IsWhiteSpace(_json[_index]))
                {
                    _index++;
                }
            }

            private Token LookAhead()
            {
                int saveIndex = _index;
                Token token = NextToken();
                _index = saveIndex;
                return token;
            }

            private Token NextToken()
            {
                EatWhitespace();
                if (_index == _json.Length)
                {
                    return Token.None;
                }

                char c = _json[_index];
                _index++;
                switch (c)
                {
                    case '{':
                        return Token.CurlyOpen;
                    case '}':
                        return Token.CurlyClose;
                    case '[':
                        return Token.SquaredOpen;
                    case ']':
                        return Token.SquaredClose;
                    case ',':
                        return Token.Comma;
                    case '"':
                        return Token.String;
                    case ':':
                        return Token.Colon;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                        _index--;
                        return Token.Number;
                }

                _index--;
                int remaining = _json.Length - _index;
                if (remaining >= 4 && _json.Substring(_index, 4) == "true")
                {
                    _index += 4;
                    return Token.True;
                }

                if (remaining >= 5 && _json.Substring(_index, 5) == "false")
                {
                    _index += 5;
                    return Token.False;
                }

                if (remaining >= 4 && _json.Substring(_index, 4) == "null")
                {
                    _index += 4;
                    return Token.Null;
                }

                return Token.None;
            }
        }
    }
}
