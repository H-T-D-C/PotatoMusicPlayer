# 公開準備: 整理候補と作業計画

確認日: 2026-09-25  
対象: リポジトリ全体（アプリ本体は `PotatoMusicPlayer/`）

この文書は公開前の確認結果と作業計画であり、ここに挙げたファイルやコードは削除していない。利用箇所が見つからないことと、公開上不要であることは同じではないため、候補ごとに根拠と確認事項を記す。

## 整理候補

### 優先度高: 削除または整理を検討

| 対象 | 根拠 | 推奨する扱い |
| --- | --- | --- |
| `PotatoMusicPlayer/Converters/TimeSpanToStringConverter.cs`、`PotatoMusicPlayer/Converters/VolumeToPercentConverter.cs` | C#・XAML 全体を検索したが、クラス名の参照が定義以外にない。 | 未使用を再確認し、削除する。`Converters/` が空になればフォルダーも不要。 |
| `PotatoMusicPlayer/ViewModels/MainViewModel.cs` の `OpenFileCommand`、`OpenFile()` | コマンドの定義と初期化以外に利用箇所がない。メソッドは「WPF 実装時」とする TODO のまま。実際のファイル選択は `MainWindow.xaml.cs` 側にある。 | コマンドとスタブを削除し、ファイル選択処理の責務を現行 UI に一本化する。関連する TODO コメントも同時に除く。 |
| `PotatoMusicPlayer/assets/speaker/speakers_0.png` ～ `speakers_3.png` | `.csproj` ではリソースとして含めているが、コード・XAML が利用しているのは `Resources/Icons/speaker_dark_*.png` と `speaker_light_*.png`。 | 旧アイコン一式と `.csproj` の項目を削除候補とする。ビルド後に音量アイコンを確認する。 |
| `PotatoMusicPlayer/PotatoMusicPlayer.csproj` の古い `None Include` | `potato.png` を追加する相対パスが現在の作業ツリー外を指し、実ファイルが存在しない。通常の WPF 実行リソースは別項目で登録済み。 | NuGet パッケージとして `.csproj` を pack する予定がなければ、この壊れた項目と `PackageIcon` の要否を整理する。NuGet パッケージを作るならパスを修正して pack 結果を確認する。 |
| `PotatoMusicPlayer/assets/testmusic/Sour Tennessee Red - John Deley and the 41 Players.mp3` | 約5 MBの音源。アプリの実行時参照や配布処理から参照されておらず、Git に追跡されている。 | 公開リポジトリに含める必要がなければ外す。サンプルとして残す場合は、配布・再配布条件とクレジット表記を確認してライセンス情報を添える。 |
| `PotatoMusicPlayer/Unnecessary Documents/sound_wave.md` | 現在の波形仕様書ではなく、波形機能の初期実装プロンプト。現在のコード・仕様と重複または不一致になりうる。 | 履歴を残す必要がなければ公開対象から除く。作業履歴として保存する場合は、利用者向け文書と分けてアーカイブする。 |
| `PotatoMusicPlayer/Unnecessary Documents/PLAYBACK_FIX_PROGRESS.md` | 特定の過去の再生バー不具合に関する進捗メモで、現行の利用者向け仕様ではない。 | issue や変更履歴として残す必要を確認し、不要なら公開対象から除く。 |
| `PotatoMusicPlayer/WaveformLoading_Plan.md`、`PotatoMusicPlayer/ThemeRefactor_Plan.md` | 実装済み機能に関する計画・経緯の文書。現行仕様書と役割が重複し、過去の記述が現状とずれる可能性がある。 | 実装記録として必要か確認する。不要なら公開対象から除くか、履歴資料として明示的にアーカイブする。 |

### 要判断: 現行の設計・仕様に必要か確認

| 対象 | 確認事項 | 推奨する扱い |
| --- | --- | --- |
| `PotatoMusicPlayer/CenterFixedFollow_Design.md` | 調査・修正経緯と完了記録を含む設計資料。現行の利用者向け仕様書ではないが、開発履歴としての価値がある。 | 開発資料として公開するか、履歴アーカイブへ移すか決める。公開する場合は README のリンクを直す。 |
| `PotatoMusicPlayer/WaveformZoom_Specification.md` | 波形・ズームの現行仕様で、アプリ仕様書から参照されている。 | 現行仕様として残す。README と仕様書の相対リンクを修正する。 |
| `PotatoMusicPlayer/PotatoMusicPlayer_Specification.md` | アプリ全体の現行仕様として波形仕様書を参照している。 | 開発者向け資料として残すか、README から利用者向け文書と明確に分けて案内する。 |

## 公開前に直すべき項目

1. **README のリンクを修正する。** リポジトリ直下の `README.md` から `WaveformZoom_Specification.md` と `PotatoMusicPlayer_Specification.md` へリンクしているが、ファイルは `PotatoMusicPlayer/` 内にあるため、現状の相対リンクでは開けない。リンク先を実際の配置に合わせ、公開する文書だけを案内する。
2. **ライセンスを決める。** リポジトリ直下に `LICENSE` が見当たらない。利用・改変・再配布の条件を決め、ソースと配布物に適用するライセンスを追加する。依存ライブラリと LibVLC のライセンス・著作権表記、同梱するネイティブファイルに必要な通知も確認する。
3. **配布物を再現できる発行手順を用意する。** `Properties/PublishProfiles/FolderProfile.pubxml` と `.pubxml.user` は `.gitignore` により Git 管理外で、プロファイルには出力先も指定されている。個人用 `.pubxml.user` は共有せず、公開可能な設定だけを管理対象にする。現在のプロファイルは `win-x64`、Self-contained、Single-file を指定しているが、実際の出力物・起動・LibVLC 再生をクリーンな Windows 10/11 環境で検証し、出力先の重複を解消する。
4. **公開ビルドを自動検証する。** GitHub Actions 等で Windows 上の restore/build を行い、少なくともリリース作成前にビルド失敗を検出する。UI・音声再生の自動テストは未整備なので、手動確認項目も用意する。
5. **Beta の配布ページを整える。** バージョン、対応 Windows／アーキテクチャ、インストール不要かどうか、起動方法、既知の制限、更新方法、報告先を README と GitHub Release に記載する。初期 Beta では ZIP 配布を基準にし、自己完結型の発行物一式を添付する。更新頻度や利用者数を見てインストーラー／自動更新を別途検討する。
6. **リポジトリの公開範囲を確認する。** `KNOWN_ISSUES.md` の記載を現状に合わせ、秘密情報、個人パス、ローカル設定、ビルド成果物が追跡されていないことを最終確認する。`.gitignore` は `.user`、`bin/`、`obj/`、`publish/` 等を除外している。公開時には個人用の発行プロファイルを含めない。

## 発行方式について

現状のプロジェクトは `net8.0-windows` と `win-x64` を対象にしており、発行プロファイルは Self-contained を指定している。Self-contained 発行は対象環境に .NET Runtime の事前導入を求めない代わりに、配布サイズが大きくなり、OS・CPU ごとの発行物が必要になる。Microsoft の .NET 発行資料を参照し、配布前にターゲット OS で実動確認する。

Single-file は「必ず exe 一つだけで完結する」という意味ではない。Microsoft の資料では、単一ファイルにまとめられる対象やネイティブライブラリの扱いに制約がある。LibVLC を含む実際の発行物を確認し、ZIP 内の必要ファイル一式として配布する。`.NET Runtime を不要にすること` と `インストーラーで配布すること` は別の選択である。

## 整理・公開の進め方

1. この一覧をもとに、履歴文書とテスト音源を公開リポジトリに残すか決定する。
2. 不要と判断したコード・画像・文書のみ削除し、プロジェクトファイル、README、相互リンクを合わせて更新する。
3. `LICENSE` と依存関係・音源の権利表記、公開可能な発行プロファイル、Windows CI を整える。
4. Release 用に Self-contained `win-x64` を発行し、Runtime 未導入環境で起動・再生・設定保存・波形操作を確認する。
5. Beta の既知問題、配布手順、更新手順、報告先を明記して GitHub Release を作成する。

## 確認に使った情報

- 参照検索では `OpenFileCommand` とコンバーター2種は宣言・初期化以外に参照がなく、`assets/speaker/` 内の画像もコード・XAMLから参照されていなかった。
- `Resources/Icons/`、`potato.png`、`potato.ico`、言語 JSON、テーマ辞書、波形・再生サービスはアプリから参照されているため、削除候補にしていない。
- コードコメントは一括削除せず、未実装スタブに付いた TODO のように、対象コードと同時に不要になるものだけを整理する。波形操作や再生制御の理由を説明するコメントは現行の挙動に関わるため保持する。
- `.NET` の Self-contained と Single-file の挙動は [Microsoft の dotnet publish 資料](https://learn.microsoft.com/dotnet/core/tools/dotnet-publish) と [Single-file deployment 資料](https://learn.microsoft.com/dotnet/core/deploying/single-file/overview) に基づく。
