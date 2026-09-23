# Potato Music Player

Windows向けの軽量な音楽プレイヤーです。C# / WPF と LibVLCSharpを使用し、VLC風の再生操作とカスタマイズ可能な設定を提供します。

## 主な機能

- MP3 / FLAC / WAV / OGGの再生、一時停止、停止、シーク
- 曲名、アーティスト、ビットレート、サンプルレート、再生時間の表示
- 波形表示と波形上のクリック／ドラッグシーク
- ループ、再生速度変更、最大200%の音量、ミュート
- Windowsタスクバーからの前へ・再生／一時停止・停止操作
- 最近使ったファイル、ライト／ダーク／システムテーマ
- 日本語／English (US)の言語切り替え
- ホットキーの変更、組み合わせキー、リセット、クリア
- エクスプローラーの「プログラムから開く」から音声ファイルを直接再生

## 動作環境

- Windows 10/11（64-bit）
- .NET 8 SDK

LibVLCSharpとVLCのWindows向け依存ファイルはNuGetから復元されます。

## ビルドと起動

リポジトリのルートで実行します。

```powershell
dotnet restore
dotnet build PotatoMusicPlayer/PotatoMusicPlayer.csproj
dotnet run --project PotatoMusicPlayer/PotatoMusicPlayer.csproj
```

Visual Studioでは `PotatoMusicPlayer.sln` または `PotatoMusicPlayer/PotatoMusicPlayer.csproj`を開いて実行できます。

## プロジェクト構成

- `PotatoMusicPlayer/MainWindow.xaml`: メイン画面、メニュー、再生・波形・音量UI
- `PotatoMusicPlayer/Views/`: 設定画面などの追加ウィンドウ
- `PotatoMusicPlayer/ViewModels/`: 再生状態とUIコマンド
- `PotatoMusicPlayer/Services/`: メディア、設定、テーマ、言語、波形、ファイル処理
- `PotatoMusicPlayer/Models/`: 設定、メディア情報、再生状態、ホットキー
- `PotatoMusicPlayer/Resources/Themes/`: テーマ用XAMLリソース
- `PotatoMusicPlayer/Resources/Languages/`: `ja-JP.json` / `en-US.json`
- `PotatoMusicPlayer/Resources/Icons/`: テーマ別スピーカーアイコン

詳細な仕様、対応状況、ロードマップは [PotatoMusicPlayer_Specification.md](PotatoMusicPlayer/PotatoMusicPlayer_Specification.md)を参照してください。

## 設定ファイルとファイル関連付け

設定は `%LOCALAPPDATA%\PotatoMusicPlayer\AppSettings.json`に保存されます。設定画面からホットキー、数値、音量、表示、言語、テーマを変更できます。

起動時に、現在のユーザーだけを対象としてMP3 / FLAC / WAV / OGGを「プログラムから開く」の候補へ登録します。既定のアプリを強制的に変更することはありません。登録済みのアプリを選択すると、渡された音声ファイルを読み込んで再生します。

## 開発上の注意

現在テストプロジェクトはありません。UIや再生処理を変更した場合は、各対応形式の再生、シーク、設定保存、テーマ切り替え、エクスプローラーからの起動を手動で確認してください。プレイリスト、イコライザー、音量正規化などは今後の拡張予定です。
