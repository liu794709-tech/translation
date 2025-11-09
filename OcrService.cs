using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Tesseract;

public class OcrService
{
    // 使用硬编码的、最不可能出错的路径
    private const string TessDataPath = @"C:\TesseractData";

    // --- 修正：将 O-crService 改回 OcrService ---
    public OcrService()
    {
        // 确保目录存在，以防万一
        Directory.CreateDirectory(TessDataPath);
    }

    public async Task<string> RecognizeTextAsync(System.Windows.Rect region)
    {
        if (region.Width <= 0 || region.Height <= 0) return string.Empty;

        using (var bitmap = CaptureScreen(region))
        {
            string fileName = $"debug_capture_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            bitmap.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName));

            return await Task.Run(() =>
            {
                try
                {
                    using (var engine = new TesseractEngine(TessDataPath, "chi_sim+eng", EngineMode.Default))
                    {
                        using (var ms = new MemoryStream())
                        {
                            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            using (var pix = Pix.LoadFromMemory(ms.ToArray()))
                            {
                                using (var page = engine.Process(pix, PageSegMode.Auto))
                                {
                                    string text = page.GetText().Trim();
                                    return text;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("OCR 识别出错: " + ex.ToString());
                    return string.Empty;
                }
            });
        }
    }

    // CaptureScreen 方法保持不变
    private Bitmap CaptureScreen(System.Windows.Rect region)
    {
        var source = PresentationSource.FromVisual(Application.Current.MainWindow) ?? PresentationSource.FromVisual(new System.Windows.Controls.Grid());
        Matrix transform;
        if (source != null && source.CompositionTarget != null) { transform = source.CompositionTarget.TransformToDevice; }
        else { using (var src = new HwndSource(new HwndSourceParameters())) { transform = src.CompositionTarget.TransformToDevice; } }
        var topLeft = transform.Transform(region.TopLeft);
        var bottomRight = transform.Transform(region.BottomRight);
        var pixelRect = new System.Drawing.Rectangle((int)topLeft.X, (int)topLeft.Y, (int)Math.Abs(bottomRight.X - topLeft.X), (int)Math.Abs(bottomRight.Y - topLeft.Y));
        if (pixelRect.Width <= 0 || pixelRect.Height <= 0) { return new Bitmap(1, 1); }
        var bmp = new Bitmap(pixelRect.Width, pixelRect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp)) { g.CopyFromScreen(pixelRect.X, pixelRect.Y, 0, 0, bmp.Size); }
        return bmp;
    }
}