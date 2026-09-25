# Potato Music Player

Windows 向けの WPF 音楽プレーヤーです。LibVLCSharp を使って音声を再生し、波形の表示・ズーム・ミニマップ操作に対応しています。

## 主な機能

- MP3 / FLAC / WAV / OGG の読み込み・再生・一時停止・停止・シーク
- 再生速度、音量（最大200%）、ミュート、1曲／全体ループ
- 最近使ったファイル、テーマ、言語、ホットキーなどの設定保存
- 非同期波形生成、逐次表示、ディスクキャッシュ
- 波形ズーム、スクロール、ミニマップ範囲操作
- 中央固定／左流の再生ヘッド表示
- エクスプローラーの「プログラムから開く」への登録（既定アプリは変更しません）

## ドキュメント

- [開発者向け親仕様書](PotatoMusicPlayer/docs/Reference/PotatoMusicPlayer_Specification.md)
- [波形・ズーム・ミニマップ詳細仕様](PotatoMusicPlayer/docs/Reference/WaveformZoom_Specification.md)
- [Windows Beta 発行ガイド](PotatoMusicPlayer/docs/Reference/Release_Guide.md)
- [過去の設計・調査資料](PotatoMusicPlayer/docs/Archive/README.md)

アーカイブ内の資料は、作成当時の設計や実装状況を記録したものです。現在の実装と差異がある場合があります。現行仕様の確認ではなく、開発の経緯や過去の状況を知るための参考として閲覧してください。

## ビルドと実行

Windows 10/11 と .NET 8 SDK が必要です。

```powershell
dotnet restore .\PotatoMusicPlayer\PotatoMusicPlayer.csproj
dotnet build .\PotatoMusicPlayer\PotatoMusicPlayer.csproj
dotnet run --project .\PotatoMusicPlayer\PotatoMusicPlayer.csproj
```

波形ズームのロジック確認は、ビルド後に PowerShell 7 で実行できます。

```powershell
pwsh -NoProfile -File .\PotatoMusicPlayer\scripts\Test-WaveformZoom.ps1
```

## Beta 配布

Beta は Windows x64 向けの ZIP として配布します。発行物には .NET Runtime を含めるため、利用者が .NET Runtime を別途インストールする必要はありません。発行と動作確認の手順は [発行ガイド](PotatoMusicPlayer/docs/Reference/Release_Guide.md) を参照してください。

## 現在の制限

- プレイリストと実際の次曲／前曲切替は未実装です。Loop: 全体は現在の曲を先頭から再生します。
- 設定画面・アプリ起動時・Windows 標準ファイル選択ダイアログで一瞬白く表示される場合があります。
- ウィンドウを閉じた後もアプリのプロセスが残ることを発行テストで確認しました。Beta 配布前に原因調査が必要です。
- UI 操作と音声再生の手動確認が必要です。自動テストプロジェクトはまだありません。

## ライセンス

本プロジェクトは [MIT License](LICENSE) で公開します。依存ライブラリはそれぞれのライセンス条件に従います。
