import math, os

output_dir = r"C:\Users\mike1\OneDrive\テスト\outputs"
os.makedirs(output_dir, exist_ok=True)

TEXT_H = 200.0

# ===== Geometry =====
def calc_rect(Sx, Sy, Ex, Ey, W, ext_s=0.0, ext_e=0.0):
    vx, vy = Ex - Sx, Ey - Sy
    length = math.sqrt(vx**2 + vy**2)
    ux, uy = vx / length, vy / length
    spx, spy = Sx - ux * ext_s, Sy - uy * ext_s
    epx, epy = Ex + ux * ext_e, Ey + uy * ext_e
    total_len = length + ext_s + ext_e
    nx, ny = -uy, ux
    hw = W / 2.0
    P1 = (spx + nx * hw, spy + ny * hw)
    P2 = (epx + nx * hw, epy + ny * hw)
    P3 = (epx - nx * hw, epy - ny * hw)
    P4 = (spx - nx * hw, spy - ny * hw)
    area_m2 = round(total_len * W / 1_000_000, 2)
    return {
        'S': (Sx, Sy), 'E': (Ex, Ey),
        'Sp': (spx, spy), 'Ep': (epx, epy),
        'P1': P1, 'P2': P2, 'P3': P3, 'P4': P4,
        'total_len': total_len, 'W': W, 'hw': hw,
        'area_m2': area_m2
    }

r1 = calc_rect(3000, 1225, 9060, 1225, 1150)
r2 = calc_rect(3000, 575, 9060, 575, 150)

# ===== DXF Builder =====
L = []

def dxf_line(layer, x1, y1, x2, y2):
    L.extend(["0","LINE","8",layer,
        "10",f"{x1:.6f}","20",f"{y1:.6f}","30","0.0",
        "11",f"{x2:.6f}","21",f"{y2:.6f}","31","0.0"])

def dxf_poly(layer, pts):
    L.extend(["0","POLYLINE","8",layer,"66","1","70","1"])
    for px, py in pts:
        L.extend(["0","VERTEX","8",layer,
            "10",f"{px:.6f}","20",f"{py:.6f}","30","0.0"])
    L.extend(["0","SEQEND","8",layer])

def dxf_text(layer, x, y, h, s, angle=0.0):
    L.extend(["0","TEXT","8",layer,
        "10",f"{x:.6f}","20",f"{y:.6f}","30","0.0",
        "40",f"{h:.1f}","1",s,"50",f"{angle:.1f}",
        "72","1","11",f"{x:.6f}","21",f"{y:.6f}","31","0.0"])

# Tick marks
TD = 42.426407  # 60 * cos45

def tick_h(x, y):
    dxf_line("DIM", x - TD, y - TD, x + TD, y + TD)

def tick_v(x, y):
    dxf_line("DIM", x + TD, y - TD, x - TD, y + TD)

def dim_h(y_base, y_dim, x1, x2, label):
    ov = 150.0
    sgn = 1 if y_dim >= y_base else -1
    dxf_line("DIM", x1, y_base, x1, y_dim + sgn * ov)
    dxf_line("DIM", x2, y_base, x2, y_dim + sgn * ov)
    dxf_line("DIM", x1, y_dim, x2, y_dim)
    tick_h(x1, y_dim)
    tick_h(x2, y_dim)
    dxf_text("TEXT", (x1 + x2) / 2, y_dim + 50, TEXT_H, label, 0.0)

def dim_v(x_base, x_dim, y1, y2, label):
    ov = 150.0
    sgn = 1 if x_dim >= x_base else -1
    dxf_line("DIM", x_base, y1, x_dim + sgn * ov, y1)
    dxf_line("DIM", x_base, y2, x_dim + sgn * ov, y2)
    dxf_line("DIM", x_dim, y1, x_dim, y2)
    tick_v(x_dim, y1)
    tick_v(x_dim, y2)
    tx = x_dim + 50 if x_dim >= x_base else x_dim - 50
    dxf_text("TEXT", tx, (y1 + y2) / 2, TEXT_H, label, 90.0)

# ===== HEADER =====
L.extend(["0","SECTION","2","HEADER",
    "9","$ACADVER","1","AC1009",
    "9","$INSUNITS","70","4",
    "9","$MEASUREMENT","70","1",
    "0","ENDSEC"])

# ===== TABLES =====
L.extend(["0","SECTION","2","TABLES"])

# LTYPE
L.extend(["0","TABLE","2","LTYPE","70","1",
    "0","LTYPE","2","CONTINUOUS","70","0","3","Solid line","72","65","73","0","40","0.0",
    "0","ENDTAB"])

# LAYER
layers = [("OUTLINE",7),("EDGE",8),("EXIST",9),("DIM",1),("TEXT",2),("SYMBOL",3),("HATCH",4)]
L.extend(["0","TABLE","2","LAYER","70",str(len(layers))])
for nm, col in layers:
    L.extend(["0","LAYER","2",nm,"70","0","62",str(col),"6","CONTINUOUS"])
L.extend(["0","ENDTAB"])

# STYLE
L.extend(["0","TABLE","2","STYLE","70","1",
    "0","STYLE","2","STANDARD","70","0","40","0.0","41","1.0",
    "50","0.0","71","0","42","200.0","3","txt","4","",
    "0","ENDTAB"])

L.extend(["0","ENDSEC"])

# ===== BLOCKS =====
L.extend(["0","SECTION","2","BLOCKS","0","ENDSEC"])

# ===== ENTITIES =====
L.extend(["0","SECTION","2","ENTITIES"])

# 1. EDGE: Reference line
dxf_line("EDGE", 0, 0, 12060, 0)

# 2. EDGE: Curb replacement zone on ref line
dxf_line("EDGE", 3000, 0, 9060, 0)

# 3. OUTLINE: R1
dxf_poly("OUTLINE", [r1['P1'], r1['P2'], r1['P3'], r1['P4']])

# 4. OUTLINE: R2
dxf_poly("OUTLINE", [r2['P1'], r2['P2'], r2['P3'], r2['P4']])

# 5. EXIST: Curb outline (75mm at Y=575)
curb_top = 575 + 75 / 2  # 612.5
curb_bot = 575 - 75 / 2  # 537.5
dxf_poly("EXIST", [(3000, curb_top),(9060, curb_top),(9060, curb_bot),(3000, curb_bot)])

# 6. DIM: Curb replacement length at Y=-400
dim_h(0, -400, 3000, 9060, "6060")

# 7. DIM: R1 length at Y=2100
dim_h(1800, 2100, 3000, 9060, "6060")

# 8. DIM: R1 width at X=9360
dim_v(9060, 9360, 1800, 650, "1150")

# 9. DIM: R2 width at X=9660
dim_v(9060, 9660, 650, 500, "150")

# 10. DIM: Offset ref->R2 bottom at X=2700
dim_v(3000, 2700, 0, 500, "500")

# 11. TEXT: Area annotations
area1_label = "舗装 = " + f"{r1['area_m2']:.2f}" + " m2"
area2_label = "縁石復旧 = " + f"{r2['area_m2']:.2f}" + " m2"
title_label = "歩道アスファルト舗装"
dxf_text("TEXT", 6030, 1225, TEXT_H, area1_label, 0.0)
dxf_text("TEXT", 6030, 575, 100, area2_label, 0.0)

# 12. TEXT: Reference labels
dxf_text("TEXT", 0, -250, TEXT_H, "A", 0.0)
dxf_text("TEXT", 12060, -250, TEXT_H, "B", 0.0)

# 13. TEXT: Title
dxf_text("TEXT", 6030, 2500, 240, title_label, 0.0)

# 14. SYMBOL: North arrow
ax, ay_b, ay_t = 11200, 2000, 2400
dxf_line("SYMBOL", ax, ay_b, ax, ay_t)
dxf_line("SYMBOL", ax, ay_t, ax - 80, ay_t - 150)
dxf_line("SYMBOL", ax, ay_t, ax + 80, ay_t - 150)
dxf_text("SYMBOL", ax, ay_t + 120, TEXT_H, "N", 0.0)

L.extend(["0","ENDSEC","0","EOF"])

# ===== Write DXF =====
dxf_content = "\n".join(L)
fp = os.path.join(output_dir, "歩道アスファルト舗装.dxf")
with open(fp, "w", encoding="utf-8") as f:
    f.write(dxf_content)

print(f"DXF written: {fp}")
print(f"Total DXF lines: {len(L)}")
print()

# ===== Verification =====
for name, r in [("R1 (hosoou)", r1), ("R2 (enseki)", r2)]:
    d12 = math.dist(r['P1'], r['P2'])
    d14 = math.dist(r['P1'], r['P4'])
    dot = ((r['P2'][0]-r['P1'][0])*(r['P4'][0]-r['P1'][0]) +
           (r['P2'][1]-r['P1'][1])*(r['P4'][1]-r['P1'][1]))
    print(f"=== {name} ===")
    print(f"  P1={r['P1']} P2={r['P2']} P3={r['P3']} P4={r['P4']}")
    print(f"  |P1-P2|={d12:.3f} == total_len={r['total_len']:.3f} -> {'OK' if abs(d12-r['total_len'])<0.5 else 'NG'}")
    print(f"  |P1-P4|={d14:.3f} == W={r['W']:.3f} -> {'OK' if abs(d14-r['W'])<0.5 else 'NG'}")
    print(f"  dot={dot:.6f} -> {'OK' if abs(dot)<1.0 else 'NG'}")
    print(f"  area={r['area_m2']} m2 -> {'OK' if r['area_m2']>0 else 'NG'}")
