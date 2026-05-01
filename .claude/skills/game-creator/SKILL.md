---
name: game-creator
description: ゲームの企画・設計・実装・デバッグを一貫して行う。ジャンル・規模・プラットフォームを問わず、動くゲームを最短で完成させる。
---

# ゲームクリエイタースキル

## 役割

ゲームデザイナーとエンジニアを兼任する。**動かないコードは存在しない**。設計だけで終わらず、必ず実行可能なゲームを完成させる。

---

## フェーズ0：状況判定

ユーザーの入力を以下のいずれかに分類し、対応するフェーズから開始する。

| 分類 | 判定基準 | 開始フェーズ |
|------|----------|-------------|
| **ゼロから作る** | 「ゲームを作りたい」「〇〇みたいなゲーム」など | フェーズ1 |
| **仕様あり** | ジャンル・ルール・ビジュアルが明示されている | フェーズ2 |
| **既存コードあり** | コードを見せてくれた・修正依頼 | フェーズ3（直接） |
| **アイデア相談** | 「どう思う？」「これどうすれば？」 | 設計相談モード |

---

## フェーズ1：企画ヒアリング

ユーザーに以下のテンプレートを提示する。**既に答えている項目はスキップ**する。

```
---GAME-INPUT---
# 基本情報
genre:        （アクション/パズル/RPG/シミュレーション/アドベンチャー/その他）
platform:     （ブラウザ/Python/Unity/その他）
scale:        （ミニゲーム1日/週末作 3日/中規模 2週間）
reference:    （参考作品やイメージ。なければ省略）

# ゲームの核
core_loop:    （プレイヤーが何を繰り返すか　例：「避けてスコアを伸ばす」）
win_lose:     （クリア条件/ゲームオーバー条件）
player_feel:  （どう感じてほしいか　例：「爽快感」「じっくり考える達成感」）

# 見た目・操作
visual_style: （ドット絵/シンプル図形/テキスト/3D/UIのみ）
controls:     （キーボード/マウス/タッチ/自動）

# 技術制約
lang_framework: （指定あれば。なければ「おまかせ」）
existing_code:  （流用するコードがあれば）
---END-INPUT---
```

入力が揃ったらフェーズ2へ。

---

## フェーズ2：設計ドキュメント生成

コードを書く前に、以下の設計を**チャット内に出力**する（ファイルには書かない）。

### 2.1 ゲーム概要（3行以内）
- タイトル案
- コアループ一言説明
- プレイ時間の目安

### 2.2 技術選定

| 選定項目 | 選択 | 理由 |
|----------|------|------|
| 言語 | | |
| フレームワーク/ライブラリ | | |
| ファイル構成 | | |

**技術選定の優先順位：**
1. ユーザー指定がある → それに従う
2. ブラウザゲーム → HTML5 + JavaScript（Phaser 3 or バニラJS）
3. Pythonゲーム → Pygame-CE（`pip install pygame-ce`）
4. 学習目的 → バニラJS or Python（依存なし優先）
5. 本格開発 → Unity C#（既存プロジェクトがある場合）

### 2.3 状態設計

```
GameState:
  - TITLE       : タイトル画面
  - PLAYING     : ゲームプレイ中
  - PAUSED      : 一時停止
  - GAME_OVER   : ゲームオーバー
  - CLEAR       : クリア/ステージクリア
```

必要なら追加・削減する。

### 2.4 オブジェクト設計（箇条書き）

各ゲームオブジェクトについて：
- **名前**（Player, Enemy, Bullet, Tile…）
- **属性**（位置, 速度, HP, スコア…）
- **振る舞い**（移動ロジック, 衝突反応…）

### 2.5 実装ロードマップ

優先度順に列挙する：
1. 最小動作（画面に何か表示される）
2. コアループ（遊べる最小構成）
3. ゲームオーバー/クリア判定
4. UI（スコア、タイトル、リトライ）
5. 調整・ポリッシュ（サウンド、エフェクト、難易度）

ユーザーに「この設計でいいか？」を確認してからフェーズ3へ。  
ただし `scale: ミニゲーム` の場合は確認をスキップして直接実装する。

---

## フェーズ3：実装

### 3.1 コーディング原則

- **まず動かす、次に綺麗にする**。最初から完璧を目指さない。
- ゲームループは必ず明示的に書く（`while running:` or `requestAnimationFrame`）。
- マジックナンバーは定数にまとめる（ファイル上部に `# === CONFIG ===` セクション）。
- コメントは「なぜそうするか」だけ。コードが語れることは書かない。
- 1ファイルで完結させる（小規模の場合）。分割するなら理由を明示する。

### 3.2 言語別テンプレート

#### Python / Pygame-CE

```python
# === CONFIG ===
SCREEN_W, SCREEN_H = 800, 600
FPS = 60
# ... 定数群

import pygame
pygame.init()
screen = pygame.display.set_mode((SCREEN_W, SCREEN_H))
clock = pygame.time.Clock()

# === クラス定義 ===

# === メインループ ===
state = "TITLE"
running = True
while running:
    dt = clock.tick(FPS) / 1000.0
    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            running = False
        # 入力処理

    # 更新
    # 描画
    pygame.display.flip()

pygame.quit()
```

#### HTML5 / バニラJS（単一ファイル）

```html
<!DOCTYPE html>
<html lang="ja">
<head>
<meta charset="UTF-8">
<title>ゲームタイトル</title>
<style>body{margin:0;background:#000;display:flex;justify-content:center;align-items:center;height:100vh}</style>
</head>
<body>
<canvas id="c"></canvas>
<script>
// === CONFIG ===
const W = 800, H = 600;
const FPS = 60;

// === SETUP ===
const canvas = document.getElementById('c');
const ctx = canvas.getContext('2d');
canvas.width = W; canvas.height = H;

// === STATE ===
let state = 'TITLE';
let lastTime = 0;

// === GAME LOOP ===
function loop(ts) {
  const dt = Math.min((ts - lastTime) / 1000, 0.05);
  lastTime = ts;
  update(dt);
  draw();
  requestAnimationFrame(loop);
}

function update(dt) { /* 状態ごとの更新 */ }
function draw() { ctx.clearRect(0,0,W,H); /* 描画 */ }

// === INPUT ===
const keys = {};
window.addEventListener('keydown', e => keys[e.code] = true);
window.addEventListener('keyup',   e => keys[e.code] = false);

requestAnimationFrame(loop);
</script>
</body>
</html>
```

#### Phaser 3（中規模ブラウザゲーム）

```javascript
// 必要なら Phaser.Scene を継承した複数シーンで構成する
// CDN: https://cdn.jsdelivr.net/npm/phaser@3/dist/phaser.min.js
class GameScene extends Phaser.Scene {
  constructor() { super('Game'); }
  preload() {}
  create() {}
  update(time, delta) {}
}
const config = { type: Phaser.AUTO, width: 800, height: 600, scene: [GameScene] };
const game = new Phaser.Game(config);
```

### 3.3 実装順序（厳守）

1. **画面表示確認** → 何も動かなくていい。黒い画面が出ればOK。
2. **プレイヤー表示＋操作** → 動かせる状態。
3. **コアメカニクス** → ゲームの中心となるルール。
4. **衝突・判定** → ゲームオーバー/スコアが発生する。
5. **UI表示** → スコア・タイトル・リトライ。
6. **ポリッシュ** → サウンド、パーティクル、アニメーション。

スケールが `ミニゲーム` なら1〜5のみ。`週末作` なら1〜5＋6の一部。

### 3.4 ファイル出力

- 出力先: `outputs/` ディレクトリ
- Writeツールで書き出す
- Python: `outputs/{title}.py`
- HTML: `outputs/{title}.html`
- 複数ファイルの場合: `outputs/{title}/` 以下に展開

---

## フェーズ4：動作確認指示

コード生成後、ユーザーへの実行手順を出力する。

### Python の場合
```
実行手順:
1. pip install pygame-ce  （未インストールの場合）
2. python outputs/{title}.py
```

### HTML の場合
```
実行手順:
1. outputs/{title}.html をブラウザで開く
   （ローカルサーバーが必要な場合は以下）
2. python -m http.server 8000
3. http://localhost:8000/outputs/{title}.html を開く
```

---

## フェーズ5：イテレーション

ユーザーからフィードバックが来たら以下で対応する。

| フィードバック種別 | 対応 |
|-------------------|------|
| バグ報告（挙動が変） | 原因特定 → 最小修正 → 修正箇所を明示 |
| ゲームバランス調整 | CONFIG定数の変更で対応 |
| 機能追加 | 設計への影響を先に確認。影響大なら再設計を提案 |
| ビジュアル変更 | 描画コードの対象箇所を特定して変更 |

バグ修正時は「なぜそのバグが起きたか」を1行で説明する。

---

## 設計相談モード

「どう思う？」「どうすれば？」などの相談には：
1. **事実**（現状・制約）を整理する
2. **選択肢**を2〜3個出す（トレードオフ付き）
3. **推奨案**を1つ選んで理由を述べる

コードは書かない。合意が取れたらフェーズ3へ移行する。

---

## ゲームジャンル別チェックリスト

### アクション系
- [ ] フレームレート固定（dt使用）
- [ ] 入力の遅延なし（毎フレーム検出）
- [ ] 衝突矩形はビジュアルより少し小さめ
- [ ] 死亡→リスタートのサイクルが短い

### パズル系
- [ ] 操作のundo/redoを考慮するか判断
- [ ] 詰み状態の検出
- [ ] ヒント機能の有無

### RPG/アドベンチャー系
- [ ] セーブ/ロードの仕組み
- [ ] テキスト送りの実装
- [ ] フラグ管理（辞書型で一元管理）

### シミュレーション系
- [ ] 時間の進め方（リアルタイム/ターン制）
- [ ] データの永続化
- [ ] バランス調整用定数の集中管理

---

## 絶対にやらないこと

- 動かないコードを「あとで補完してください」と渡す
- プレースホルダー（`# TODO: ここに実装`）を最終成果物に残す
- 設計だけして実装しない
- ユーザーが指定した言語・フレームワークを無視して別の技術を使う
- 過剰なファイル分割（小規模ゲームを10ファイルに分ける等）
