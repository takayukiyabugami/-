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
    w: "./assets/piece-pawn-white-normal.svg",
    b: "./assets/piece-pawn-black-normal.svg"
  },
  promotedPawn: {
    w: "./assets/piece-pawn-white-promoted.svg",
    b: "./assets/piece-pawn-black-promoted.svg"
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
const BUILD_VERSION = "v2026.04.26-pawn-concept.1";
const MOTION_TARGET_FPS = 100;

const boardElement = document.getElementById("board");
const statusElement = document.getElementById("status");
const movesListElement = document.getElementById("movesList");
const resetButton = document.getElementById("resetBtn");
const versionBadgeElement = document.getElementById("versionBadge");

const state = {
  ...createInitialState(),
  selected: null,
  legalMovesForSelected: [],
  animationLock: false
};

initializeBoardUI();
updateVersionBadge();
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

function updateVersionBadge(text = BUILD_VERSION) {
  if (versionBadgeElement) {
    versionBadgeElement.textContent = text;
  }
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
    renderBoard();
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

function createPieceImage(piece, className = "piece") {
  const assetSource = getPieceAsset(piece);
  const image = document.createElement("img");
  image.className = `${className} piece-${piece.type} ${piece.color === "w" ? "team-white" : "team-black"}`;
  image.alt = `${piece.color === "w" ? "White" : "Black"} ${piece.type}`;
  image.src = assetSource;
  image.draggable = false;
  if (NATIVE_COLOR_TYPES.has(piece.type) || piece.promotedFrom === "pawn") {
    image.classList.add("native-color");
  }
  if (piece.promotedFrom === "pawn") {
    image.classList.add("promoted-pawn");
  }
  return image;
}

function getPieceAsset(piece) {
  if (piece.promotedFrom === "pawn") {
    return PIECE_ASSETS.promotedPawn[piece.color];
  }

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
  const movingPiece = state.board[move.fromRow][move.fromCol];
  if (!sourceSquare || !targetSquare || !movingPiece) {
    return Promise.resolve();
  }

  const boardRect = boardElement.getBoundingClientRect();
  const sourceRect = sourceSquare.getBoundingClientRect();
  const targetRect = targetSquare.getBoundingClientRect();
  const startX = sourceRect.left - boardRect.left + sourceRect.width / 2;
  const startY = sourceRect.top - boardRect.top + sourceRect.height / 2;
  const endX = targetRect.left - boardRect.left + targetRect.width / 2;
  const endY = targetRect.top - boardRect.top + targetRect.height / 2;
  const moveX = endX - startX;
  const moveY = endY - startY;
  const distance = Math.hypot(move.toRow - move.fromRow, move.toCol - move.fromCol);
  const attackAngle = `${Math.atan2(moveY, moveX) * 180 / Math.PI}deg`;
  const profile = getMoveAnimationProfile(move, movingPiece, distance);
  const sourcePieceElement = sourceSquare.querySelector(".piece");
  const capturedPieceElement = profile.isCapture ? targetSquare.querySelector(".piece") : null;
  const effects = [];
  const effectTimers = [];
  const ghost = document.createElement("div");
  ghost.className = `move-ghost piece-${movingPiece.type} ${profile.classNames.join(" ")}`;
  ghost.style.left = `${startX}px`;
  ghost.style.top = `${startY}px`;
  ghost.style.width = `${sourceRect.width}px`;
  ghost.style.height = `${sourceRect.height}px`;
  ghost.style.setProperty("--move-x", `${moveX}px`);
  ghost.style.setProperty("--move-y", `${moveY}px`);
  ghost.style.setProperty("--move-duration", `${profile.durationMs}ms`);
  ghost.style.setProperty("--run-angle", attackAngle);
  ghost.style.setProperty("--attack-angle", attackAngle);
  ghost.style.setProperty("--ghost-x", "0px");
  ghost.style.setProperty("--ghost-y", "0px");
  ghost.appendChild(createPieceImage(movingPiece, "piece-icon"));

  sourceSquare.classList.add("attacking");
  if (profile.isCapture) {
    targetSquare.classList.add("under-attack");
    effectTimers.push(window.setTimeout(() => {
      targetSquare.classList.add("slash-impact");
      effects.push(...createCaptureEffects(movingPiece, sourceSquare, targetSquare, attackAngle));
      if (movingPiece.type === "pawn" && capturedPieceElement) {
        capturedPieceElement.classList.add("defeated-by-pawn");
        targetSquare.classList.add("collapse-impact");
        updateVersionBadge(`${BUILD_VERSION} pawn-stab ${MOTION_TARGET_FPS}fps`);
      }
    }, profile.impactDelayMs));
  } else {
    targetSquare.classList.add("move-landing");
  }
  boardElement.appendChild(ghost);
  ghost.classList.add("moving");
  if (sourcePieceElement) {
    sourcePieceElement.remove();
  }
  updateVersionBadge(`${BUILD_VERSION} ${profile.label} ${MOTION_TARGET_FPS}fps`);

  return animateMoveGhost(ghost, moveX, moveY, profile).then(() => {
    ghost.remove();
    effectTimers.forEach((timerId) => window.clearTimeout(timerId));
    effects.forEach((effect) => effect.remove());
    sourceSquare.classList.remove("attacking");
    targetSquare.classList.remove("under-attack");
    targetSquare.classList.remove("move-landing", "slash-impact", "collapse-impact");
    updateVersionBadge(BUILD_VERSION);
  });
}

function getMoveAnimationProfile(move, piece, distance) {
  if (move.capture) {
    return {
      classNames: piece.type === "pawn" ? ["running", "pawn-stab"] : ["running", "slashing"],
      durationMs: piece.type === "pawn" ? 2100 : piece.type === "queen" ? 1300 : 1200,
      impactDelayMs: piece.type === "pawn" ? 1420 : 420,
      motionKind: piece.type === "pawn" ? "pawn-stab" : "linear",
      label: piece.type === "pawn" ? "pawn-stab" : "slash",
      isCapture: true
    };
  }

  if ((piece.type === "pawn" || piece.type === "king") && distance <= 1.1) {
    return {
      classNames: ["walking"],
      durationMs: 1800,
      impactDelayMs: 0,
      motionKind: "linear",
      label: "walk",
      isCapture: false
    };
  }

  return {
    classNames: ["running"],
    durationMs: Math.round(Math.min(1200, 880 + distance * 100)),
    impactDelayMs: 0,
    motionKind: "linear",
    label: "run",
    isCapture: false
  };
}

function animateMoveGhost(ghost, moveX, moveY, profile) {
  const durationMs = profile.durationMs;
  const frameMs = 1000 / MOTION_TARGET_FPS;
  let startTime = null;
  let lastPaintTime = 0;

  return new Promise((resolve) => {
    function step(now) {
      if (startTime === null) {
        startTime = now;
      }

      const elapsed = now - startTime;
      if (elapsed - lastPaintTime >= frameMs || elapsed >= durationMs) {
        const t = Math.min(elapsed / durationMs, 1);
        const eased = getMotionProgress(t, profile.motionKind);
        ghost.style.setProperty("--ghost-x", `${moveX * eased}px`);
        ghost.style.setProperty("--ghost-y", `${moveY * eased}px`);
        lastPaintTime = elapsed;
      }

      if (elapsed >= durationMs) {
        ghost.style.setProperty("--ghost-x", `${moveX}px`);
        ghost.style.setProperty("--ghost-y", `${moveY}px`);
        resolve();
        return;
      }

      requestAnimationFrame(step);
    }

    requestAnimationFrame(step);
  });
}

function getMotionProgress(t, motionKind) {
  if (motionKind !== "pawn-stab") {
    return t;
  }

  if (t < 0.48) {
    return t / 0.48 * 0.9;
  }
  if (t < 0.62) {
    return 0.9 - ((t - 0.48) / 0.14) * 0.14;
  }
  if (t < 0.72) {
    return 0.76 + ((t - 0.62) / 0.1) * 0.3;
  }
  if (t < 0.86) {
    return 1.06 - ((t - 0.72) / 0.14) * 0.1;
  }
  return 0.96 + ((t - 0.86) / 0.14) * 0.04;
}

function createCaptureEffects(piece, sourceSquare, targetSquare, attackAngle) {
  const originEffect = createCaptureEffectElement("origin", piece.type, attackAngle);
  const targetEffect = createCaptureEffectElement("target", piece.type, attackAngle);
  sourceSquare.appendChild(originEffect);
  targetSquare.appendChild(targetEffect);
  return [originEffect, targetEffect];
}

function createCaptureEffectElement(role, pieceType, attackAngle) {
  const effect = document.createElement("span");
  effect.className = `capture-fx ${role} piece-${pieceType}`;
  effect.style.setProperty("--attack-angle", attackAngle);
  return effect;
}

console.info("[Chess] Domain/UI split enabled.", {
  deterministicHash: computeDeterministicHash(state),
  firstSquare: toAlgebraic(7, 0)
});
