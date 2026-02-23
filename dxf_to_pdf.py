"""DXF -> PDF converter with robust Japanese text rendering.

Usage:
    python dxf_to_pdf.py                   # convert all DXF in outputs/
    python dxf_to_pdf.py path/to/file.dxf  # convert specific DXF
"""

from __future__ import annotations

import argparse
import glob
import math
import os
import re
import sys
from pathlib import Path

import ezdxf
import matplotlib
import matplotlib.pyplot as plt
from ezdxf import bbox, recover
from ezdxf.addons.drawing import Frontend, RenderContext
from ezdxf.addons.drawing.config import Configuration, TextPolicy
from ezdxf.addons.drawing.matplotlib import MatplotlibBackend
from matplotlib import font_manager

WIN_FONTS = Path(r"C:\Windows\Fonts")
OUTPUT_DIR = Path(r"C:\Users\mike1\OneDrive\繝・せ繝・outputs")
JP_FONT_CANDIDATES = (
    WIN_FONTS / "msgothic.ttc",
    WIN_FONTS / "YuGothM.ttc",
    WIN_FONTS / "YuGothR.ttc",
    WIN_FONTS / "meiryo.ttc",
)
PAPER_SIZES_MM: dict[str, tuple[float, float]] = {
    "A4": (297.0, 210.0),
    "A3": (420.0, 297.0),
}

# Let ezdxf font resolver search in Windows font folder.
ezdxf.options.support_dirs = [str(WIN_FONTS)]
MOJIBAKE_HINT_CHARS = set("ﾆ停樞ｦ窶窶｡ﾋ・ｰﾅ窶ｹﾅ椎ｽ窶倪吮懌昶｢窶凪藩懌┐ﾅ｡窶ｺﾅ毒ｾﾅｸ")


def _pick_japanese_font() -> Path | None:
    for candidate in JP_FONT_CANDIDATES:
        if candidate.exists():
            return candidate
    return None


def _jp_score(text: str) -> int:
    score = 0
    for ch in text:
        o = ord(ch)
        if 0x3040 <= o <= 0x30FF:  # Hiragana/Katakana
            score += 2
        elif 0x4E00 <= o <= 0x9FFF:  # CJK
            score += 3
        elif ch == "繝ｼ":
            score += 1
    return score


def _looks_like_mojibake(text: str) -> bool:
    for ch in text:
        o = ord(ch)
        if 0xDC80 <= o <= 0xDCFF:
            return True
        if ch in MOJIBAKE_HINT_CHARS:
            return True
    return False


def _repair_cp1252_to_cp932(text: str) -> str:
    """Reverse mojibake caused by cp1252-decoded Shift_JIS bytes.

    recover.readfile() may return cp1252 text mixed with surrogate escapes.
    This conversion restores original cp932 strings in that case.
    """
    if not text:
        return text
    try:
        candidate = text.encode("cp1252", errors="surrogateescape").decode("cp932", errors="replace")
    except Exception:
        return text
    if _jp_score(candidate) > _jp_score(text):
        return candidate
    return text


def _sanitize_text(text: str) -> str:
    """Remove invalid code points that can break font backends."""
    cleaned: list[str] = []
    for ch in text:
        o = ord(ch)
        if 0xD800 <= o <= 0xDFFF:
            continue
        if o < 0x20 and ch not in ("\t", "\n", "\r"):
            continue
        cleaned.append(ch)
    return "".join(cleaned)


def _safe_console_text(text: str) -> str:
    enc = sys.stdout.encoding or "utf-8"
    return text.encode(enc, errors="backslashreplace").decode(enc, errors="replace")


def _decode_dxf_text(text: str) -> str:
    """Decode mojibake and common DXF control sequences/unicode escapes."""
    if not text:
        return ""
    out = _repair_cp1252_to_cp932(text)
    out = out.replace("%%d", "ﾂｰ").replace("%%p", "ﾂｱ").replace("%%c", "竚")

    def repl(match: re.Match[str]) -> str:
        try:
            return chr(int(match.group(1), 16))
        except ValueError:
            return match.group(0)

    out = re.sub(r"\\U\+([0-9A-Fa-f]{4})", repl, out)
    return _sanitize_text(out)


def _read_dxf_robust(dxf_path: Path):
    """Try normal read first, then recover parser for broken tables/handles."""
    read_attempts = (
        ("cp932", lambda: ezdxf.readfile(str(dxf_path), encoding="cp932")),
        ("utf-8", lambda: ezdxf.readfile(str(dxf_path), encoding="utf-8")),
    )
    for enc_name, attempt in read_attempts:
        try:
            return attempt(), []
        except Exception:
            continue
    doc, auditor = recover.readfile(str(dxf_path))
    msgs = [
        f"recover: errors={len(auditor.errors)} fixes={len(auditor.fixes)}"
    ]
    return doc, msgs


def _force_black(ax) -> None:
    """Unify rendered geometry/text colors to black for print output."""
    import matplotlib.collections as mc
    import matplotlib.lines as ml
    import matplotlib.patches as mp
    import matplotlib.text as mt

    for artist in ax.get_children():
        if isinstance(artist, mt.Text):
            artist.set_color("black")
        elif isinstance(artist, ml.Line2D):
            artist.set_color("black")
        elif isinstance(artist, mc.LineCollection):
            artist.set_color("black")
        elif isinstance(artist, mc.PathCollection):
            artist.set_edgecolor("black")
            artist.set_facecolor("black")
        elif isinstance(artist, mp.Patch):
            artist.set_edgecolor("black")
            if artist.get_facecolor()[3] > 0:
                artist.set_facecolor("black")


def _points_per_drawing_unit(ax) -> float:
    fig = ax.figure
    fig.canvas.draw()
    y0, y1 = ax.get_ylim()
    data_h = abs(y1 - y0)
    if data_h <= 0.0:
        return 1.0
    bbox = ax.get_window_extent()
    axis_h_in = bbox.height / fig.dpi
    return (axis_h_in * 72.0) / data_h


def _segments_from_msp(msp) -> list[tuple[float, float, float, float]]:
    """Extract visible geometry as line segments in drawing units."""
    segs: list[tuple[float, float, float, float]] = []

    def add_poly(points: list[tuple[float, float]], closed: bool) -> None:
        if len(points) < 2:
            return
        for i in range(len(points) - 1):
            x1, y1 = points[i]
            x2, y2 = points[i + 1]
            segs.append((x1, y1, x2, y2))
        if closed:
            x1, y1 = points[-1]
            x2, y2 = points[0]
            segs.append((x1, y1, x2, y2))

    def add_arc(cx: float, cy: float, r: float, a0_deg: float, a1_deg: float) -> None:
        if r <= 0:
            return
        a0 = a0_deg
        a1 = a1_deg
        if a1 <= a0:
            a1 += 360.0
        span = a1 - a0
        n = max(12, int(span / 10.0))
        prev = None
        for i in range(n + 1):
            t = math.radians(a0 + span * i / n)
            x = cx + r * math.cos(t)
            y = cy + r * math.sin(t)
            if prev is not None:
                segs.append((prev[0], prev[1], x, y))
            prev = (x, y)

    for e in msp:
        t = e.dxftype()
        try:
            if t == "LINE":
                s = e.dxf.start
                g = e.dxf.end
                segs.append((float(s.x), float(s.y), float(g.x), float(g.y)))
            elif t == "LWPOLYLINE":
                pts = [(float(x), float(y)) for x, y in e.get_points("xy")]
                add_poly(pts, bool(e.closed))
            elif t == "POLYLINE":
                pts = [(float(v.dxf.location.x), float(v.dxf.location.y)) for v in e.vertices]
                add_poly(pts, bool(e.is_closed))
            elif t == "ARC":
                c = e.dxf.center
                add_arc(
                    float(c.x),
                    float(c.y),
                    float(e.dxf.radius),
                    float(e.dxf.start_angle),
                    float(e.dxf.end_angle),
                )
            elif t == "CIRCLE":
                c = e.dxf.center
                add_arc(float(c.x), float(c.y), float(e.dxf.radius), 0.0, 360.0)
        except Exception:
            continue
    return segs


def _bbox_data(ax, artist) -> tuple[float, float, float, float] | None:
    fig = ax.figure
    fig.canvas.draw()
    renderer = fig.canvas.get_renderer()
    bbox = artist.get_window_extent(renderer=renderer)
    if bbox.width <= 0 or bbox.height <= 0:
        return None
    pts = ax.transData.inverted().transform([[bbox.x0, bbox.y0], [bbox.x1, bbox.y1]])
    x0, y0 = pts[0]
    x1, y1 = pts[1]
    return (min(x0, x1), min(y0, y1), max(x0, x1), max(y0, y1))


def _expand_rect(rect: tuple[float, float, float, float], pad: float) -> tuple[float, float, float, float]:
    x0, y0, x1, y1 = rect
    return (x0 - pad, y0 - pad, x1 + pad, y1 + pad)


def _rect_overlap(a: tuple[float, float, float, float], b: tuple[float, float, float, float]) -> bool:
    ax0, ay0, ax1, ay1 = a
    bx0, by0, bx1, by1 = b
    return (ax0 <= bx1) and (ax1 >= bx0) and (ay0 <= by1) and (ay1 >= by0)


def _point_in_rect(px: float, py: float, rect: tuple[float, float, float, float]) -> bool:
    x0, y0, x1, y1 = rect
    return (x0 <= px <= x1) and (y0 <= py <= y1)


def _orient(ax: float, ay: float, bx: float, by: float, cx: float, cy: float) -> float:
    return (bx - ax) * (cy - ay) - (by - ay) * (cx - ax)


def _on_segment(ax: float, ay: float, bx: float, by: float, px: float, py: float) -> bool:
    eps = 1e-9
    return (
        min(ax, bx) - eps <= px <= max(ax, bx) + eps
        and min(ay, by) - eps <= py <= max(ay, by) + eps
    )


def _segments_intersect(
    a1x: float,
    a1y: float,
    a2x: float,
    a2y: float,
    b1x: float,
    b1y: float,
    b2x: float,
    b2y: float,
) -> bool:
    o1 = _orient(a1x, a1y, a2x, a2y, b1x, b1y)
    o2 = _orient(a1x, a1y, a2x, a2y, b2x, b2y)
    o3 = _orient(b1x, b1y, b2x, b2y, a1x, a1y)
    o4 = _orient(b1x, b1y, b2x, b2y, a2x, a2y)
    eps = 1e-9

    if (o1 > eps and o2 < -eps or o1 < -eps and o2 > eps) and (o3 > eps and o4 < -eps or o3 < -eps and o4 > eps):
        return True
    if abs(o1) <= eps and _on_segment(a1x, a1y, a2x, a2y, b1x, b1y):
        return True
    if abs(o2) <= eps and _on_segment(a1x, a1y, a2x, a2y, b2x, b2y):
        return True
    if abs(o3) <= eps and _on_segment(b1x, b1y, b2x, b2y, a1x, a1y):
        return True
    if abs(o4) <= eps and _on_segment(b1x, b1y, b2x, b2y, a2x, a2y):
        return True
    return False


def _rect_hits_segment(rect: tuple[float, float, float, float], seg: tuple[float, float, float, float]) -> bool:
    x0, y0, x1, y1 = rect
    sx1, sy1, sx2, sy2 = seg
    if _point_in_rect(sx1, sy1, rect) or _point_in_rect(sx2, sy2, rect):
        return True
    edges = (
        (x0, y0, x1, y0),
        (x1, y0, x1, y1),
        (x1, y1, x0, y1),
        (x0, y1, x0, y0),
    )
    for ex1, ey1, ex2, ey2 in edges:
        if _segments_intersect(sx1, sy1, sx2, sy2, ex1, ey1, ex2, ey2):
            return True
    return False


def _hits_any_segment(rect: tuple[float, float, float, float], segments: list[tuple[float, float, float, float]]) -> bool:
    for seg in segments:
        if _rect_hits_segment(rect, seg):
            return True
    return False


def _place_text_with_avoidance(
    ax,
    segments: list[tuple[float, float, float, float]],
    occupied: list[tuple[float, float, float, float]],
    *,
    x: float,
    y: float,
    text: str,
    fontproperties,
    fontsize: float,
    rotation: float,
    ha: str,
    va: str,
    color: str,
    clip_on: bool,
    dxf_height: float,
) -> None:
    step = max(dxf_height * 1.2, 120.0)
    candidates = [
        (0.0, 0.0),
        (0.0, step),
        (step, 0.0),
        (-step, 0.0),
        (0.0, -step),
        (step, step),
        (-step, step),
        (step, -step),
        (-step, -step),
        (0.0, step * 2.0),
        (step * 2.0, 0.0),
        (-step * 2.0, 0.0),
        (0.0, -step * 2.0),
    ]
    pad = max(dxf_height * 0.10, 12.0)

    for ox, oy in candidates:
        artist = ax.text(
            x + ox,
            y + oy,
            text,
            fontproperties=fontproperties,
            fontsize=fontsize,
            rotation=rotation,
            rotation_mode="anchor",
            ha=ha,
            va=va,
            color=color,
            clip_on=clip_on,
        )
        rect = _bbox_data(ax, artist)
        if rect is None:
            occupied.append((x + ox, y + oy, x + ox, y + oy))
            return
        rect2 = _expand_rect(rect, pad)
        if _hits_any_segment(rect2, segments) or any(_rect_overlap(rect2, r) for r in occupied):
            artist.remove()
            continue
        occupied.append(rect2)
        return

    # Fallback: keep original anchor if no clear spot found nearby.
    artist = ax.text(
        x,
        y,
        text,
        fontproperties=fontproperties,
        fontsize=fontsize,
        rotation=rotation,
        rotation_mode="anchor",
        ha=ha,
        va=va,
        color=color,
        clip_on=clip_on,
    )
    rect = _bbox_data(ax, artist)
    if rect is not None:
        occupied.append(_expand_rect(rect, pad))


def _draw_text_entities(ax, msp, jp_font: Path | None) -> None:
    font_prop = font_manager.FontProperties(fname=str(jp_font)) if jp_font else None
    ppu = _points_per_drawing_unit(ax)
    segments = _segments_from_msp(msp)
    occupied: list[tuple[float, float, float, float]] = []

    def to_fontsize(height_dxf: float) -> float:
        # Keep a lower bound for tiny labels.
        return max(height_dxf * ppu, 5.0)

    def align_from_attachment(attachment: int) -> tuple[str, str]:
        # MTEXT attachment: 1..9 (TL,TC,TR,ML,MC,MR,BL,BC,BR)
        table = {
            1: ("left", "top"),
            2: ("center", "top"),
            3: ("right", "top"),
            4: ("left", "center"),
            5: ("center", "center"),
            6: ("right", "center"),
            7: ("left", "bottom"),
            8: ("center", "bottom"),
            9: ("right", "bottom"),
        }
        return table.get(attachment, ("left", "bottom"))

    def align_from_text(entity) -> tuple[str, str]:
        # TEXT halign: 0=left,1=center,2=right,3=aligned,4=middle,5=fit
        # TEXT valign: 0=baseline,1=bottom,2=middle,3=top
        h_map = {0: "left", 1: "center", 2: "right", 4: "center"}
        v_map = {0: "baseline", 1: "bottom", 2: "center", 3: "top"}
        halign = h_map.get(getattr(entity.dxf, "halign", 0), "left")
        valign = v_map.get(getattr(entity.dxf, "valign", 0), "baseline")
        return halign, valign

    for e in msp.query("TEXT"):
        txt = _decode_dxf_text(getattr(e.dxf, "text", ""))
        if not txt:
            continue
        ins = e.dxf.insert
        has_align = (getattr(e.dxf, "halign", 0) != 0) or (getattr(e.dxf, "valign", 0) != 0)
        if has_align and e.dxf.hasattr("align_point"):
            ins = e.dxf.align_point
        rot = float(getattr(e.dxf, "rotation", 0.0))
        h = float(getattr(e.dxf, "height", 200.0))
        ha, va = align_from_text(e)
        _place_text_with_avoidance(
            ax,
            segments,
            occupied,
            x=ins.x,
            y=ins.y,
            text=txt,
            fontproperties=font_prop,
            fontsize=to_fontsize(h),
            rotation=rot,
            ha=ha,
            va=va,
            color="black",
            clip_on=True,
            dxf_height=h,
        )

    for e in msp.query("MTEXT"):
        txt = _decode_dxf_text(e.plain_text())
        if not txt:
            continue
        ins = e.dxf.insert
        rot = 0.0
        if e.dxf.hasattr("rotation"):
            rot = float(e.dxf.rotation)
        h = float(getattr(e.dxf, "char_height", 200.0))
        ha, va = align_from_attachment(int(getattr(e.dxf, "attachment_point", 7)))
        _place_text_with_avoidance(
            ax,
            segments,
            occupied,
            x=ins.x,
            y=ins.y,
            text=txt,
            fontproperties=font_prop,
            fontsize=to_fontsize(h),
            rotation=rot,
            ha=ha,
            va=va,
            color="black",
            clip_on=True,
            dxf_height=h,
        )


def _apply_fixed_scale_view(
    ax,
    msp,
    scale_den: float,
    paper_size: str,
    margin_mm: float,
) -> bool:
    if scale_den <= 0:
        return False
    if paper_size not in PAPER_SIZES_MM:
        return False
    paper_w, paper_h = PAPER_SIZES_MM[paper_size]
    draw_w = paper_w - 2.0 * margin_mm
    draw_h = paper_h - 2.0 * margin_mm
    if draw_w <= 0 or draw_h <= 0:
        return False
    try:
        ext = bbox.extents(msp)
    except Exception:
        return False
    cx = (ext.extmin.x + ext.extmax.x) / 2.0
    cy = (ext.extmin.y + ext.extmax.y) / 2.0
    view_w = draw_w * scale_den
    view_h = draw_h * scale_den
    ax.set_xlim(cx - view_w / 2.0, cx + view_w / 2.0)
    ax.set_ylim(cy - view_h / 2.0, cy + view_h / 2.0)
    return True


def convert(
    dxf_path: str,
    scale_den: float | None = None,
    paper_size: str = "A4",
    margin_mm: float = 10.0,
) -> str:
    dxf_file = Path(dxf_path)
    jp_font = _pick_japanese_font()
    if jp_font is not None:
        fp = font_manager.FontProperties(fname=str(jp_font))
        matplotlib.rcParams["font.family"] = "sans-serif"
        matplotlib.rcParams["font.sans-serif"] = [
            fp.get_name(),
            "Yu Gothic",
            "MS Gothic",
            "DejaVu Sans",
        ]

    doc, notes = _read_dxf_robust(dxf_file)
    for note in notes:
        print(f"  豕ｨ諢・ {note}")

    # Force style font for any internal text-shape processing.
    if jp_font is not None:
        for style in doc.styles:
            try:
                style.dxf.font = jp_font.name
            except Exception:
                pass

    msp = doc.modelspace()
    paper_key = paper_size.upper()
    paper_w, paper_h = PAPER_SIZES_MM.get(paper_key, PAPER_SIZES_MM["A4"])
    # Clamp margin to keep a drawable area.
    mm = max(0.0, min(margin_mm, min(paper_w, paper_h) * 0.45))
    left = mm / paper_w
    bottom = mm / paper_h
    width = max(0.01, 1.0 - 2.0 * left)
    height = max(0.01, 1.0 - 2.0 * bottom)

    fig = plt.figure(figsize=(paper_w / 25.4, paper_h / 25.4))
    ax = fig.add_axes([left, bottom, width, height])
    fig.patch.set_facecolor("white")
    ax.set_facecolor("white")

    cfg = Configuration().with_changes(text_policy=TextPolicy.IGNORE)
    ctx = RenderContext(doc)
    backend = MatplotlibBackend(ax)
    Frontend(ctx, backend, config=cfg).draw_layout(msp, finalize=True)

    if scale_den is not None:
        _apply_fixed_scale_view(ax, msp, scale_den, paper_key, mm)

    if scale_den is None:
        ax.set_aspect("equal")
    else:
        ax.set_aspect("equal", adjustable="box")
    _draw_text_entities(ax, msp, jp_font)
    _force_black(ax)
    ax.axis("off")

    if scale_den is None:
        pdf_path = dxf_file.with_suffix(".pdf")
    else:
        scale_tag = f"{int(scale_den)}" if float(scale_den).is_integer() else str(scale_den).replace(".", "p")
        pdf_path = dxf_file.with_name(f"{dxf_file.stem}_S{scale_tag}.pdf")

    try:
        if scale_den is None:
            fig.savefig(str(pdf_path), dpi=180, bbox_inches="tight", facecolor="white")
        else:
            fig.savefig(str(pdf_path), dpi=180, facecolor="white")
    except PermissionError:
        pdf_path = dxf_file.with_name(dxf_file.stem + "_new.pdf")
        if scale_den is None:
            fig.savefig(str(pdf_path), dpi=180, bbox_inches="tight", facecolor="white")
        else:
            fig.savefig(str(pdf_path), dpi=180, facecolor="white")
    plt.close(fig)
    return str(pdf_path)


def main():
    parser = argparse.ArgumentParser(description="Convert DXF to PDF.")
    parser.add_argument("targets", nargs="*", help="DXF files to convert")
    parser.add_argument(
        "--scale",
        type=float,
        default=None,
        help="Drawing scale denominator, e.g. 100 for 1/100",
    )
    parser.add_argument(
        "--paper",
        choices=sorted(PAPER_SIZES_MM.keys()),
        default="A4",
        help="Paper size in landscape orientation",
    )
    parser.add_argument(
        "--margin-mm",
        type=float,
        default=10.0,
        help="Page margin in mm",
    )
    args = parser.parse_args()

    if args.targets:
        targets = args.targets
    else:
        targets = glob.glob(str(OUTPUT_DIR / "*.dxf"))

    if not targets:
        print("No DXF files found.")
        return

    for dxf in targets:
        print(_safe_console_text(f"Converting: {dxf}"))
        try:
            pdf = convert(
                dxf,
                scale_den=args.scale,
                paper_size=args.paper,
                margin_mm=args.margin_mm,
            )
            print(_safe_console_text(f"  OK: {pdf}"))
        except Exception as e:
            print(_safe_console_text(f"  ERROR: {e}"))


if __name__ == "__main__":
    main()
