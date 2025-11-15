using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Tesseract;
using Application = System.Windows.Application;
using Matrix = System.Windows.Media.Matrix;
using PresentationSource = System.Windows.PresentationSource;

public class OcrService
{
    private const string TessDataPath = @"C:\TesseractData";

    public OcrService()
    {
        Directory.CreateDirectory(TessDataPath);
    }

    public async Task<string> RecognizeTextAsync(System.Windows.Rect region, string language)
    {
        if (region.Width <= 0 || region.Height <= 0) return string.Empty;

        using (var originalBitmap = CaptureScreen(region))
        {
            if (originalBitmap == null) return string.Empty;

            Bitmap bitmapToProcess = originalBitmap;

            if (ShouldInvert(originalBitmap))
            {
                bitmapToProcess = InvertBitmap(originalBitmap);
            }

            string result = await PerformOcr(bitmapToProcess, language);

            if (bitmapToProcess != originalBitmap)
            {
                bitmapToProcess.Dispose();
            }

            return result;
        }
    }

    private async Task<string> PerformOcr(Bitmap imageToProcess, string language)
    {
        string ocrLanguages;
        switch (language?.ToLower())
        {
            case "zh": ocrLanguages = "chi_sim+eng"; break;
            case "jp": ocrLanguages = "jpn+eng"; break;
            case "en": ocrLanguages = "eng"; break;
            case "kor": ocrLanguages = "kor"; break;
            case "auto": default: ocrLanguages = "chi_sim+eng+jpn+kor"; break;
        }

        return await Task.Run(() =>
        {
            try
            {
                using (var engine = new TesseractEngine(TessDataPath, ocrLanguages, EngineMode.Default))
                {
                    using (var ms = new MemoryStream())
                    {
                        // --- 核心修正：明确指定 System.Drawing.Imaging.ImageFormat ---
                        imageToProcess.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        using (var pix = Pix.LoadFromMemory(ms.ToArray()))
                        {
                            using (var page = engine.Process(pix, PageSegMode.Auto))
                            {
                                return page.GetText().Trim();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"OCR 引擎初始化失败！\n\n" +
                                      $"请确保在 C:\\TesseractData 文件夹中，\n" +
                                      $"已放入所需的语言文件 (例如: '{ocrLanguages.Split('+')[0]}.traineddata')。\n\n" +
                                      $"详细错误: {ex.Message}";
                MessageBox.Show(errorMessage, "OCR 错误");
                return string.Empty;
            }
        });
    }

    private bool ShouldInvert(Bitmap bmp)
    {
        if (bmp == null || bmp.Width < 10 || bmp.Height < 10) return false;

        long totalBrightness = 0;
        int sampleCount = 0;
        int step = Math.Max(1, Math.Min(bmp.Width, bmp.Height) / 20);

        for (int y = 0; y < bmp.Height; y += step)
        {
            for (int x = 0; x < bmp.Width; x += step)
            {
                Color pixel = bmp.GetPixel(x, y);
                totalBrightness += pixel.R + pixel.G + pixel.B;
                sampleCount++;
            }
        }

        if (sampleCount == 0) return false;

        double averageBrightness = (double)totalBrightness / (sampleCount * 3);

        return averageBrightness < 128;
    }

    private Bitmap InvertBitmap(Bitmap source)
    {
        Bitmap invertedImage = new Bitmap(source.Width, source.Height, source.PixelFormat);
        BitmapData sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
        BitmapData destData = invertedImage.LockBits(new Rectangle(0, 0, invertedImage.Width, invertedImage.Height), ImageLockMode.WriteOnly, invertedImage.PixelFormat);

        try
        {
            int bytesPerPixel = Image.GetPixelFormatSize(source.PixelFormat) / 8;
            if (bytesPerPixel < 3)
            {
                source.UnlockBits(sourceData);
                invertedImage.UnlockBits(destData);
                invertedImage.Dispose(); // 释放新创建的 bitmap
                return source; // 不是彩色图，直接返回原图
            }
            int byteCount = sourceData.Stride * source.Height;
            byte[] pixels = new byte[byteCount];
            IntPtr ptrFirstPixel = sourceData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, byteCount);

            for (int i = 0; i < byteCount; i += bytesPerPixel)
            {
                pixels[i] = (byte)(255 - pixels[i]);
                pixels[i + 1] = (byte)(255 - pixels[i + 1]);
                pixels[i + 2] = (byte)(255 - pixels[i + 2]);
            }
            Marshal.Copy(pixels, 0, destData.Scan0, byteCount);
        }
        finally
        {
            source.UnlockBits(sourceData);
            invertedImage.UnlockBits(destData);
        }

        return invertedImage;
    }

    private Bitmap CaptureScreen(System.Windows.Rect region)
    {
        PresentationSource source = PresentationSource.FromVisual(Application.Current.MainWindow);
        if (source == null)
        {
            foreach (Window win in Application.Current.Windows)
            {
                if (win.IsVisible)
                {
                    source = PresentationSource.FromVisual(win);
                    break;
                }
            }
        }

        Matrix transform;
        if (source?.CompositionTarget != null)
        {
            transform = source.CompositionTarget.TransformToDevice;
        }
        else
        {
            using (var src = new System.Windows.Interop.HwndSource(new System.Windows.Interop.HwndSourceParameters()))
            {
                transform = src.CompositionTarget.TransformToDevice;
            }
        }

        var topLeft = transform.Transform(region.TopLeft);
        var bottomRight = transform.Transform(region.BottomRight);
        var pixelRect = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)Math.Abs(bottomRight.X - topLeft.X), (int)Math.Abs(bottomRight.Y - topLeft.Y));

        if (pixelRect.Width <= 0 || pixelRect.Height <= 0) { return new Bitmap(1, 1); }

        var bmp = new Bitmap(pixelRect.Width, pixelRect.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(pixelRect.X, pixelRect.Y, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
        }
        return bmp;
    }
}