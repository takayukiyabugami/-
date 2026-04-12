import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  BOARD_SIZE,
  createInitialState,
  generateLegalMovesForPiece,
  applyMove,
  computeDeterministicHash,
  replayFromLog
} from "../domain.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const goldenPath = path.resolve(__dirname, "..", "..", "chess-spec", "golden-moves.json");
const golden = JSON.parse(fs.readFileSync(goldenPath, "utf8"));

for (const testCase of golden.cases) {
  const state = buildInitialState(testCase.initial);
  const accepted = [];
  let lastRejectReason = null;

  for (const moveText of testCase.moves) {
    const parsed = parseLongMove(moveText);
    const legalMoves = generateLegalMovesForPiece(state, parsed.fromRow, parsed.fromCol);
    const move = legalMoves.find(
      (candidate) =>
        candidate.toRow === parsed.toRow &&
        candidate.toCol === parsed.toCol
    );

    if (!move) {
      accepted.push(false);
      lastRejectReason = inferRejectReason(state, parsed);
      break;
    }

    if (parsed.promotion) {
      const promotionMap = { q: "queen", r: "rook", b: "bishop", n: "knight" };
      move.promotion = true;
      move.promotionType = promotionMap[parsed.promotion];
    }

    const result = applyMove(state, move, {
      choosePromotion: () => move.promotionType || "queen"
    });
    accepted.push(Boolean(result.accepted));
    if (!result.accepted) {
      lastRejectReason = result.rejectReason || "Unknown";
      break;
    }
  }

  assert.deepEqual(
    accepted,
    testCase.expectedAccepted,
    `${testCase.id}: accepted mismatch`
  );

  if (testCase.expectedRejectReason) {
    assert.equal(lastRejectReason, testCase.expectedRejectReason, `${testCase.id}: reject reason mismatch`);
  }

  const replay = {
    version: 1,
    initialState: buildInitialState(testCase.initial),
    moves: testCase.moves
  };
  const replayA = replayFromLog(replay);
  const replayB = replayFromLog(replay);
  assert.equal(replayA.failedPlyIndex, replayB.failedPlyIndex, `${testCase.id}: replay failed index mismatch`);
  assert.equal(
    computeDeterministicHash(replayA.state),
    computeDeterministicHash(replayB.state),
    `${testCase.id}: deterministic hash mismatch`
  );
}

console.log(`golden-parity: ${golden.cases.length} cases passed`);

function buildInitialState(name) {
  switch (name) {
    case "standard":
      return createInitialState();
    case "castle-ready-white":
      return fromPieces("w", [
        ["w", "king", "e1"],
        ["w", "rook", "h1"],
        ["b", "king", "e8"]
      ]);
    case "en-passant-ready":
      {
        const state = fromPieces("w", [
          ["w", "king", "e1"],
          ["b", "king", "e8"],
          ["w", "pawn", "e5"],
          ["b", "pawn", "d5"],
          ["b", "pawn", "a7"]
        ]);
        // Represents a just-played d7-d5 so white can play e5xd6 en passant.
        state.enPassant = parseSquare("d6");
        return state;
      }
    case "promotion-ready":
      return fromPieces("w", [
        ["w", "king", "e1"],
        ["b", "king", "e8"],
        ["w", "pawn", "a7"]
      ]);
    case "self-check-trap":
      return fromPieces("w", [
        ["w", "king", "e1"],
        ["b", "king", "a8"],
        ["b", "rook", "e8"],
        ["w", "rook", "e2"]
      ]);
    default:
      throw new Error(`unknown initial setup: ${name}`);
  }
}

function fromPieces(turn, descriptors) {
  const state = createInitialState();
  state.turn = turn;
  state.board = Array.from({ length: BOARD_SIZE }, () => Array.from({ length: BOARD_SIZE }, () => null));
  state.history = [];
  state.enPassant = null;
  state.gameOver = false;

  let id = 1;
  for (const [color, type, square] of descriptors) {
    const { row, col } = parseSquare(square);
    state.board[row][col] = { color, type, hasMoved: false, id: id++ };
  }
  return state;
}

function parseLongMove(move) {
  const normalized = move.trim().toLowerCase();
  const from = parseSquare(normalized.slice(0, 2));
  const to = parseSquare(normalized.slice(2, 4));
  const promotion = normalized.length === 5 ? normalized[4] : null;
  return { fromRow: from.row, fromCol: from.col, toRow: to.row, toCol: to.col, promotion };
}

function parseSquare(square) {
  const file = square.charCodeAt(0) - 97;
  const rank = Number(square[1]);
  return {
    row: 8 - rank,
    col: file
  };
}

function inferRejectReason(state, parsedMove) {
  const piece = state.board[parsedMove.fromRow]?.[parsedMove.fromCol];
  if (!piece) {
    return "NoPieceAtSource";
  }
  if (piece.color !== state.turn) {
    return "NotYourTurn";
  }
  return "MoveLeavesKingInCheck";
}
