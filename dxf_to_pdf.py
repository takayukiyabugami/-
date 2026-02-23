"""DXF → PDF 変換スクリプト（日本語フォント対応）

使い方:
    python dxf_to_pdf.py                   # outputs/ 内の全DXFを変換
    python dxf_to_pdf.py path/to/file.dxf  # 指定ファイルのみ変換
"""

import sys
import os
import glob
import matplotlib
import matplotlib.pyplot as plt
import ezdxf
from ezdxf.addons.drawing import RenderContext, Frontend
from ezdxf.addons.drawing.matplotlib import MatplotlibBackend

# ---- ezdxf フォント設定 ----
# support_dirs に Windows フォントディレクトリを追加してezdxfが見つけられるようにする
WIN_FONTS = r"C:\Windows\Fonts"
ezdxf.options.support_dirs = [WIN_FONTS]

OUTPUT_DIR = r"C:\Users\mike1\OneDrive\テスト\outputs"

# 優先使用フォント（ezdxf が PathPatch としてテキストを描くため、ここで指定したフォントが使われる）
JP_FONT_FILE = "YuGothM.ttc"  # Yu Gothic Medium


def _force_black(ax):
    """描画後に全アーティストを黒に統一する"""
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
            if artist.get_facecolor()[3] > 0:  # alpha > 0 なら塗りも黒
                artist.set_facecolor("black")


def convert(dxf_path: str) -> str:
    # UTF-8 を明示指定（省略するとlatin-1で読まれて文字化けする）
    doc = ezdxf.readfile(dxf_path, encoding="utf-8")

    # ezdxf が使うフォントを日本語対応のものに差し替える
    # テキストは PathPatch として描画されるため、ここで指定したフォントが直接使われる
    for style in doc.styles:
        style.dxf.font = JP_FONT_FILE

    msp = doc.modelspace()

    fig = plt.figure(figsize=(297 / 25.4, 210 / 25.4))  # A4 横
    ax = fig.add_axes([0.05, 0.05, 0.90, 0.90])
    fig.patch.set_facecolor("white")
    ax.set_facecolor("white")

    ctx = RenderContext(doc)
    backend = MatplotlibBackend(ax)
    Frontend(ctx, backend).draw_layout(msp, finalize=True)

    _force_black(ax)

    ax.set_aspect("equal")
    ax.axis("off")

    pdf_path = os.path.splitext(dxf_path)[0] + ".pdf"
    fig.savefig(pdf_path, dpi=150, bbox_inches="tight", facecolor="white")
    plt.close(fig)
    return pdf_path


def main():
    if len(sys.argv) > 1:
        targets = sys.argv[1:]
    else:
        targets = glob.glob(os.path.join(OUTPUT_DIR, "*.dxf"))

    if not targets:
        print("変換対象のDXFファイルが見つからない。")
        return

    for dxf in targets:
        print(f"変換中: {dxf}")
        try:
            pdf = convert(dxf)
            print(f"  → {pdf}")
        except Exception as e:
            print(f"  エラー: {e}")


if __name__ == "__main__":
    main()
