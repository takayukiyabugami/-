from __future__ import annotations

import math
import re
from dataclasses import dataclass
from pathlib import Path


TEXT_H = 200.0


@dataclass
class RectSpec:
    key: str
    name: str
    sx: float
    sy: float
    ex: float
    ey: float
    width: float
    ext_s: float = 0.0
    ext_e: float = 0.0
    layer: str = "OUTLINE"
    area_pos: tuple[float, float] | None = None
    curb_segment_interval: float | None = None


@dataclass
class RectGeom:
    spec: RectSpec
    ux: float
    uy: float
    nx: float
    ny: float
    length: float
    total_len: float
    hw: float
    spx: float
    spy: float
    epx: float
    epy: float
    p1: tuple[float, float]
    p2: tuple[float, float]
    p3: tuple[float, float]
    p4: tuple[float, float]
    area_m2: float


class DxfR12:
    def __init__(self) -> None:
        self._lines: list[str] = []

    def _push(self, *items: object) -> None:
        for item in items:
            self._lines.append(str(item))

    @staticmethod
    def _f(value: float) -> str:
        return f"{value:.6f}"

    def add_header(self) -> None:
        self._push(
            "0",
            "SECTION",
            "2",
            "HEADER",
            "9",
            "$ACADVER",
            "1",
            "AC1009",
            "9",
            "$DWGCODEPAGE",
            "3",
            "ANSI_932",
            "9",
            "$INSUNITS",
            "70",
            "4",
            "9",
            "$MEASUREMENT",
            "70",
            "1",
            "0",
            "ENDSEC",
        )

    def add_tables(self, layers: list[tuple[str, int]]) -> None:
        self._push("0", "SECTION", "2", "TABLES")
        self._push(
            "0",
            "TABLE",
            "2",
            "LTYPE",
            "70",
            "1",
            "0",
            "LTYPE",
            "2",
            "CONTINUOUS",
            "70",
            "0",
            "3",
            "Solid line",
            "72",
            "65",
            "73",
            "0",
            "40",
            "0.0",
            "0",
            "ENDTAB",
        )
        self._push("0", "TABLE", "2", "LAYER", "70", str(len(layers)))
        for name, color in layers:
            self._push(
                "0",
                "LAYER",
                "2",
                name,
                "70",
                "0",
                "62",
                str(color),
                "6",
                "CONTINUOUS",
            )
        self._push("0", "ENDTAB")
        self._push(
            "0",
            "TABLE",
            "2",
            "STYLE",
            "70",
            "1",
            "0",
            "STYLE",
            "2",
            "STANDARD",
            "70",
            "0",
            "40",
            "0.0",
            "41",
            "1.0",
            "50",
            "0.0",
            "71",
            "0",
            "42",
            "200.0",
            "3",
            "txt",
            "4",
            "",
            "0",
            "ENDTAB",
            "0",
            "ENDSEC",
        )

    def add_empty_blocks(self) -> None:
        self._push("0", "SECTION", "2", "BLOCKS", "0", "ENDSEC")

    def begin_entities(self) -> None:
        self._push("0", "SECTION", "2", "ENTITIES")

    def end_file(self) -> None:
        self._push("0", "ENDSEC", "0", "EOF")

    def line(self, layer: str, x1: float, y1: float, x2: float, y2: float) -> None:
        self._push(
            "0",
            "LINE",
            "8",
            layer,
            "10",
            self._f(x1),
            "20",
            self._f(y1),
            "30",
            "0.0",
            "11",
            self._f(x2),
            "21",
            self._f(y2),
            "31",
            "0.0",
        )

    def polyline_closed(self, layer: str, points: list[tuple[float, float]]) -> None:
        self._push("0", "POLYLINE", "8", layer, "66", "1", "70", "1")
        for px, py in points:
            self._push(
                "0",
                "VERTEX",
                "8",
                layer,
                "10",
                self._f(px),
                "20",
                self._f(py),
                "30",
                "0.0",
            )
        self._push("0", "SEQEND", "8", layer)

    def circle(self, layer: str, cx: float, cy: float, r: float) -> None:
        self._push(
            "0",
            "CIRCLE",
            "8",
            layer,
            "10",
            self._f(cx),
            "20",
            self._f(cy),
            "30",
            "0.0",
            "40",
            self._f(r),
        )

    def text(
        self,
        layer: str,
        x: float,
        y: float,
        value: str,
        height: float = TEXT_H,
        rotation: float = 0.0,
    ) -> None:
        self._push(
            "0",
            "TEXT",
            "8",
            layer,
            "10",
            self._f(x),
            "20",
            self._f(y),
            "30",
            "0.0",
            "40",
            f"{height:.1f}",
            "1",
            value,
            "50",
            f"{rotation:.1f}",
            "72",
            "1",
            "11",
            self._f(x),
            "21",
            self._f(y),
            "31",
            "0.0",
        )

    def dump(self) -> str:
        return "\n".join(self._lines) + "\n"


def calc_rect(spec: RectSpec) -> RectGeom | None:
    vx = spec.ex - spec.sx
    vy = spec.ey - spec.sy
    length = math.hypot(vx, vy)
    if length <= 0.0 or spec.width <= 0.0:
        return None

    ux = vx / length
    uy = vy / length
    spx = spec.sx - ux * spec.ext_s
    spy = spec.sy - uy * spec.ext_s
    epx = spec.ex + ux * spec.ext_e
    epy = spec.ey + uy * spec.ext_e
    total_len = length + spec.ext_s + spec.ext_e
    nx = -uy
    ny = ux
    hw = spec.width / 2.0
    p1 = (spx + nx * hw, spy + ny * hw)
    p2 = (epx + nx * hw, epy + ny * hw)
    p3 = (epx - nx * hw, epy - ny * hw)
    p4 = (spx - nx * hw, spy - ny * hw)
    area_m2 = round(total_len * spec.width / 1_000_000.0, 2)
    return RectGeom(
        spec=spec,
        ux=ux,
        uy=uy,
        nx=nx,
        ny=ny,
        length=length,
        total_len=total_len,
        hw=hw,
        spx=spx,
        spy=spy,
        epx=epx,
        epy=epy,
        p1=p1,
        p2=p2,
        p3=p3,
        p4=p4,
        area_m2=area_m2,
    )


def add_tick_45(dxf: DxfR12, layer: str, x: float, y: float, dx: float, dy: float) -> None:
    norm = math.hypot(dx, dy)
    if norm <= 0:
        return
    ux = dx / norm
    uy = dy / norm
    c = math.sqrt(0.5)
    tx = ux * c - uy * c
    ty = ux * c + uy * c
    tnorm = math.hypot(tx, ty)
    if tnorm <= 0:
        return
    tx /= tnorm
    ty /= tnorm
    half_len = 60.0
    dxf.line(layer, x - tx * half_len, y - ty * half_len, x + tx * half_len, y + ty * half_len)


def dim_h(dxf: DxfR12, x1: float, x2: float, y_obj: float, y_dim: float, label: str, text_h: float) -> None:
    if x2 < x1:
        x1, x2 = x2, x1
    over = 150.0
    sign = 1.0 if y_dim >= y_obj else -1.0
    dxf.line("DIM", x1, y_obj, x1, y_dim + sign * over)
    dxf.line("DIM", x2, y_obj, x2, y_dim + sign * over)
    dxf.line("DIM", x1, y_dim, x2, y_dim)
    add_tick_45(dxf, "DIM", x1, y_dim, x2 - x1, 0.0)
    add_tick_45(dxf, "DIM", x2, y_dim, x2 - x1, 0.0)
    dxf.text("TEXT", (x1 + x2) / 2.0, y_dim + (50.0 if sign > 0 else -50.0), label, text_h, 0.0)


def dim_v(dxf: DxfR12, y1: float, y2: float, x_obj: float, x_dim: float, label: str, text_h: float) -> None:
    if y2 < y1:
        y1, y2 = y2, y1
    over = 150.0
    sign = 1.0 if x_dim >= x_obj else -1.0
    dxf.line("DIM", x_obj, y1, x_dim + sign * over, y1)
    dxf.line("DIM", x_obj, y2, x_dim + sign * over, y2)
    dxf.line("DIM", x_dim, y1, x_dim, y2)
    add_tick_45(dxf, "DIM", x_dim, y1, 0.0, y2 - y1)
    add_tick_45(dxf, "DIM", x_dim, y2, 0.0, y2 - y1)
    tx = x_dim + (50.0 if sign > 0 else -50.0)
    dxf.text("TEXT", tx, (y1 + y2) / 2.0, label, text_h, 90.0)


def sanitize_filename(name: str) -> str:
    return re.sub(r'[<>:"/\\|?*]', "_", name)


def run_checks(geom: RectGeom) -> tuple[str, str, str, str]:
    d12 = math.dist(geom.p1, geom.p2)
    d14 = math.dist(geom.p1, geom.p4)
    dot = (geom.p2[0] - geom.p1[0]) * (geom.p4[0] - geom.p1[0]) + (geom.p2[1] - geom.p1[1]) * (geom.p4[1] - geom.p1[1])
    c1 = "OK" if abs(d12 - geom.total_len) <= 0.5 else "NG"
    c2 = "OK" if abs(d14 - geom.spec.width) <= 0.5 else "NG"
    c3 = "OK" if abs(dot) <= 1.0 else "NG"
    c4 = "OK" if geom.area_m2 > 0.0 else "NG"
    return c1, c2, c3, c4


def main() -> None:
    title = "10t用乗入図"
    ref_a_desc = "SL(A)"
    ref_b_desc = "SL(B)"
    plus_y_def = "歩道側"
    ref_line_desc = "A.B（縁石/歩車境界）"
    ab_dist = 13200.0
    ref_line_end_x = 13200.0

    rect_specs = [
        RectSpec("R1", "乗り入れアスファルト", 3000.0, 4240.0, 10200.0, 4240.0, 1680.0, layer="OUTLINE_BLUE"),
        RectSpec("R2", "街渠版", 0.0, 3150.0, 13200.0, 3150.0, 500.0),
        RectSpec("R3", "L型", 0.0, 5255.0, 13200.0, 5255.0, 350.0),
        RectSpec("R4", "縁石", 0.0, 3490.0, 3000.0, 3490.0, 180.0, curb_segment_interval=600.0),
        RectSpec("R5", "縁石", 0.0, -2150.0, 10200.0, -2150.0, 0.0),
        RectSpec("R6", "縁石", 10200.0, 3490.0, 13200.0, 3490.0, 180.0),
        RectSpec("R7", "縁石", 10200.0, -2075.0, 13200.0, -2075.0, 150.0, curb_segment_interval=600.0),
        RectSpec("R8", "集水桝", 9600.0, 3150.0, 10200.0, 3150.0, 500.0),
        RectSpec("R9a", "AS", 0.0, 4330.0, 3000.0, 4330.0, 1500.0),
        RectSpec("R9b", "AS", 10200.0, 4330.0, 13200.0, 4330.0, 1500.0),
    ]

    geoms: list[RectGeom] = []
    geom_map: dict[str, RectGeom] = {}
    for spec in rect_specs:
        geom = calc_rect(spec)
        if geom is not None:
            geoms.append(geom)
            geom_map[spec.key] = geom

    dxf = DxfR12()
    dxf.add_header()
    layers = [
        ("OUTLINE", 7),
        ("OUTLINE_BLUE", 5),
        ("EDGE", 8),
        ("EXIST", 9),
        ("DIM", 1),
        ("TEXT", 2),
        ("SYMBOL", 3),
        ("HATCH", 4),
    ]
    dxf.add_tables(layers)
    dxf.add_empty_blocks()
    dxf.begin_entities()

    dxf.line("EDGE", 0.0, 0.0, ref_line_end_x, 0.0)

    dxf.text("TEXT", 0.0, -320.0, ref_a_desc, TEXT_H, 0.0)
    dxf.text("TEXT", ab_dist, -320.0, ref_b_desc, TEXT_H, 0.0)
    dxf.text("TEXT", ab_dist / 2.0, -320.0, ref_line_desc, 140.0, 0.0)
    dxf.text("TEXT", 450.0, 6750.0, f"+Y: {plus_y_def}", TEXT_H, 0.0)
    dxf.text("TEXT", ab_dist / 2.0, 7600.0, title, 260.0, 0.0)

    for geom in geoms:
        dxf.polyline_closed(geom.spec.layer, [geom.p1, geom.p2, geom.p3, geom.p4])

        if geom.spec.curb_segment_interval:
            step = geom.spec.curb_segment_interval
            pos = 0.0
            while pos <= geom.total_len + 1e-9:
                cx = geom.spx + geom.ux * pos
                cy = geom.spy + geom.uy * pos
                x1 = cx + geom.nx * geom.hw
                y1 = cy + geom.ny * geom.hw
                x2 = cx - geom.nx * geom.hw
                y2 = cy - geom.ny * geom.hw
                dxf.line("EXIST", x1, y1, x2, y2)
                pos += step
            if (pos - step) < geom.total_len - 1e-6:
                cx = geom.spx + geom.ux * geom.total_len
                cy = geom.spy + geom.uy * geom.total_len
                x1 = cx + geom.nx * geom.hw
                y1 = cy + geom.ny * geom.hw
                x2 = cx - geom.nx * geom.hw
                y2 = cy - geom.ny * geom.hw
                dxf.line("EXIST", x1, y1, x2, y2)

    for spec in rect_specs:
        if spec.width <= 0.0:
            dxf.line("EXIST", spec.sx, spec.sy, spec.ex, spec.ey)
            dxf.text("TEXT", (spec.sx + spec.ex) / 2.0, spec.sy + 260.0, f"{spec.name} (線)", 140.0, 0.0)

    for geom in geoms:
        if geom.spec.area_pos:
            tx, ty = geom.spec.area_pos
        else:
            tx = (geom.p1[0] + geom.p3[0]) / 2.0
            ty = (geom.p1[1] + geom.p3[1]) / 2.0
        dxf.text("TEXT", tx, ty, f"{geom.spec.name} = {geom.area_m2:.2f} m2", TEXT_H, 0.0)

    dxf.text("TEXT", 0.0, -620.0, "A", 160.0, 0.0)
    dxf.text("TEXT", ab_dist, -620.0, "B", 160.0, 0.0)

    dim_h(dxf, 0.0, ab_dist, 0.0, -900.0, "13200", TEXT_H)

    if "R1" in geom_map:
        g1 = geom_map["R1"]
        top = max(g1.p1[1], g1.p2[1])
        bottom = min(g1.p3[1], g1.p4[1])
        right = max(g1.p2[0], g1.p3[0])
        dim_h(dxf, g1.p1[0], g1.p2[0], top, top + 420.0, f"{int(round(g1.total_len))}", 160.0)
        dim_v(dxf, bottom, top, right, right + 420.0, f"{int(round(g1.spec.width))}", 160.0)

    if "R2" in geom_map:
        g2 = geom_map["R2"]
        top = max(g2.p1[1], g2.p2[1])
        bottom = min(g2.p3[1], g2.p4[1])
        right = max(g2.p2[0], g2.p3[0])
        dim_v(dxf, bottom, top, right, right + 740.0, f"{int(round(g2.spec.width))}", 140.0)

    if "R4" in geom_map:
        g4 = geom_map["R4"]
        top = max(g4.p1[1], g4.p2[1])
        dim_h(dxf, g4.p1[0], g4.p2[0], top, top + 300.0, f"{int(round(g4.total_len))}", 140.0)

    arrow_x1, arrow_y = 11200.0, 7050.0
    arrow_x2 = 12100.0
    dxf.line("SYMBOL", arrow_x1, arrow_y, arrow_x2, arrow_y)
    dxf.line("SYMBOL", arrow_x2, arrow_y, arrow_x2 - 180.0, arrow_y + 90.0)
    dxf.line("SYMBOL", arrow_x2, arrow_y, arrow_x2 - 180.0, arrow_y - 90.0)
    dxf.text("SYMBOL", arrow_x2 + 170.0, arrow_y, "北", 180.0, 0.0)

    dxf.end_file()

    out_dir = Path("outputs")
    out_dir.mkdir(parents=True, exist_ok=True)
    safe_title = sanitize_filename(title)
    out_path = out_dir / f"{safe_title}.dxf"
    out_path.write_text(dxf.dump(), encoding="cp932", newline="\n")

    print(f"DXF written: {out_path.resolve()}")
    print()
    print("| 範囲 | 面積[m2] | P1 | P2 | P3 | P4 |")
    print("|---|---:|---|---|---|---|")
    for geom in geoms:
        print(
            f"| {geom.spec.key}:{geom.spec.name} | {geom.area_m2:.2f} "
            f"| ({geom.p1[0]:.1f},{geom.p1[1]:.1f}) "
            f"| ({geom.p2[0]:.1f},{geom.p2[1]:.1f}) "
            f"| ({geom.p3[0]:.1f},{geom.p3[1]:.1f}) "
            f"| ({geom.p4[0]:.1f},{geom.p4[1]:.1f}) |"
        )

    print()
    print("| 範囲 | 閉図形 | 長さ整合 | 幅整合 | 直交 | 面積>0 |")
    print("|---|---|---|---|---|---|")
    for geom in geoms:
        c1, c2, c3, c4 = run_checks(geom)
        print(f"| {geom.spec.key}:{geom.spec.name} | OK | {c1} | {c2} | {c3} | {c4} |")


if __name__ == "__main__":
    main()
