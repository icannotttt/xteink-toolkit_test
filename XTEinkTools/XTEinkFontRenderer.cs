using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        x8 = 8,          // 8倍超采样（高质量模式）
        x16 = 16,        // 16倍超采样（超高质量）
        x32 = 32,        // 32倍超采样（极致质量）
        x64 = 64         // 64倍超采样（终极质量）
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

        // SuperSampling字体缓存
        private Font _cachedSuperSamplingFont;
        private float _cachedSuperSamplingSize;
        private SuperSamplingMode _cachedSuperSamplingMode;

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

            // ===== 1. 超采样前：渲染大字 ===================================
            this._tempGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            this._tempGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            this._tempGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; // 最高质量插值
            this._tempGraphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            // 注意：TextRenderingHint 通过 syncSettings() 根据用户AAMode动态设置
        }

        /// <summary>
        /// 使用矢量路径进行SuperSampling渲染（高效方案）
        /// 避免超大Bitmap和二次插值，直接从矢量到目标尺寸
        /// </summary>
        /// <param name="charCodePoint">字符码点</param>
        /// <param name="targetWidth">目标宽度</param>
        /// <param name="targetHeight">目标高度</param>
        /// <returns>SuperSampling渲染后的位图</returns>
        private Bitmap RenderVectorSuperSampling(int charCodePoint, int targetWidth, int targetHeight)
        {
            char chr = (char)charCodePoint;
            int scale = (int)SuperSampling;

            // 创建目标尺寸的位图（恒定内存占用）
            Bitmap result = new Bitmap(targetWidth, targetHeight);
            result.SetResolution(96, 96);

            using (Graphics g = Graphics.FromImage(result))
            {
                g.Clear(Color.Black);

                // 设置高质量矢量渲染
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // 处理边框
                if (RenderBorder)
                {
                    g.DrawRectangle(Pens.White, 0, 0, targetWidth - 1, targetHeight - 1);
                }

                // 创建矢量路径
                using (GraphicsPath gp = new GraphicsPath())
                {
                    // 将字符转换为矢量路径，使用SuperSampling倍数的字体大小
                    float vectorFontSize = this.Font.SizeInPoints * scale;
                    gp.AddString(chr.ToString(), this.Font.FontFamily, (int)this.Font.Style,
                               vectorFontSize, PointF.Empty, StringFormat.GenericTypographic);

                    // 创建变换矩阵：缩小到目标尺寸
                    using (Matrix matrix = new Matrix(1.0f / scale, 0, 0, 1.0f / scale, 0, 0))
                    {
                        // 应用坐标变换处理
                        ApplyVectorTransforms(matrix, targetWidth, targetHeight, scale, charCodePoint);

                        // 应用变换到路径
                        gp.Transform(matrix);

                        // 填充矢量路径到位图
                        g.FillPath(Brushes.White, gp);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 应用矢量变换（垂直字体、间距、对齐等）
        /// </summary>
        /// <param name="matrix">变换矩阵</param>
        /// <param name="targetWidth">目标宽度</param>
        /// <param name="targetHeight">目标高度</param>
        /// <param name="scale">SuperSampling缩放比例</param>
        /// <param name="charCodePoint">字符码点</param>
        private void ApplyVectorTransforms(Matrix matrix, int targetWidth, int targetHeight, int scale, int charCodePoint)
        {
            // 处理垂直字体变换
            if (IsVerticalFont)
            {
                matrix.Translate(0, targetHeight);
                matrix.Rotate(-90);
            }

            // 处理行对齐
            bool shouldSetLineAlignmentToCenter = true;
            if (IsOldLineAlignment)
            {
                shouldSetLineAlignmentToCenter = LineSpacingPx < 0;
            }
            if (shouldSetLineAlignmentToCenter)
            {
                if (IsVerticalFont)
                {
                    matrix.Translate(LineSpacingPx / 2.0f, 0);
                }
                else
                {
                    matrix.Translate(0, LineSpacingPx / 2.0f);
                }
            }

            // 处理字符间距（仅对非ASCII字符）
            if (CharSpacingPx != 0 && (char)charCodePoint > 255)
            {
                if (IsVerticalFont)
                {
                    matrix.Translate(0, CharSpacingPx / 2.0f);
                }
                else
                {
                    matrix.Translate(CharSpacingPx / 2.0f, 0);
                }
            }
        }

        /// <summary>
        /// 应用SuperSampling缩放处理
        /// SuperSampling分为两个阶段：
        /// 1. 字体渲染阶段：在高分辨率画布上渲染字体（需要抗锯齿）
        /// 2. 缩放阶段：将高分辨率位图缩放到目标尺寸（避免过度平滑）
        /// </summary>
        /// <param name="sourceBitmap">源位图</param>
        /// <param name="targetWidth">目标宽度</param>
        /// <param name="targetHeight">目标高度</param>
        /// <param name="charCodePoint">字符码点，用于选择最佳插值算法</param>
        /// <returns>缩放后的位图</returns>
        private Bitmap ApplySuperSampling(Bitmap sourceBitmap, int targetWidth, int targetHeight, int charCodePoint)
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
                    // ===== 2. 超采样后：缩图回目标尺寸 ==============================
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half; // 避免半像素偏移

                    // 统一使用HighQualityBilinear保证亮度一致性
                    // 虽然可能牺牲部分锐利度，但优先保证字符间亮度统一
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
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

            // 创建缩放后的字体（如果需要），使用缓存避免重复创建
            Font renderFont = this.Font;
            if (SuperSampling != SuperSamplingMode.None)
            {
                float scaledSize = this.Font.Size * scale;

                // 检查缓存是否有效
                if (_cachedSuperSamplingFont == null ||
                    _cachedSuperSamplingSize != scaledSize ||
                    _cachedSuperSamplingMode != SuperSampling ||
                    !_cachedSuperSamplingFont.FontFamily.Equals(this.Font.FontFamily) ||
                    _cachedSuperSamplingFont.Style != this.Font.Style)
                {
                    // 缓存无效，创建新字体并更新缓存
                    _cachedSuperSamplingFont?.Dispose();
                    _cachedSuperSamplingFont = new Font(this.Font.FontFamily, scaledSize, this.Font.Style);
                    _cachedSuperSamplingSize = scaledSize;
                    _cachedSuperSamplingMode = SuperSampling;
                }

                renderFont = _cachedSuperSamplingFont;
            }

            // 绘制字符
            this._tempGraphics.DrawString(chr.ToString(), renderFont, Brushes.White, 0, 0, _format);

            // SuperSampling处理流程
            if (SuperSampling != SuperSamplingMode.None)
            {
                // 使用矢量SuperSampling：直接从矢量到目标尺寸，避免超大Bitmap
                using (Bitmap vectorBitmap = RenderVectorSuperSampling(charCodePoint, renderer.Width, renderer.Height))
                {
                    // SuperSampling模式下，对特殊字符进行算法优化
                    int optimizedThreshold = CalculateOptimizedThreshold(vectorBitmap, this.LightThrehold, charCodePoint);
                    renderer.LoadFromBitmap(charCodePoint, vectorBitmap, 0, 0, optimizedThreshold);
                }
            }
            else
            {
                // 无SuperSampling，直接使用用户阈值
                renderer.LoadFromBitmap(charCodePoint, _tempRenderSurface, 0, 0, this.LightThrehold);
            }
        }

        /// <summary>
        /// 为SuperSampling模式计算优化的二值化阈值
        /// 对标点符号和复杂字符进行专门优化，但不依赖用户权重
        /// </summary>
        /// <param name="bitmap">SuperSampling处理后的位图</param>
        /// <param name="userThreshold">用户设置的原始阈值</param>
        /// <param name="charCodePoint">当前字符的Unicode码点</param>
        /// <returns>优化后的阈值</returns>
        private int CalculateOptimizedThreshold(Bitmap bitmap, int userThreshold, int charCodePoint)
        {
            try
            {
                // 检查字符类型
                bool isPunctuation = IsPunctuationCharacter(charCodePoint);
                bool isComplexCharacter = IsComplexCharacter(charCodePoint);

                // 分析图像像素分布
                var histogram = new int[256];
                int totalPixels = bitmap.Width * bitmap.Height;
                int nonBlackPixels = 0;

                // 统计灰度分布 - 使用快速像素访问
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                try
                {
                    unsafe
                    {
                        byte* ptr = (byte*)bitmapData.Scan0;
                        int stride = bitmapData.Stride;

                        for (int y = 0; y < bitmap.Height; y++)
                        {
                            byte* row = ptr + (y * stride);
                            for (int x = 0; x < bitmap.Width; x++)
                            {
                                // BGR格式
                                int b = row[x * 3];
                                int g = row[x * 3 + 1];
                                int r = row[x * 3 + 2];

                                int gray = (int)(r * 0.299 + g * 0.587 + b * 0.114);
                                histogram[gray]++;
                                if (gray > 0) nonBlackPixels++;
                            }
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                // 如果图像主要是黑色（背景），直接使用用户阈值
                if (nonBlackPixels < totalPixels * 0.05)
                {
                    return userThreshold;
                }

                // 统一亮度处理：所有字符都使用完全相同的补偿策略，确保亮度一致性
                int unifiedThreshold = CompensateForSuperSamplingBrightening(userThreshold);
                return unifiedThreshold;
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
            if ((charCodePoint >= 0x0021 && charCodePoint <= 0x002F) ||  // !"#$%&'()*+,-./
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
                if (charCodePoint <= 0xFFFF)
                {
                    char ch = (char)charCodePoint;
                    return char.IsPunctuation(ch) || char.IsSymbol(ch);
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// 判断字符是否为复杂字符（基于Unicode范围和已知复杂字符）
        /// </summary>
        /// <param name="charCodePoint">字符的Unicode码点</param>
        /// <returns>是否为复杂字符</returns>
        private bool IsComplexCharacter(int charCodePoint)
        {
            // 一些已知的特别复杂的汉字
            int[] complexChars = {
                0x526A, // 剪
                0x9F52, // 齒
                0x9F61, // 齡
                0x8B9E, // 謞
                0x8B93, // 讓
                0x9EBC, // 麼
                0x7E41, // 繁
                0x9F77, // 靷
                0x9F72, // 靲
                0x9F78, // 靸
                0x8056, // 聖
                0x9F6C, // 齬
                0x9F50, // 齐
                0x8B4F  // 譏
            };

            // 检查是否为已知复杂字符
            for (int i = 0; i < complexChars.Length; i++)
            {
                if (charCodePoint == complexChars[i])
                    return true;
            }

            // 一些复杂字符的Unicode范围
            // CJK统一汉字扩展A区 (U+3400-U+4DBF) - 通常比较复杂
            if (charCodePoint >= 0x3400 && charCodePoint <= 0x4DBF)
                return true;

            // CJK兼容汉字 (U+F900-U+FAFF) - 通常是复杂的异体字
            if (charCodePoint >= 0xF900 && charCodePoint <= 0xFAFF)
                return true;

            return false;
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

        /// <summary>
        /// 补偿SuperSampling导致的亮度损失
        /// SuperSampling缩放过程会让字体变浅，需要降低阈值来补偿
        /// </summary>
        /// <param name="userThreshold">用户设置的原始阈值</param>
        /// <returns>补偿后的阈值</returns>
        private int CompensateForSuperSamplingBrightening(int userThreshold)
        {
            // 基础补偿：根据SuperSampling级别
            int baseCompensation = 0;
            switch (SuperSampling)
            {
                case SuperSamplingMode.x2:
                    baseCompensation = 6;   // 2倍采样：轻微补偿
                    break;
                case SuperSamplingMode.x4:
                    baseCompensation = 12;  // 4倍采样：中等补偿
                    break;
                case SuperSamplingMode.x8:
                    baseCompensation = 18;  // 8倍采样：适度补偿
                    break;
                case SuperSamplingMode.x16:
                    baseCompensation = 22;  // 16倍采样：较大补偿
                    break;
                case SuperSamplingMode.x32:
                    baseCompensation = 26;  // 32倍采样：高级补偿
                    break;
                case SuperSamplingMode.x64:
                    baseCompensation = 30;  // 64倍采样：终极补偿
                    break;
                default:
                    baseCompensation = 0;
                    break;
            }

            // 应用补偿：降低阈值让更多像素保持黑色
            int compensatedThreshold = userThreshold - baseCompensation;

            // 确保阈值在合理范围内
            compensatedThreshold = Math.Max(20, Math.Min(235, compensatedThreshold));

            return compensatedThreshold;
        }

        /// <summary>
        /// 判断字符是否需要平滑曲线处理
        /// 包含弯钩、撇捺、圆弧等曲线笔画的字符需要使用Bilinear插值避免变形
        /// </summary>
        /// <param name="charCodePoint">字符的Unicode码点</param>
        /// <returns>是否需要平滑曲线处理</returns>
        private bool NeedsSmoothCurveProcessing(int charCodePoint)
        {
            // ASCII字符中包含曲线的字符
            if (charCodePoint <= 127)
            {
                char ch = (char)charCodePoint;
                // 包含曲线的英文字母和数字
                if ("036689BCDGJOPQRSabcdegopqsuy".Contains(ch))
                    return true;
                // 包含曲线的标点符号
                if ("()[]{}@&".Contains(ch))
                    return true;
                return false;
            }

            // 中文字符中常见的包含明显曲线笔画的字符
            int[] curveChars = {
                // 常见的包含弯钩的字符
                0x4F60, // 你 - 竖弯钩
                0x6211, // 我 - 斜钩
                0x4E86, // 了 - 亅钩
                0x5728, // 在 - 横折钩
                0x4E0D, // 不 - 竖钩
                0x53EF, // 可 - 竖钩
                0x662F, // 是 - 竖弯钩
                0x8FD9, // 这 - 竖弯钩
                0x90A3, // 那 - 竖弯钩
                0x5C31, // 就 - 横折钩
                0x53BB, // 去 - 横钩
                0x6CA1, // 没 - 竖弯钩
                0x8BF4, // 说 - 竖弯钩

                // 包含撇捺等曲线的字符
                0x4EBA, // 人 - 撇捺
                0x5929, // 天 - 撇捺
                0x5927, // 大 - 撇捺
                0x592A, // 太 - 撇捺
                0x5C0F, // 小 - 撇点
                0x6587, // 文 - 撇点
                0x529B, // 力 - 横折钩
                0x4E5D, // 九 - 横折弯钩
                0x4E38, // 丸 - 横折钩

                // 包含圆弧或曲线较多的字符
                0x56FD, // 国 - 内含曲线
                0x56DE, // 回 - 内含曲线
                0x56E0, // 因 - 内含曲线
                0x5706, // 圆 - 明显曲线
                0x5708, // 圈 - 明显曲线
                0x7403, // 球 - 包含曲线部分
            };

            // 检查是否为已知包含曲线的字符
            for (int i = 0; i < curveChars.Length; i++)
            {
                if (charCodePoint == curveChars[i])
                    return true;
            }

            // 对于其他中文字符，使用笔画特征判断
            if (charCodePoint >= 0x4E00 && charCodePoint <= 0x9FFF) // CJK统一汉字
            {
                // 对于复杂字符，倾向于使用平滑处理
                if (IsComplexCharacter(charCodePoint))
                    return true;

                // 默认情况下，中文字符可能包含曲线，使用保守策略
                // 但为了保持性能，只对常见的弯钩字符特殊处理
                return false;
            }

            // 其他Unicode字符（如全角标点）可能也需要平滑处理
            if (charCodePoint >= 0xFF00 && charCodePoint <= 0xFFEF) // 全角字符
            {
                return true; // 全角字符通常需要平滑处理
            }

            return false;
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
            try
            {
                _cachedSuperSamplingFont?.Dispose();
            }
            catch { }
        }
    }
}
