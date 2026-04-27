using System;
using System.Drawing;
using System.Windows.Forms;

namespace OLA
{
    public partial class Form1
    {
        // V5 Light Minimal Tool UI layer.
        // Visual-only styling. No task logic or event binding changes.
        private void ApplyDesignMdStyle()
        {
            SuspendLayout();

            var appBg = Color.FromArgb(245, 247, 250);
            var panel = Color.FromArgb(255, 255, 255);
            var panelAlt = Color.FromArgb(249, 250, 252);
            var inputBg = Color.FromArgb(255, 255, 255);
            var line = Color.FromArgb(218, 225, 233);
            var lineSoft = Color.FromArgb(235, 239, 244);
            var text = Color.FromArgb(33, 43, 54);
            var muted = Color.FromArgb(88, 101, 117);
            var header = Color.FromArgb(241, 245, 249);
            var neutral = Color.FromArgb(245, 247, 250);
            var neutralHover = Color.FromArgb(235, 240, 246);
            var neutralDown = Color.FromArgb(225, 232, 240);
            var accentText = Color.FromArgb(37, 99, 235);
            var dangerText = text;

            BackColor = appBg;
            ForeColor = text;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Text = "武林外传_模拟器";

            ApplyControlStyle(this, appBg, panel, panelAlt, inputBg, line, lineSoft, text, muted, neutral, neutralHover, neutralDown, accentText, dangerText);
            ApplyGridStyle(moniqi_liebiao, panel, panelAlt, header, line, text, muted);
            ApplyStatusStyle(statusStrip1, yunxingshijian, lineSoft, muted, accentText);

            ResumeLayout(false);
        }

        private void ApplyControlStyle(Control parent, Color appBg, Color panel, Color panelAlt, Color inputBg, Color line, Color lineSoft, Color text, Color muted, Color neutral, Color neutralHover, Color neutralDown, Color accentText, Color dangerText)
        {
            foreach (Control control in parent.Controls)
            {
                switch (control)
                {
                    case TabControl tab:
                        tab.BackColor = appBg;
                        tab.ForeColor = text;
                        tab.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
                        tab.Appearance = TabAppearance.Normal;
                        tab.ItemSize = new Size(86, 26);
                        tab.SizeMode = TabSizeMode.Fixed;
                        break;

                    case TabPage page:
                        page.BackColor = panel;
                        page.ForeColor = text;
                        page.UseVisualStyleBackColor = false;
                        break;

                    case Label label:
                        label.ForeColor = muted;
                        label.BackColor = Color.Transparent;
                        label.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
                        break;

                    case Button button:
                        StyleButton(button, text, muted, neutral, neutralHover, neutralDown, accentText, dangerText, line);
                        break;

                    case ComboBox combo:
                        combo.BackColor = inputBg;
                        combo.ForeColor = text;
                        combo.FlatStyle = FlatStyle.Flat;
                        combo.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
                        break;

                    case MaskedTextBox input:
                        input.BackColor = inputBg;
                        input.ForeColor = text;
                        input.BorderStyle = BorderStyle.FixedSingle;
                        input.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
                        break;

                    case ListBox list:
                        list.BackColor = inputBg;
                        list.ForeColor = text;
                        list.BorderStyle = BorderStyle.FixedSingle;
                        list.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
                        break;

                    case CheckBox check:
                        check.ForeColor = text;
                        check.BackColor = panel;
                        check.FlatStyle = FlatStyle.Standard;
                        check.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
                        break;
                }

                if (control.HasChildren)
                {
                    ApplyControlStyle(control, appBg, panel, panelAlt, inputBg, line, lineSoft, text, muted, neutral, neutralHover, neutralDown, accentText, dangerText);
                }
            }
        }

        private void StyleButton(Button button, Color text, Color muted, Color neutral, Color neutralHover, Color neutralDown, Color accentText, Color dangerText, Color line)
        {
            var foreColor = text;
            var borderColor = line;
            var baseColor = neutral;
            var hoverColor = neutralHover;
            var downColor = neutralDown;

            // Keep all buttons visually consistent with the main light UI.
            // No filled colors and no warning color. Use only a slightly stronger text color for primary actions.
            if (button == quanbu_qidong || button == queding_shezhi)
            {
                foreColor = text;
                borderColor = line;
            }
            else
            {
                foreColor = muted;
                borderColor = line;
            }

            button.UseVisualStyleBackColor = false;
            button.BackColor = baseColor;
            button.ForeColor = foreColor;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = borderColor;
            button.FlatAppearance.MouseOverBackColor = hoverColor;
            button.FlatAppearance.MouseDownBackColor = downColor;
            button.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Cursor = Cursors.Hand;
        }

        private void ApplyGridStyle(DataGridView grid, Color panel, Color panelAlt, Color header, Color line, Color text, Color muted)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = panel;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.GridColor = line;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.RowHeadersVisible = false;
            grid.AllowUserToResizeRows = false;

            grid.ColumnHeadersDefaultCellStyle.BackColor = header;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = text;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = header;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = text;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grid.DefaultCellStyle.BackColor = panel;
            grid.DefaultCellStyle.ForeColor = text;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 239, 247);
            grid.DefaultCellStyle.SelectionForeColor = text;
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            grid.AlternatingRowsDefaultCellStyle.BackColor = panelAlt;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = text;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 239, 247);
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = text;
            grid.AlternatingRowsDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grid.RowsDefaultCellStyle.BackColor = panel;
            grid.RowsDefaultCellStyle.ForeColor = text;
            grid.RowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 239, 247);
            grid.RowsDefaultCellStyle.SelectionForeColor = text;
            grid.RowsDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void ApplyStatusStyle(StatusStrip status, ToolStripStatusLabel label, Color bg, Color muted, Color accentText)
        {
            status.BackColor = bg;
            status.ForeColor = muted;
            status.SizingGrip = false;
            label.ForeColor = accentText;
            label.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        }
    }
}
