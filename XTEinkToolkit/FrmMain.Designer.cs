namespace XTEinkToolkit
{
    partial class FrmMain
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnPreview = new System.Windows.Forms.LinkLabel();
            this.previewSurface = new XTEinkToolkit.Controls.CanvasControl();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.chkRenderGridFit = new System.Windows.Forms.CheckBox();
            this.chkRenderAntiAltas = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.numFontGamma = new System.Windows.Forms.TrackBar();
            this.label4 = new System.Windows.Forms.Label();
            this.numLineSpacing = new System.Windows.Forms.NumericUpDown();
            this.numFontSizePt = new System.Windows.Forms.NumericUpDown();
            this.btnSelectFont = new System.Windows.Forms.Button();
            this.lblFontSource = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btnDoGeneration = new System.Windows.Forms.Button();
            this.fontDialog = new System.Windows.Forms.FontDialog();
            this.debounceTimer = new System.Windows.Forms.Timer(this.components);
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFontGamma)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLineSpacing)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFontSizePt)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(8, 8);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(849, 860);
            this.panel1.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnPreview);
            this.groupBox1.Controls.Add(this.previewSurface);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(274, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(8);
            this.groupBox1.Size = new System.Drawing.Size(575, 860);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "字体效果预览";
            // 
            // btnPreview
            // 
            this.btnPreview.AutoSize = true;
            this.btnPreview.Location = new System.Drawing.Point(95, 0);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new System.Drawing.Size(65, 20);
            this.btnPreview.TabIndex = 1;
            this.btnPreview.TabStop = true;
            this.btnPreview.Text = "查看预览";
            this.btnPreview.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.btnPreview_LinkClicked);
            // 
            // previewSurface
            // 
            this.previewSurface.BackColor = System.Drawing.Color.Silver;
            this.previewSurface.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.previewSurface.CanvasSize = new System.Drawing.Size(100, 100);
            this.previewSurface.Dock = System.Windows.Forms.DockStyle.Fill;
            this.previewSurface.Location = new System.Drawing.Point(8, 27);
            this.previewSurface.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.previewSurface.Name = "previewSurface";
            this.previewSurface.ScaleMode = XTEinkToolkit.Controls.CanvasControl.RenderScaleMode.Zoom;
            this.previewSurface.Size = new System.Drawing.Size(559, 825);
            this.previewSurface.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.panel3);
            this.panel2.Controls.Add(this.btnDoGeneration);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Padding = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.panel2.Size = new System.Drawing.Size(274, 860);
            this.panel2.TabIndex = 1;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.groupBox2);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.panel3.Size = new System.Drawing.Size(266, 815);
            this.panel3.TabIndex = 2;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.chkRenderGridFit);
            this.groupBox2.Controls.Add(this.chkRenderAntiAltas);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.numFontGamma);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.numLineSpacing);
            this.groupBox2.Controls.Add(this.numFontSizePt);
            this.groupBox2.Controls.Add(this.btnSelectFont);
            this.groupBox2.Controls.Add(this.lblFontSource);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(266, 807);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "字体选项";
            // 
            // chkRenderGridFit
            // 
            this.chkRenderGridFit.AutoSize = true;
            this.chkRenderGridFit.Checked = true;
            this.chkRenderGridFit.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRenderGridFit.Location = new System.Drawing.Point(100, 395);
            this.chkRenderGridFit.Name = "chkRenderGridFit";
            this.chkRenderGridFit.Size = new System.Drawing.Size(84, 24);
            this.chkRenderGridFit.TabIndex = 7;
            this.chkRenderGridFit.Text = "主干提示";
            this.toolTip1.SetToolTip(this.chkRenderGridFit, "GDI+字体渲染的一个特性。\r\n主干提示可以根据文字的笔画对渲染进行调整，减少笔画粗细不均匀的情况");
            this.chkRenderGridFit.UseVisualStyleBackColor = true;
            this.chkRenderGridFit.CheckedChanged += new System.EventHandler(this.chkRenderGridFit_CheckedChanged);
            // 
            // chkRenderAntiAltas
            // 
            this.chkRenderAntiAltas.AutoSize = true;
            this.chkRenderAntiAltas.Checked = true;
            this.chkRenderAntiAltas.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRenderAntiAltas.Location = new System.Drawing.Point(10, 395);
            this.chkRenderAntiAltas.Name = "chkRenderAntiAltas";
            this.chkRenderAntiAltas.Size = new System.Drawing.Size(84, 24);
            this.chkRenderAntiAltas.TabIndex = 7;
            this.chkRenderAntiAltas.Text = "字体平滑";
            this.toolTip1.SetToolTip(this.chkRenderAntiAltas, "是否开启GDI+字体灰度抗锯齿功能\r\n开启功能并不会真的抗锯齿，只是在内部渲染时允许包含灰度信息。\r\n当开启此功能后，可以调整字体亮度。");
            this.chkRenderAntiAltas.UseVisualStyleBackColor = true;
            this.chkRenderAntiAltas.CheckedChanged += new System.EventHandler(this.chkRenderGridFit_CheckedChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 312);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(121, 20);
            this.label6.TabIndex = 6;
            this.label6.Text = "行间距（像素）：";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 367);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(107, 20);
            this.label5.TabIndex = 6;
            this.label5.Text = "字体渲染模式：";
            // 
            // numFontGamma
            // 
            this.numFontGamma.Location = new System.Drawing.Point(6, 264);
            this.numFontGamma.Maximum = 254;
            this.numFontGamma.Minimum = 1;
            this.numFontGamma.Name = "numFontGamma";
            this.numFontGamma.Size = new System.Drawing.Size(254, 45);
            this.numFontGamma.TabIndex = 5;
            this.numFontGamma.TickFrequency = 16;
            this.numFontGamma.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.numFontGamma.Value = 127;
            this.numFontGamma.ValueChanged += new System.EventHandler(this.numFontGamma_ValueChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 241);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(135, 20);
            this.label4.TabIndex = 4;
            this.label4.Text = "字体亮度（粗细）：";
            // 
            // numLineSpacing
            // 
            this.numLineSpacing.Location = new System.Drawing.Point(6, 336);
            this.numLineSpacing.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.numLineSpacing.Name = "numLineSpacing";
            this.numLineSpacing.Size = new System.Drawing.Size(254, 26);
            this.numLineSpacing.TabIndex = 3;
            this.toolTip1.SetToolTip(this.numLineSpacing, "字体的行间距。\r\n由于阅星曈渲染文字时会向下取整，因此少量调整间距可能不会产生效果。\r\n此时需要调大更多的值才能生效。\r\n这个值也可以理解为行间距至少为多少像素。" +
        "");
            this.numLineSpacing.ValueChanged += new System.EventHandler(this.numLineSpacing_ValueChanged);
            // 
            // numFontSizePt
            // 
            this.numFontSizePt.DecimalPlaces = 2;
            this.numFontSizePt.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numFontSizePt.Location = new System.Drawing.Point(6, 210);
            this.numFontSizePt.Maximum = new decimal(new int[] {
            72,
            0,
            0,
            0});
            this.numFontSizePt.Minimum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.numFontSizePt.Name = "numFontSizePt";
            this.numFontSizePt.Size = new System.Drawing.Size(254, 26);
            this.numFontSizePt.TabIndex = 3;
            this.numFontSizePt.Value = new decimal(new int[] {
            2175,
            0,
            0,
            131072});
            this.numFontSizePt.ValueChanged += new System.EventHandler(this.numFontSizePt_ValueChanged);
            // 
            // btnSelectFont
            // 
            this.btnSelectFont.Location = new System.Drawing.Point(6, 148);
            this.btnSelectFont.Name = "btnSelectFont";
            this.btnSelectFont.Size = new System.Drawing.Size(254, 36);
            this.btnSelectFont.TabIndex = 2;
            this.btnSelectFont.Text = "选择字体";
            this.btnSelectFont.UseVisualStyleBackColor = true;
            this.btnSelectFont.Click += new System.EventHandler(this.btnSelectFont_Click);
            // 
            // lblFontSource
            // 
            this.lblFontSource.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblFontSource.Font = new System.Drawing.Font("宋体", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblFontSource.Location = new System.Drawing.Point(6, 49);
            this.lblFontSource.Name = "lblFontSource";
            this.lblFontSource.Size = new System.Drawing.Size(254, 96);
            this.lblFontSource.TabIndex = 1;
            this.lblFontSource.Text = "宋体\r\n中国智造，惠及全球ABCabc123";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 187);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(107, 20);
            this.label3.TabIndex = 0;
            this.label3.Text = "字体大小 (Pt)：";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "字体名称：";
            // 
            // btnDoGeneration
            // 
            this.btnDoGeneration.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnDoGeneration.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnDoGeneration.Location = new System.Drawing.Point(0, 815);
            this.btnDoGeneration.Name = "btnDoGeneration";
            this.btnDoGeneration.Size = new System.Drawing.Size(266, 45);
            this.btnDoGeneration.TabIndex = 1;
            this.btnDoGeneration.Text = "生成字体";
            this.btnDoGeneration.UseVisualStyleBackColor = true;
            this.btnDoGeneration.Click += new System.EventHandler(this.btnDoGeneration_Click);
            // 
            // fontDialog
            // 
            this.fontDialog.MaxSize = 72;
            this.fontDialog.MinSize = 8;
            // 
            // debounceTimer
            // 
            this.debounceTimer.Interval = 1;
            this.debounceTimer.Tick += new System.EventHandler(this.debounceTimer_Tick);
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(865, 876);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MinimumSize = new System.Drawing.Size(629, 555);
            this.Name = "FrmMain";
            this.Padding = new System.Windows.Forms.Padding(8);
            this.Text = "字体文件转换 - 阅星曈工具箱";
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.panel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFontGamma)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLineSpacing)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFontSizePt)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Controls.CanvasControl previewSurface;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnDoGeneration;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.LinkLabel btnPreview;
        private System.Windows.Forms.Label lblFontSource;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSelectFont;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numFontSizePt;
        private System.Windows.Forms.TrackBar numFontGamma;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox chkRenderGridFit;
        private System.Windows.Forms.CheckBox chkRenderAntiAltas;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown numLineSpacing;
        private System.Windows.Forms.FontDialog fontDialog;
        private System.Windows.Forms.Timer debounceTimer;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}

