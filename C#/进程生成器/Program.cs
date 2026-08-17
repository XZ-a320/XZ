using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

#region Job对象封装（子进程自动终止）
public static class JobHelper
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    private enum JobObjectInfoType { ExtendedLimitInformation = 9 }

    private const uint JOB_OBJECT_LIMIT_KILL_ON_CLOSE = 0x2000;

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount;
        public ulong ReadTransferCount, WriteTransferCount, OtherTransferCount;
    }

    public static IntPtr CreateKillOnCloseJob()
    {
        IntPtr hJob = CreateJobObject(IntPtr.Zero, null);
        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_CLOSE;
        int size = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
        IntPtr infoPtr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(info, infoPtr, false);
        SetInformationJobObject(hJob, JobObjectInfoType.ExtendedLimitInformation, infoPtr, (uint)size);
        Marshal.FreeHGlobal(infoPtr);
        return hJob;
    }

    public static void AssignProcess(IntPtr hJob, Process proc)
    {
        AssignProcessToJobObject(hJob, proc.Handle);
    }
}
#endregion

#region 幽灵进程窗口
public partial class GhostForm : Form
{
    private bool _isFullScreen = false;
    private FormBorderStyle _lastBorderStyle;
    private Rectangle _lastBounds;

    public GhostForm(string title, string? iconPath)
    {
        this.Text = title;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Size = new Size(600, 400);
        this.MinimumSize = new Size(200, 150);

        if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
        {
            try { this.Icon = new Icon(iconPath); } catch { }
        }

        var menu = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("窗口");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("全屏 (F11)", null, (s, e) => ToggleFullScreen()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("关闭", null, (s, e) => this.Close()));
        menu.Items.Add(fileMenu);
        this.Controls.Add(menu);
        this.MainMenuStrip = menu;

        var label = new Label
        {
            Text = "这是一个空窗口进程\n按 F11 全屏 / 恢复",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Microsoft YaHei", 12)
        };
        this.Controls.Add(label);

        this.KeyPreview = true;
        this.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.F11)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
        };

        this.FormClosing += (s, e) => Application.Exit();
    }

    private void ToggleFullScreen()
    {
        if (!_isFullScreen)
        {
            _lastBorderStyle = this.FormBorderStyle;
            _lastBounds = this.Bounds;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.Bounds = Screen.PrimaryScreen!.Bounds;
            _isFullScreen = true;
        }
        else
        {
            this.FormBorderStyle = _lastBorderStyle;
            this.Bounds = _lastBounds;
            _isFullScreen = false;
        }
    }
}
#endregion

#region 主窗体
public partial class MainForm : Form
{
    private TextBox txtProcessName;
    private TextBox txtTitle;
    private TextBox txtIconPath;
    private Button btnBrowseIcon;
    private CheckBox chkChildProcess;
    private Button btnCreate;
    private ListBox lstProcesses;
    private IntPtr _jobHandle = IntPtr.Zero;
    private bool _hasJob = false;

    public MainForm()
    {
        this.Text = "空进程生成器";
        this.Size = new Size(520, 420);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(12)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 6; i++) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        table.Controls.Add(new Label { Text = "进程名(EXE):", Anchor = AnchorStyles.Right }, 0, 0);
        txtProcessName = new TextBox { Text = "GhostApp", Dock = DockStyle.Fill };
        table.Controls.Add(txtProcessName, 1, 0);

        table.Controls.Add(new Label { Text = "窗口标题:", Anchor = AnchorStyles.Right, Margin = new Padding(0, 6, 0, 0) }, 0, 1);
        txtTitle = new TextBox { Text = "我的窗口", Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 0) };
        table.Controls.Add(txtTitle, 1, 1);

        table.Controls.Add(new Label { Text = "图标(ICO):", Anchor = AnchorStyles.Right, Margin = new Padding(0, 6, 0, 0) }, 0, 2);
        var iconPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
        txtIconPath = new TextBox { Width = 220 };
        btnBrowseIcon = new Button { Text = "浏览...", AutoSize = true };
        iconPanel.Controls.Add(txtIconPath);
        iconPanel.Controls.Add(btnBrowseIcon);
        table.Controls.Add(iconPanel, 1, 2);

        btnBrowseIcon.Click += (s, e) =>
        {
            using var dlg = new OpenFileDialog { Filter = "图标文件 (*.ico)|*.ico|所有文件 (*.*)|*.*" };
            if (dlg.ShowDialog() == DialogResult.OK)
                txtIconPath.Text = dlg.FileName;
        };

        chkChildProcess = new CheckBox { Text = "作为子进程（关闭本软件时自动结束）", Checked = true, AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
        table.Controls.Add(chkChildProcess, 1, 3);
        table.SetColumnSpan(chkChildProcess, 2);

        btnCreate = new Button { Text = "创建空进程", AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
        table.Controls.Add(btnCreate, 1, 4);
        table.SetColumnSpan(btnCreate, 2);
        btnCreate.Click += BtnCreate_Click;

        table.Controls.Add(new Label { Text = "已创建的进程:", Anchor = AnchorStyles.Right, Margin = new Padding(0, 10, 0, 0) }, 0, 5);
        lstProcesses = new ListBox { Dock = DockStyle.Fill, Height = 100, Margin = new Padding(0, 10, 0, 0) };
        table.Controls.Add(lstProcesses, 1, 5);

        this.Controls.Add(table);

        _jobHandle = JobHelper.CreateKillOnCloseJob();
        _hasJob = true;

        this.FormClosing += (s, e) => { /* 关闭时 Job 自动清理子进程 */ };
    }

    private void BtnCreate_Click(object? sender, EventArgs e)
    {
        string processName = txtProcessName.Text.Trim();
        if (string.IsNullOrWhiteSpace(processName))
        {
            MessageBox.Show("请输入进程名（不含扩展名）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            processName += ".exe";

        string windowTitle = string.IsNullOrWhiteSpace(txtTitle.Text) ? "空窗口" : txtTitle.Text;
        string? iconPath = string.IsNullOrWhiteSpace(txtIconPath.Text) ? null : txtIconPath.Text.Trim();

        try
        {
            string currentExe = Application.ExecutablePath;
            string tempDir = Path.Combine(Path.GetTempPath(), "GhostProcessCreator");
            Directory.CreateDirectory(tempDir);
            string targetExe = Path.Combine(tempDir, processName);
            File.Copy(currentExe, targetExe, true);

            string args = $"--ghost --title \"{windowTitle}\"";
            if (iconPath != null)
                args += $" --icon \"{iconPath}\"";

            var psi = new ProcessStartInfo(targetExe, args)
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

            Process? proc = Process.Start(psi);

            if (chkChildProcess.Checked && proc != null && _hasJob)
            {
                JobHelper.AssignProcess(_jobHandle, proc);
            }

            lstProcesses.Items.Add($"{processName}  (PID: {proc?.Id})");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"创建失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
#endregion

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        bool isGhost = false;
        string ghostTitle = "空窗口";
        string? ghostIcon = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--ghost", StringComparison.OrdinalIgnoreCase))
                isGhost = true;
            else if (args[i].Equals("--title", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                ghostTitle = args[++i];
            else if (args[i].Equals("--icon", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                ghostIcon = args[++i];
        }

        if (isGhost)
            Application.Run(new GhostForm(ghostTitle, ghostIcon));
        else
            Application.Run(new MainForm());
    }
}