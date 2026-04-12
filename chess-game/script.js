import {
  BOARD_SIZE,
  createInitialState,
  generateLegalMovesForPiece,
  applyMove,
  evaluateGameState,
  toAlgebraic,
  colorName,
  buildReplayLog,
  replayFromLog,
  computeDeterministicHash
} from "./domain.js";

const PIECE_ASSETS = {
  pawn: {
    w: "./assets/piece-pawn-white.svg",
    b: "./assets/piece-pawn-black.svg"
  },
  rook: "./assets/rook-carriage.svg",
  bishop: "./assets/bishop-horse-naginata.svg",
  knight: {
    w: "./assets/piece-knight-white.svg",
    b: "./assets/piece-knight-black.svg"
  },
  queen: "./assets/queen-robot.svg",
  king: "./assets/king-fat.svg"
};

const NATIVE_COLOR_TYPES = new Set(["pawn", "knight"]);

const boardElement = document.getElementById("board");
const statusElement = document.getElementById("status");
const movesListElement = document.getElementById("movesList");
const resetButton = document.getElementById("resetBtn");

const state = {
  ...createInitialState(),
  selected: null,
  legalMovesForSelected: [],
  animationLock: false
};

initializeBoardUI();
renderBoard();
renderMoves();
updateStatus();

resetButton.addEventListener("click", () => {
  Object.assign(state, createInitialState(), {
    selected: null,
    legalMovesForSelected: [],
    animationLock: false
  });
  renderBoard();
  renderMoves();
  updateStatus();
});

window.chessReplay = {
  exportReplay: () => buildReplayLog(createInitialState(), movesFromHistory()),
  verifyReplay: (replay) => replayFromLog(replay),
  deterministicHash: () => computeDeterministicHash(state)
};

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

async function onSquareClick(event) {
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
      await commitMove(chosenMove);
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
  state.animationLock = true;
  clearSelection();
  renderBoard();

  await playSimpleMoveAnimation(move);

  const result = applyMove(state, move, {
    choosePromotion: () => choosePromotionType(state.turn)
  });

  if (!result.accepted) {
    state.animationLock = false;
    updateStatus();
    return;
  }

  renderMoves();
  updateStatus();
  renderBoard();
  state.animationLock = false;
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
    default:
      return "queen";
  }
}

function clearSelection() {
  state.selected = null;
  state.legalMovesForSelected = [];
}

function renderBoard() {
  const legalTargets = new Map();
  state.legalMovesForSelected.forEach((move) => {
    legalTargets.set(`${move.toRow},${move.toCol}`, move);
  });

  for (let row = 0; row < BOARD_SIZE; row += 1) {
    for (let col = 0; col < BOARD_SIZE; col += 1) {
      const squareElement = getSquareElement(row, col);
      const piece = state.board[row][col];
      squareElement.innerHTML = "";

      squareElement.classList.remove("selected", "legal", "capture", "in-check");
      if (state.selected && state.selected.row === row && state.selected.col === col) {
        squareElement.classList.add("selected");
      }

      const legalMove = legalTargets.get(`${row},${col}`);
      if (legalMove) {
        squareElement.classList.add("legal");
        if (legalMove.capture) {
          squareElement.classList.add("capture");
        }
      }

      if (piece) {
        squareElement.appendChild(createPieceImage(piece));
      }

      addCoordinates(squareElement, row, col);
    }
  }

  const game = evaluateGameState(state);
  if (game.inCheck) {
    const checkSquare = findKingSquare(state.turn);
    if (checkSquare) {
      getSquareElement(checkSquare.row, checkSquare.col).classList.add("in-check");
    }
  }
}

function createPieceImage(piece) {
  const assetSource = getPieceAsset(piece);
  const image = document.createElement("img");
  image.className = `piece piece-${piece.type} ${piece.color === "w" ? "team-white" : "team-black"}`;
  image.alt = `${piece.color === "w" ? "White" : "Black"} ${piece.type}`;
  image.src = assetSource;
  image.draggable = false;
  if (NATIVE_COLOR_TYPES.has(piece.type)) {
    image.classList.add("native-color");
  }
  return image;
}

function getPieceAsset(piece) {
  const configured = PIECE_ASSETS[piece.type];
  if (typeof configured === "string") {
    return configured;
  }
  return configured[piece.color] || configured.w || configured.b;
}

function addCoordinates(squareElement, row, col) {
  const oldLabels = squareElement.querySelectorAll(".coord");
  oldLabels.forEach((label) => label.remove());

  if (col === 0) {
    const rank = document.createElement("span");
    rank.className = "coord rank";
    rank.textContent = String(8 - row);
    squareElement.appendChild(rank);
  }
  if (row === 7) {
    const file = document.createElement("span");
    file.className = "coord file";
    file.textContent = String.fromCharCode(97 + col);
    squareElement.appendChild(file);
  }
}

function updateStatus() {
  const game = evaluateGameState(state);
  const side = colorName(state.turn);
  if (game.checkmate) {
    statusElement.textContent = `Checkmate. ${side} to move has no legal move.`;
    return;
  }
  if (game.stalemate) {
    statusElement.textContent = `Stalemate. ${side} to move has no legal move.`;
    return;
  }
  if (game.inCheck) {
    statusElement.textContent = `${side} to move (in check).`;
    return;
  }
  statusElement.textContent = `${side} to move.`;
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

function getSquareElement(row, col) {
  return boardElement.querySelector(`.square[data-row="${row}"][data-col="${col}"]`);
}

function findKingSquare(color) {
  for (let row = 0; row < BOARD_SIZE; row += 1) {
    for (let col = 0; col < BOARD_SIZE; col += 1) {
      const piece = state.board[row][col];
      if (piece && piece.color === color && piece.type === "king") {
        return { row, col };
      }
    }
  }
  return null;
}

function movesFromHistory() {
  const moves = [];
  for (const notation of state.history) {
    if (notation === "O-O" || notation === "O-O-O") {
      continue;
    }
    const clean = notation.replace(/[+#].*$/, "");
    const [from, toWithPromo] = clean.split(/[-x]/);
    if (!from || !toWithPromo) {
      continue;
    }
    const [to] = toWithPromo.split("=");
    const fromRow = 8 - Number(from[1]);
    const fromCol = from.charCodeAt(0) - 97;
    const toRow = 8 - Number(to[1]);
    const toCol = to.charCodeAt(0) - 97;
    moves.push({ fromRow, fromCol, toRow, toCol });
  }
  return moves;
}

function playSimpleMoveAnimation(move) {
  const sourceSquare = getSquareElement(move.fromRow, move.fromCol);
  const targetSquare = getSquareElement(move.toRow, move.toCol);
  if (!sourceSquare || !targetSquare) {
    return Promise.resolve();
  }

  sourceSquare.classList.add("attacking");
  targetSquare.classList.add("under-attack");

  return new Promise((resolve) => {
    setTimeout(() => {
      sourceSquare.classList.remove("attacking");
      targetSquare.classList.remove("under-attack");
      resolve();
    }, 120);
  });
}

console.info("[Chess] Domain/UI split enabled.", {
  deterministicHash: computeDeterministicHash(state),
  firstSquare: toAlgebraic(7, 0)
});
