using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenLocker
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string passwordFile = Path.Combine(exeDir, "password.txt");
            string tutorialFile = Path.Combine(exeDir, "教程.txt");

            if (!File.Exists(passwordFile))
            {
                CreateDefaultFiles(passwordFile, tutorialFile);
                MessageBox.Show("已生成 password.txt 和 教程.txt。\n请编辑密码和选项后重新运行程序。",
                    "初始化完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var config = ReadConfig(passwordFile);
            Application.Run(new LockScreenForm(config));
        }

        static void CreateDefaultFiles(string passwordFile, string tutorialFile)
        {
            File.WriteAllText(passwordFile,
                "Abc123456\n" +
                "R\n" +
                "电脑被锁定，请填写密码");

            string tutorial = "配置文件说明（password.txt）\r\n" +
                "第一行：密码（超过16位取前16位，默认 Abc123456）\r\n" +
                "第二行：功能按钮显示选项（R=显示重启，S=显示关机，RS=显示关机和重启，F=不显示，默认 R）\r\n" +
                "第三行：锁屏显示的文字（默认 电脑被锁定，请填写密码）\r\n\r\n" +
                "修改后重新运行程序即可生效。\r\n\r\n" +
                "注意：本程序通过低级键盘钩子拦截Win键、Alt+Tab、Alt+F4、Ctrl+Esc、Ctrl+Shift+Esc等快捷键。\r\n" +
                "但无法拦截Ctrl+Alt+Delete（系统安全序列），该组合键会打开系统安全界面，此时锁屏程序可能被绕过，请知悉。";
            File.WriteAllText(tutorialFile, tutorial);
        }

        static LockConfig ReadConfig(string passwordFile)
        {
            string[] lines = File.ReadAllLines(passwordFile);
            string password = lines.Length > 0 ? lines[0].Trim() : "";
            if (password.Length > 16) password = password.Substring(0, 16);

            string optionStr = lines.Length > 1 ? lines[1].Trim().ToUpper() : "R";
            bool showRestart = optionStr.Contains('R');
            bool showShutdown = optionStr.Contains('S');
            if (optionStr == "F") { showRestart = false; showShutdown = false; }

            string message = lines.Length > 2 ? lines[2].Trim() : "电脑被锁定，请填写密码";
            if (string.IsNullOrEmpty(message)) message = "电脑被锁定，请填写密码";

            return new LockConfig(password, showRestart, showShutdown, message);
        }
    }

    public class LockConfig
    {
        public string Password { get; }
        public bool ShowRestart { get; }
        public bool ShowShutdown { get; }
        public string Message { get; }

        public LockConfig(string password, bool showRestart, bool showShutdown, string message)
        {
            Password = password;
            ShowRestart = showRestart;
            ShowShutdown = showShutdown;
            Message = message;
        }
    }

    public class LockScreenForm : Form
    {
        private readonly LockConfig _config;
        private TextBox _passwordBox = null!;
        private Button _btnOk = null!;
        private Button _btnRestart = null!;
        private Button _btnShutdown = null!;
        private bool _allowClose = false;

        // 低级键盘钩子
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        // 虚拟键码
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int VK_TAB = 0x09;
        private const int VK_MENU = 0x12;      // Alt
        private const int VK_CONTROL = 0x11;
        private const int VK_ESCAPE = 0x1B;
        private const int VK_SHIFT = 0x10;
        private const int VK_F4 = 0x73;
        private const int VK_LAUNCH_APP1 = 0xB6; // 搜索键
        private const int VK_LAUNCH_APP2 = 0xB7;
        private const int VK_BROWSER_SEARCH = 0xAA;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static LockScreenForm? _instance = null;

        public LockScreenForm(LockConfig config)
        {
            _config = config;
            _proc = HookCallback;
            _instance = this;
            InitializeComponents();
            SetupFullScreen();
            InstallHook();
        }

        private void InitializeComponents()
        {
            this.BackColor = Color.White;
            this.ForeColor = Color.Black;
            this.KeyPreview = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.KeyDown += OnKeyDown;
            this.FormClosing += OnFormClosing;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(30)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblMessage = new Label
            {
                Text = _config.Message,
                Font = new Font("Microsoft YaHei", 18, FontStyle.Bold),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            mainLayout.Controls.Add(lblMessage, 0, 0);

            var passwordPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 20, 0, 20)
            };
            var lblPassword = new Label
            {
                Text = "密码：",
                Font = new Font("Microsoft YaHei", 12),
                AutoSize = true
            };
            _passwordBox = new TextBox
            {
                Width = 200,
                Font = new Font("Microsoft YaHei", 12),
                PasswordChar = '*',
                MaxLength = 16
            };
            _passwordBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    CheckPassword();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            passwordPanel.Controls.Add(lblPassword);
            passwordPanel.Controls.Add(_passwordBox);
            mainLayout.Controls.Add(passwordPanel, 0, 1);

            _btnOk = new Button
            {
                Text = "确定",
                Font = new Font("Microsoft YaHei", 12),
                AutoSize = true,
                Padding = new Padding(20, 5, 20, 5),
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 10, 0, 0)
            };
            _btnOk.Click += (s, e) => CheckPassword();
            mainLayout.Controls.Add(_btnOk, 0, 2);

            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 30, 0, 0)
            };

            if (_config.ShowRestart)
            {
                _btnRestart = CreateButton("重启", BtnRestart_Click);
                buttonPanel.Controls.Add(_btnRestart);
            }
            if (_config.ShowShutdown)
            {
                _btnShutdown = CreateButton("关机", BtnShutdown_Click);
                buttonPanel.Controls.Add(_btnShutdown);
            }

            mainLayout.Controls.Add(buttonPanel, 0, 3);
            this.Controls.Add(mainLayout);

            this.Activated += (s, e) => _passwordBox.Focus();
        }

        private Button CreateButton(string text, EventHandler clickHandler)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Microsoft YaHei", 12),
                AutoSize = true,
                Padding = new Padding(20, 5, 20, 5),
                Margin = new Padding(5)
            };
            btn.Click += clickHandler;
            return btn;
        }

        private void BtnRestart_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("确定要重启计算机吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _allowClose = true;
                Process.Start("shutdown", "/r /t 0");
                this.Close();
            }
        }

        private void BtnShutdown_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("确定要关闭计算机吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _allowClose = true;
                Process.Start("shutdown", "/s /t 0");
                this.Close();
            }
        }

        private void CheckPassword()
        {
            string input = _passwordBox.Text;
            if ((string.IsNullOrEmpty(_config.Password) && string.IsNullOrEmpty(input)) ||
                input == _config.Password)
            {
                _allowClose = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("密码错误，请重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _passwordBox.Clear();
                _passwordBox.Focus();
            }
        }

        private void SetupFullScreen()
        {
            int minX = Screen.AllScreens.Min(s => s.Bounds.X);
            int minY = Screen.AllScreens.Min(s => s.Bounds.Y);
            int maxX = Screen.AllScreens.Max(s => s.Bounds.Right);
            int maxY = Screen.AllScreens.Max(s => s.Bounds.Bottom);
            this.Bounds = new Rectangle(minX, minY, maxX - minX, maxY - minY);
            this.TopMost = true;
            this.BringToFront();
            this.Focus();
            SetForegroundWindow(this.Handle);
        }

        private void InstallHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private void UninstallHook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool alt = (GetKeyState(VK_MENU) & 0x8000) != 0;
                bool ctrl = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
                bool shift = (GetKeyState(VK_SHIFT) & 0x8000) != 0;

                // 拦截Win键及其所有组合（包括Win+Q等）
                if (vkCode == VK_LWIN || vkCode == VK_RWIN)
                {
                    return (IntPtr)1;
                }

                // 拦截Alt+Tab
                if (vkCode == VK_TAB && alt)
                {
                    return (IntPtr)1;
                }

                // 拦截Alt+F4
                if (vkCode == VK_F4 && alt)
                {
                    return (IntPtr)1;
                }

                // 拦截Ctrl+Esc（开始菜单）
                if (vkCode == VK_ESCAPE && ctrl)
                {
                    return (IntPtr)1;
                }

                // 拦截Ctrl+Shift+Esc（任务管理器）
                if (vkCode == VK_ESCAPE && ctrl && shift)
                {
                    return (IntPtr)1;
                }

                // 拦截键盘上的搜索功能键
                if (vkCode == VK_LAUNCH_APP1 || vkCode == VK_LAUNCH_APP2 || vkCode == VK_BROWSER_SEARCH)
                {
                    return (IntPtr)1;
                }

                // 拦截Ctrl+Alt+Delete（尝试，但通常无法成功，系统会优先处理）
                // 注意：这一行可能无效，但加上也不会有害
                if (vkCode == 0x2E && ctrl && alt) // VK_DELETE
                {
                    // 我们故意不拦截，因为无法真正阻止系统安全序列
                    // 拦截可能导致系统不稳定，所以不处理
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // 窗体级拦截，作为补充
            if (e.KeyCode == Keys.F4 && e.Alt ||
                e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin ||
                e.KeyCode == Keys.Tab && e.Alt ||
                e.KeyCode == Keys.Escape && e.Control)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!_allowClose)
            {
                e.Cancel = true;
                return;
            }
            UninstallHook();
            _instance = null;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            UninstallHook();
            _instance = null;
        }
    }
}
