# Potato Music Player

Windows向けのWPF音楽プレーヤーです。LibVLCSharpで再生し、TagLibSharpでメタデータを取得します。

## 現在の機能

- MP3 / FLAC / WAV / OGG の読み込み・再生・一時停止・停止・シーク
- 再生速度、音量（最大200%）、ミュート、1曲／全体ループ
- 最近使ったファイル、テーマ、言語、ホットキーなどの設定保存
- 非同期波形生成と、中央固定／左流の2種類の再生ヘッド表示
- 波形の逐次表示、生成結果のディスクキャッシュと上限管理
- 波形ズーム、スクロール、ミニマップ範囲操作
- 波形の表示範囲・指定箇所の秒数／パーセンテージ入力
- 波形の横方向・縦方向詳細度、既定ズーム、ズーム復元
- 曲末から次の再生を開始するまでの待機時間設定

波形に関する仕様・操作・既知の問題は [WaveformZoom_Specification.md](WaveformZoom_Specification.md) にまとめています。アプリ全体の設計方針と実装状況は [PotatoMusicPlayer_Specification.md](PotatoMusicPlayer_Specification.md) を参照してください。

## ビルド

Windows 10/11 と .NET 8 SDK が必要です。

```powershell
dotnet restore
dotnet build PotatoMusicPlayer.csproj
```

実行する場合:

```powershell
dotnet run --project PotatoMusicPlayer.csproj
```

## 構成

- `Models/`: 再生状態、設定、波形ズーム状態
- `Services/`: LibVLC再生、設定保存、波形解析、ズーム計算
- `ViewModels/`: 再生状態と操作ロジック
- `Views/`: 設定画面、入力ダイアログ
- `MainWindow.xaml(.cs)`: メイン画面と波形・ミニマップ操作
- `Resources/Languages/`: 日本語／英語の表示文言

## 現在の制限

- プレイリストと実際の次曲／前曲切替は未実装です。Loop: 全体は現在の曲を先頭から再生します。
- 設定画面・アプリ起動時・Windows標準ファイル選択ダイアログで一瞬白く表示される場合があります。WPF／Windowsの初期描画に起因する既知問題です。
- UI操作を中心とした手動確認が必要で、自動テストプロジェクトはまだありません。

## 開発メモ

既存の変更を含む作業ツリーでは、`potato.ico` をユーザー変更として扱い、勝手に戻さないでください。ビルド成果物（`bin/`、`obj/`、`build-check/`）はコミット対象外です。
