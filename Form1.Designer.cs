namespace OLA
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            moniqi_liebiao = new DataGridView();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            shuaxin_liebiao = new Button();
            guanbi_suoyou = new Button();
            queding_shezhi = new Button();
            yixuan_renwu = new ListBox();
            renwu_liebiao = new ListBox();
            label6 = new Label();
            label2 = new Label();
            shifou_huanhao = new CheckBox();
            shifou_zhiding = new CheckBox();
            zidong_denglu = new CheckBox();
            pailie_fangshi = new ComboBox();
            lujing_shuru = new MaskedTextBox();
            qufu_xuanze = new ComboBox();
            moniqi_xuanze = new ComboBox();
            duokai_shuliang = new MaskedTextBox();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            模拟器选择 = new Label();
            label1 = new Label();
            tabPage2 = new TabPage();
            quanbu_qidong = new Button();
            tingzhi_xuanzhong = new Button();
            huifu_suoyou = new Button();
            zanting_suoyou = new Button();
            quanbu_tingzhi = new Button();
            statusStrip1 = new StatusStrip();
            yunxingshijian = new ToolStripStatusLabel();
            timer_runtime = new System.Windows.Forms.Timer(components);
            xuhao = new DataGridViewTextBoxColumn();
            moniqi = new DataGridViewTextBoxColumn();
            zhanghao = new DataGridViewTextBoxColumn();
            mima = new DataGridViewTextBoxColumn();
            zhuangtai = new DataGridViewTextBoxColumn();
            jubing = new DataGridViewTextBoxColumn();
            yichang = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)moniqi_liebiao).BeginInit();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // moniqi_liebiao
            // 
            moniqi_liebiao.AllowUserToAddRows = false;
            moniqi_liebiao.AllowUserToDeleteRows = false;
            moniqi_liebiao.AllowUserToResizeColumns = false;
            moniqi_liebiao.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.TopCenter;
            moniqi_liebiao.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            moniqi_liebiao.BackgroundColor = Color.White;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = SystemColors.Control;
            dataGridViewCellStyle2.Font = new Font("Microsoft YaHei UI", 9F);
            dataGridViewCellStyle2.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
            moniqi_liebiao.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            moniqi_liebiao.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            moniqi_liebiao.Columns.AddRange(new DataGridViewColumn[] { xuhao, moniqi, zhanghao, mima, zhuangtai, jubing, yichang });
            moniqi_liebiao.Location = new Point(-1, 2);
            moniqi_liebiao.Name = "moniqi_liebiao";
            moniqi_liebiao.ReadOnly = true;
            moniqi_liebiao.RowHeadersVisible = false;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.TopCenter;
            moniqi_liebiao.RowsDefaultCellStyle = dataGridViewCellStyle3;
            moniqi_liebiao.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            moniqi_liebiao.Size = new Size(791, 297);
            moniqi_liebiao.TabIndex = 0;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Location = new Point(-1, 306);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(600, 204);
            tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(shuaxin_liebiao);
            tabPage1.Controls.Add(guanbi_suoyou);
            tabPage1.Controls.Add(queding_shezhi);
            tabPage1.Controls.Add(yixuan_renwu);
            tabPage1.Controls.Add(renwu_liebiao);
            tabPage1.Controls.Add(label6);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(shifou_huanhao);
            tabPage1.Controls.Add(shifou_zhiding);
            tabPage1.Controls.Add(zidong_denglu);
            tabPage1.Controls.Add(pailie_fangshi);
            tabPage1.Controls.Add(lujing_shuru);
            tabPage1.Controls.Add(qufu_xuanze);
            tabPage1.Controls.Add(moniqi_xuanze);
            tabPage1.Controls.Add(duokai_shuliang);
            tabPage1.Controls.Add(label5);
            tabPage1.Controls.Add(label4);
            tabPage1.Controls.Add(label3);
            tabPage1.Controls.Add(模拟器选择);
            tabPage1.Controls.Add(label1);
            tabPage1.Location = new Point(4, 26);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(592, 174);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "基础设置";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // shuaxin_liebiao
            // 
            shuaxin_liebiao.Location = new Point(222, 68);
            shuaxin_liebiao.Name = "shuaxin_liebiao";
            shuaxin_liebiao.Size = new Size(84, 26);
            shuaxin_liebiao.TabIndex = 19;
            shuaxin_liebiao.Text = "刷新列表";
            shuaxin_liebiao.UseVisualStyleBackColor = true;
            shuaxin_liebiao.Click += shuaxin_liebiao_Click;
            // 
            // guanbi_suoyou
            // 
            guanbi_suoyou.Location = new Point(222, 101);
            guanbi_suoyou.Name = "guanbi_suoyou";
            guanbi_suoyou.Size = new Size(84, 26);
            guanbi_suoyou.TabIndex = 18;
            guanbi_suoyou.Text = "关闭所有";
            guanbi_suoyou.UseVisualStyleBackColor = true;
            guanbi_suoyou.Click += guanbi_suoyou_Click;
            // 
            // queding_shezhi
            // 
            queding_shezhi.Location = new Point(222, 137);
            queding_shezhi.Name = "queding_shezhi";
            queding_shezhi.Size = new Size(84, 26);
            queding_shezhi.TabIndex = 17;
            queding_shezhi.Text = "确定";
            queding_shezhi.UseVisualStyleBackColor = true;
            // 
            // yixuan_renwu
            // 
            yixuan_renwu.FormattingEnabled = true;
            yixuan_renwu.Location = new Point(498, 32);
            yixuan_renwu.Name = "yixuan_renwu";
            yixuan_renwu.Size = new Size(85, 123);
            yixuan_renwu.TabIndex = 16;
            // 
            // renwu_liebiao
            // 
            renwu_liebiao.FormattingEnabled = true;
            renwu_liebiao.Items.AddRange(new object[] { "主线任务", "支线任务", "每日活跃", "每日签到", "挂机任务" });
            renwu_liebiao.Location = new Point(395, 32);
            renwu_liebiao.Name = "renwu_liebiao";
            renwu_liebiao.Size = new Size(85, 123);
            renwu_liebiao.TabIndex = 15;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(513, 17);
            label6.Name = "label6";
            label6.Size = new Size(56, 17);
            label6.TabIndex = 14;
            label6.Text = "已选任务";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(410, 17);
            label2.Name = "label2";
            label2.Size = new Size(56, 17);
            label2.TabIndex = 13;
            label2.Text = "任务列表";
            // 
            // shifou_huanhao
            // 
            shifou_huanhao.AutoSize = true;
            shifou_huanhao.Location = new Point(225, 41);
            shifou_huanhao.Name = "shifou_huanhao";
            shifou_huanhao.Size = new Size(75, 21);
            shifou_huanhao.TabIndex = 12;
            shifou_huanhao.Text = "是否换号";
            shifou_huanhao.UseVisualStyleBackColor = true;
            // 
            // shifou_zhiding
            // 
            shifou_zhiding.AutoSize = true;
            shifou_zhiding.Location = new Point(316, 10);
            shifou_zhiding.Name = "shifou_zhiding";
            shifou_zhiding.Size = new Size(75, 21);
            shifou_zhiding.TabIndex = 11;
            shifou_zhiding.Text = "是否置顶";
            shifou_zhiding.UseVisualStyleBackColor = true;
            // 
            // zidong_denglu
            // 
            zidong_denglu.AutoSize = true;
            zidong_denglu.Location = new Point(225, 10);
            zidong_denglu.Name = "zidong_denglu";
            zidong_denglu.Size = new Size(75, 21);
            zidong_denglu.TabIndex = 10;
            zidong_denglu.Text = "自动登录";
            zidong_denglu.UseVisualStyleBackColor = true;
            // 
            // pailie_fangshi
            // 
            pailie_fangshi.BackColor = SystemColors.Window;
            pailie_fangshi.FormattingEnabled = true;
            pailie_fangshi.Items.AddRange(new object[] { "平铺排序", "隐藏窗口" });
            pailie_fangshi.Location = new Point(80, 137);
            pailie_fangshi.Name = "pailie_fangshi";
            pailie_fangshi.Size = new Size(136, 25);
            pailie_fangshi.TabIndex = 9;
            // 
            // lujing_shuru
            // 
            lujing_shuru.Location = new Point(80, 104);
            lujing_shuru.Name = "lujing_shuru";
            lujing_shuru.Size = new Size(136, 23);
            lujing_shuru.TabIndex = 8;
            lujing_shuru.Text = "D:\\leidian\\LDPlayer9";
            // 
            // qufu_xuanze
            // 
            qufu_xuanze.FormattingEnabled = true;
            qufu_xuanze.Items.AddRange(new object[] { "一区", "二区" });
            qufu_xuanze.Location = new Point(80, 73);
            qufu_xuanze.Name = "qufu_xuanze";
            qufu_xuanze.Size = new Size(136, 25);
            qufu_xuanze.TabIndex = 7;
            // 
            // moniqi_xuanze
            // 
            moniqi_xuanze.FormattingEnabled = true;
            moniqi_xuanze.Items.AddRange(new object[] { "雷电模拟器", "MuMu模拟器" });
            moniqi_xuanze.Location = new Point(80, 38);
            moniqi_xuanze.Name = "moniqi_xuanze";
            moniqi_xuanze.Size = new Size(136, 25);
            moniqi_xuanze.TabIndex = 6;
            moniqi_xuanze.Click += moniqi_xuanze_SelectedIndexChanged;
            // 
            // duokai_shuliang
            // 
            duokai_shuliang.Location = new Point(80, 7);
            duokai_shuliang.Name = "duokai_shuliang";
            duokai_shuliang.Size = new Size(31, 23);
            duokai_shuliang.TabIndex = 5;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(20, 143);
            label5.Name = "label5";
            label5.Size = new Size(56, 17);
            label5.TabIndex = 4;
            label5.Text = "排列方式";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(47, 111);
            label4.Name = "label4";
            label4.Size = new Size(32, 17);
            label4.TabIndex = 3;
            label4.Text = "路径";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(34, 80);
            label3.Name = "label3";
            label3.Size = new Size(44, 17);
            label3.TabIndex = 2;
            label3.Text = "服务器";
            // 
            // 模拟器选择
            // 
            模拟器选择.AutoSize = true;
            模拟器选择.Location = new Point(7, 45);
            模拟器选择.Name = "模拟器选择";
            模拟器选择.Size = new Size(68, 17);
            模拟器选择.TabIndex = 1;
            模拟器选择.Text = "模拟器选择";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(20, 17);
            label1.Name = "label1";
            label1.Size = new Size(56, 17);
            label1.TabIndex = 0;
            label1.Text = "多开数量";
            // 
            // tabPage2
            // 
            tabPage2.Location = new Point(4, 26);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(592, 174);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "通用设置";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // quanbu_qidong
            // 
            quanbu_qidong.Location = new Point(609, 339);
            quanbu_qidong.Name = "quanbu_qidong";
            quanbu_qidong.Size = new Size(84, 26);
            quanbu_qidong.TabIndex = 2;
            quanbu_qidong.Text = "全部启动";
            quanbu_qidong.UseVisualStyleBackColor = true;
            quanbu_qidong.Click += quanbu_qidong_Click;
            // 
            // tingzhi_xuanzhong
            // 
            tingzhi_xuanzhong.Location = new Point(609, 411);
            tingzhi_xuanzhong.Name = "tingzhi_xuanzhong";
            tingzhi_xuanzhong.Size = new Size(84, 26);
            tingzhi_xuanzhong.TabIndex = 3;
            tingzhi_xuanzhong.Text = "停止选中";
            tingzhi_xuanzhong.UseVisualStyleBackColor = true;
            // 
            // huifu_suoyou
            // 
            huifu_suoyou.Location = new Point(701, 376);
            huifu_suoyou.Name = "huifu_suoyou";
            huifu_suoyou.Size = new Size(84, 26);
            huifu_suoyou.TabIndex = 4;
            huifu_suoyou.Text = "恢复";
            huifu_suoyou.UseVisualStyleBackColor = true;
            // 
            // zanting_suoyou
            // 
            zanting_suoyou.Location = new Point(609, 375);
            zanting_suoyou.Name = "zanting_suoyou";
            zanting_suoyou.Size = new Size(84, 26);
            zanting_suoyou.TabIndex = 5;
            zanting_suoyou.Text = "暂停";
            zanting_suoyou.UseVisualStyleBackColor = true;
            // 
            // quanbu_tingzhi
            // 
            quanbu_tingzhi.Location = new Point(701, 339);
            quanbu_tingzhi.Name = "quanbu_tingzhi";
            quanbu_tingzhi.Size = new Size(84, 26);
            quanbu_tingzhi.TabIndex = 6;
            quanbu_tingzhi.Text = "全部停止";
            quanbu_tingzhi.UseVisualStyleBackColor = true;
            quanbu_tingzhi.Click += quanbu_tingzhi_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { yunxingshijian });
            statusStrip1.Location = new Point(0, 513);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(785, 22);
            statusStrip1.TabIndex = 7;
            // 
            // yunxingshijian
            // 
            yunxingshijian.Name = "yunxingshijian";
            yunxingshijian.Size = new Size(135, 17);
            yunxingshijian.Text = "脚本运行时间: 00:00:00";
            // 
            // timer_runtime
            // 
            timer_runtime.Interval = 1000;
            timer_runtime.Tick += timer_runtime_Tick;
            // 
            // xuhao
            // 
            xuhao.HeaderText = "序号";
            xuhao.Name = "xuhao";
            xuhao.ReadOnly = true;
            xuhao.SortMode = DataGridViewColumnSortMode.NotSortable;
            xuhao.Width = 56;
            // 
            // moniqi
            // 
            moniqi.HeaderText = "模拟器";
            moniqi.Name = "moniqi";
            moniqi.ReadOnly = true;
            moniqi.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // zhanghao
            // 
            zhanghao.HeaderText = "账号";
            zhanghao.Name = "zhanghao";
            zhanghao.ReadOnly = true;
            zhanghao.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // mima
            // 
            mima.HeaderText = "密码";
            mima.Name = "mima";
            mima.ReadOnly = true;
            mima.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // zhuangtai
            // 
            zhuangtai.HeaderText = "状态";
            zhuangtai.Name = "zhuangtai";
            zhuangtai.ReadOnly = true;
            zhuangtai.SortMode = DataGridViewColumnSortMode.NotSortable;
            zhuangtai.Width = 177;
            // 
            // jubing
            // 
            jubing.HeaderText = "句柄";
            jubing.Name = "jubing";
            jubing.ReadOnly = true;
            jubing.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // yichang
            // 
            yichang.HeaderText = "异常";
            yichang.Name = "yichang";
            yichang.ReadOnly = true;
            yichang.SortMode = DataGridViewColumnSortMode.NotSortable;
            yichang.Width = 150;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(785, 535);
            Controls.Add(statusStrip1);
            Controls.Add(quanbu_tingzhi);
            Controls.Add(zanting_suoyou);
            Controls.Add(huifu_suoyou);
            Controls.Add(tingzhi_xuanzhong);
            Controls.Add(quanbu_qidong);
            Controls.Add(tabControl1);
            Controls.Add(moniqi_liebiao);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "Form1";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)moniqi_liebiao).EndInit();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView moniqi_liebiao;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.ComboBox qufu_xuanze;
        private System.Windows.Forms.ComboBox moniqi_xuanze;
        private System.Windows.Forms.MaskedTextBox duokai_shuliang;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label 模拟器选择;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.ComboBox pailie_fangshi;
        private System.Windows.Forms.MaskedTextBox lujing_shuru;
        private System.Windows.Forms.ListBox renwu_liebiao;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox shifou_huanhao;
        private System.Windows.Forms.CheckBox shifou_zhiding;
        private System.Windows.Forms.CheckBox zidong_denglu;
        private System.Windows.Forms.ListBox yixuan_renwu;
        private System.Windows.Forms.Button shuaxin_liebiao;
        private System.Windows.Forms.Button guanbi_suoyou;
        private System.Windows.Forms.Button queding_shezhi;
        private System.Windows.Forms.Button quanbu_qidong;
        private System.Windows.Forms.Button tingzhi_xuanzhong;
        private System.Windows.Forms.Button huifu_suoyou;
        private System.Windows.Forms.Button zanting_suoyou;
        private System.Windows.Forms.Button quanbu_tingzhi;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel yunxingshijian;
        private System.Windows.Forms.Timer timer_runtime;
        private DataGridViewTextBoxColumn xuhao;
        private DataGridViewTextBoxColumn moniqi;
        private DataGridViewTextBoxColumn zhanghao;
        private DataGridViewTextBoxColumn mima;
        private DataGridViewTextBoxColumn zhuangtai;
        private DataGridViewTextBoxColumn jubing;
        private DataGridViewTextBoxColumn yichang;
    }
}