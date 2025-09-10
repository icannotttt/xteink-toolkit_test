using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTEinkTools
{
    /// <summary>
    /// 超采样模式
    /// </summary>
    public enum SuperSamplingMode
    {
        None = 1,        // 无超采样
        x2 = 2,          // 2倍超采样
        x4 = 4,          // 4倍超采样
        x8 = 8           // 8倍超采样（高质量模式）
    }

    /// <summary>
    /// 字形渲染器
    /// </summary>
    public class XTEinkFontRenderer : IDisposable
    {

        public enum AntiAltasMode
        {
            System1BitGridFit,
            System1Bit,
            SystemAntiAltasGridFit,
            SystemAntiAltas,
        }

        public AntiAltasMode AAMode { get; set; } = AntiAltasMode.System1BitGridFit;

        public int LightThrehold { get; set; } = 128;
        public int LineSpacingPx { get; set; } = 0;
        public int CharSpacingPx { get; set; } = 0;
        public bool IsVerticalFont { get; set; } = false;
        public Font Font { get; set; } = SystemFonts.DefaultFont;
        public bool IsOldLineAlignment { get; set; }
        public bool RenderBorder { get; set; }

        /// <summary>
        /// 超采样模式，默认为None（无超采样）
        /// </summary>
        public SuperSamplingMode SuperSampling { get; set; } = SuperSamplingMode.None;

        private Bitmap _tempRenderSurface;
        private Graphics _tempGraphics;

        public delegate void RenderMethod(int x, int y, bool pixel);

        public Size GetFontRenderSize()
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            {
                bmp.SetResolution(96, 96);
                using(Graphics g = Graphics.FromImage(bmp))
                {
                    StringFormat strfmt = new StringFormat(StringFormat.GenericTypographic);
                    // strfmt.FormatFlags |= StringFormatFlags.FitBlackBox | StringFormatFlags.NoClip;
                    SizeF sf = g.MeasureString("坐", this.Font,999,strfmt); 
                    //if (IsVerticalFont)
                    //{
                    //    sf = new SizeF(sf.Height, sf.Width);
                    //}
                    Size s = new Size((int)Math.Round(sf.Width) + this.CharSpacingPx, (int)Math.Round(sf.Height) + this.LineSpacingPx);
                    if(s.Height < 5)
                    {
                        s.Height = 5;
                        LineSpacingPx = (int)(sf.Height - 5);
                    }
                    if (s.Width < 5)
                    {
                        s.Width = 5;
                        CharSpacingPx = (int)(sf.Width - 5);
                    }
                    return s;
                }
            }

        }

        StringFormat _format = new StringFormat(StringFormat.GenericTypographic)
        {
        };

        private void ensureRenderSurfaceSize(int width,int height)
        {
            // 计算SuperSampling所需的实际尺寸
            int actualWidth = width;
            int actualHeight = height;

            if (SuperSampling != SuperSamplingMode.None)
            {
                int scale = (int)SuperSampling;
                actualWidth = width * scale;
                actualHeight = height * scale;
            }

            if (this._tempRenderSurface != null && this._tempGraphics != null) {
                if (this._tempRenderSurface.Width == actualWidth && this._tempRenderSurface.Height == actualHeight)
                {
                    return;
                }
            }

            try
            {
                this._tempGraphics?.Dispose();
            }
            catch { }
            try
            {
                this._tempRenderSurface?.Dispose();
            }catch { }

            this._tempRenderSurface = new Bitmap(actualWidth, actualHeight);
            this._tempRenderSurface.SetResolution(96, 96);
            this._tempGraphics = Graphics.FromImage(this._tempRenderSurface);
            this._tempGraphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

            // SuperSampling模式下使用专门优化的渲染选项
            if (SuperSampling != SuperSamplingMode.None)
            {
                this._tempGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                this._tempGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                this._tempGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                this._tempGraphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            }
            else
            {
                // 普通模式：同样追求高质量，为小屏幕优化每个像素
                this._tempGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                this._tempGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                this._tempGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                this._tempGraphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            }
        }

        /// <summary>
        /// 应用SuperSampling缩放处理
        /// </summary>
        /// <param name="sourceBitmap">源位图</param>
        /// <param name="targetWidth">目标宽度</param>
        /// <param name="targetHeight">目标高度</param>
        /// <returns>缩放后的位图</returns>
        private Bitmap ApplySuperSampling(Bitmap sourceBitmap, int targetWidth, int targetHeight)
        {
            if (SuperSampling == SuperSamplingMode.None)
            {
                return sourceBitmap; // 无需处理，直接返回原图
            }

            try
            {
                // 创建目标尺寸的位图
                Bitmap targetBitmap = new Bitmap(targetWidth, targetHeight);
                targetBitmap.SetResolution(96, 96);

                using (Graphics g = Graphics.FromImage(targetBitmap))
                {
                    // 设置专门针对文字优化的缩放选项
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                    // 使用Bicubic算法更好地保留字体曲线和抗锯齿细节
                    g.DrawImage(sourceBitmap,
                        new Rectangle(0, 0, targetWidth, targetHeight),
                        new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height),
                        GraphicsUnit.Pixel);
                }

                return targetBitmap;
            }
            catch
            {
                // 如果SuperSampling失败，则返回原图
                return sourceBitmap;
            }
        }

        private void syncSettings()
        {
            if (this._tempGraphics != null) {
                TextRenderingHint targetHint = TextRenderingHint.SingleBitPerPixelGridFit;

                // 始终按用户选择的抗锯齿模式设置，让用户完全控制渲染行为
                switch (AAMode)
                {
                    case AntiAltasMode.System1BitGridFit:
                        targetHint = TextRenderingHint.SingleBitPerPixelGridFit; break;
                    case AntiAltasMode.System1Bit:
                        targetHint = TextRenderingHint.SingleBitPerPixel; break;
                    case AntiAltasMode.SystemAntiAltasGridFit:
                        targetHint = TextRenderingHint.AntiAliasGridFit; break;
                    case AntiAltasMode.SystemAntiAltas:
                        targetHint = TextRenderingHint.AntiAlias; break;
                }

                if(this._tempGraphics.TextRenderingHint != targetHint)
                {
                    this._tempGraphics.TextRenderingHint = targetHint;
                }
                if (IsVerticalFont)
                {
                    _format.FormatFlags |= StringFormatFlags.DirectionVertical;
                }
            }
            
        }

        public void RenderFont(int charCodePoint,XTEinkFontBinary renderer)
        {
            ensureRenderSurfaceSize(renderer.Width, renderer.Height);
            syncSettings();
            char chr = (char)charCodePoint;

            // 计算SuperSampling缩放比例
            int scale = (int)SuperSampling;

            // 清空画布
            this._tempGraphics.Clear(Color.Black);
            this._tempGraphics.ResetTransform();

            // 绘制边框（考虑缩放）
            if (RenderBorder)
            {
                int borderWidth = (renderer.Width * scale) - 1;
                int borderHeight = (renderer.Height * scale) - 1;
                this._tempGraphics.DrawRectangle(Pens.White, 0, 0, borderWidth, borderHeight);
            }

            // 处理垂直字体变换（考虑缩放）
            if (IsVerticalFont)
            {
                this._tempGraphics.TranslateTransform(0, renderer.Height * scale);
                this._tempGraphics.RotateTransform(-90);
            }

            // 处理行对齐（考虑缩放）
            bool shouldSetLineAlignmentToCenter = true;
            if (IsOldLineAlignment)
            {
                shouldSetLineAlignmentToCenter = LineSpacingPx < 0;
            }
            if (shouldSetLineAlignmentToCenter)
            {
                if (IsVerticalFont)
                {
                    this._tempGraphics.TranslateTransform((LineSpacingPx * scale) / 2, 0);
                }
                else
                {
                    this._tempGraphics.TranslateTransform(0, (LineSpacingPx * scale) / 2);
                }
            }

            // 处理字符间距（仅对非ASCII字符，考虑缩放）
            if (CharSpacingPx != 0 && charCodePoint > 255)
            {
                if (IsVerticalFont)
                {
                    this._tempGraphics.TranslateTransform(0, (CharSpacingPx * scale) / 2);
                }
                else
                {
                    this._tempGraphics.TranslateTransform((CharSpacingPx * scale) / 2, 0);
                }
            }

            // 创建缩放后的字体（如果需要）
            Font renderFont = this.Font;
            if (SuperSampling != SuperSamplingMode.None)
            {
                float scaledSize = this.Font.Size * scale;
                renderFont = new Font(this.Font.FontFamily, scaledSize, this.Font.Style);
            }

            // 绘制字符
            this._tempGraphics.DrawString(chr.ToString(), renderFont, Brushes.White, 0, 0, _format);

            // 应用SuperSampling处理
            if (SuperSampling != SuperSamplingMode.None)
            {
                using (Bitmap scaledBitmap = ApplySuperSampling(_tempRenderSurface, renderer.Width, renderer.Height))
                {
                    // SuperSampling模式使用优化的阈值策略，保留更多灰度细节
                    int optimizedThreshold = CalculateOptimizedThreshold(scaledBitmap, this.LightThrehold, charCodePoint);
                    renderer.LoadFromBitmap(charCodePoint, scaledBitmap, 0, 0, optimizedThreshold);
                }

                // 释放缩放字体资源
                if (renderFont != this.Font)
                {
                    renderFont.Dispose();
                }
            }
            else
            {
                // 无SuperSampling，使用原有逻辑
                renderer.LoadFromBitmap(charCodePoint, _tempRenderSurface, 0, 0, this.LightThrehold);
            }
        }

        /// <summary>
        /// 为SuperSampling模式计算优化的二值化阈值
        /// 分析图像特征，保留更多抗锯齿细节，对标点符号特殊处理
        /// </summary>
        /// <param name="bitmap">SuperSampling处理后的位图</param>
        /// <param name="userThreshold">用户设置的原始阈值</param>
        /// <param name="charCodePoint">当前字符的Unicode码点</param>
        /// <returns>优化后的阈值</returns>
        private int CalculateOptimizedThreshold(Bitmap bitmap, int userThreshold, int charCodePoint)
        {
            try
            {
                // 检查是否为标点符号
                bool isPunctuation = IsPunctuationCharacter(charCodePoint);

                // 分析图像像素分布
                var histogram = new int[256];
                int totalPixels = bitmap.Width * bitmap.Height;
                int nonBlackPixels = 0;
                int uniqueGrayLevels = 0;

                // 统计灰度分布
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Color pixel = bitmap.GetPixel(x, y);
                        int gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                        histogram[gray]++;
                        if (gray > 0) nonBlackPixels++;
                    }
                }

                // 计算实际的灰度级别数量
                for (int i = 0; i < 256; i++)
                {
                    if (histogram[i] > 0) uniqueGrayLevels++;
                }

                // 如果图像主要是黑色（背景），或者只有很少的灰度级别（可能是1bit模式），使用用户阈值
                if (nonBlackPixels < totalPixels * 0.1 || uniqueGrayLevels < 8)
                {
                    return userThreshold;
                }

                // 标点符号使用保守的阈值策略，避免模糊
                if (isPunctuation)
                {
                    // 对标点符号，主要使用用户阈值，稍微调整以保持清晰
                    int scale = (int)SuperSampling;
                    // 标点符号倾向于使用更高的阈值来保持锐利
                    int conservativeThreshold = userThreshold + (scale - 1) * 5;
                    return Math.Min(200, Math.Max(userThreshold, conservativeThreshold));
                }

                // 对普通文字字符使用改进的Otsu算法
                int otsuThreshold = CalculateOtsuThreshold(histogram, totalPixels);

                // 根据SuperSampling级别调整策略
                int scale2 = (int)SuperSampling;
                double adjustmentFactor = 1.0 - (scale2 - 1) * 0.1; // 高倍SuperSampling使用更低阈值保留细节

                // 混合用户阈值和Otsu阈值，倾向于保留更多细节
                int optimizedThreshold = (int)(otsuThreshold * adjustmentFactor * 0.7 + userThreshold * 0.3);

                // 确保阈值在合理范围内
                return Math.Max(32, Math.Min(192, optimizedThreshold));
            }
            catch
            {
                // 如果计算失败，回退到用户阈值
                return userThreshold;
            }
        }

        /// <summary>
        /// 判断字符是否为标点符号
        /// </summary>
        /// <param name="charCodePoint">字符的Unicode码点</param>
        /// <returns>是否为标点符号</returns>
        private bool IsPunctuationCharacter(int charCodePoint)
        {
            // ASCII标点符号
            if ((charCodePoint >= 0x0020 && charCodePoint <= 0x002F) ||  // 空格和基本标点
                (charCodePoint >= 0x003A && charCodePoint <= 0x0040) ||  // :;<=>?@
                (charCodePoint >= 0x005B && charCodePoint <= 0x0060) ||  // [\]^_`
                (charCodePoint >= 0x007B && charCodePoint <= 0x007E))    // {|}~
            {
                return true;
            }

            // 常用Unicode标点符号范围
            if ((charCodePoint >= 0x2000 && charCodePoint <= 0x206F) ||  // 通用标点
                (charCodePoint >= 0x3000 && charCodePoint <= 0x303F) ||  // CJK标点
                (charCodePoint >= 0xFF00 && charCodePoint <= 0xFFEF))    // 全角标点
            {
                return true;
            }

            // 使用.NET内置的标点判断作为补充
            try
            {
                char ch = (char)charCodePoint;
                return char.IsPunctuation(ch) || char.IsSymbol(ch);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 使用Otsu算法计算最佳二值化阈值
        /// </summary>
        /// <param name="histogram">像素灰度直方图</param>
        /// <param name="totalPixels">总像素数</param>
        /// <returns>最佳阈值</returns>
        private int CalculateOtsuThreshold(int[] histogram, int totalPixels)
        {
            double sum = 0;
            for (int i = 0; i < 256; i++)
                sum += i * histogram[i];

            double sumB = 0;
            int wB = 0;
            double maximum = 0.0;
            int threshold = 0;

            for (int i = 0; i < 256; i++)
            {
                wB += histogram[i];
                if (wB == 0) continue;

                int wF = totalPixels - wB;
                if (wF == 0) break;

                sumB += i * histogram[i];
                double mB = sumB / wB;
                double mF = (sum - sumB) / wF;
                double between = wB * wF * (mB - mF) * (mB - mF);

                if (between > maximum)
                {
                    maximum = between;
                    threshold = i;
                }
            }

            return threshold;
        }

        void IDisposable.Dispose()
        {
            try
            {
                this._tempGraphics?.Dispose();
            }
            catch { }
            try
            {
                this._tempRenderSurface?.Dispose();
            }
            catch { }

        }
    }
}
