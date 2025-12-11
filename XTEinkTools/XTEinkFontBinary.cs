using System;
using System.IO;

namespace XTEinkTools
{
    /// <summary>
    /// 阅星曈字体二进制文件的数据结构，提供对每个字形像素层的访问（支持4灰阶）
    /// </summary>
    public class XTEinkFontBinary
    {
        // 灰阶配置（2bit = 4级灰阶）
        private const int GRAY_LEVELS = 4;          // 4级灰阶
        private const int GRAY_LEVEL_BITS = 2;      // 每个像素占用2bit

        private byte[] fontbin;
        private int width;
        private int height;
        private int widthByte;  // 每行占用的字节数
        private int charByte;   // 每个字符占用的总字节数
        private const int totalChar = 0x10000;      // 支持的总字符数（Unicode基本平面）

        public int Width { get => width; }
        public int Height { get => height; }

        /// <summary>
        /// 以指定的宽高在内存里新建一个二进制文件
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public XTEinkFontBinary(int width, int height)
        {
            this.width = width;
            this.height = height;

            // 计算每行需要的字节数（2bit per pixel）
            widthByte = (width * GRAY_LEVEL_BITS + 7) / 8;  // 向上取整
            charByte = widthByte * height;

            // 初始化字体二进制数组
            fontbin = new byte[charByte * totalChar];
        }

        /// <summary>
        /// 将输入的字体名称包装成正确的文件名
        /// </summary>
        /// <param name="title">字体名称</param>
        /// <returns></returns>
        public string GetSuggestedFileName(string title)
        {
            return $"{title} {width}×{height}.bin";
        }

        /// <summary>
        /// 从指定的二进制流加载字体数据
        /// </summary>
        /// <param name="stream"></param>
        public void loadFromFile(Stream stream)
        {
            int pos = 0;
            int len = 0;
            byte[] buf = new byte[1024];
            while ((len = stream.Read(buf, 0, buf.Length)) > 0)
            {
                Array.Copy(buf, 0, fontbin, pos, len);
                pos += len;
            }
        }

        /// <summary>
        /// 将内存中的字体二进制保存到流
        /// </summary>
        /// <param name="stream"></param>
        public void saveToFile(Stream stream)
        {
            stream.Write(fontbin, 0, fontbin.Length);
        }

        private int GetFirstByte(int charCode)
        {
            return charCode * charByte;
        }

        /// <summary>
        /// 获取某个字形的像素灰阶值（0-3）
        /// </summary>
        /// <param name="charCode">Unicode编码</param>
        /// <param name="x">横坐标</param>
        /// <param name="y">纵坐标</param>
        /// <returns>灰阶值（0-3）</returns>
        public int GetPixel(int charCode, int x, int y)
        {
            var pos = GetPixelOffset(charCode, x, y);
            return GetBitAt(fontbin[pos.index], pos.pos);
        }

        /// <summary>
        /// 设置某个字形的像素灰阶值（0-3）
        /// </summary>
        /// <param name="charCode">Unicode编码</param>
        /// <param name="x">横坐标</param>
        /// <param name="y">纵坐标</param>
        /// <param name="grayLevel">灰阶值（0-3）</param>
        public void SetPixel(int charCode, int x, int y, int grayLevel)
        {
            // 确保灰阶值在有效范围内
            grayLevel = Math.Clamp(grayLevel, 0, GRAY_LEVELS - 1);
            
            var pos = GetPixelOffset(charCode, x, y);
            fontbin[pos.index] = SetBitAt(fontbin[pos.index], pos.pos, grayLevel);
        }

        /// <summary>
        /// 计算像素在二进制数组中的位置
        /// </summary>
        private (int index, int pos) GetPixelOffset(int charCode, int x, int y)
        {
            int fb = GetFirstByte(charCode);
            fb += y * widthByte;                     // 计算行偏移
            int byteIndex = x / (8 / GRAY_LEVEL_BITS);  // 计算字节索引（每字节存4个像素）
            fb += byteIndex;
            int pixelPos = x % (8 / GRAY_LEVEL_BITS);   // 计算字节内的像素位置（0-3）
            return (fb, pixelPos);
        }

        // 2bit像素操作掩码（每字节4个像素）
        private readonly byte[] bitMask = new byte[] { 0xC0, 0x30, 0x0C, 0x03 }; // 11000000, 00110000, 00001100, 00000011

        /// <summary>
        /// 从字节中提取2bit灰阶值
        /// </summary>
        private int GetBitAt(byte b, int pixelPos)
        {
            return (b & bitMask[pixelPos]) >> (6 - pixelPos * GRAY_LEVEL_BITS);
        }

        /// <summary>
        /// 向字节中写入2bit灰阶值
        /// </summary>
        private byte SetBitAt(byte b, int pixelPos, int grayLevel)
        {
            // 清除原有位后设置新值
            return (byte)((b & ~bitMask[pixelPos]) | (grayLevel << (6 - pixelPos * GRAY_LEVEL_BITS)));
        }
    }
}
