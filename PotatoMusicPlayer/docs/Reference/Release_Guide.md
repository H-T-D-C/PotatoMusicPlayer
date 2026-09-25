# Windows Beta 発行ガイド

## 配布方式

初期 Beta は Windows x64 向けの ZIP 配布とする。Self-contained 発行で .NET Runtime を同梱するため、利用者側で .NET Runtime を別途インストールする必要がない。LibVLC などのネイティブ依存ファイルを含め、発行フォルダー内のファイル一式を ZIP にする。インストーラーや自動更新は、更新頻度と利用状況を見て別途導入を判断する。

## 発行

リポジトリ直下で Windows 上の PowerShell を使い、Release 構成で発行する。

```powershell
dotnet restore .\PotatoMusicPlayer\PotatoMusicPlayer.csproj
dotnet publish .\PotatoMusicPlayer\PotatoMusicPlayer.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  --output .\artifacts\publish\win-x64
```

`PublishSingleFile=false` にして依存 DLL とネイティブファイルをフォルダー内に明示する。Release はデバッグシンボルを出力しない設定とし、開発マシンの絶対パスを配布バイナリへ含めない。特定のプロファイルやローカル環境に依存しないよう、配布物はこのコマンドから再現する。

LibVLC の NuGet パッケージには x86 と x64 のネイティブファイルが含まれるため、win-x64 版では使わない x86 ディレクトリを発行先から除いて ZIP を小さくする。削除対象が発行先の内側にあることを確認してから実行する。

```powershell
$publishRoot = (Resolve-Path .\artifacts\publish\win-x64).Path
$x86Directory = Join-Path $publishRoot 'libvlc\win-x86'
if (Test-Path -LiteralPath $x86Directory) {
  $resolvedX86 = (Resolve-Path -LiteralPath $x86Directory).Path
  if (-not $resolvedX86.StartsWith($publishRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The x86 directory is outside the publish directory.'
  }
  Remove-Item -LiteralPath $resolvedX86 -Recurse -Force
}
```

発行が成功したら、発行フォルダーの中身を ZIP にする。

```powershell
Compress-Archive `
  -Path .\artifacts\publish\win-x64\* `
  -DestinationPath .\artifacts\PotatoMusicPlayer-beta-win-x64.zip `
  -Force
```

## 配布前の確認

1. ZIP に `libvlc/win-x64` が含まれ、`libvlc/win-x86` と `.pdb` が含まれないことを確認する。展開後に `PotatoMusicPlayer.exe` が起動し、ウィンドウを閉じた後にプロセスも終了することを確認する。この終了確認は現時点で失敗している。
2. .NET Runtime がインストールされていない Windows 10/11 x64 環境でも起動することを確認する。
3. 音声ファイルを開いて再生、停止、シーク、速度・音量変更ができることを確認する。LibVLC の DLL とプラグインを含むため、特に実際の音声出力を確認する。
4. 波形の生成、キャッシュ、ズーム、ミニマップ操作、設定の保存と再起動後の復元を確認する。
5. ZIP を別のフォルダーへ展開して再度起動し、開発環境のファイルに依存していないことを確認する。
6. Release にバージョン、対応環境、主な変更、既知の制限、更新・報告方法を記載する。
7. Debug ビルド後に `pwsh -NoProfile -File .\PotatoMusicPlayer\scripts\Test-WaveformZoom.ps1` を実行する。
8. 同梱される NuGet 依存パッケージと LibVLC のライセンス条件・著作権通知を確認し、必要な通知を配布物へ含める。

発行物を単一の exe として扱わない。Single-file 発行ではネイティブライブラリ等が別ファイルのまま残る場合があるため、LibVLC を含む実ファイルを検証したうえで同梱一式を配布する。
