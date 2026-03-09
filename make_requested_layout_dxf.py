from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path


TEXT_H = 140.0


@dataclass
class RectItem:
    no: int
    name: str
    sx: float
    sy: float
    px: float
    py: float
    slice_step: float | None = None


@dataclass
class CircleItem:
    no: int
    name: str
    diameter: float
    px: float
    py: float


class DxfR12:
    def __init__(self) -> None:
        self.lines: list[str] = []

    def _push(self, *items: object) -> None:
        for item in items:
            self.lines.append(str(item))

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
        for layer_name, color in layers:
            self._push(
                "0",
                "LAYER",
                "2",
                layer_name,
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
        for x, y in points:
            self._push(
                "0",
                "VERTEX",
                "8",
                layer,
                "10",
                self._f(x),
                "20",
                self._f(y),
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
        return "\n".join(self.lines) + "\n"


def rect_points(px: float, py: float, sx: float, sy: float) -> list[tuple[float, float]]:
    hx = sx / 2.0
    hy = sy / 2.0
    return [
        (px - hx, py + hy),
        (px + hx, py + hy),
        (px + hx, py - hy),
        (px - hx, py - hy),
    ]


def add_rect(dxf: DxfR12, item: RectItem) -> list[tuple[float, float]]:
    points = rect_points(item.px, item.py, item.sx, item.sy)
    dxf.polyline_closed("STRUCT", points)

    if item.slice_step and item.slice_step > 0:
        x_left = item.px - item.sx / 2.0
        x_right = item.px + item.sx / 2.0
        y_top = item.py + item.sy / 2.0
        y_bottom = item.py - item.sy / 2.0
        x = x_left + item.slice_step
        while x < x_right - 1e-9:
            dxf.line("MARK", x, y_top, x, y_bottom)
            x += item.slice_step

    dxf.text("TEXT", item.px, item.py + item.sy / 2.0 + 110.0, f"{item.no}. {item.name}", 90.0, 0.0)
    return points


def add_circle(dxf: DxfR12, item: CircleItem) -> list[tuple[float, float]]:
    radius = item.diameter / 2.0
    dxf.circle("STRUCT", item.px, item.py, radius)
    dxf.text("TEXT", item.px, item.py + radius + 110.0, f"{item.no}. {item.name}", 90.0, 0.0)
    return [
        (item.px - radius, item.py - radius),
        (item.px + radius, item.py + radius),
    ]


def main() -> None:
    point_a = (0.0, 0.0)
    point_b = (10360.0, 0.0)

    rect_items = [
        RectItem(1, "NTT 電気弁", 1240.0, 640.0, 8930.0, 8400.0),
        RectItem(4, "電気箱", 1550.0, 500.0, 2235.0, 5910.0),
        RectItem(5, "枡", 620.0, 310.0, 1860.0, 4965.0),
        RectItem(6, "As", 8000.0, 3700.0, 6360.0, 7210.0),
        RectItem(7, "エプロン", 8000.0, 180.0, 6360.0, 5270.0, slice_step=2000.0),
        RectItem(8, "街渠", 9360.0, 500.0, 5680.0, 4930.0),
        RectItem(9, "エプロン", 1000.0, 180.0, 1860.0, 5270.0),
        RectItem(10, "鯨", 1360.0, 180.0, 680.0, 5270.0),
        RectItem(11, "防止柵", 2900.0, 110.0, 1890.0, 5495.0),
        RectItem(12, "縦エプロン", 8000.0, 200.0, 6360.0, -1900.0, slice_step=2000.0),
        RectItem(13, "縦エプロン", 2060.0, 200.0, 1330.0, -1900.0),
        RectItem(15, "エプロン穴", 320.0, 120.0, 7360.0, -2040.0),
        RectItem(16, "防止柵", 2900.0, 790.0, 3290.0, -1605.0),
        RectItem(17, "箱", 1550.0, 1230.0, 1275.0, -1385.0),
        RectItem(18, "木", 1840.0, 1240.0, 5680.0, 5980.0),
    ]

    circle_items = [
        CircleItem(2, "マンホール", 700.0, 3310.0, 6950.0),
        CircleItem(3, "止水栓", 160.0, 7060.0, 8440.0),
    ]

    whale_poly_offset = (0.0, -2000.0)
    whale_poly_rel = [(0.0, 0.0), (300.0, 0.0), (300.0, 200.0), (0.0, 50.0)]
    whale_poly_abs = [(x + whale_poly_offset[0], y + whale_poly_offset[1]) for x, y in whale_poly_rel]

    dxf = DxfR12()
    dxf.add_header()
    dxf.add_tables(
        [
            ("BASE", 8),
            ("STRUCT", 7),
            ("MARK", 1),
            ("TEXT", 2),
        ]
    )
    dxf.add_empty_blocks()
    dxf.begin_entities()

    all_points: list[tuple[float, float]] = [point_a, point_b]

    dxf.line("BASE", point_a[0], point_a[1], point_b[0], point_b[1])
    dxf.text("TEXT", point_a[0], point_a[1] - 220.0, "A(0,0)", 100.0, 0.0)
    dxf.text("TEXT", point_b[0], point_b[1] - 220.0, "B(10360,0)", 100.0, 0.0)

    for item in rect_items:
        all_points.extend(add_rect(dxf, item))

    for item in circle_items:
        all_points.extend(add_circle(dxf, item))

    dxf.polyline_closed("STRUCT", whale_poly_abs)
    center_x = sum(p[0] for p in whale_poly_abs) / len(whale_poly_abs)
    center_y = sum(p[1] for p in whale_poly_abs) / len(whale_poly_abs)
    dxf.text("TEXT", center_x, center_y + 160.0, "14. 鯨", 90.0, 0.0)
    all_points.extend(whale_poly_abs)

    min_x = min(p[0] for p in all_points)
    max_x = max(p[0] for p in all_points)
    max_y = max(p[1] for p in all_points)
    title_x = (min_x + max_x) / 2.0
    dxf.text("TEXT", title_x, max_y + 500.0, "指定配置図", 220.0, 0.0)
    dxf.text("TEXT", title_x, max_y + 250.0, "基準 AB: A(0,0) B(10360,0)", 120.0, 0.0)

    dxf.end_file()

    out_dir = Path("outputs")
    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / "requested_layout_ab_10360.dxf"
    out_path.write_text(dxf.dump(), encoding="cp932", newline="\n")

    print(f"DXF written: {out_path.resolve()}")
    print("Items: 18")


if __name__ == "__main__":
    main()
