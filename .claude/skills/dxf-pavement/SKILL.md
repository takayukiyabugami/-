---
name: dxf-pavement
description: 矩形の舗装復旧範囲をDXF R12 ASCIIで自動生成する（中心線方式）
---

# DXF舗装復旧図 自動生成スキル

## 概要

土木工事における矩形の舗装復旧範囲を中心線方式で計算し、DXF R12 ASCII形式で出力する。
出力は「閉ポリライン＋寸法線＋寸法値＋面積注記＋座標表＋チェック」を必ず含む。

- 単位：mm（面積のみm²）
- 座標系：右=+X、上=+Y
- 角度：度数法（0°=+X、反時計回り+）
- 許容誤差：±0.5mm
- DXF版：R12（AC1009）

## 入力テンプレート

スキル呼び出し時、ユーザーに以下のテンプレートを提示し、値を埋めてもらう。
ユーザーが既に値を与えている場合はそのまま使う。

```
---PAVEMENT-INPUT---
title: （図面タイトル）
# 基準
ref_A_desc: （A点の説明 例：既設桝中心）
ref_B_desc: （B点の説明 例：既設桝中心）
A_to_B_dist: （A→B距離L [mm]。B=(L,0)として扱う）
plus_Y_def: （+Yの定義 例：歩道側）
ref_line: （基準線の説明 例：縁石天端/歩車境界）
ref_line_end_X: （基準線の表示終端X [mm]。X=0〜この値）

# 縁石据替（なければ省略）
curb_start_X: （据替開始X [mm]）
curb_end_X: （据替終了X [mm]）

# 復旧範囲R1（必須）
R1_name: （名称 例：表層AS）
CL1_S: （中心線始点 Xs,Ys [mm]）
CL1_E: （中心線終点 Xe,Ye [mm]）
W1: （幅 [mm]）
EXT_S1: （始点側延長 [mm]。0なら省略可）
EXT_E1: （終点側延長 [mm]。0なら省略可）
R1_area_text_pos: （面積注記位置 X,Y [mm]。省略なら自動配置）

# 復旧範囲R2（あれば。路盤・基層等）
R2_name:
CL2_S:
CL2_E:
W2:
EXT_S2:
EXT_E2:
R2_area_text_pos:

# 欠き取り（あれば）
CLEAR: （逃げ [mm]。省略なら0）
# 角形桝（複数可）
SQ1_center: （中心X,Y [mm]）
SQ1_size: （W×H [mm] 例：300×300）
# 円形MH（複数可）
MH1_center: （中心X,Y [mm]）
MH1_R: （半径 [mm]。またはφで直径指定）

# 表示
text_h: 200（変更するなら値）
north_arrow: 有/無
---END-INPUT---
```

## Step 1: 入力解析・検証

1. `---PAVEMENT-INPUT---` と `---END-INPUT---` の間を抽出する。テンプレート外に値がある場合もそれを使う。
2. 必須項目チェック：title, A_to_B_dist, CL1_S, CL1_E, W1 が無ければユーザーに確認する。
3. デフォルト値：
   - EXT_S = 0, EXT_E = 0（省略時）
   - text_h = 200（省略時）
   - CLEAR = 0（省略時）
   - ref_line_end_X = A_to_B_dist + 2000（省略時）
4. 基準点座標：A = (0, 0)、B = (A_to_B_dist, 0)

## Step 2: 座標計算

全ての中間値を小数6桁以上で保持する。各復旧範囲Rn について以下を実行する。

### 2.1 中心線ベクトル
```
v = E - S = (Ex - Sx, Ey - Sy)
len_v = sqrt(vx² + vy²)
u = (vx/len_v, vy/len_v)    ← 単位ベクトル（中心線方向）
```

### 2.2 延長端点
```
S' = (Sx - ux*EXT_S, Sy - uy*EXT_S)
E' = (Ex + ux*EXT_E, Ey + uy*EXT_E)
total_len = len_v + EXT_S + EXT_E
```

### 2.3 法線ベクトル
```
n = (-uy, ux)    ← 左法線（90°反時計回り）
```

### 2.4 外形4点（時計回り）
```
hw = W / 2
P1 = (S'x + nx*hw, S'y + ny*hw)    ← 始点左
P2 = (E'x + nx*hw, E'y + ny*hw)    ← 終点左
P3 = (E'x - nx*hw, E'y - ny*hw)    ← 終点右
P4 = (S'x - nx*hw, S'y - ny*hw)    ← 始点右
```

### 2.5 面積
```
area_mm2 = total_len × W
area_m2 = area_mm2 / 1,000,000   ← 小数2桁に丸める
```

### 2.6 検証（必須）
以下を全て確認する。失敗したら2.1からやり直す。
- |P1-P2| = total_len（±0.5mm）
- |P1-P4| = W（±0.5mm）
- (P2-P1)・(P4-P1) = 0（直交チェック、±1.0以内）
- area_m2 > 0

## Step 3: 寸法線計算

寸法はDIMレイヤにLINE、寸法値はTEXTレイヤにTEXTで表現する。DXF DIMENSIONエンティティは使わない。

### 寸法構成要素
各寸法セットは以下4種のエンティティで構成する：
- **補助線**（DIMレイヤ LINE）：対象点から寸法線まで＋寸法線を越えて150mm延長
- **寸法線**（DIMレイヤ LINE）：対象辺からオフセット位置に描画
- **ティック線**（DIMレイヤ LINE）：寸法線両端に45°線、長さ120mm
- **寸法値**（TEXTレイヤ TEXT）：寸法線中央に配置

### オフセット距離
- 第1段：対象辺から300mm
- 第2段：600mm
- 第3段：900mm

### ティック線の座標計算
寸法線の方向ベクトルを d とする。ティック半長 = 60mm。
```
tick_dir_x = (dx*cos45 - dy*sin45)  ← cos45 = sin45 = 0.707107
tick_dir_y = (dx*sin45 + dy*cos45)
tick_unit = tick_dir / |tick_dir|

端点Tにおけるティック線：
  tick_start = (Tx - tick_unit_x*60, Ty - tick_unit_y*60)
  tick_end   = (Tx + tick_unit_x*60, Ty + tick_unit_y*60)
```

### 寸法の優先順位と配置
1. **縁石据替延長**（あれば）：基準線Y=0上方 第1段、水平
2. **復旧延長**（S'〜E'間）：外形上辺から上方 第1段（縁石なし時）or 第2段、水平
3. **復旧幅W**：外形右辺から右方 第1段、垂直
4. **オフセット**（基準線から復旧範囲までの距離）：必要に応じて第2〜3段
5. **欠き取り寸法**：必要時のみ

### 寸法値TEXT配置
- 水平寸法：寸法線中央の上側（+50mm）、角度0°
- 垂直寸法：寸法線中央の右側（+50mm）、角度90°
- 値：mm単位、整数（面積のみm²で小数2桁）

## レイヤ定義

| レイヤ | ACI色番号 | 用途 |
|--------|-----------|------|
| OUTLINE | 7 | 復旧外形 閉ポリライン |
| EDGE | 8 | 基準線（Y=0） |
| EXIST | 9 | 現況線・参考線 |
| DIM | 1 | 寸法線・補助線・ティック |
| TEXT | 2 | 寸法値・面積注記 |
| SYMBOL | 3 | 桝・MH記号 |
| HATCH | 4 | ハッチ（任意） |

## DXFエンティティテンプレート

以下のテンプレートにそのまま座標値を代入して使う。各行に空白やインデントを入れないこと。

### LINE
```
0
LINE
8
{layer}
10
{start_x:.6f}
20
{start_y:.6f}
30
0.0
11
{end_x:.6f}
21
{end_y:.6f}
31
0.0
```

### 閉ポリライン（POLYLINE + VERTEX + SEQEND）
```
0
POLYLINE
8
{layer}
66
1
70
1
0
VERTEX
8
{layer}
10
{P1x:.6f}
20
{P1y:.6f}
30
0.0
0
VERTEX
8
{layer}
10
{P2x:.6f}
20
{P2y:.6f}
30
0.0
0
VERTEX
8
{layer}
10
{P3x:.6f}
20
{P3y:.6f}
30
0.0
0
VERTEX
8
{layer}
10
{P4x:.6f}
20
{P4y:.6f}
30
0.0
0
SEQEND
8
{layer}
```

### CIRCLE
```
0
CIRCLE
8
{layer}
10
{center_x:.6f}
20
{center_y:.6f}
30
0.0
40
{radius:.6f}
```

### TEXT
```
0
TEXT
8
{layer}
10
{insert_x:.6f}
20
{insert_y:.6f}
30
0.0
40
{height:.1f}
1
{text_string}
50
{rotation:.1f}
72
1
11
{align_x:.6f}
21
{align_y:.6f}
31
0.0
```
- 72=1 は水平中央揃え。11/21/31 が実際の配置点になる。

## Step 4: DXFファイル組立

以下の順でDXFファイル全体を構成する。

### 4.1 HEADER
```
0
SECTION
2
HEADER
9
$ACADVER
1
AC1009
9
$INSUNITS
70
4
9
$MEASUREMENT
70
1
0
ENDSEC
```

### 4.2 TABLES

#### LTYPE テーブル
```
0
SECTION
2
TABLES
0
TABLE
2
LTYPE
70
1
0
LTYPE
2
CONTINUOUS
70
0
3
Solid line
72
65
73
0
40
0.0
0
ENDTAB
```

#### LAYER テーブル
レイヤ定義表の全7レイヤを登録する。各レイヤのエントリ：
```
0
LAYER
2
{layer_name}
70
0
62
{color_number}
6
CONTINUOUS
```
LAYERテーブル全体を `0 TABLE / 2 LAYER / 70 7` で開始し、全レイヤ追加後 `0 ENDTAB` で閉じる。

#### STYLE テーブル
```
0
TABLE
2
STYLE
70
1
0
STYLE
2
STANDARD
70
0
40
0.0
41
1.0
50
0.0
71
0
42
200.0
3
txt
4

0
ENDTAB
0
ENDSEC
```

### 4.3 BLOCKS（空）
```
0
SECTION
2
BLOCKS
0
ENDSEC
```

### 4.4 ENTITIES
```
0
SECTION
2
ENTITIES
```
以下の順でエンティティを出力する：
1. **EDGE**：基準線 LINE（(0,0) → (ref_line_end_X, 0)）
2. **OUTLINE**：復旧外形R1 閉ポリライン（P1→P2→P3→P4）。R2があればそれも。
3. **EXIST**：現況線（入力があれば）
4. **DIM**：全寸法線・補助線・ティック線
5. **TEXT**：全寸法値TEXT、面積注記TEXT（「R1_name = area_m2 ㎡」形式）
6. **SYMBOL**：欠き取り形状（角形ポリライン or CIRCLE）
7. **HATCH**：ハッチ（任意、なくても可）

最後：
```
0
ENDSEC
```

### 4.5 EOF
```
0
EOF
```

## Step 5: 出力手順

以下の順番を厳守する。

### 1) DXFファイル書き出し
- ファイルパス：`outputs/{title}.dxf`（titleから使えない文字は除去）
- Writeツールで書き出す
- 各グループコードと値は1行ずつ、先頭空白なし

### 2) 座標表
マークダウン表で表示する：
```
| 点名 | X (mm) | Y (mm) |
|------|--------|--------|
| A    | 0.000  | 0.000  |
| B    | ...    | 0.000  |
| S    | ...    | ...    |
| E    | ...    | ...    |
| S'   | ...    | ...    |
| E'   | ...    | ...    |
| P1   | ...    | ...    |
| P2   | ...    | ...    |
| P3   | ...    | ...    |
| P4   | ...    | ...    |
```
欠き取りがあればその主要点も追加。

### 3) チェック（短文）
- 閉図形確認：P1→P2→P3→P4→P1 が閉じているか → OK/NG
- 面積検証：total_len × W / 1,000,000 = area_m2 → OK/NG
- 寸法整合：W = 2 × hw → OK/NG
- 直交チェック → OK/NG

## 縁石据替（任意拡張）

入力に curb_start_X, curb_end_X がある場合：
- EDGEレイヤに基準線上の据替区間を追加（LINE: (curb_start_X, 0) → (curb_end_X, 0)）
- 据替延長 = curb_end_X - curb_start_X
- DIM+TEXTで据替延長の寸法を追加（第1段、Y=0 の上方300mm）

## 欠き取り（任意拡張）

### 角形桝
- 中心座標と外形寸法(W×H)から4頂点を計算
- CLEARがある場合：外形に2×CLEARを加算した「欠き取り境界」を別ポリラインで描く
- SYMBOLレイヤに閉ポリライン

### 円形MH
- 中心座標と半径Rから CIRCLE を描く（φ指定なら R = φ/2）
- CLEARがある場合：R + CLEAR の「欠き取り境界」を別CIRCLEで描く
- SYMBOLレイヤに配置

### 欠き取り寸法
- 欠き取り中心から最寄りの復旧外形辺までの距離を寸法として追加（必要時）

## 注意事項

- Markdown、コードブロック、説明文はDXF本文に含めない。座標表とチェックはDXFファイルの外に出力する。
- DXF内の全座標は小数6桁（例：1500.000000）で出力する。
- 各エンティティのグループコード行と値行はそれぞれ独立した行にする（同一行に複数のグループコードを入れない）。
- 複数の復旧範囲（R1, R2…）がある場合、それぞれについてStep 2〜5を繰り返す。全てのOUTLINEポリラインは同じENTITIESセクション内に出力する。
