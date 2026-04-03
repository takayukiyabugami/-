import * as THREE from "https://unpkg.com/three@0.164.1/build/three.module.js";
import { OrbitControls } from "https://unpkg.com/three@0.164.1/examples/jsm/controls/OrbitControls.js";

const config = {
  board: {
    size: 8,
    tileSize: 1
  },
  movement: {
    stepMs: 340,
    bobAmp: 0.08,
    turnLerp: 0.18
  }
};

const statusEl = document.getElementById("status");
const canvas = document.getElementById("scene");

const scene = new THREE.Scene();
scene.background = new THREE.Color("#dbe7f6");

const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
renderer.setSize(window.innerWidth, window.innerHeight);

const camera = new THREE.PerspectiveCamera(52, window.innerWidth / window.innerHeight, 0.1, 120);
camera.position.set(7.6, 8.6, 7.8);

const controls = new OrbitControls(camera, renderer.domElement);
controls.enableDamping = true;
controls.target.set(0, 0, 0);
controls.minDistance = 5;
controls.maxDistance = 22;
controls.maxPolarAngle = Math.PI * 0.48;

const hemi = new THREE.HemisphereLight("#f8fbff", "#6d849a", 0.92);
scene.add(hemi);

const keyLight = new THREE.DirectionalLight("#ffffff", 0.84);
keyLight.position.set(5, 9, 3);
scene.add(keyLight);

const fillLight = new THREE.DirectionalLight("#bdd7ff", 0.34);
fillLight.position.set(-4, 5, -6);
scene.add(fillLight);

const boardTiles = [];
const boardGroup = new THREE.Group();
scene.add(boardGroup);

const tileHeight = 0.22;
const boardWidth = config.board.size * config.board.tileSize;

const frame = new THREE.Mesh(
  new THREE.BoxGeometry(boardWidth + 0.58, 0.44, boardWidth + 0.58),
  new THREE.MeshStandardMaterial({ color: "#6b4a2d", roughness: 0.88, metalness: 0.06 })
);
frame.position.y = -0.32;
scene.add(frame);

for (let row = 0; row < config.board.size; row += 1) {
  for (let col = 0; col < config.board.size; col += 1) {
    const tile = new THREE.Mesh(
      new THREE.BoxGeometry(config.board.tileSize, tileHeight, config.board.tileSize),
      new THREE.MeshStandardMaterial({
        color: (row + col) % 2 === 0 ? "#ebe3d2" : "#4f6546",
        roughness: 0.88,
        metalness: 0.04
      })
    );
    const cell = cellToWorld(row, col);
    tile.position.set(cell.x, -tileHeight / 2, cell.z);
    tile.userData = { row, col, isTile: true };
    boardTiles.push(tile);
    boardGroup.add(tile);
  }
}

const pawn = createPawnMesh();
const pieceGroup = pawn.group;
const pieceBodyMaterial = pawn.bodyMaterial;
scene.add(pieceGroup);

const raycaster = new THREE.Raycaster();
const pointer = new THREE.Vector2();
const clock = new THREE.Clock();

const state = {
  selected: false,
  pieceCell: {
    row: config.board.size - 2,
    col: Math.floor(config.board.size / 2)
  },
  activeStep: null,
  queue: [],
  yaw: 0,
  targetYaw: 0
};

placePiece(state.pieceCell);
updateSelectionVisual();
updateStatus();

window.addEventListener("resize", onResize);
window.addEventListener("pointerdown", onPointerDown);

requestAnimationFrame(tick);

function onResize() {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
}

function onPointerDown(event) {
  if (event.button !== 0) {
    return;
  }

  pointer.x = (event.clientX / window.innerWidth) * 2 - 1;
  pointer.y = -(event.clientY / window.innerHeight) * 2 + 1;
  raycaster.setFromCamera(pointer, camera);

  const intersections = raycaster.intersectObjects([pieceGroup, ...boardTiles], true);
  if (intersections.length === 0) {
    return;
  }

  const hit = intersections[0].object;
  const tile = nearestTile(hit);

  if (!tile && objectBelongsToPiece(hit)) {
    state.selected = !state.selected;
    updateSelectionVisual();
    updateStatus();
    return;
  }

  if (!tile) {
    return;
  }

  if (!state.selected) {
    return;
  }

  const { row, col } = tile.userData;
  enqueuePathTo(row, col);
}

function objectBelongsToPiece(object) {
  let node = object;
  while (node) {
    if (node === pieceGroup) {
      return true;
    }
    node = node.parent;
  }
  return false;
}

function nearestTile(object) {
  let node = object;
  while (node) {
    if (node.userData && node.userData.isTile) {
      return node;
    }
    node = node.parent;
  }
  return null;
}

function enqueuePathTo(row, col) {
  if (!inBounds(row, col)) {
    return;
  }

  const tail = queueTailCell();
  if (tail.row === row && tail.col === col) {
    return;
  }

  const path = manhattanPath(tail, { row, col });
  if (path.length === 0) {
    return;
  }

  state.queue.push(...path);
  updateStatus();
}

function queueTailCell() {
  if (state.queue.length > 0) {
    return state.queue[state.queue.length - 1];
  }
  if (state.activeStep) {
    return state.activeStep.toCell;
  }
  return state.pieceCell;
}

function manhattanPath(fromCell, toCell) {
  const result = [];
  let row = fromCell.row;
  let col = fromCell.col;

  while (row !== toCell.row) {
    row += Math.sign(toCell.row - row);
    result.push({ row, col });
  }

  while (col !== toCell.col) {
    col += Math.sign(toCell.col - col);
    result.push({ row, col });
  }

  return result;
}

function beginStep(nextCell) {
  const fromCell = { ...state.pieceCell };
  const fromPos = worldVector(fromCell.row, fromCell.col);
  const toPos = worldVector(nextCell.row, nextCell.col);

  state.targetYaw = Math.atan2(toPos.x - fromPos.x, toPos.z - fromPos.z);
  state.activeStep = {
    fromPos,
    toPos,
    toCell: { ...nextCell },
    elapsedMs: 0,
    durationMs: config.movement.stepMs
  };
  updateStatus();
}

function stepMovement(deltaMs) {
  if (!state.activeStep && state.queue.length > 0) {
    const next = state.queue.shift();
    beginStep(next);
  }

  if (!state.activeStep) {
    const idle = worldVector(state.pieceCell.row, state.pieceCell.col);
    pieceGroup.position.set(idle.x, pieceBaseY(), idle.z);
    state.yaw = dampAngle(state.yaw, state.targetYaw, config.movement.turnLerp);
    pieceGroup.rotation.y = state.yaw;
    return;
  }

  const step = state.activeStep;
  step.elapsedMs += deltaMs;
  const t = Math.min(step.elapsedMs / step.durationMs, 1);
  const eased = easeInOutQuad(t);

  pieceGroup.position.lerpVectors(step.fromPos, step.toPos, eased);
  pieceGroup.position.y = pieceBaseY() + Math.sin(Math.PI * t) * config.movement.bobAmp;

  state.yaw = dampAngle(state.yaw, state.targetYaw, config.movement.turnLerp);
  pieceGroup.rotation.y = state.yaw;

  if (t >= 1) {
    state.pieceCell = { ...step.toCell };
    state.activeStep = null;
    updateStatus();
  }
}

function tick() {
  const deltaMs = clock.getDelta() * 1000;
  stepMovement(deltaMs);
  controls.update();
  renderer.render(scene, camera);
  requestAnimationFrame(tick);
}

function updateSelectionVisual() {
  if (state.selected) {
    pieceBodyMaterial.emissive.set("#66bbff");
    pieceBodyMaterial.emissiveIntensity = 0.38;
  } else {
    pieceBodyMaterial.emissive.set("#000000");
    pieceBodyMaterial.emissiveIntensity = 0.0;
  }
}

function updateStatus() {
  if (!state.selected) {
    statusEl.textContent = "Click the pawn to select it.";
    return;
  }

  const moving = state.activeStep ? 1 : 0;
  const queuedCount = state.queue.length + moving;
  if (queuedCount > 0) {
    statusEl.textContent = `Selected. Walking queue: ${queuedCount}`;
  } else {
    statusEl.textContent = "Selected. Click a tile to queue movement.";
  }
}

function pieceBaseY() {
  return 0.13;
}

function worldVector(row, col) {
  const cell = cellToWorld(row, col);
  return new THREE.Vector3(cell.x, pieceBaseY(), cell.z);
}

function placePiece(cell) {
  const pos = worldVector(cell.row, cell.col);
  pieceGroup.position.copy(pos);
  state.targetYaw = 0;
  state.yaw = 0;
  pieceGroup.rotation.y = 0;
}

function cellToWorld(row, col) {
  const half = (config.board.size - 1) / 2;
  return {
    x: (col - half) * config.board.tileSize,
    z: (row - half) * config.board.tileSize
  };
}

function inBounds(row, col) {
  return row >= 0 && row < config.board.size && col >= 0 && col < config.board.size;
}

function easeInOutQuad(t) {
  return t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2;
}

function dampAngle(current, target, factor) {
  const twoPi = Math.PI * 2;
  let delta = (target - current + Math.PI) % twoPi - Math.PI;
  if (delta < -Math.PI) {
    delta += twoPi;
  }
  return current + delta * factor;
}

function createPawnMesh() {
  const group = new THREE.Group();

  const baseMat = new THREE.MeshStandardMaterial({
    color: "#d4dbe6",
    roughness: 0.44,
    metalness: 0.09
  });

  const bodyMat = new THREE.MeshStandardMaterial({
    color: "#f2f6ff",
    roughness: 0.41,
    metalness: 0.08,
    emissive: "#000000"
  });

  const base = new THREE.Mesh(new THREE.CylinderGeometry(0.26, 0.34, 0.16, 28), baseMat);
  base.position.y = 0.08;
  group.add(base);

  const body = new THREE.Mesh(new THREE.CylinderGeometry(0.16, 0.22, 0.28, 28), bodyMat);
  body.position.y = 0.28;
  group.add(body);

  const collar = new THREE.Mesh(new THREE.TorusGeometry(0.145, 0.03, 10, 26), baseMat);
  collar.rotation.x = Math.PI / 2;
  collar.position.y = 0.42;
  group.add(collar);

  const head = new THREE.Mesh(new THREE.SphereGeometry(0.13, 26, 16), bodyMat);
  head.position.y = 0.54;
  group.add(head);

  return { group, bodyMaterial: bodyMat };
}
