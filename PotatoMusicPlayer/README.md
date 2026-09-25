# Potato Music Player

Windows向けの軽量な音楽プレイヤーです。C# / WPF と LibVLCSharp を使用し、VLC風の再生操作とカスタマイズ可能な設定を提供します。

## 主な機能

- MP3 / FLAC / WAV / OGG の再生、一時停止、停止、シーク
- 曲名、アーティスト、ビットレート、サンプルレート、再生時間の表示
- 波形表示、ミニマップ、ズーム、中央固定／左流しの再生位置表示
- 波形・ミニマップからのシーク。ミニマップの枠外ではドラッグ中に範囲を確認し、マウスを離した位置へ移動
- 波形の逐次表示とディスクキャッシュ
- ループ、再生速度変更、最大200%の音量、ミュート
- Windowsタスクバーからの前へ・再生／一時停止・停止操作
- 最近使ったファイル、ライト／ダーク／システムテーマ
- 日本語／English (US) の言語切り替え
- ホットキーの変更、組み合わせキー、リセット、クリア

## 動作環境

- Windows 10/11（64-bit）
- .NET 8 SDK

LibVLCSharpとVLCのWindows向け依存ファイルはNuGetから復元されます。

## ビルドと起動

リポジトリのルートで実行します。

```powershell
dotnet restore
dotnet build PotatoMusicPlayer.csproj
dotnet run --project PotatoMusicPlayer.csproj
```

Visual Studioでは `PotatoMusicPlayer.csproj` を開き、通常のWPFアプリとして実行できます。

## プロジェクト構成

- `MainWindow.xaml`: メイン画面、メニュー、再生・波形・音量UI
- `MainWindow.Waveform.cs`: 波形とミニマップの描画・マウス操作
- `Views/`: 設定画面などの追加ウィンドウ
- `ViewModels/`: 再生状態とUIコマンド
- `Services/`: メディア、設定、テーマ、言語、波形、ファイル処理
- `Models/`: 設定、メディア情報、再生状態、ホットキー
- `Resources/Themes/`: テーマ用XAMLリソース
- `Resources/Languages/`: `ja-JP.json` / `en-US.json`。言語追加時はここへJSONを追加します
- `Resources/Icons/`: テーマ別スピーカーアイコン
- `Unnecessary Documents/`: 実装時の調査・計画メモと旧個別仕様

詳細な仕様、対応状況、ロードマップは [PotatoMusicPlayer_Specification.md](PotatoMusicPlayer_Specification.md) を参照してください。

## 設定ファイル

設定は `%LOCALAPPDATA%\PotatoMusicPlayer\AppSettings.json` に保存されます。設定画面からホットキー、数値、音量、表示、言語、テーマを変更できます。

## 開発メモ

現在テストプロジェクトはありません。UI変更や再生処理の変更時は、各対応形式の再生、シーク、設定保存、テーマ切り替えを手動で確認してください。プレイリスト、イコライザー、音量正規化などは今後の拡張予定です。
