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
    // 注意：SuperSampling现在使用bool控制，true=32x终极超采样，false=无超采样

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
        /// 是否启用32x终极超采样，默认为false（无超采样）
        /// </summary>
        public bool EnableUltimateSuperSampling { get; set; } = false;

        private Bitmap _tempRenderSurface;
        private Graphics _tempGraphics;

        // SuperSampling字体缓存
        private Font _cachedSuperSamplingFont;
        private float _cachedSuperSamplingSize;
        private bool _cachedSuperSamplingEnabled;

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
            // 如果启用终极超采样，直接使用目标尺寸（RenderUltimateSuperSampling独立处理）
            int actualWidth = width;
            int actualHeight = height;

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
        /// 终极SuperSampling渲染系统（32x + Bayer抖动 + Gamma校正）
        /// 固定32x超采样，结合16x16 Bayer抖动，实现激光打印级质量
        /// </summary>
        /// <param name="charCodePoint">字符码点</param>
        /// <param name="targetWidth">目标宽度</param>
        /// <param name="targetHeight">目标高度</param>
        /// <returns>1-bit黑白位图</returns>
        private Bitmap RenderUltimateSuperSampling(int charCodePoint, int targetWidth, int targetHeight)
        {
            char chr = (char)charCodePoint;
            const int ULTRA_SCALE = 32; // 32x超采样：内存友好，但仍保持高质量

            // 第一步：创建32x超高分辨率灰度图
            int ultraWidth = targetWidth * ULTRA_SCALE;
            int ultraHeight = targetHeight * ULTRA_SCALE;

            Bitmap ultraBitmap = new Bitmap(ultraWidth, ultraHeight);
            ultraBitmap.SetResolution(96, 96);

            using (Graphics g = Graphics.FromImage(ultraBitmap))
            {
                g.Clear(Color.Black);

                // 设置终极质量渲染参数
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                // 处理边框
                if (RenderBorder)
                {
                    g.DrawRectangle(Pens.White, 0, 0, ultraWidth - 1, ultraHeight - 1);
                }

                // 创建矢量路径
                using (GraphicsPath gp = new GraphicsPath())
                {
                    // 修正DPI换算问题：先转点→像素，再乘scale
                    // 1英寸 = 72点，屏幕96 dpi，所以需要 points * 96/72 转换
                    float emSizeInPoints = this.Font.SizeInPoints;           // 用户原始字体点数
                    float pixelSize = emSizeInPoints * 96f / 72f;            // 转换为像素尺寸
                    float superPixelSize = pixelSize * ULTRA_SCALE;          // 再乘超采样倍数

                    gp.AddString(chr.ToString(), this.Font.FontFamily, (int)this.Font.Style,
                               superPixelSize, PointF.Empty, StringFormat.GenericTypographic);

                    // 亚像素偏移优化：0.125px精度微调
                    using (Matrix matrix = new Matrix())
                    {
                        // 先做亚像素偏移（1/8像素精度）
                        float subPixelOffset = -0.125f * ULTRA_SCALE;
                        matrix.Translate(subPixelOffset, subPixelOffset);

                        // 再做标准缩放变换
                        matrix.Scale(1.0f / ULTRA_SCALE, 1.0f / ULTRA_SCALE, MatrixOrder.Append);

                        // 应用变换处理
                        ApplyUltraVectorTransforms(matrix, targetWidth, targetHeight, ULTRA_SCALE, charCodePoint);

                        // 墨水扩散预收缩（预补偿打印扩散）
                        if (ShouldApplyInkCompensation(charCodePoint))
                        {
                            ApplyInkExpansionCompensation(gp, 0.05f * ULTRA_SCALE); // 0.05mm收缩
                        }

                        gp.Transform(matrix);
                        g.FillPath(Brushes.White, gp);
                    }
                }
            }

            // 第二步：方向敏感平滑（仅对斜线/弯钩字符）
            if (NeedsSmoothCurveProcessing(charCodePoint))
            {
                ApplyDirectionalSmoothing(ultraBitmap);
            }

            // 第三步：32x灰度图 → 16x16 Bayer抖动 → 1-bit
            Bitmap result = ApplyBayerDithering(ultraBitmap, targetWidth, targetHeight, charCodePoint);

            ultraBitmap.Dispose();
            return result;
        }

        /// <summary>
        /// 16x16 Bayer抖动矩阵（经典有序抖动）
        /// </summary>
        private static readonly int[,] BayerMatrix16x16 = {
            {0,128,32,160,8,136,40,168,2,130,34,162,10,138,42,170},
            {192,64,224,96,200,72,232,104,194,66,226,98,202,74,234,106},
            {48,176,16,144,56,184,24,152,50,178,18,146,58,186,26,154},
            {240,112,208,80,248,120,216,88,242,114,210,82,250,122,218,90},
            {12,140,44,172,4,132,36,164,14,142,46,174,6,134,38,166},
            {204,76,236,108,196,68,228,100,206,78,238,110,198,70,230,102},
            {60,188,28,156,52,180,20,148,62,190,30,158,54,182,22,150},
            {252,124,220,92,244,116,212,84,254,126,222,94,246,118,214,86},
            {3,131,35,163,11,139,43,171,1,129,33,161,9,137,41,169},
            {195,67,227,99,203,75,235,107,193,65,225,97,201,73,233,105},
            {51,179,19,147,59,187,27,155,49,177,17,145,57,185,25,153},
            {243,115,211,83,251,123,219,91,241,113,209,81,249,121,217,89},
            {15,143,47,175,7,135,39,167,13,141,45,173,5,133,37,165},
            {207,79,239,111,199,71,231,103,205,77,237,109,197,69,229,101},
            {63,191,31,159,55,183,23,151,61,189,29,157,53,181,21,149},
            {255,127,223,95,247,119,215,87,253,125,221,93,245,117,213,85}
        };

        /// <summary>
        /// 应用16x16 Bayer抖动（32x灰度 → 1-bit）
        /// </summary>
        private Bitmap ApplyBayerDithering(Bitmap grayBitmap, int targetWidth, int targetHeight, int charCodePoint)
        {
            Bitmap result = new Bitmap(targetWidth, targetHeight);
            result.SetResolution(96, 96);

            const int ULTRA_SCALE = 32; // 与RenderUltimateSuperSampling保持一致
            bool isPunctuation = IsPunctuationCharacter(charCodePoint);

            // 计算线性光gamma校正的阈值
            double userThresholdLinear = Math.Pow(this.LightThrehold / 255.0, 2.2);

            // 标点符号额外加粗：阈值-4（相当于加粗1.5%）
            int adjustedThreshold = this.LightThrehold;
            if (isPunctuation)
            {
                adjustedThreshold = Math.Max(16, this.LightThrehold - 4);
            }
            double adjustedThresholdLinear = Math.Pow(adjustedThreshold / 255.0, 2.2);

            // 使用LockBits获取快速像素访问
            var resultData = result.LockBits(new Rectangle(0, 0, targetWidth, targetHeight),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    uint* resultPtr = (uint*)resultData.Scan0;
                    int stride = resultData.Stride / 4;

                    for (int y = 0; y < targetHeight; y++)
                    {
                        uint* row = resultPtr + y * stride;

                        for (int x = 0; x < targetWidth; x++)
                        {
                            // 从32x灰度图采样对应区域的平均灰度
                            double avgGray = SampleUltraRegion(grayBitmap, x, y, ULTRA_SCALE);

                            // 纯黑保护区：灰度<10直接为黑，不参与抖动
                            if (avgGray < 10)
                            {
                                row[x] = 0xFF000000; // 黑色 ARGB
                                continue;
                            }

                            // 线性光空间gamma校正阈值判断
                            double grayLinear = Math.Pow(avgGray / 255.0, 2.2);

                            // 16x16 Bayer抖动判断
                            int bayerX = x % 16;
                            int bayerY = y % 16;
                            int bayerThreshold = BayerMatrix16x16[bayerY, bayerX];

                            // 在线性光空间比较：灰度 vs (阈值 + Bayer扰动)
                            double combinedThreshold = adjustedThresholdLinear + (bayerThreshold / 255.0 - 0.5) * 0.1;

                            // 写入像素：白色或黑色
                            row[x] = grayLinear > combinedThreshold ? 0xFFFFFFFF : 0xFF000000;
                        }
                    }
                }
            }
            finally
            {
                result.UnlockBits(resultData);
            }

            return result;
        }

        /// <summary>
        /// 从32x灰度图采样指定区域的平均灰度
        /// </summary>
        private double SampleUltraRegion(Bitmap ultraBitmap, int targetX, int targetY, int scale)
        {
            int startX = targetX * scale;
            int startY = targetY * scale;
            int endX = Math.Min(startX + scale, ultraBitmap.Width);
            int endY = Math.Min(startY + scale, ultraBitmap.Height);

            double totalGray = 0;
            int pixelCount = 0;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    Color pixel = ultraBitmap.GetPixel(x, y);
                    int gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    totalGray += gray;
                    pixelCount++;
                }
            }

            return pixelCount > 0 ? totalGray / pixelCount : 0;
        }

        /// <summary>
        /// 应用超高精度矢量变换（用于32x终极采样）
        /// </summary>
        /// <param name="matrix">变换矩阵</param>
        /// <param name="targetWidth">目标宽度</param>
        /// <param name="targetHeight">目标高度</param>
        /// <param name="ultraScale">超高精度缩放比例（固定32）</param>
        /// <param name="charCodePoint">字符码点</param>
        private void ApplyUltraVectorTransforms(Matrix matrix, int targetWidth, int targetHeight, int ultraScale, int charCodePoint)
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
        /// 判断字符是否需要墨水扩散补偿
        /// </summary>
        /// <param name="charCodePoint">字符码点</param>
        /// <returns>是否需要墨水补偿</returns>
        private bool ShouldApplyInkCompensation(int charCodePoint)
        {
            // 细线条字符（如 1, i, l, I 等）不需要补偿，避免过细消失
            if (charCodePoint <= 127)
            {
                char ch = (char)charCodePoint;
                if ("1iIl|!.,:;".Contains(ch))
                    return false;
            }

            // 标点符号通常较细，不需要补偿
            if (IsPunctuationCharacter(charCodePoint))
                return false;

            // 对于中等粗细的笔画字符进行补偿
            return true;
        }

        /// <summary>
        /// 应用墨水扩散预收缩补偿
        /// 通过GraphicsPath.Widen实现0.05mm精度的路径内缩
        /// </summary>
        /// <param name="path">要处理的GraphicsPath</param>
        /// <param name="shrinkAmount">收缩量（像素）</param>
        private void ApplyInkExpansionCompensation(GraphicsPath path, float shrinkAmount)
        {
            try
            {
                if (shrinkAmount <= 0) return;

                // 创建收缩笔尖（负值表示内缩）
                using (Pen shrinkPen = new Pen(Color.Black, -shrinkAmount * 2))
                {
                    shrinkPen.LineJoin = LineJoin.Round;
                    shrinkPen.StartCap = LineCap.Round;
                    shrinkPen.EndCap = LineCap.Round;

                    // 对路径进行内缩处理
                    path.Widen(shrinkPen);
                }
            }
            catch
            {
                // 如果Widen操作失败（例如路径太细），保持原路径不变
            }
        }

        /// <summary>
        /// 应用方向敏感的平滑处理
        /// 对斜线/弯钩字符进行1x3/3x1高斯滤波
        /// </summary>
        /// <param name="bitmap">要处理的32x超高分辨率位图</param>
        private void ApplyDirectionalSmoothing(Bitmap bitmap)
        {
            try
            {
                // 简化的方向敏感平滑：3x3高斯核
                float[,] gaussianKernel = {
                    {0.0625f, 0.125f, 0.0625f},
                    {0.125f,  0.25f,  0.125f},
                    {0.0625f, 0.125f, 0.0625f}
                };

                int width = bitmap.Width;
                int height = bitmap.Height;

                // 创建临时数组存储灰度值
                byte[,] grayData = new byte[height, width];
                byte[,] smoothedData = new byte[height, width];

                // 提取灰度数据
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color pixel = bitmap.GetPixel(x, y);
                        grayData[y, x] = (byte)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    }
                }

                // 应用高斯滤波（只对非横竖线区域）
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        // 检测是否为纯横线或纯竖线区域
                        bool isHorizontalLine = IsHorizontalLineRegion(grayData, x, y);
                        bool isVerticalLine = IsVerticalLineRegion(grayData, x, y);

                        if (isHorizontalLine || isVerticalLine)
                        {
                            // 横竖线保持锐利，不进行平滑
                            smoothedData[y, x] = grayData[y, x];
                        }
                        else
                        {
                            // 斜线/弯钩区域进行平滑
                            float sum = 0;
                            for (int ky = -1; ky <= 1; ky++)
                            {
                                for (int kx = -1; kx <= 1; kx++)
                                {
                                    sum += grayData[y + ky, x + kx] * gaussianKernel[ky + 1, kx + 1];
                                }
                            }
                            smoothedData[y, x] = (byte)Math.Min(255, Math.Max(0, sum));
                        }
                    }
                }

                // 将平滑后的数据写回位图
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        byte gray = smoothedData[y, x];
                        Color smoothColor = Color.FromArgb(gray, gray, gray);
                        bitmap.SetPixel(x, y, smoothColor);
                    }
                }
            }
            catch
            {
                // 如果平滑处理失败，保持原图不变
            }
        }

        /// <summary>
        /// 检测是否为水平线区域
        /// </summary>
        private bool IsHorizontalLineRegion(byte[,] grayData, int x, int y)
        {
            // 检查垂直方向的梯度是否显著大于水平方向
            int verticalGradient = Math.Abs(grayData[y - 1, x] - grayData[y + 1, x]);
            int horizontalGradient = Math.Abs(grayData[y, x - 1] - grayData[y, x + 1]);

            return verticalGradient > horizontalGradient * 2 && verticalGradient > 30;
        }

        /// <summary>
        /// 检测是否为垂直线区域
        /// </summary>
        private bool IsVerticalLineRegion(byte[,] grayData, int x, int y)
        {
            // 检查水平方向的梯度是否显著大于垂直方向
            int horizontalGradient = Math.Abs(grayData[y, x - 1] - grayData[y, x + 1]);
            int verticalGradient = Math.Abs(grayData[y - 1, x] - grayData[y + 1, x]);

            return horizontalGradient > verticalGradient * 2 && horizontalGradient > 30;
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
            if (!EnableUltimateSuperSampling)
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

            // SuperSampling处理流程
            if (EnableUltimateSuperSampling)
            {
                // 使用终极SuperSampling系统：32x固定采样 + 16x16 Bayer抖动
                using (Bitmap ultimateBitmap = RenderUltimateSuperSampling(charCodePoint, renderer.Width, renderer.Height))
                {
                    // 终极SuperSampling已经包含所有优化，直接使用用户阈值
                    renderer.LoadFromBitmap(charCodePoint, ultimateBitmap, 0, 0, this.LightThrehold);
                }
            }
            else
            {
                // 无SuperSampling，使用传统渲染
                this._tempGraphics.Clear(Color.Black);
                this._tempGraphics.ResetTransform();

                // 绘制边框
                if (RenderBorder)
                {
                    this._tempGraphics.DrawRectangle(Pens.White, 0, 0, renderer.Width - 1, renderer.Height - 1);
                }

                // 处理垂直字体变换
                if (IsVerticalFont)
                {
                    this._tempGraphics.TranslateTransform(0, renderer.Height);
                    this._tempGraphics.RotateTransform(-90);
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
                        this._tempGraphics.TranslateTransform(LineSpacingPx / 2, 0);
                    }
                    else
                    {
                        this._tempGraphics.TranslateTransform(0, LineSpacingPx / 2);
                    }
                }

                // 处理字符间距（仅对非ASCII字符）
                if (CharSpacingPx != 0 && charCodePoint > 255)
                {
                    if (IsVerticalFont)
                    {
                        this._tempGraphics.TranslateTransform(0, CharSpacingPx / 2);
                    }
                    else
                    {
                        this._tempGraphics.TranslateTransform(CharSpacingPx / 2, 0);
                    }
                }

                // 绘制字符
                this._tempGraphics.DrawString(chr.ToString(), this.Font, Brushes.White, 0, 0, _format);

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
            // 32x终极超采样固定补偿
            int baseCompensation = EnableUltimateSuperSampling ? 30 : 0; // 32x采样：终极补偿

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
