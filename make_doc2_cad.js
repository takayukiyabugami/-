const fs = require("fs");
const path = require("path");

const outDir = path.join(__dirname, "outputs");
fs.mkdirSync(outDir, { recursive: true });

const DXF = [];
const SVG = [];

function push(...items) {
  for (const item of items) DXF.push(String(item));
}

function fmt(n) {
  return Number(n).toFixed(6);
}

function line(layer, x1, y1, x2, y2) {
  push("0", "LINE", "8", layer, "10", fmt(x1), "20", fmt(y1), "30", "0.0", "11", fmt(x2), "21", fmt(y2), "31", "0.0");
}

function poly(layer, pts, closed = true) {
  push("0", "POLYLINE", "8", layer, "66", "1", "70", closed ? "1" : "0");
  for (const [x, y] of pts) {
    push("0", "VERTEX", "8", layer, "10", fmt(x), "20", fmt(y), "30", "0.0");
  }
  push("0", "SEQEND", "8", layer);
}

function text(layer, x, y, h, s, rot = 0, align = "CENTER") {
  push("0", "TEXT", "8", layer, "10", fmt(x), "20", fmt(y), "30", "0.0", "40", h.toFixed(1), "1", s, "50", rot.toFixed(1));
  if (align === "CENTER") {
    push("72", "1", "73", "2", "11", fmt(x), "21", fmt(y), "31", "0.0");
  }
}

function circle(layer, x, y, r) {
  push("0", "CIRCLE", "8", layer, "10", fmt(x), "20", fmt(y), "30", "0.0", "40", fmt(r));
}

function dist(a, b) {
  return Math.hypot(a[0] - b[0], a[1] - b[1]);
}

function circleIntersection(p0, r0, p1, r1, side) {
  const [x0, y0] = p0;
  const [x1, y1] = p1;
  const dx = x1 - x0;
  const dy = y1 - y0;
  const d = Math.hypot(dx, dy);
  const a = (r0 * r0 - r1 * r1 + d * d) / (2 * d);
  const h = Math.sqrt(Math.max(0, r0 * r0 - a * a));
  const xm = x0 + (a * dx) / d;
  const ym = y0 + (a * dy) / d;
  const rx = -(dy * h) / d;
  const ry = (dx * h) / d;
  const c1 = [xm + rx, ym + ry];
  const c2 = [xm - rx, ym - ry];
  return side === "upper" ? (c1[1] > c2[1] ? c1 : c2) : (c1[1] < c2[1] ? c1 : c2);
}

function dimH(x1, x2, yBase, yDim, label) {
  const s = yDim >= yBase ? 1 : -1;
  const ext = 180;
  line("DIM", x1, yBase, x1, yDim + s * ext);
  line("DIM", x2, yBase, x2, yDim + s * ext);
  line("DIM", x1, yDim, x2, yDim);
  tick(x1, yDim);
  tick(x2, yDim);
  text("TEXT", (x1 + x2) / 2, yDim + s * 280, 250, label);
}

function dimAligned(p1, p2, off, label) {
  const vx = p2[0] - p1[0];
  const vy = p2[1] - p1[1];
  const len = Math.hypot(vx, vy);
  const ux = vx / len;
  const uy = vy / len;
  const nx = -uy;
  const ny = ux;
  const a = [p1[0] + nx * off, p1[1] + ny * off];
  const b = [p2[0] + nx * off, p2[1] + ny * off];
  line("DIM", p1[0], p1[1], a[0], a[1]);
  line("DIM", p2[0], p2[1], b[0], b[1]);
  line("DIM", a[0], a[1], b[0], b[1]);
  tick(a[0], a[1]);
  tick(b[0], b[1]);
  const ang = (Math.atan2(vy, vx) * 180) / Math.PI;
  text("TEXT", (a[0] + b[0]) / 2, (a[1] + b[1]) / 2 + 220, 250, label, ang);
}

function dimV(xBase, xDim, y1, y2, label) {
  const s = xDim >= xBase ? 1 : -1;
  const ext = 180;
  line("DIM", xBase, y1, xDim + s * ext, y1);
  line("DIM", xBase, y2, xDim + s * ext, y2);
  line("DIM", xDim, y1, xDim, y2);
  tick(xDim, y1);
  tick(xDim, y2);
  text("TEXT", xDim + s * 280, (y1 + y2) / 2, 250, label, 90);
}

function tick(x, y) {
  const d = 65;
  line("DIM", x - d, y - d, x + d, y + d);
}

function dxfHeader() {
  push(
    "0", "SECTION", "2", "HEADER",
    "9", "$ACADVER", "1", "AC1009",
    "9", "$INSUNITS", "70", "4",
    "9", "$MEASUREMENT", "70", "1",
    "0", "ENDSEC",
    "0", "SECTION", "2", "TABLES",
    "0", "TABLE", "2", "LTYPE", "70", "1",
    "0", "LTYPE", "2", "CONTINUOUS", "70", "0", "3", "Solid line", "72", "65", "73", "0", "40", "0.0",
    "0", "ENDTAB",
    "0", "TABLE", "2", "LAYER", "70", "6"
  );
  const layers = [["FRAME", 8], ["OUTLINE", 7], ["CENTER", 3], ["DIM", 1], ["TEXT", 2], ["NOTE", 4]];
  for (const [name, color] of layers) {
    push("0", "LAYER", "2", name, "70", "0", "62", color, "6", "CONTINUOUS");
  }
  push(
    "0", "ENDTAB",
    "0", "TABLE", "2", "STYLE", "70", "1",
    "0", "STYLE", "2", "STANDARD", "70", "0", "40", "0.0", "41", "1.0", "50", "0.0", "71", "0", "42", "250.0", "3", "txt", "4", "",
    "0", "ENDTAB",
    "0", "ENDSEC",
    "0", "SECTION", "2", "BLOCKS", "0", "ENDSEC",
    "0", "SECTION", "2", "ENTITIES"
  );
}

function dxfFooter() {
  push("0", "ENDSEC", "0", "EOF");
}

// Real-size geometry in millimetres. Plot at 1:100 on A4 landscape.
const W = 29700;
const H = 21000;
const margin = 1000;
const titleH = 2100;
const origin = [8200, 9600];

const TL = [origin[0], origin[1] + 1350];
const BL = [origin[0], origin[1]];
const TR = [origin[0] + 11600, origin[1] + 900];
const BR = [origin[0] + 11600, origin[1]];
const U = circleIntersection(TL, 8200, TR, 6400, "upper");
const L = circleIntersection(BL, 6300, BR, 6500, "lower");

dxfHeader();

// A4 sheet frame represented in model space for 1:100 plotting.
poly("FRAME", [[0, 0], [W, 0], [W, H], [0, H]]);
poly("FRAME", [[margin, margin], [W - margin, margin], [W - margin, H - margin], [margin, H - margin]]);
line("FRAME", margin, margin + titleH, W - margin, margin + titleH);
line("FRAME", W - 9500, margin, W - 9500, margin + titleH);
line("FRAME", W - 5200, margin, W - 5200, margin + titleH);
text("TEXT", W - 7350, margin + 1380, 320, "A4 / SCALE 1:100");
text("TEXT", W - 7350, margin + 620, 260, "CAD TRACE FROM 2026-04-27 IMAGE");
text("TEXT", W - 3000, margin + 1380, 320, "UNIT: mm");
text("TEXT", W - 3000, margin + 620, 260, "A4 LANDSCAPE");

// Main figure.
poly("OUTLINE", [TL, U, TR, BR, L, BL]);
line("OUTLINE", TL[0], TL[1], TR[0], TR[1]);
line("OUTLINE", BL[0], BL[1], BR[0], BR[1]);
line("CENTER", TL[0], TL[1] - 420, BR[0], BR[1] + 420);

circle("NOTE", TL[0], TL[1], 80);
circle("NOTE", TR[0], TR[1], 80);
circle("NOTE", BL[0], BL[1], 80);
circle("NOTE", BR[0], BR[1], 80);

// Dimensions.
dimH(TL[0], TR[0], TL[1], TL[1] + 950, "11600");
dimH(BL[0], BR[0], BL[1], BL[1] - 850, "11600");
dimV(TL[0], TL[0] - 850, BL[1], TL[1], "1350");
dimV(TR[0], TR[0] + 850, BR[1], TR[1], "900");
dimAligned(TL, U, 500, "8200");
dimAligned(U, TR, 500, "6400");
dimAligned(BL, L, -500, "6300");
dimAligned(L, BR, -500, "6500");

text("TEXT", (TL[0] + TR[0]) / 2, (TL[1] + TR[1]) / 2 - 560, 300, "0.14%", -2.2);
text("TEXT", TR[0] + 1800, TR[1] - 1800, 300, "1.14%", -70);
text("TEXT", BL[0] + 2500, L[1] - 1000, 300, "1.27%", -35);
text("NOTE", margin + 500, H - margin - 450, 280, "NOTE: Handwritten source was traced from visible dimensions. Verify unreadable dimensions before construction.", 0, "LEFT");

// Verification notes off the printed title area but inside DXF.
text("NOTE", margin + 500, margin + 1550, 220, `CHECK upper: ${dist(TL, U).toFixed(0)} / ${dist(U, TR).toFixed(0)} mm`, 0, "LEFT");
text("NOTE", margin + 500, margin + 1050, 220, `CHECK lower: ${dist(BL, L).toFixed(0)} / ${dist(L, BR).toFixed(0)} mm`, 0, "LEFT");
text("NOTE", margin + 500, margin + 550, 220, `CHECK ends: left ${dist(TL, BL).toFixed(0)} / right ${dist(TR, BR).toFixed(0)} mm`, 0, "LEFT");

dxfFooter();

const dxfPath = path.join(outDir, "doc2_A4_1-100.dxf");
fs.writeFileSync(dxfPath, DXF.join("\n") + "\n", "utf8");

// Lightweight SVG preview for quick visual inspection.
const svgW = 1188;
const svgH = 840;
const s = 0.04;
function sx(x) { return x * s; }
function sy(y) { return svgH - y * s; }
function svgLine(a, b, color = "#111", width = 1) {
  SVG.push(`<line x1="${sx(a[0]).toFixed(2)}" y1="${sy(a[1]).toFixed(2)}" x2="${sx(b[0]).toFixed(2)}" y2="${sy(b[1]).toFixed(2)}" stroke="${color}" stroke-width="${width}" />`);
}
function svgPoly(pts, color = "#111") {
  SVG.push(`<polygon points="${pts.map(([x, y]) => `${sx(x).toFixed(2)},${sy(y).toFixed(2)}`).join(" ")}" fill="none" stroke="${color}" stroke-width="1.4" />`);
}
SVG.push(`<svg xmlns="http://www.w3.org/2000/svg" width="${svgW}" height="${svgH}" viewBox="0 0 ${svgW} ${svgH}">`);
SVG.push(`<rect x="0" y="0" width="${svgW}" height="${svgH}" fill="white" />`);
svgPoly([[0, 0], [W, 0], [W, H], [0, H]], "#888");
svgPoly([[margin, margin], [W - margin, margin], [W - margin, H - margin], [margin, H - margin]], "#999");
svgPoly([TL, U, TR, BR, L, BL], "#111");
svgLine(TL, TR);
svgLine(BL, BR);
svgLine([TL[0], TL[1] - 420], [BR[0], BR[1] + 420], "#0a7", 1);
SVG.push(`</svg>`);
fs.writeFileSync(path.join(outDir, "doc2_A4_1-100_preview.svg"), SVG.join("\n") + "\n", "utf8");

console.log(dxfPath);
console.log(JSON.stringify({ TL, U, TR, BR, L, BL }, null, 2));
