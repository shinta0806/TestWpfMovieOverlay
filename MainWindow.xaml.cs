// ============================================================================
// 
// WPF での動画再生時にテキストや図を上書きするサンプルコード
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TestWpfMovieOverlay
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public MainWindow()
		{
			InitializeComponent();

#if DEBUG
			Title = "［デバッグ］" + Title;
#endif
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 動画再生
		private MediaPlayer mMediaPlayer;

		// テキスト等の合成用ビットマップ
		private RenderTargetBitmap mBmp;

		// 描画用
		DrawingVisual mDrawingVisual;

		// フレームレート算出用
		private Stopwatch mStopWatch;

		// フレームレート算出用
		private Int64 mPrevElapsed;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 描画更新のタイミングで動画とテキスト等を合成
		// --------------------------------------------------------------------
		void CompositionTargetRendering(object sender, EventArgs e)
		{
			// 動画読み込み途中の場合は何もしない
			if (mMediaPlayer.DownloadProgress < 1.0 || mMediaPlayer.NaturalVideoWidth == 0)
			{
				return;
			}

			// フレームレート算出
			// 動画のフレームレートではなく、WPF 描画更新タイミングのフレームレートであることに注意
			Int64 aCurrent = mStopWatch.ElapsedMilliseconds;
			Int32 aDiff = (Int32)(aCurrent - mPrevElapsed);
			if (aDiff == 0)
			{
				return;
			}
			String aRate = (1000 / aDiff).ToString() + " fps (WPF)";
			mPrevElapsed = aCurrent;

			// 合成
			using (DrawingContext aDrawingContext = mDrawingVisual.RenderOpen())
			{
				// 動画を描画
				aDrawingContext.DrawVideo(mMediaPlayer, new Rect(0, 0, ImageComposition.Width, ImageComposition.Height));

				// テキストの背景枠（四角形）を描画
				aDrawingContext.DrawRectangle(Brushes.LightGray, null, new Rect(20, 20, 150, 60));

				// フレームレートを描画
				aDrawingContext.DrawText(CreateFormattedText(aRate, Brushes.Red), new Point(30, 20));

				// 動画再生位置を描画
				aDrawingContext.DrawText(CreateFormattedText(mMediaPlayer.Position.ToString(@"mm\:ss\.ff"), Brushes.Blue), new Point(30, 40));
			}

			mBmp.Render(mDrawingVisual);
		}

		// --------------------------------------------------------------------
		// 文字列を FormattedText にする
		// --------------------------------------------------------------------
		private FormattedText CreateFormattedText(String oText, SolidColorBrush oBrush)
		{
			return new FormattedText(oText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
					 new Typeface(FontFamily, FontStyles.Normal, FontWeights.Normal, new FontStretch()), 20, oBrush);
		}

		// --------------------------------------------------------------------
		// 動画再生
		// この段階では単に内部で再生するのみで、出力先は無い
		// --------------------------------------------------------------------
		private void Play(String oPath)
		{
			// 既に再生中のものは停止
			if (mMediaPlayer != null)
			{
				mMediaPlayer.Stop();
			}

			// 読み込み
			mMediaPlayer = new MediaPlayer();
			mMediaPlayer.Open(new Uri(oPath, UriKind.Absolute));

			// ボリューム調整（1.0 で最大）
			mMediaPlayer.Volume = 0.2;

			// 再生
			mMediaPlayer.Play();
		}

		// --------------------------------------------------------------------
		// 上書きの準備
		// --------------------------------------------------------------------
		private void PrepareOverlay()
		{
			// 動画とテキスト等の合成結果を保持するビットマップを Image コントロールのソースにする
			mBmp = new RenderTargetBitmap((Int32)ImageComposition.Width, (Int32)ImageComposition.Height, 96, 96, PixelFormats.Pbgra32);
			ImageComposition.Source = mBmp;

			// メンバー変数アロケート
			mDrawingVisual = new DrawingVisual();
			mStopWatch = new Stopwatch();
			mStopWatch.Start();

			// 描画更新イベントハンドラーの設定
			CompositionTarget.Rendering += CompositionTargetRendering;
		}

		// ====================================================================
		// IDE 生成イベントハンドラー
		// ====================================================================
		private void Window_DragOver(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.None;
			e.Handled = true;

			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				// ファイル類のときのみドラッグを受け付ける
				e.Effects = DragDropEffects.Copy;
			}
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			try
			{
				String[] aDropFiles = e.Data.GetData(DataFormats.FileDrop, false) as String[];
				if (aDropFiles == null)
				{
					return;
				}

				Play(aDropFiles[0]);
				PrepareOverlay();
			}
			catch (Exception oExcep)
			{
				MessageBox.Show(oExcep.Message);
			}
		}
	}
	// public partial class MainWindow ___END___
}
// namespace TestWpfMovieOverlay ___END___
