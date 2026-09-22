# Potato Music Player

Windows 64bit 向け音楽プレイヤー。C# + WPF + LibVLCSharp。

## セットアップ

1. Visual Studio 2022 で `PotatoMusicPlayer.sln` を開く
2. NuGet パッケージを復元（ビルド時に自動、または `dotnet restore`）
3. `dotnet build` → `dotnet run`

必要環境: .NET 8 SDK, Windows 10/11 64bit

## 現在の実装状況（Phase 1〜2 途中）

### 完成
- ファイル再生（LibVLCSharp）: 再生/一時停止/停止/シーク/速度変更/音量
- メタデータ表示（曲名/アーティスト/ビットレート/サンプルレート）
- MVVM構成（Models / Services / ViewModels）
- 設定の保存・読み込み（JSON, `%APPDATA%\Local\PotatoMusicPlayer\`）
- 最近使ったファイル管理
- ダークテーマ UI（タイトルバー統合メニュー付き MainWindow）
- アプリ内ホットキー（ウィンドウにフォーカスがある時のみ有効）
- 波形データ抽出ロジック（NAudo使用、WaveformService）

### 未実装（次フェーズ）
- **波形の実際の描画**（WaveformCanvas への棒グラフ描画コード）
- **グローバルホットキー**（アプリが非フォーカスでも効くもの。HotKeyService, Win32 RegisterHotKey が必要）
- **設定画面 UI**（SettingsWindow.xaml。ホットキーのカスタマイズ、衝突チェック含む）
- **タスクバー統合**（サムネイルツールバーボタン: 再生/停止/前へ/次へ）
- 音量スライダーの実際の MediaService への反映（現状UIの表示のみ実装）
- 多言語対応（英語/日本語切り替え）
- 出力デバイス選択

## 既知の注意点

- **OGG (Vorbis) ファイルの波形抽出**: Windows Media Foundation は Vorbis を標準サポートしていないため、
  環境によっては波形が生成できない場合があります（再生自体はLibVLCなので問題ありません）。
  対応が必要な場合は `NVorbis` パッケージの追加を検討してください。
- LibVLCSharp 使用のため、実行環境に VLC ランタイムが必要です。`VideoLAN.LibVLC.Windows` パッケージが
  自動的にバンドルするため、通常は追加インストール不要です。

## 未確認事項

- ウィンドウ初期サイズ: 仕様書内で「900×400」(概要欄)と「400×150」(ウィンドウ仕様欄)の記載が
  混在しています。現状は 400×150 を採用しています。
