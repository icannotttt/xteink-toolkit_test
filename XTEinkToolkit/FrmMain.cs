using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XTEinkTools;

namespace XTEinkToolkit
{
    // TODO: 允许字体旋转
    // TODO: 允许横屏预览
    // TODO: 允许加载TTF字体文件而无需安装
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
            try
            {
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch {
                this.Icon = SystemIcons.Application;
            }
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            previewSurface.ScaleMode = XTEinkToolkit.Controls.CanvasControl.RenderScaleMode.PreferCenter;
            previewSurface.CanvasSize = new System.Drawing.Size(480, 800);
            DoPreview();
        }

        private void btnSelectFont_Click(object sender, EventArgs e)
        {
            fontDialog.Font = lblFontSource.Font;
            if (fontDialog.ShowDialog(this) == DialogResult.OK)
            {
                lblFontSource.Font = fontDialog.Font;
                lblFontSource.Text = fontDialog.Font.Name + "\r\n中国智造，惠及全球ABCabc123";
                numFontSizePt.ValueChanged -= numFontSizePt_ValueChanged;
                numFontSizePt.Value = (decimal)fontDialog.Font.Size;
                numFontSizePt.ValueChanged += numFontSizePt_ValueChanged;

            }
            DoPreview();
        }

        private void btnPreview_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DoPreviewDirect();
        }

        private void numFontSizePt_ValueChanged(object sender, EventArgs e)
        {
            float newSize = (float)numFontSizePt.Value;
            Font oldFont = lblFontSource.Font;
            lblFontSource.Font = new Font(oldFont.FontFamily, newSize, oldFont.Style);
            oldFont.Dispose();
            DoPreview();
        }
        private void numFontGamma_ValueChanged(object sender, EventArgs e)
        {
            DoPreview();
        }

        private void numLineSpacing_ValueChanged(object sender, EventArgs e)
        {

            DoPreview();
        }


        private void chkRenderGridFit_CheckedChanged(object sender, EventArgs e)
        {
            numFontGamma.Enabled = chkRenderAntiAltas.Checked;
            DoPreview();
        }


        void DoPreview()
        {
            debounceCd = 18;
            debounceTimer.Enabled = true;
        }

        private int debounceCd = 0;
        private void debounceTimer_Tick(object sender, EventArgs e)
        {
            debounceCd--;
            if (debounceCd < 0)
            {
                debounceTimer.Enabled = false;
                DoPreviewDirect();
            }
        }
        string previewString = Properties.Resources.previewTexts;
        void DoPreviewDirect()
        {
            debounceTimer.Enabled = false;
            btnPreview.Enabled = false;
            btnPreview.Text = "渲染中...";
            Application.DoEvents();
            using (XTEinkFontRenderer renderer = new XTEinkFontRenderer())
            {
                renderer.Font = lblFontSource.Font;
                renderer.LineSpacingPx = (int)numLineSpacing.Value;
                renderer.LightThrehold = numFontGamma.Value;

                XTEinkFontRenderer.AntiAltasMode[] aaModesEnum = new XTEinkFontRenderer.AntiAltasMode[] {
                    XTEinkFontRenderer.AntiAltasMode.System1Bit, // 0x0
                    XTEinkFontRenderer.AntiAltasMode.System1BitGridFit, // 0x1
                    XTEinkFontRenderer.AntiAltasMode.SystemAntiAltas, // 0x2
                    XTEinkFontRenderer.AntiAltasMode.SystemAntiAltasGridFit //0x3
                };
                var whichAAMode = (chkRenderAntiAltas.Checked ? 2 : 0) + (chkRenderGridFit.Checked ? 1 : 0);
                renderer.AAMode = aaModesEnum[whichAAMode];
                Size fontRenderSize = renderer.GetFontRenderSize();

                XTEinkFontBinary fontBinary = new XTEinkFontBinary(fontRenderSize.Width, fontRenderSize.Height);

                Utility.RenderPreview(previewString, fontBinary, renderer, previewSurface.GetGraphics(), previewSurface.CanvasSize);
                previewSurface.Commit();
            }
            GC.Collect();
            btnPreview.Enabled = true;
            btnPreview.Text = "查看预览";
        }

        private string GetRenderTargetSize()
        {
            using (XTEinkFontRenderer renderer = new XTEinkFontRenderer())
            {
                renderer.Font = lblFontSource.Font;
                renderer.LineSpacingPx = (int)numLineSpacing.Value;
                renderer.LightThrehold = numFontGamma.Value;

                XTEinkFontRenderer.AntiAltasMode[] aaModesEnum = new XTEinkFontRenderer.AntiAltasMode[] {
                    XTEinkFontRenderer.AntiAltasMode.System1Bit, // 0x0
                    XTEinkFontRenderer.AntiAltasMode.System1BitGridFit, // 0x1
                    XTEinkFontRenderer.AntiAltasMode.SystemAntiAltas, // 0x2
                    XTEinkFontRenderer.AntiAltasMode.SystemAntiAltasGridFit //0x3
                };
                var whichAAMode = (chkRenderAntiAltas.Checked ? 2 : 0) + (chkRenderGridFit.Checked ? 1 : 0);
                renderer.AAMode = aaModesEnum[whichAAMode];
                Size fontRenderSize = renderer.GetFontRenderSize();
                return fontRenderSize.Width+"×"+fontRenderSize.Height;
            }
        }

        private void btnDoGeneration_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "BIN字体文件|*." + GetRenderTargetSize() + ".bin";
            if (sfd.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            string savePath = sfd.FileName;
            btnDoGeneration.Enabled = false;
            EssentialDialogs.ProgressDialog.RunWork(this, (ps) =>
            {
                ps.SetMessage("正在渲染字体...");
                using (XTEinkFontRenderer renderer = new XTEinkFontRenderer())
                {
                    Invoke(new Action(() =>
                    {

                        renderer.Font = lblFontSource.Font;
                        renderer.LineSpacingPx = (int)numLineSpacing.Value;
                        renderer.LightThrehold = numFontGamma.Value;

                        XTEinkFontRenderer.AntiAltasMode[] aaModesEnum = new XTEinkFontRenderer.AntiAltasMode[] {
                        XTEinkFontRenderer.AntiAltasMode.System1Bit, // 0x0
                        XTEinkFontRenderer.AntiAltasMode.System1BitGridFit, // 0x1
                        XTEinkFontRenderer.AntiAltasMode.SystemAntiAltas, // 0x2
                        XTEinkFontRenderer.AntiAltasMode.SystemAntiAltasGridFit //0x3
                    };
                        var whichAAMode = (chkRenderAntiAltas.Checked ? 2 : 0) + (chkRenderGridFit.Checked ? 1 : 0);
                        renderer.AAMode = aaModesEnum[whichAAMode];
                    }));
                    Size fontRenderSize = renderer.GetFontRenderSize();

                    XTEinkFontBinary fontBinary = new XTEinkFontBinary(fontRenderSize.Width, fontRenderSize.Height);
                    var maxCharRange = 65536;
                    for (int i = 0; i < maxCharRange; i++)
                    {
                        try
                        {
                            ps.SetProgress(i, maxCharRange);
                            ps.SetMessage($"正在渲染字体...({i}/{maxCharRange})");
                            renderer.RenderFont(i, fontBinary);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                    using(var stream = File.Create(savePath))
                    {
                        fontBinary.saveToFile(stream);
                    }
                }

            }, (err) =>
            {
                btnDoGeneration.Enabled = true;
                if (err != null)
                {
                    MessageBox.Show(this,"渲染字体时出错：\r\n" + err.GetType().FullName + ": " + err.Message, "渲染字体时发生错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show(this,"字体文件导出成功！","到处完成",MessageBoxButtons.OK,MessageBoxIcon.Information);
                }
            });
        }
    }
}
