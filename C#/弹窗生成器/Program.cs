using System;
using System.Drawing;
using System.Windows.Forms;

public enum CustomMessageBoxIcon
{
    None,
    Error,
    Warning,
    Information,
    Question
}

#region CustomMessageBox
public class CustomMessageBox : Form
{
    private string? _clickedButtonText;
    private TableLayoutPanel _buttonPanel;

    private CustomMessageBox(string message, string title,
        string[] buttonTexts, CustomMessageBoxIcon icon)
    {
        this.Text = title;
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ShowInTaskbar = false;
        this.AutoSize = true;
        this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        this.Padding = new Padding(12);

        TableLayoutPanel mainPanel = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = icon == CustomMessageBoxIcon.None ? 1 : 2,
            RowCount = 2,
            Dock = DockStyle.Fill
        };
        if (icon != CustomMessageBoxIcon.None)
        {
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        }
        else
        {
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        }
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        if (icon != CustomMessageBoxIcon.None)
        {
            PictureBox iconBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = GetIconImage(icon),
                Size = new Size(32, 32),
                Margin = new Padding(0, 0, 12, 0),
                Anchor = AnchorStyles.Top
            };
            mainPanel.Controls.Add(iconBox, 0, 0);
        }

        Label lblMessage = new Label
        {
            Text = message,
            AutoSize = true,
            MaximumSize = new Size(400, 0),
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            Padding = new Padding(0, 5, 0, 10)
        };
        int msgColumn = icon == CustomMessageBoxIcon.None ? 0 : 1;
        mainPanel.Controls.Add(lblMessage, msgColumn, 0);

        _buttonPanel = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = buttonTexts.Length,
            RowCount = 1,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            Margin = new Padding(0, 10, 0, 0)
        };
        for (int i = 0; i < buttonTexts.Length; i++)
            _buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        for (int i = 0; i < buttonTexts.Length; i++)
        {
            Button btn = new Button
            {
                Text = buttonTexts[i],
                AutoSize = true,
                MinimumSize = new Size(75, 26),
                Margin = new Padding(3)
            };
            btn.Click += (sender, e) =>
            {
                _clickedButtonText = btn.Text;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            _buttonPanel.Controls.Add(btn, i, 0);
        }

        int buttonColumnSpan = icon == CustomMessageBoxIcon.None ? 1 : 2;
        mainPanel.Controls.Add(_buttonPanel, 0, 1);
        mainPanel.SetColumnSpan(_buttonPanel, buttonColumnSpan);

        this.Controls.Add(mainPanel);

        this.FormClosing += (sender, e) =>
        {
            if (this.DialogResult != DialogResult.OK)
                _clickedButtonText = null;
        };
    }

    public static string? Show(string message, string title,
        string[] buttonTexts, CustomMessageBoxIcon icon = CustomMessageBoxIcon.None)
    {
        using (var box = new CustomMessageBox(message, title, buttonTexts, icon))
        {
            box.ShowDialog();
            return box._clickedButtonText;
        }
    }

    private Image? GetIconImage(CustomMessageBoxIcon icon)
    {
        return icon switch
        {
            CustomMessageBoxIcon.Error => SystemIcons.Error.ToBitmap(),
            CustomMessageBoxIcon.Warning => SystemIcons.Warning.ToBitmap(),
            CustomMessageBoxIcon.Information => SystemIcons.Information.ToBitmap(),
            CustomMessageBoxIcon.Question => SystemIcons.Question.ToBitmap(),
            _ => null
        };
    }
}
#endregion

#region MainForm
public class MainForm : Form
{
    private TextBox txtTitle;
    private TextBox txtMessage;
    private TextBox txtButtons;
    private ComboBox cmbIcon;
    private Button btnShow;
    private Label lblResult;

    public MainForm()
    {
        this.Text = "自定义弹窗生成器";
        this.Size = new Size(480, 320);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(12)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 5; i++) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        table.Controls.Add(new Label { Text = "弹窗标题:", Anchor = AnchorStyles.Right }, 0, 0);
        txtTitle = new TextBox { Text = "提示", Dock = DockStyle.Fill };
        table.Controls.Add(txtTitle, 1, 0);

        table.Controls.Add(new Label { Text = "消息内容:", Anchor = AnchorStyles.Right, Margin = new Padding(0, 8, 0, 0) }, 0, 1);
        txtMessage = new TextBox { Text = "确定要执行此操作吗？", Dock = DockStyle.Fill, Multiline = true, Height = 50, Margin = new Padding(0, 8, 0, 0) };
        table.Controls.Add(txtMessage, 1, 1);

        table.Controls.Add(new Label { Text = "按钮文字:", Anchor = AnchorStyles.Right, Margin = new Padding(0, 8, 0, 0) }, 0, 2);
        var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
        txtButtons = new TextBox { Text = "是,否,取消", Width = 250 };
        btnPanel.Controls.Add(txtButtons);
        btnPanel.Controls.Add(new Label { Text = "多个按钮用英文逗号分隔", ForeColor = Color.Gray, AutoSize = true });
        table.Controls.Add(btnPanel, 1, 2);

        table.Controls.Add(new Label { Text = "图标:", Anchor = AnchorStyles.Right, Margin = new Padding(0, 8, 0, 0) }, 0, 3);
        cmbIcon = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Margin = new Padding(0, 8, 0, 0) };
        cmbIcon.Items.AddRange(new[] { "无", "错误", "警告", "信息", "询问" });
        cmbIcon.SelectedIndex = 2;
        table.Controls.Add(cmbIcon, 1, 3);

        var actionPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        btnShow = new Button { Text = "显示弹窗", AutoSize = true };
        lblResult = new Label { Text = "结果：未点击", AutoSize = true, Margin = new Padding(12, 6, 0, 0), ForeColor = Color.Blue };
        actionPanel.Controls.Add(btnShow);
        actionPanel.Controls.Add(lblResult);
        table.Controls.Add(actionPanel, 1, 4);
        table.SetColumnSpan(actionPanel, 2);

        this.Controls.Add(table);

        btnShow.Click += BtnShow_Click;
    }

    private void BtnShow_Click(object? sender, EventArgs e)
    {
        string title = string.IsNullOrWhiteSpace(txtTitle.Text) ? "提示" : txtTitle.Text;
        string message = string.IsNullOrWhiteSpace(txtMessage.Text) ? " " : txtMessage.Text;
        string[] buttons = txtButtons.Text.Split(',', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < buttons.Length; i++) buttons[i] = buttons[i].Trim();
        if (buttons.Length == 0) buttons = new[] { "确定" };

        CustomMessageBoxIcon icon = cmbIcon.SelectedIndex switch
        {
            1 => CustomMessageBoxIcon.Error,
            2 => CustomMessageBoxIcon.Warning,
            3 => CustomMessageBoxIcon.Information,
            4 => CustomMessageBoxIcon.Question,
            _ => CustomMessageBoxIcon.None
        };

        string? clicked = CustomMessageBox.Show(message, title, buttons, icon);
        lblResult.Text = clicked == null ? "结果：直接关闭了窗口" : $"结果：你点击了 \"{clicked}\"";
    }
}
#endregion

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}