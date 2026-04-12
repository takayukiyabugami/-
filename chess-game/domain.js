export const FILES = "abcdefgh";
export const BOARD_SIZE = 8;

export function createPiece(color, type, id) {
  return { color, type, hasMoved: false, id };
}

export function createStartingBoard() {
  let nextId = 1;
  const make = (color, type) => createPiece(color, type, nextId++);
  return [
    [make("b", "rook"), make("b", "knight"), make("b", "bishop"), make("b", "queen"), make("b", "king"), make("b", "bishop"), make("b", "knight"), make("b", "rook")],
    Array.from({ length: BOARD_SIZE }, () => make("b", "pawn")),
    Array.from({ length: BOARD_SIZE }, () => null),
    Array.from({ length: BOARD_SIZE }, () => null),
    Array.from({ length: BOARD_SIZE }, () => null),
    Array.from({ length: BOARD_SIZE }, () => null),
    Array.from({ length: BOARD_SIZE }, () => make("w", "pawn")),
    [make("w", "rook"), make("w", "knight"), make("w", "bishop"), make("w", "queen"), make("w", "king"), make("w", "bishop"), make("w", "knight"), make("w", "rook")]
  ];
}

export function createInitialState() {
  return {
    board: createStartingBoard(),
    turn: "w",
    enPassant: null,
    history: [],
    gameOver: false
  };
}

export function cloneState(currentState) {
  return {
    board: currentState.board.map((row) =>
      row.map((piece) => (piece ? { ...piece } : null))
    ),
    turn: currentState.turn,
    enPassant: currentState.enPassant ? { ...currentState.enPassant } : null,
    history: [...currentState.history],
    gameOver: currentState.gameOver
  };
}

export function evaluateGameState(currentState) {
  const current = currentState.turn;
  const inCheck = isKingInCheck(currentState, current);
  const hasMoves = hasAnyLegalMove(currentState, current);
  return {
    inCheck,
    checkmate: inCheck && !hasMoves,
    stalemate: !inCheck && !hasMoves
  };
}

export function hasAnyLegalMove(currentState, color) {
  for (let row = 0; row < BOARD_SIZE; row += 1) {
    for (let col = 0; col < BOARD_SIZE; col += 1) {
      const piece = currentState.board[row][col];
      if (!piece || piece.color !== color) {
        continue;
      }
      if (generateLegalMovesForPiece(currentState, row, col).length > 0) {
        return true;
      }
    }
  }
  return false;
}

export function generateLegalMovesForPiece(currentState, row, col) {
  const piece = currentState.board[row][col];
  if (!piece || piece.color !== currentState.turn) {
    return [];
  }

  const pseudoMoves = generatePseudoLegalMoves(currentState, row, col);
  const legalMoves = [];
  for (const move of pseudoMoves) {
    const trialState = cloneState(currentState);
    applyMove(trialState, move, { simulate: true });
    if (!isKingInCheck(trialState, piece.color)) {
      legalMoves.push(move);
    }
  }

  return legalMoves;
}

function generatePseudoLegalMoves(currentState, row, col) {
  const piece = currentState.board[row][col];
  if (!piece) {
    return [];
  }

  switch (piece.type) {
    case "pawn":
      return pawnMoves(currentState, row, col, piece);
    case "knight":
      return knightMoves(currentState, row, col, piece);
    case "bishop":
      return slidingMoves(currentState, row, col, piece, [[-1, -1], [-1, 1], [1, -1], [1, 1]]);
    case "rook":
      return slidingMoves(currentState, row, col, piece, [[-1, 0], [1, 0], [0, -1], [0, 1]]);
    case "queen":
      return slidingMoves(currentState, row, col, piece, [[-1, -1], [-1, 1], [1, -1], [1, 1], [-1, 0], [1, 0], [0, -1], [0, 1]]);
    case "king":
      return kingMoves(currentState, row, col, piece);
    default:
      return [];
  }
}

function pawnMoves(currentState, row, col, piece) {
  const moves = [];
  const direction = piece.color === "w" ? -1 : 1;
  const startRow = piece.color === "w" ? 6 : 1;
  const promotionRow = piece.color === "w" ? 0 : 7;

  const oneForwardRow = row + direction;
  if (inBounds(oneForwardRow, col) && !currentState.board[oneForwardRow][col]) {
    moves.push({
      fromRow: row,
      fromCol: col,
      toRow: oneForwardRow,
      toCol: col,
      capture: false,
      promotion: oneForwardRow === promotionRow
    });

    const twoForwardRow = row + direction * 2;
    if (row === startRow && !currentState.board[twoForwardRow][col]) {
      moves.push({
        fromRow: row,
        fromCol: col,
        toRow: twoForwardRow,
        toCol: col,
        capture: false,
        special: "double-step"
      });
    }
  }

  for (const dc of [-1, 1]) {
    const captureRow = row + direction;
    const captureCol = col + dc;
    if (!inBounds(captureRow, captureCol)) {
      continue;
    }

    const target = currentState.board[captureRow][captureCol];
    if (target && target.color !== piece.color) {
      moves.push({
        fromRow: row,
        fromCol: col,
        toRow: captureRow,
        toCol: captureCol,
        capture: true,
        promotion: captureRow === promotionRow
      });
      continue;
    }

    if (
      currentState.enPassant &&
      currentState.enPassant.row === captureRow &&
      currentState.enPassant.col === captureCol
    ) {
      moves.push({
        fromRow: row,
        fromCol: col,
        toRow: captureRow,
        toCol: captureCol,
        capture: true,
        special: "en-passant"
      });
    }
  }

  return moves;
}

function knightMoves(currentState, row, col, piece) {
  const moves = [];
  const offsets = [[-2, -1], [-2, 1], [-1, -2], [-1, 2], [1, -2], [1, 2], [2, -1], [2, 1]];

  for (const [dr, dc] of offsets) {
    const nr = row + dr;
    const nc = col + dc;
    if (!inBounds(nr, nc)) {
      continue;
    }
    const target = currentState.board[nr][nc];
    if (!target || target.color !== piece.color) {
      moves.push({
        fromRow: row,
        fromCol: col,
        toRow: nr,
        toCol: nc,
        capture: Boolean(target)
      });
    }
  }

  return moves;
}

function slidingMoves(currentState, row, col, piece, directions) {
  const moves = [];
  for (const [dr, dc] of directions) {
    let nr = row + dr;
    let nc = col + dc;
    while (inBounds(nr, nc)) {
      const target = currentState.board[nr][nc];
      if (!target) {
        moves.push({
          fromRow: row,
          fromCol: col,
          toRow: nr,
          toCol: nc,
          capture: false
        });
      } else {
        if (target.color !== piece.color) {
          moves.push({
            fromRow: row,
            fromCol: col,
            toRow: nr,
            toCol: nc,
            capture: true
          });
        }
        break;
      }
      nr += dr;
      nc += dc;
    }
  }
  return moves;
}

function kingMoves(currentState, row, col, piece) {
  const moves = [];
  for (let dr = -1; dr <= 1; dr += 1) {
    for (let dc = -1; dc <= 1; dc += 1) {
      if (dr === 0 && dc === 0) {
        continue;
      }
      const nr = row + dr;
      const nc = col + dc;
      if (!inBounds(nr, nc)) {
        continue;
      }
      const target = currentState.board[nr][nc];
      if (!target || target.color !== piece.color) {
        moves.push({
          fromRow: row,
          fromCol: col,
          toRow: nr,
          toCol: nc,
          capture: Boolean(target)
        });
      }
    }
  }

  if (!piece.hasMoved && !isKingInCheck(currentState, piece.color)) {
    if (canCastleKingSide(currentState, piece.color)) {
      moves.push({
        fromRow: row,
        fromCol: col,
        toRow: row,
        toCol: col + 2,
        capture: false,
        special: "castle-king"
      });
    }
    if (canCastleQueenSide(currentState, piece.color)) {
      moves.push({
        fromRow: row,
        fromCol: col,
        toRow: row,
        toCol: col - 2,
        capture: false,
        special: "castle-queen"
      });
    }
  }

  return moves;
}

function canCastleKingSide(currentState, color) {
  const row = color === "w" ? 7 : 0;
  const king = currentState.board[row][4];
  const rook = currentState.board[row][7];
  if (!king || !rook || king.type !== "king" || rook.type !== "rook" || king.hasMoved || rook.hasMoved) {
    return false;
  }
  if (currentState.board[row][5] || currentState.board[row][6]) {
    return false;
  }

  const enemy = oppositeColor(color);
  if (
    isSquareAttacked(currentState, row, 5, enemy) ||
    isSquareAttacked(currentState, row, 6, enemy)
  ) {
    return false;
  }
  return true;
}

function canCastleQueenSide(currentState, color) {
  const row = color === "w" ? 7 : 0;
  const king = currentState.board[row][4];
  const rook = currentState.board[row][0];
  if (!king || !rook || king.type !== "king" || rook.type !== "rook" || king.hasMoved || rook.hasMoved) {
    return false;
  }
  if (currentState.board[row][1] || currentState.board[row][2] || currentState.board[row][3]) {
    return false;
  }

  const enemy = oppositeColor(color);
  if (
    isSquareAttacked(currentState, row, 3, enemy) ||
    isSquareAttacked(currentState, row, 2, enemy)
  ) {
    return false;
  }
  return true;
}

export function applyMove(targetState, move, options = {}) {
  const { simulate = false, choosePromotion = null } = options;
  const board = targetState.board;
  const piece = board[move.fromRow][move.fromCol];
  if (!piece) {
    return { accepted: false, rejectReason: "NoPieceAtSource" };
  }

  const movingColor = piece.color;
  const defendingColor = oppositeColor(movingColor);
  const targetBeforeMove = board[move.toRow][move.toCol];
  let capture = Boolean(targetBeforeMove);
  let promotionType = null;
  targetState.enPassant = null;

  if (move.special === "castle-king" || move.special === "castle-queen") {
    const rookFromCol = move.special === "castle-king" ? 7 : 0;
    const rookToCol = move.special === "castle-king" ? 5 : 3;
    const rook = board[move.fromRow][rookFromCol];
    board[move.fromRow][rookToCol] = rook;
    board[move.fromRow][rookFromCol] = null;
    if (rook) {
      rook.hasMoved = true;
    }
  }

  if (move.special === "en-passant") {
    const pawnRow = move.fromRow;
    const capturedPawn = board[pawnRow][move.toCol];
    if (capturedPawn) {
      board[pawnRow][move.toCol] = null;
      capture = true;
    }
  }

  board[move.toRow][move.toCol] = piece;
  board[move.fromRow][move.fromCol] = null;
  piece.hasMoved = true;

  if (piece.type === "pawn" && move.special === "double-step") {
    targetState.enPassant = {
      row: (move.fromRow + move.toRow) / 2,
      col: move.fromCol
    };
  }

  if (piece.type === "pawn" && (move.toRow === 0 || move.toRow === 7)) {
    if (simulate) {
      promotionType = "queen";
    } else if (typeof choosePromotion === "function") {
      promotionType = choosePromotion(movingColor);
    } else {
      promotionType = "queen";
    }
    piece.type = promotionType;
  }

  targetState.turn = defendingColor;
  if (simulate) {
    return { accepted: true, notation: null, capture, promotionType };
  }

  const notation = formatNotation(targetState, move, capture, promotionType, defendingColor);
  if (!simulate) {
    targetState.history.push(notation);
  }

  const status = evaluateGameState(targetState);
  targetState.gameOver = status.checkmate || status.stalemate;

  return { accepted: true, notation, capture, promotionType };
}

function formatNotation(currentState, move, capture, promotionType, defendingColor) {
  if (move.special === "castle-king") {
    return "O-O";
  }
  if (move.special === "castle-queen") {
    return "O-O-O";
  }

  const from = toAlgebraic(move.fromRow, move.fromCol);
  const to = toAlgebraic(move.toRow, move.toCol);
  const separator = capture ? "x" : "-";
  let notation = `${from}${separator}${to}`;
  if (promotionType) {
    notation += `=${pieceLetter(promotionType)}`;
  }

  const inCheck = isKingInCheck(currentState, defendingColor);
  const hasMoves = hasAnyLegalMove(currentState, defendingColor);
  if (inCheck && !hasMoves) {
    notation += "#";
  } else if (inCheck) {
    notation += "+";
  }
  return notation;
}

export function isKingInCheck(currentState, color) {
  const kingPos = findKingPosition(currentState.board, color);
  if (!kingPos) {
    return false;
  }
  return isSquareAttacked(currentState, kingPos.row, kingPos.col, oppositeColor(color));
}

function findKingPosition(board, color) {
  for (let row = 0; row < BOARD_SIZE; row += 1) {
    for (let col = 0; col < BOARD_SIZE; col += 1) {
      const piece = board[row][col];
      if (piece && piece.color === color && piece.type === "king") {
        return { row, col };
      }
    }
  }
  return null;
}

export function isSquareAttacked(currentState, targetRow, targetCol, attackerColor) {
  const board = currentState.board;

  for (let row = 0; row < BOARD_SIZE; row += 1) {
    for (let col = 0; col < BOARD_SIZE; col += 1) {
      const piece = board[row][col];
      if (!piece || piece.color !== attackerColor) {
        continue;
      }

      if (piece.type === "pawn") {
        const step = attackerColor === "w" ? -1 : 1;
        if (targetRow === row + step && (targetCol === col - 1 || targetCol === col + 1)) {
          return true;
        }
        continue;
      }

      if (piece.type === "knight") {
        const dr = Math.abs(targetRow - row);
        const dc = Math.abs(targetCol - col);
        if ((dr === 2 && dc === 1) || (dr === 1 && dc === 2)) {
          return true;
        }
        continue;
      }

      if (piece.type === "king") {
        const dr = Math.abs(targetRow - row);
        const dc = Math.abs(targetCol - col);
        if (dr <= 1 && dc <= 1) {
          return true;
        }
        continue;
      }

      const directions =
        piece.type === "bishop"
          ? [[-1, -1], [-1, 1], [1, -1], [1, 1]]
          : piece.type === "rook"
            ? [[-1, 0], [1, 0], [0, -1], [0, 1]]
            : [[-1, -1], [-1, 1], [1, -1], [1, 1], [-1, 0], [1, 0], [0, -1], [0, 1]];

      for (const [dr, dc] of directions) {
        let nr = row + dr;
        let nc = col + dc;
        while (inBounds(nr, nc)) {
          if (nr === targetRow && nc === targetCol) {
            return true;
          }
          if (board[nr][nc]) {
            break;
          }
          nr += dr;
          nc += dc;
        }
      }
    }
  }

  return false;
}

export function buildReplayLog(initialState, moves, version = 1) {
  return {
    version,
    initialState: exportState(initialState),
    moves: moves.map(moveToLongAlgebraic)
  };
}

export function replayFromLog(replayLog) {
  const state = importState(replayLog.initialState);
  for (let i = 0; i < replayLog.moves.length; i += 1) {
    const move = parseLongAlgebraic(replayLog.moves[i], state);
    const legal = generateLegalMovesForPiece(state, move.fromRow, move.fromCol)
      .find((candidate) => candidate.toRow === move.toRow && candidate.toCol === move.toCol && (candidate.special || "") === (move.special || ""));
    if (!legal) {
      return { state, failedPlyIndex: i, failedMove: replayLog.moves[i] };
    }
    applyMove(state, legal, { simulate: false });
  }

  return { state, failedPlyIndex: -1, failedMove: null };
}

export function exportState(state) {
  return JSON.parse(JSON.stringify(state));
}

export function importState(snapshot) {
  return JSON.parse(JSON.stringify(snapshot));
}

export function computeDeterministicHash(state) {
  const input = JSON.stringify({
    turn: state.turn,
    enPassant: state.enPassant,
    board: state.board.map((row) => row.map((piece) =>
      piece ? [piece.id, piece.color, piece.type, piece.hasMoved ? 1 : 0] : null
    ))
  });
  let hash = 2166136261;
  for (let i = 0; i < input.length; i += 1) {
    hash ^= input.charCodeAt(i);
    hash = Math.imul(hash, 16777619);
  }
  return hash >>> 0;
}

function parseLongAlgebraic(text) {
  const normalized = text.trim().toLowerCase();
  const from = normalized.slice(0, 2);
  const to = normalized.slice(2, 4);
  const promotion = normalized.length > 4 ? normalized[4] : null;
  return {
    fromRow: 8 - Number(from[1]),
    fromCol: FILES.indexOf(from[0]),
    toRow: 8 - Number(to[1]),
    toCol: FILES.indexOf(to[0]),
    promotion
  };
}

function moveToLongAlgebraic(move) {
  const from = toAlgebraic(move.fromRow, move.fromCol);
  const to = toAlgebraic(move.toRow, move.toCol);
  return `${from}${to}`;
}

function pieceLetter(type) {
  switch (type) {
    case "queen":
      return "Q";
    case "rook":
      return "R";
    case "bishop":
      return "B";
    case "knight":
      return "N";
    case "king":
      return "K";
    default:
      return "";
  }
}

export function toAlgebraic(row, col) {
  return `${FILES[col]}${8 - row}`;
}

export function colorName(color) {
  return color === "w" ? "White" : "Black";
}

function inBounds(row, col) {
  return row >= 0 && row < BOARD_SIZE && col >= 0 && col < BOARD_SIZE;
}

function oppositeColor(color) {
  return color === "w" ? "b" : "w";
}
