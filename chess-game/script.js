const PIECE_ASSETS = {
  pawn: "./assets/pawn-heavy-armored-warrior.png",
  rook: "./assets/rook-carriage.svg",
  bishop: "./assets/bishop-horse-naginata.svg",
  knight: "./assets/knight-ninja.svg",
  queen: "./assets/queen-robot.svg",
  king: "./assets/king-fat.svg"
};

const FILES = "abcdefgh";
const BOARD_SIZE = 8;
const MOVE_ANIMATION_BASE_MS = {
  pawn: 560,
  rook: 380,
  bishop: 370,
  queen: 390,
  king: 340
};
const CAPTURE_ANIMATION_MS = {
  pawn: 720,
  rook: 700,
  knight: 760,
  bishop: 720,
  queen: 780,
  king: 760
};

const boardElement = document.getElementById("board");
const statusElement = document.getElementById("status");
const movesListElement = document.getElementById("movesList");
const resetButton = document.getElementById("resetBtn");

const state = {
  board: createStartingBoard(),
  turn: "w",
  selected: null,
  legalMovesForSelected: [],
  enPassant: null,
  gameOver: false,
  history: [],
  checkSquare: null,
  animationLock: false,
  animationTimer: null,
  animationResolve: null,
  animationToken: 0
};

function createStartingBoard() {
  return [
    [
      createPiece("b", "rook"),
      createPiece("b", "knight"),
      createPiece("b", "bishop"),
      createPiece("b", "queen"),
      createPiece("b", "king"),
      createPiece("b", "bishop"),
      createPiece("b", "knight"),
      createPiece("b", "rook")
    ],
    Array.from({ length: BOARD_SIZE }, () => createPiece("b", "pawn")),
    Array.from({ length: BOARD_SIZE }, () => null),
    Array.from({ length: BOARD_SIZE }, () => null),
    Array.from({ length: BOARD_SIZE }, () => null),
    Array.from({ length: BOARD_SIZE }, () => null),
    Array.from({ length: BOARD_SIZE }, () => createPiece("w", "pawn")),
    [
      createPiece("w", "rook"),
      createPiece("w", "knight"),
      createPiece("w", "bishop"),
      createPiece("w", "queen"),
      createPiece("w", "king"),
      createPiece("w", "bishop"),
      createPiece("w", "knight"),
      createPiece("w", "rook")
    ]
  ];
}

function createPiece(color, type) {
  return { color, type, hasMoved: false };
}

function resetGame() {
  clearPendingAnimationWait();
  clearTransientEffects();
  state.animationToken += 1;
  state.animationLock = false;
  state.board = createStartingBoard();
  state.turn = "w";
  state.selected = null;
  state.legalMovesForSelected = [];
  state.enPassant = null;
  state.gameOver = false;
  state.history = [];
  state.checkSquare = null;
  renderMoves();
  updateStatus();
  renderBoard();
}

function initializeBoardUI() {
  boardElement.innerHTML = "";

  for (let row = 0; row < BOARD_SIZE; row += 1) {
    for (let col = 0; col < BOARD_SIZE; col += 1) {
      const square = document.createElement("button");
      square.type = "button";
      square.className = `square ${(row + col) % 2 === 0 ? "light" : "dark"}`;
      square.dataset.row = String(row);
      square.dataset.col = String(col);
      square.addEventListener("click", onSquareClick);
      boardElement.appendChild(square);
    }
  }
}

function onSquareClick(event) {
  if (state.gameOver || state.animationLock) {
    return;
  }

  const target = event.currentTarget;
  const row = Number(target.dataset.row);
  const col = Number(target.dataset.col);

  if (state.selected) {
    const chosenMove = state.legalMovesForSelected.find(
      (move) => move.toRow === row && move.toCol === col
    );

    if (chosenMove) {
      commitMove(chosenMove);
      return;
    }
  }

  const piece = state.board[row][col];
  if (!piece || piece.color !== state.turn) {
    clearSelection();
    renderBoard();
    return;
  }

  state.selected = { row, col };
  state.legalMovesForSelected = generateLegalMovesForPiece(state, row, col);
  renderBoard();
}

async function commitMove(move) {
  const mover = state.board[move.fromRow][move.fromCol];
  if (!mover) {
    return;
  }

  const token = state.animationToken + 1;
  state.animationToken = token;
  state.animationLock = true;
  clearSelection();
  renderBoard();

  try {
    const isCapture = move.capture || move.special === "en-passant";

    if (mover.type !== "knight") {
      await playMovementAnimation(move, mover);
    }

    if (token !== state.animationToken) {
      return;
    }

    if (isCapture && mover.type !== "pawn") {
      const attackOrigin = mover.type === "knight"
        ? { row: move.fromRow, col: move.fromCol }
        : { row: move.toRow, col: move.toCol };
      await playCaptureAnimation(move, mover.type, attackOrigin);
    }

    if (token !== state.animationToken) {
      return;
    }

    finalizeMove(move);
  } finally {
    if (token === state.animationToken) {
      state.animationLock = false;
    }
    clearPendingAnimationWait();
    clearTransientEffects();
  }
}

function finalizeMove(move) {
  const playedMove = applyMove(state, move, { simulate: false });
  clearSelection();
  state.history.push(playedMove.notation);
  renderMoves();
  evaluateGameState();
  updateStatus();
  renderBoard();
}

function playMovementAnimation(move, piece) {
  const sourceSquare = getSquareElement(move.fromRow, move.fromCol);
  const targetSquare = getSquareElement(move.toRow, move.toCol);
  const sourcePieceImage = sourceSquare ? sourceSquare.querySelector(".piece-icon") : null;

  if (!sourceSquare || !targetSquare || !sourcePieceImage) {
    return Promise.resolve();
  }

  const boardRect = boardElement.getBoundingClientRect();
  const sourceRect = sourceSquare.getBoundingClientRect();
  const targetRect = targetSquare.getBoundingClientRect();

  const startX = sourceRect.left + sourceRect.width / 2 - boardRect.left;
  const startY = sourceRect.top + sourceRect.height / 2 - boardRect.top;
  const moveX = targetRect.left + targetRect.width / 2 - (sourceRect.left + sourceRect.width / 2);
  const moveY = targetRect.top + targetRect.height / 2 - (sourceRect.top + sourceRect.height / 2);
  const moveDistance = Math.hypot(moveX, moveY);
  const moveAngle = attackAngleDeg(move.fromRow, move.fromCol, move.toRow, move.toCol);

  const baseDuration = MOVE_ANIMATION_BASE_MS[piece.type] || 340;
  const moveDuration = piece.type === "pawn"
    ? Math.max(520, Math.min(860, Math.round(baseDuration + moveDistance * 0.1)))
    : Math.max(260, Math.min(900, Math.round(baseDuration + moveDistance * 0.18)));
  const movementClass = piece.type === "pawn" ? "walking" : "running";

  const ghost = document.createElement("div");
  ghost.className = `move-ghost piece-${piece.type} ${piece.color === "w" ? "team-white" : "team-black"} ${movementClass}`;
  ghost.style.left = `${startX}px`;
  ghost.style.top = `${startY}px`;
  ghost.style.width = `${sourceRect.width * 0.86}px`;
  ghost.style.height = `${sourceRect.height * 0.86}px`;
  ghost.style.setProperty("--move-x", `${moveX}px`);
  ghost.style.setProperty("--move-y", `${moveY}px`);
  ghost.style.setProperty("--move-duration", `${moveDuration}ms`);
  ghost.style.setProperty("--run-angle", `${moveAngle}deg`);

  const ghostImage = sourcePieceImage.cloneNode(true);
  ghostImage.classList.add("ghost-piece");
  ghost.appendChild(ghostImage);

  sourcePieceImage.classList.add("moving-source-hidden");
  boardElement.appendChild(ghost);

  requestAnimationFrame(() => {
    ghost.classList.add("moving");
  });

  return waitForAnimation(moveDuration, () => {
    sourcePieceImage.classList.remove("moving-source-hidden");
    ghost.remove();
  });
}

function playCaptureAnimation(move, pieceType, attackOrigin) {
  const sourceCell = attackOrigin || { row: move.fromRow, col: move.fromCol };
  const sourceSquare = getSquareElement(sourceCell.row, sourceCell.col);
  const targetCell = captureCellForAnimation(move);
  const targetSquare = getSquareElement(targetCell.row, targetCell.col);
  if (!sourceSquare || !targetSquare) {
    return Promise.resolve();
  }

  const sourceClass = `attacking-${pieceType}`;
  const targetClass = `under-attack-${pieceType}`;
  const angle = attackAngleDeg(move.fromRow, move.fromCol, targetCell.row, targetCell.col);

  sourceSquare.classList.add("attacking", sourceClass);
  targetSquare.classList.add("under-attack", targetClass);

  const originFx = document.createElement("span");
  originFx.className = `capture-fx origin piece-${pieceType}`;
  originFx.style.setProperty("--attack-angle", `${angle}deg`);
  sourceSquare.appendChild(originFx);

  const targetFx = document.createElement("span");
  targetFx.className = `capture-fx target piece-${pieceType}`;
  targetFx.style.setProperty("--attack-angle", `${angle}deg`);
  targetSquare.appendChild(targetFx);

  const duration = CAPTURE_ANIMATION_MS[pieceType] || 680;
  return waitForAnimation(duration, () => {
    originFx.remove();
    targetFx.remove();
    sourceSquare.classList.remove("attacking", sourceClass);
    targetSquare.classList.remove("under-attack", targetClass);
  });
}

function waitForAnimation(durationMs, cleanup) {
  return new Promise((resolve) => {
    let completed = false;
    const finish = () => {
      if (completed) {
        return;
      }
      completed = true;
      if (state.animationTimer) {
        clearTimeout(state.animationTimer);
      }
      state.animationTimer = null;
      state.animationResolve = null;
      if (cleanup) {
        cleanup();
      }
      resolve();
    };

    state.animationResolve = finish;
    state.animationTimer = setTimeout(finish, durationMs);
  });
}

function clearPendingAnimationWait() {
  if (state.animationResolve) {
    state.animationResolve();
    return;
  }
  if (state.animationTimer) {
    clearTimeout(state.animationTimer);
    state.animationTimer = null;
  }
}

function clearTransientEffects() {
  const transientNodes = boardElement.querySelectorAll(".capture-fx, .move-ghost");
  transientNodes.forEach((node) => node.remove());
  const movingSources = boardElement.querySelectorAll(".moving-source-hidden");
  movingSources.forEach((node) => node.classList.remove("moving-source-hidden"));
  const animatedSquares = boardElement.querySelectorAll(".attacking, .under-attack");
  animatedSquares.forEach((square) => {
    square.className = square.className
      .split(" ")
      .filter((className) => !className.startsWith("attacking-") && !className.startsWith("under-attack"))
      .filter((className) => className !== "attacking" && className !== "under-attack")
      .join(" ");
  });
}

function clearSelection() {
  state.selected = null;
  state.legalMovesForSelected = [];
}

function renderMoves() {
  movesListElement.innerHTML = "";
  for (let i = 0; i < state.history.length; i += 1) {
    const entry = document.createElement("li");
    const moveNumber = Math.floor(i / 2) + 1;
    const prefix = i % 2 === 0 ? `${moveNumber}. ` : "";
    entry.textContent = `${prefix}${state.history[i]}`;
    movesListElement.appendChild(entry);
  }
}

function renderBoard() {
  const squares = Array.from(boardElement.children);
  const legalTargets = new Map();

  state.legalMovesForSelected.forEach((move) => {
    legalTargets.set(`${move.toRow},${move.toCol}`, move);
  });

  const checkedKing = state.checkSquare
    ? `${state.checkSquare.row},${state.checkSquare.col}`
    : null;

  for (let row = 0; row < BOARD_SIZE; row += 1) {
    for (let col = 0; col < BOARD_SIZE; col += 1) {
      const index = row * BOARD_SIZE + col;
      const squareElement = squares[index];
      const piece = state.board[row][col];

      squareElement.replaceChildren();
      squareElement.classList.remove("selected", "legal", "capture", "in-check");

      if (piece) {
        squareElement.appendChild(createPieceImage(piece));
      }

      if (state.selected && state.selected.row === row && state.selected.col === col) {
        squareElement.classList.add("selected");
      }

      const targetKey = `${row},${col}`;
      const legalMove = legalTargets.get(targetKey);
      if (legalMove) {
        squareElement.classList.add("legal");
        if (legalMove.capture || legalMove.special === "en-passant") {
          squareElement.classList.add("capture");
        }
      }

      if (checkedKing === targetKey) {
        squareElement.classList.add("in-check");
      }

      if ((row === 7 && col >= 0) || (col === 0 && row <= 7)) {
        addCoordinates(squareElement, row, col);
      }
    }
  }
}

function createPieceImage(piece) {
  const pieceImage = document.createElement("img");
  pieceImage.className = `piece-icon piece-${piece.type} ${piece.color === "w" ? "team-white" : "team-black"}`;
  pieceImage.src = PIECE_ASSETS[piece.type];
  pieceImage.alt = `${piece.color === "w" ? "White" : "Black"} ${piece.type}`;
  pieceImage.draggable = false;
  return pieceImage;
}

function addCoordinates(squareElement, row, col) {
  const oldLabels = squareElement.querySelectorAll(".coord");
  oldLabels.forEach((label) => label.remove());

  if (col === 0) {
    const rankLabel = document.createElement("span");
    rankLabel.className = "coord rank";
    rankLabel.textContent = String(8 - row);
    squareElement.appendChild(rankLabel);
  }

  if (row === 7) {
    const fileLabel = document.createElement("span");
    fileLabel.className = "coord file";
    fileLabel.textContent = FILES[col];
    squareElement.appendChild(fileLabel);
  }
}

function updateStatus() {
  if (state.gameOver) {
    const currentColor = colorName(state.turn);
    const opponentColor = colorName(oppositeColor(state.turn));
    const hasMoves = hasAnyLegalMove(state, state.turn);
    const inCheck = isKingInCheck(state, state.turn);
    if (!hasMoves && inCheck) {
      statusElement.textContent = `Checkmate. ${opponentColor} wins.`;
    } else if (!hasMoves) {
      statusElement.textContent = "Stalemate. Draw.";
    } else {
      statusElement.textContent = `Game over. ${currentColor} to move.`;
    }
    return;
  }

  const inCheck = isKingInCheck(state, state.turn);
  if (inCheck) {
    statusElement.textContent = `${colorName(state.turn)} is in check.`;
  } else {
    statusElement.textContent = `${colorName(state.turn)} to move.`;
  }
}

function evaluateGameState() {
  const inCheck = isKingInCheck(state, state.turn);
  state.checkSquare = inCheck ? findKingPosition(state.board, state.turn) : null;
  const hasMoves = hasAnyLegalMove(state, state.turn);

  if (!hasMoves) {
    state.gameOver = true;
  } else {
    state.gameOver = false;
  }
}

function hasAnyLegalMove(currentState, color) {
  for (let row = 0; row < BOARD_SIZE; row += 1) {
    for (let col = 0; col < BOARD_SIZE; col += 1) {
      const piece = currentState.board[row][col];
      if (!piece || piece.color !== color) {
        continue;
      }
      const legalMoves = generateLegalMovesForPiece(currentState, row, col);
      if (legalMoves.length > 0) {
        return true;
      }
    }
  }
  return false;
}

function generateLegalMovesForPiece(currentState, row, col) {
  const piece = currentState.board[row][col];
  if (!piece) {
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
      return slidingMoves(currentState, row, col, piece, [
        [-1, -1],
        [-1, 1],
        [1, -1],
        [1, 1]
      ]);
    case "rook":
      return slidingMoves(currentState, row, col, piece, [
        [-1, 0],
        [1, 0],
        [0, -1],
        [0, 1]
      ]);
    case "queen":
      return slidingMoves(currentState, row, col, piece, [
        [-1, -1],
        [-1, 1],
        [1, -1],
        [1, 1],
        [-1, 0],
        [1, 0],
        [0, -1],
        [0, 1]
      ]);
    case "king":
      return kingMoves(currentState, row, col, piece);
    default:
      return [];
  }
}

function pawnMoves(currentState, row, col, piece) {
  const moves = [];
  const forward = piece.color === "w" ? -1 : 1;
  const startRow = piece.color === "w" ? 6 : 1;
  const promotionRow = piece.color === "w" ? 0 : 7;

  const oneForwardRow = row + forward;
  if (inBounds(oneForwardRow, col) && !currentState.board[oneForwardRow][col]) {
    moves.push({
      fromRow: row,
      fromCol: col,
      toRow: oneForwardRow,
      toCol: col,
      promotion: oneForwardRow === promotionRow
    });

    const twoForwardRow = row + forward * 2;
    if (
      row === startRow &&
      inBounds(twoForwardRow, col) &&
      !currentState.board[twoForwardRow][col]
    ) {
      moves.push({
        fromRow: row,
        fromCol: col,
        toRow: twoForwardRow,
        toCol: col,
        special: "double-step"
      });
    }
  }

  for (const deltaCol of [-1, 1]) {
    const captureRow = row + forward;
    const captureCol = col + deltaCol;
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
    }
  }

  if (currentState.enPassant) {
    const ep = currentState.enPassant;
    if (
      ep.row === row + forward &&
      Math.abs(ep.col - col) === 1 &&
      inBounds(ep.row, ep.col)
    ) {
      moves.push({
        fromRow: row,
        fromCol: col,
        toRow: ep.row,
        toCol: ep.col,
        capture: true,
        special: "en-passant"
      });
    }
  }

  return moves;
}

function knightMoves(currentState, row, col, piece) {
  const moves = [];
  const offsets = [
    [-2, -1],
    [-2, 1],
    [-1, -2],
    [-1, 2],
    [1, -2],
    [1, 2],
    [2, -1],
    [2, 1]
  ];

  for (const [dr, dc] of offsets) {
    const nextRow = row + dr;
    const nextCol = col + dc;
    if (!inBounds(nextRow, nextCol)) {
      continue;
    }
    const target = currentState.board[nextRow][nextCol];
    if (!target || target.color !== piece.color) {
      moves.push({
        fromRow: row,
        fromCol: col,
        toRow: nextRow,
        toCol: nextCol,
        capture: Boolean(target)
      });
    }
  }

  return moves;
}

function slidingMoves(currentState, row, col, piece, directions) {
  const moves = [];

  for (const [dr, dc] of directions) {
    let nextRow = row + dr;
    let nextCol = col + dc;
    while (inBounds(nextRow, nextCol)) {
      const target = currentState.board[nextRow][nextCol];
      if (!target) {
        moves.push({
          fromRow: row,
          fromCol: col,
          toRow: nextRow,
          toCol: nextCol
        });
      } else {
        if (target.color !== piece.color) {
          moves.push({
            fromRow: row,
            fromCol: col,
            toRow: nextRow,
            toCol: nextCol,
            capture: true
          });
        }
        break;
      }
      nextRow += dr;
      nextCol += dc;
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

      const nextRow = row + dr;
      const nextCol = col + dc;
      if (!inBounds(nextRow, nextCol)) {
        continue;
      }

      const target = currentState.board[nextRow][nextCol];
      if (!target || target.color !== piece.color) {
        moves.push({
          fromRow: row,
          fromCol: col,
          toRow: nextRow,
          toCol: nextCol,
          capture: Boolean(target)
        });
      }
    }
  }

  if (!piece.hasMoved && !isKingInCheck(currentState, piece.color)) {
    const homeRow = piece.color === "w" ? 7 : 0;
    if (row === homeRow && col === 4) {
      if (canCastleKingSide(currentState, piece.color)) {
        moves.push({
          fromRow: row,
          fromCol: col,
          toRow: homeRow,
          toCol: 6,
          special: "castle-king"
        });
      }

      if (canCastleQueenSide(currentState, piece.color)) {
        moves.push({
          fromRow: row,
          fromCol: col,
          toRow: homeRow,
          toCol: 2,
          special: "castle-queen"
        });
      }
    }
  }

  return moves;
}

function canCastleKingSide(currentState, color) {
  const row = color === "w" ? 7 : 0;
  const rook = currentState.board[row][7];
  if (!rook || rook.type !== "rook" || rook.color !== color || rook.hasMoved) {
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
  const rook = currentState.board[row][0];
  if (!rook || rook.type !== "rook" || rook.color !== color || rook.hasMoved) {
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

function applyMove(targetState, move, options = { simulate: false }) {
  const board = targetState.board;
  const piece = board[move.fromRow][move.fromCol];
  const movingColor = piece.color;
  const defendingColor = oppositeColor(movingColor);
  const targetBeforeMove = board[move.toRow][move.toCol];

  let capture = Boolean(targetBeforeMove);
  let promotionType = null;
  let notation = "";

  targetState.enPassant = null;

  if (move.special === "castle-king" || move.special === "castle-queen") {
    const rookFromCol = move.special === "castle-king" ? 7 : 0;
    const rookToCol = move.special === "castle-king" ? 5 : 3;
    const rook = board[move.fromRow][rookFromCol];
    board[move.fromRow][rookToCol] = rook;
    board[move.fromRow][rookFromCol] = null;
    rook.hasMoved = true;
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
    promotionType = options.simulate ? "queen" : choosePromotionType(movingColor);
    piece.type = promotionType;
  }

  if (!options.simulate) {
    notation = formatNotation(targetState, move, capture, promotionType, defendingColor);
  }

  targetState.turn = defendingColor;
  return { notation };
}

function choosePromotionType(color) {
  const answer = prompt(
    `${colorName(color)} pawn promotion. Choose: q, r, b, n`,
    "q"
  );

  switch ((answer || "").trim().toLowerCase()) {
    case "r":
      return "rook";
    case "b":
      return "bishop";
    case "n":
      return "knight";
    case "q":
    default:
      return "queen";
  }
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

function isKingInCheck(currentState, color) {
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

function isSquareAttacked(currentState, targetRow, targetCol, attackerColor) {
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
          ? [
              [-1, -1],
              [-1, 1],
              [1, -1],
              [1, 1]
            ]
          : piece.type === "rook"
            ? [
                [-1, 0],
                [1, 0],
                [0, -1],
                [0, 1]
              ]
            : [
                [-1, -1],
                [-1, 1],
                [1, -1],
                [1, 1],
                [-1, 0],
                [1, 0],
                [0, -1],
                [0, 1]
              ];

      for (const [dr, dc] of directions) {
        let scanRow = row + dr;
        let scanCol = col + dc;
        while (inBounds(scanRow, scanCol)) {
          if (scanRow === targetRow && scanCol === targetCol) {
            return true;
          }
          if (board[scanRow][scanCol]) {
            break;
          }
          scanRow += dr;
          scanCol += dc;
        }
      }
    }
  }

  return false;
}

function cloneState(currentState) {
  return {
    board: currentState.board.map((row) =>
      row.map((piece) => (piece ? { ...piece } : null))
    ),
    turn: currentState.turn,
    selected: null,
    legalMovesForSelected: [],
    enPassant: currentState.enPassant ? { ...currentState.enPassant } : null,
    gameOver: currentState.gameOver,
    history: [...currentState.history],
    checkSquare: currentState.checkSquare ? { ...currentState.checkSquare } : null
  };
}

function getSquareElement(row, col) {
  const index = row * BOARD_SIZE + col;
  return boardElement.children[index] || null;
}

function captureCellForAnimation(move) {
  if (move.special === "en-passant") {
    return { row: move.fromRow, col: move.toCol };
  }
  return { row: move.toRow, col: move.toCol };
}

function attackAngleDeg(fromRow, fromCol, toRow, toCol) {
  const deltaY = toRow - fromRow;
  const deltaX = toCol - fromCol;
  return (Math.atan2(deltaY, deltaX) * 180) / Math.PI;
}

function inBounds(row, col) {
  return row >= 0 && row < BOARD_SIZE && col >= 0 && col < BOARD_SIZE;
}

function oppositeColor(color) {
  return color === "w" ? "b" : "w";
}

function toAlgebraic(row, col) {
  return `${FILES[col]}${8 - row}`;
}

function colorName(color) {
  return color === "w" ? "White" : "Black";
}

resetButton.addEventListener("click", resetGame);
initializeBoardUI();
evaluateGameState();
updateStatus();
renderBoard();
