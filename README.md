# Bakery Light Sync Window

Unity の `Light` コンポーネントを一覧表示し、対応する Bakery ライトコンポーネントを追加・削除・確認できる Unity Editor 拡張です。

手動で Bakery 用コンポーネントを探して付けたり、設定を見比べたりする手間を減らすことを目的としています。  
特に、**Bakery 導入後の Light セットアップ補助** と **Unity Light と Bakery Light の対応確認** をしやすくするためのツールです。

主な機能:

- Scene 内の Light を一覧表示
- 対応する Bakery コンポーネントの追加・削除
- Unity Light と Bakery コンポーネントの主要パラメータ比較
- 一括追加 / 一括削除
- ライトモードの確認と変更
- Realtime Light に Bakery コンポーネントを追加する際の挙動設定
- 日本語 / 英語 UI 切り替え

対応関係:

- `Point Light` → `BakeryPointLight`
- `Spot Light` → `BakeryPointLight`
- `Directional Light` → `BakeryDirectLight`
- `Area Light` → `BakeryLightMesh`

---

## 想定環境

- VRChatに対応した、Unityバージョン(Unity 2022.3.22f1)
- Bakery GPU Lightmapper(ツール作成時ver1.98)
- VRChat Creator Companion（VCC）または ALCOM

---

## インストール方法

### VCC の VPM から導入する方法(推奨）

1. VCC にこのパッケージの VPM リポジトリを追加します。  

[Add to VCC](https://wata23.github.io/wata23-packages/add-repo/)


2. VCC で対象プロジェクトを開きます。
3. `Manage Project` から本ツールを追加します。
4. Unity を開き、メニューから以下を実行します。

```text
Tools > Bakery Light Sync
```

### Release から Unity Package を導入する方法

1. GitHub の `Releases` から `.unitypackage` をダウンロードします。  


[最新のReleaseはこちら](https://github.com/wata23/BakeryLightSync/releases/latest)


2. Unity で `.unitypackage` をインポートします。
3. Unity を開き、メニューから以下を実行します。

```text
Tools > Bakery Light Sync
```

---

## 使い方

### Light 一覧を確認する

ウィンドウを開くと、現在開いている Scene 内の Unity `Light`コンポーネントが一覧表示されます。  
同じ GameObject に対応する Bakery コンポーネントが存在する場合、その直下に Bakery 側の行も表示されます。

表示項目の例:

- Object
- Component
- Light Type
- Light Mode
- Enabled
- Color
- Intensity
- Range / Cutoff
- Spot Angle
- Area Size
- Cookie

### Bakery コンポーネントを個別に追加 / 削除する

各 Unity `Light`コンポーネントの行の左側にチェックボックスがあります。

- **ON** → 対応する Bakery コンポーネントを追加
- **OFF** → 同じ GameObject 上の対応する Bakery コンポーネントを削除

### Bakery コンポーネントを一括で追加 / 削除する

ウィンドウ上部のボタンから一括操作が可能です。

- `Add Bakery Components To All...`
- `Remove Bakery Components From All...`

### ライトモードを確認 / 変更する

**ライトモードの項目では**、`Realtime / Mixed / Baked` の状態を確認できます。  
必要に応じて、リスト上から変更することもできます。

### 設定について

右上の **設定** から、Realtime Light に Bakery コンポーネントを追加する際の挙動を設定できます。

例:

- Realtime のまま追加
- Mixed に変更してから追加
- Baked に変更してから追加
- 一括追加時のみ確認

---

## 注意点

- **ベイクの実行自体はこのツールでは行いません。**
- Bakeryがなくても動作するように作っていますが、インポートされない限りは何もできません。
- パラメータをコピーする方法をとっているため、Bakery のバージョンによっては機能しない場合があります。
- `Area Light` は `BakeryLightMesh` に変換されます。このとき、`MeshFilter`、`MeshRenderer`、Quad Mesh、Material、Transform Scale などが変更される場合があります。
- `Area Light` の `CutOff` については、必要に応じて Bakery 側で **`Match lightmapped to area light`** を実行してください。
- 一括削除では、**Unity Light と同じ GameObject 上にある対応 Bakery コンポーネントのみ削除**します。各種Bakeryコンポーネントが単体で付いている場合は、このツールは反応しません。

---

## ライセンス

PolyForm Noncommercial 1.0.0
Required Notice: Copyright (c) 2026 Wata23

本ソフトウェアは PolyForm Noncommercial 1.0.0 の下で提供されています。
非商用目的に限り、使用・改変・再配布が可能です。配布時は、ライセンス文またはその参照先と、必要な Required Notice をあわせて提供してください。
