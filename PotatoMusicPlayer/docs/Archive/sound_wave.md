# Potato Music Player - 波形表示機能実装プロンプト

## 現状
- C# + WPF で Windows 音楽プレイヤー開発中
- LibVLCSharp でMP3/FLAC/WAV/OGG再生
- 900×400のダークテーマUI
- メイン画面: メタデータ表示、再生制御、ホットキー対応

## 実装タスク: 音声波形表示

### 要件
1. **波形データ取得**
   - 音声ファイル読み込み時、モノラル表示の波形を生成
   - 動画編集ソフト風：各バーは該当区間のピーク振幅（正規化 0.0～1.0）
   - NAudio使用（既存WaveformService.csで抽出ロジック実装済み）
   - 非同期生成：ファイル読み込み中にUIをブロックしない
   - 生成進捗通知（0.0～1.0）をプログレス表示に使用

2. **UI描画(Canvas)**
   - MainWindow.xaml に Canvas x:Name="WaveformCanvas" が存在
   - 背景色: #2D2D2D、バー色: #78B4FF (WaveformBrush)
   - ウィンドウリサイズ時に波形も再描画
   - 波形バー幅はウィンドウ幅に応じて可変（推奨最小1px, 最大4px）

3. **現在位置のサムネイル**
   - 再生中、現在位置を示す白いカーソル線を波形上に常時更新
   - UpdatePlaybackDisplay()タイマーで毎100ms更新

4. **表示/非表示切り替え**
   - MainWindow.xaml.cs の ShowWaveformMenuItem.IsChecked で制御済み
   - メニュー「表示」→「波形を表示」で on/off

5. **ドラッグシーク連携**
   - 波形上をクリック/ドラッグで、その位置に再生ヘッドをシーク可能
   - 既存のSeekBar(Slider)と同じく、ドラッグ中はタイマーが位置をリセットしないよう配慮

### 実装ファイル
- **MainWindow.xaml.cs**: 
  - `Canvas WaveformCanvas` のイベントハンドラ追加（MouseDown/MouseMove/MouseUp）
  - 波形描画ロジック(`DrawWaveform(float[] data)`, `DrawPlaybackCursor(double position)`)
  - ウィンドウリサイズ時に波形再描画（SizeChanged イベント）
  
- **MainViewModel.cs** (WaveformService活用):
  - `Task LoadWaveformAsync(string filePath)`: WaveformService使用、波形取得
  - `float[] CurrentWaveformData`: プロパティ、現在の波形データ保持
  - ロード進捗イベント: Progress通知 → UI バー表示

- **MainWindow.xaml**:
  - 既存Canvas要素の確認、必要に応じて高さ調整

### 技術ノート
- WaveformService.GenerateWaveformAsync() は既実装
- NAudio デコード: MP3/WAV は AudioFileReader、FLAC/OGG は MediaFoundationReader フォールバック
- OGG (Vorbis) は環境依存（Windows Media Foundation サポート状況）
  → 波形生成失敗時は空データ返却（再生自体は影響なし）
- Slider ドラッグと Canvas ドラッグ同期: _isDraggingSeekBar フラグ活用

### コード例（参考）
```csharp
// Canvas上の クリック→シーク
private void WaveformCanvas_MouseUp(object sender, MouseButtonEventArgs e)
{
    Point pos = e.GetPosition(WaveformCanvas);
    double ratio = pos.X / WaveformCanvas.ActualWidth;
    double seekSeconds = _viewModel.PlaybackState.Duration.TotalSeconds * ratio;
    _viewModel.SetPosition(seekSeconds);
}

// 波形描画
private void DrawWaveform(float[] data)
{
    if (data == null || data.Length == 0) return;
    
    WaveformCanvas.Children.Clear();
    int barWidth = Math.Max(1, (int)(WaveformCanvas.ActualWidth / data.Length));
    
    for (int i = 0; i < data.Length; i++)
    {
        double height = data[i] * WaveformCanvas.ActualHeight;
        Rectangle bar = new Rectangle
        {
            Width = barWidth,
            Height = height,
            Fill = new SolidColorBrush(Color.FromRgb(120, 180, 255)),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        Canvas.SetLeft(bar, i * barWidth);
        WaveformCanvas.Children.Add(bar);
    }
}

// 現在位置カーソル（毎tick更新）
private void DrawPlaybackCursor(double positionSeconds, double durationSeconds)
{
    if (durationSeconds <= 0) return;
    
    double ratio = positionSeconds / durationSeconds;
    double cursorX = ratio * WaveformCanvas.ActualWidth;
    
    // Line を描画（または Transform で既存ラインを移動）
}
```

### デバッグポイント
1. WaveformService.GenerateWaveformAsync() が実際にデータを返しているか確認
2. Canvas リサイズ時に適切に再描画されるか
3. ドラッグシーク時に タイマーによる位置リセットと競合していないか
4. メモリ: 波形データ保持が大きすぎないか（`float[数千]` 程度なら問題なし）

---