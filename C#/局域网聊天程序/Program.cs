using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace 局域网聊天程序;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new MainForm());
    }
}

public static class ChatCrypto
{
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("LANChat2024");

    public static string Encrypt(string plainText, string password)
    {
        if (string.IsNullOrEmpty(password)) return plainText;
        using var aes = Aes.Create();
        aes.Key = DeriveKey(password);
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherText, string password)
    {
        if (string.IsNullOrEmpty(password)) return cipherText;
        byte[] fullCipher = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = DeriveKey(password);
        byte[] iv = new byte[16];
        byte[] cipher = new byte[fullCipher.Length - 16];
        Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
        Buffer.BlockCopy(fullCipher, 16, cipher, 0, cipher.Length);
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        byte[] plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] DeriveKey(string password)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, Salt, 10000, HashAlgorithmName.SHA256, 32);
    }
}

public static class MessageEscaper
{
    public static string Escape(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        var sb = new StringBuilder(raw.Length + 10);
        foreach (char c in raw)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\r': sb.Append("\\r"); break;
                case '\n': sb.Append("\\n"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    public static string Unescape(string escaped)
    {
        if (string.IsNullOrEmpty(escaped)) return escaped;
        var sb = new StringBuilder(escaped.Length);
        for (int i = 0; i < escaped.Length; i++)
        {
            if (escaped[i] == '\\' && i + 1 < escaped.Length)
            {
                char next = escaped[i + 1];
                if (next == 'n') { sb.Append('\n'); i++; }
                else if (next == 'r') { sb.Append('\r'); i++; }
                else if (next == '\\') { sb.Append('\\'); i++; }
                else { sb.Append('\\'); }
            }
            else
            {
                sb.Append(escaped[i]);
            }
        }
        return sb.ToString();
    }
}

public class MainForm : Form
{
    private Label titleLabel;
    private Button createButton;
    private Button joinButton;
    private int clickCount = 0;

    public MainForm()
    {
        Text = "局域网聊天程序";
        Size = new Size(420, 280);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        titleLabel = new Label
        {
            Text = "局域网聊天程序",
            Font = new Font("微软雅黑", 16, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(80, 30),
            Cursor = Cursors.Hand
        };
        titleLabel.Click += TitleLabel_Click;

        createButton = new Button
        {
            Text = "创建局域网聊天",
            Size = new Size(200, 40),
            Location = new Point(110, 80),
            Font = new Font("微软雅黑", 11)
        };
        createButton.Click += CreateButton_Click;

        joinButton = new Button
        {
            Text = "加入局域网聊天",
            Size = new Size(200, 40),
            Location = new Point(110, 140),
            Font = new Font("微软雅黑", 11)
        };
        joinButton.Click += JoinButton_Click;

        Controls.Add(titleLabel);
        Controls.Add(createButton);
        Controls.Add(joinButton);
    }

    private void TitleLabel_Click(object? sender, EventArgs e)
    {
        clickCount++;
        if (clickCount == 10)
        {
            clickCount = 0;
            using var easterEgg = new EasterEggForm();
            easterEgg.ShowDialog(this);
        }
    }

    private void CreateButton_Click(object? sender, EventArgs e)
    {
        var createForm = new FormCreate(this);
        createForm.Show();
        Hide();
    }

    private void JoinButton_Click(object? sender, EventArgs e)
    {
        var joinForm = new FormJoin(this);
        joinForm.Show();
        Hide();
    }
}

public class EasterEggForm : Form
{
    public EasterEggForm()
    {
        Text = "彩蛋";
        Size = new Size(480, 220);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var label = new Label
        {
            Text = "你发现了隐藏彩蛋，这是小泽在.exe程序里第一次放彩蛋，很无聊，但就是想试试！",
            AutoSize = false,
            Size = new Size(440, 60),
            Location = new Point(20, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("微软雅黑", 10)
        };

        var exitButton = new Button
        {
            Text = "退出",
            Size = new Size(100, 35),
            Location = new Point(190, 110)
        };
        exitButton.Click += (s, e) => Close();

        Controls.Add(label);
        Controls.Add(exitButton);
    }
}

public class FormCreate : Form
{
    private MainForm? mainForm;
    private Label ipLabel;
    private NumericUpDown portInput;
    private TextBox passwordInput;
    private Button createButton;
    private bool chatOpened;

    public int Port => (int)portInput.Value;
    public string Password => passwordInput.Text.Trim();

    public FormCreate(MainForm owner)
    {
        mainForm = owner;
        Text = "创建局域网聊天";
        Size = new Size(450, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        ipLabel = new Label
        {
            Text = "当前IP: " + GetLocalIPv4Address(),
            AutoSize = true,
            Location = new Point(20, 20),
            Font = new Font("微软雅黑", 10)
        };

        var portLabel = new Label { Text = "端口:", Location = new Point(20, 60), AutoSize = true, Font = new Font("微软雅黑", 10) };
        portInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 65535,
            Value = 5000,
            Location = new Point(100, 58),
            Width = 90,
            Font = new Font("微软雅黑", 10)
        };

        var passwordLabel = new Label { Text = "密码:", Location = new Point(20, 100), AutoSize = true, Font = new Font("微软雅黑", 10) };
        passwordInput = new TextBox
        {
            Location = new Point(100, 98),
            Width = 100,
            MaxLength = 6,
            Font = new Font("微软雅黑", 10)
        };
        passwordInput.KeyPress += PasswordInput_KeyPress;

        var hintLabel = new Label
        {
            Text = "仅允许6位数字，可留空",
            AutoSize = true,
            Location = new Point(210, 100),
            ForeColor = Color.Gray,
            Font = new Font("微软雅黑", 8)
        };

        createButton = new Button
        {
            Text = "创建聊天",
            Size = new Size(120, 40),
            Location = new Point(160, 150),
            Font = new Font("微软雅黑", 11)
        };
        createButton.Click += CreateButton_Click;

        var backButton = new Button
        {
            Text = "返回主页",
            Size = new Size(100, 35),
            Location = new Point(20, 210),
            Font = new Font("微软雅黑", 9)
        };
        backButton.Click += (s, e) => Close();

        Controls.Add(ipLabel);
        Controls.Add(portLabel);
        Controls.Add(portInput);
        Controls.Add(passwordLabel);
        Controls.Add(passwordInput);
        Controls.Add(hintLabel);
        Controls.Add(createButton);
        Controls.Add(backButton);

        FormClosed += FormCreate_FormClosed;
    }

    private void FormCreate_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (!chatOpened)
            mainForm?.Show();
    }

    private static string GetLocalIPv4Address()
    {
        var candidate = Dns.GetHostEntry(Dns.GetHostName())
            .AddressList
            .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
            .Select(ip => ip.ToString())
            .ToList();

        var preferred = candidate.Where(ip => ip.StartsWith("192.168.")).ToList();
        return preferred.Any() ? string.Join(", ", preferred) : (candidate.Any() ? string.Join(", ", candidate) : "未找到");
    }

    private void PasswordInput_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
        {
            e.Handled = true;
        }
    }

    private void CreateButton_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(Password) && Password.Length != 6)
        {
            MessageBox.Show("密码必须为6位数字或留空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var chatForm = new FormChat(
            isHost: true,
            localPort: Port,
            password: Password,
            mainForm: mainForm);
        chatForm.Show();
        chatOpened = true;
        Close();
    }
}

public class FormJoin : Form
{
    private MainForm? mainForm;
    private TextBox ipSegment1;
    private TextBox ipSegment2;
    private TextBox ipSegment3;
    private TextBox ipSegment4;
    private NumericUpDown portInput;
    private TextBox passwordInput;
    private Button joinButton;
    private bool chatOpened;

    public string RemoteIp => $"{ipSegment1.Text}.{ipSegment2.Text}.{ipSegment3.Text}.{ipSegment4.Text}";
    public int RemotePort => (int)portInput.Value;
    public string Password => passwordInput.Text;
    public TcpClient? ConnectedClient { get; private set; }

    public FormJoin(MainForm owner)
    {
        mainForm = owner;
        Text = "加入局域网聊天";
        Size = new Size(480, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var ipLabel = new Label { Text = "对方IP:", Location = new Point(20, 20), AutoSize = true, Font = new Font("微软雅黑", 10) };
        ipSegment1 = CreateIpTextBox(new Point(100, 18));
        ipSegment2 = CreateIpTextBox(new Point(155, 18));
        ipSegment3 = CreateIpTextBox(new Point(210, 18));
        ipSegment4 = CreateIpTextBox(new Point(265, 18));

        var dotLabels = new[] { ".", ".", "." };
        for (int i = 0; i < 3; i++)
        {
            Controls.Add(new Label
            {
                Text = dotLabels[i],
                AutoSize = true,
                Location = new Point(143 + i * 55, 22),
                Font = new Font("微软雅黑", 10)
            });
        }

        var portLabel = new Label { Text = "端口:", Location = new Point(20, 65), AutoSize = true, Font = new Font("微软雅黑", 10) };
        portInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 65535,
            Value = 5000,
            Location = new Point(100, 63),
            Width = 90,
            Font = new Font("微软雅黑", 10)
        };

        var passwordLabel = new Label { Text = "密码:", Location = new Point(20, 105), AutoSize = true, Font = new Font("微软雅黑", 10) };
        passwordInput = new TextBox { Location = new Point(100, 103), Width = 100, Font = new Font("微软雅黑", 10) };
        var passwordHint = new Label
        {
            Text = "未设置留空",
            AutoSize = true,
            Location = new Point(210, 105),
            ForeColor = Color.Gray,
            Font = new Font("微软雅黑", 8)
        };

        joinButton = new Button
        {
            Text = "加入聊天",
            Size = new Size(120, 40),
            Location = new Point(170, 150),
            Font = new Font("微软雅黑", 11)
        };
        joinButton.Click += async (s, e) => await JoinButton_Click();

        var backButton = new Button
        {
            Text = "返回主页",
            Size = new Size(100, 35),
            Location = new Point(20, 210),
            Font = new Font("微软雅黑", 9)
        };
        backButton.Click += (s, e) => Close();

        Controls.Add(ipLabel);
        Controls.Add(ipSegment1);
        Controls.Add(ipSegment2);
        Controls.Add(ipSegment3);
        Controls.Add(ipSegment4);
        Controls.Add(portLabel);
        Controls.Add(portInput);
        Controls.Add(passwordLabel);
        Controls.Add(passwordInput);
        Controls.Add(passwordHint);
        Controls.Add(joinButton);
        Controls.Add(backButton);

        FormClosed += FormJoin_FormClosed;
    }

    private void FormJoin_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (!chatOpened)
            mainForm?.Show();
    }

    private TextBox CreateIpTextBox(Point location)
    {
        var tb = new TextBox
        {
            Location = location,
            Width = 45,
            MaxLength = 3,
            TextAlign = HorizontalAlignment.Center,
            Font = new Font("微软雅黑", 10)
        };
        tb.KeyPress += IpSegment_KeyPress;
        return tb;
    }

    private void IpSegment_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
        {
            e.Handled = true;
        }
    }

    private async Task JoinButton_Click()
    {
        if (!int.TryParse(ipSegment1.Text, out int seg1) || seg1 < 0 || seg1 > 255 ||
            !int.TryParse(ipSegment2.Text, out int seg2) || seg2 < 0 || seg2 > 255 ||
            !int.TryParse(ipSegment3.Text, out int seg3) || seg3 < 0 || seg3 > 255 ||
            !int.TryParse(ipSegment4.Text, out int seg4) || seg4 < 0 || seg4 > 255)
        {
            MessageBox.Show("IP地址格式错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(RemoteIp, RemotePort);
            var stream = client.GetStream();
            var writer = new StreamWriter(stream) { AutoFlush = true };
            var reader = new StreamReader(stream);

            await writer.WriteLineAsync($"PASS:{Password}");

            var response = await reader.ReadLineAsync();
            if (response == "OK")
            {
                ConnectedClient = client;
                var chatForm = new FormChat(
                    isHost: false,
                    remoteIp: RemoteIp,
                    remotePort: RemotePort,
                    password: Password,
                    client: client,
                    mainForm: mainForm);
                chatForm.Show();
                chatOpened = true;
                Close();
            }
            else
            {
                client.Close();
                MessageBox.Show("密码错误或连接被拒绝", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"连接失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

public class FormChat : Form
{
    private readonly bool isHost;
    private readonly string? password;
    private TcpClient? client;
    private NetworkStream? stream;
    private StreamReader? reader;
    private StreamWriter? writer;
    private TcpListener? listener;
    private CancellationTokenSource? cts;
    private readonly List<ClientInfo> connectedClients = new();
    private readonly string localIp;
    private MainForm? mainForm;

    private Label titleLabel = null!;
    private Button manageButton = null!;
    private Button returnOrDisbandButton = null!;
    private RichTextBox chatHistory = null!;
    private TextBox inputBox = null!;
    private Panel dragPanel = null!;
    private Button sendButton = null!;
    private CheckBox enterSendCheckBox = null!;
    private bool dragging;
    private int dragStartY;
    private int inputStartHeight;

    public FormChat(bool isHost, int localPort = 0, string? password = null,
        string? remoteIp = null, int remotePort = 0, TcpClient? client = null, MainForm? mainForm = null)
    {
        this.isHost = isHost;
        this.password = password;
        this.client = client;
        this.mainForm = mainForm;

        localIp = GetLocalIPv4();
        Text = "局域网聊天";
        Size = new Size(680, 650);
        MinimumSize = new Size(500, 450);
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterScreen;

        InitializeComponent();

        titleLabel.Text = localIp;

        if (isHost)
        {
            manageButton.Visible = true;
            returnOrDisbandButton.Text = "解散聊天";
            returnOrDisbandButton.Visible = true;
            Task.Run(() => StartHost(localPort));
        }
        else if (client is not null)
        {
            returnOrDisbandButton.Text = "返回主页";
            returnOrDisbandButton.Visible = true;
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream) { AutoFlush = true };
            cts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                try
                {
                    if (writer is not null)
                        await writer.WriteLineAsync($"MYIP:{localIp}");
                }
                catch { }
            });

            Task.Run(() => ReceiveMessages(cts.Token));
        }

        FormClosing += FormChat_FormClosing;
    }

    private static string GetLocalIPv4()
    {
        var candidates = Dns.GetHostEntry(Dns.GetHostName())
            .AddressList
            .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
            .Select(ip => ip.ToString())
            .ToList();
        var preferred = candidates.Where(ip => ip.StartsWith("192.168.")).ToList();
        return preferred.Any() ? preferred.First() : (candidates.Any() ? candidates.First() : "未知IP");
    }

    private void InitializeComponent()
    {
        titleLabel = new Label
        {
            Font = new Font("微软雅黑", 11, FontStyle.Bold),
            Location = new Point(10, 10),
            AutoSize = true
        };

        manageButton = new Button
        {
            Text = "聊天设置",
            Size = new Size(90, 30),
            Location = new Point(ClientSize.Width - 200, 5),
            Visible = false,
            Font = new Font("微软雅黑", 9)
        };
        manageButton.Click += ManageButton_Click;

        returnOrDisbandButton = new Button
        {
            Size = new Size(90, 30),
            Location = new Point(ClientSize.Width - 100, 5),
            Visible = false,
            Font = new Font("微软雅黑", 9)
        };
        returnOrDisbandButton.Click += ReturnOrDisbandButton_Click;

        chatHistory = new RichTextBox
        {
            Location = new Point(10, 45),
            Size = new Size(ClientSize.Width - 25, 350),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            WordWrap = true,
            DetectUrls = true,
            Font = new Font("微软雅黑", 10)
        };

        dragPanel = new Panel
        {
            Height = 6,
            Cursor = Cursors.HSplit,
            BackColor = Color.LightGray,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        dragPanel.MouseDown += DragPanel_MouseDown;
        dragPanel.MouseMove += DragPanel_MouseMove;
        dragPanel.MouseUp += DragPanel_MouseUp;

        inputBox = new TextBox
        {
            Multiline = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Height = 80,
            ScrollBars = ScrollBars.Vertical,
            AcceptsReturn = true,
            Font = new Font("微软雅黑", 10)
        };
        inputBox.KeyDown += InputBox_KeyDown;

        sendButton = new Button
        {
            Text = "发送",
            Size = new Size(80, 30),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Font = new Font("微软雅黑", 10)
        };
        sendButton.Click += async (s, e) => await SendMessage();

        enterSendCheckBox = new CheckBox
        {
            Text = "回车发送",
            Checked = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            AutoSize = true,
            Font = new Font("微软雅黑", 9)
        };

        Controls.Add(titleLabel);
        Controls.Add(manageButton);
        Controls.Add(returnOrDisbandButton);
        Controls.Add(chatHistory);
        Controls.Add(dragPanel);
        Controls.Add(inputBox);
        Controls.Add(sendButton);
        Controls.Add(enterSendCheckBox);

        LayoutComponents();
        SizeChanged += (s, e) => LayoutComponents();
    }

    private void LayoutComponents()
    {
        manageButton.Location = new Point(ClientSize.Width - 200, 5);
        returnOrDisbandButton.Location = new Point(ClientSize.Width - 100, 5);
        int bottomAreaY = ClientSize.Height - inputBox.Height - 45;
        chatHistory.Height = bottomAreaY - chatHistory.Top - 10;
        dragPanel.Location = new Point(10, bottomAreaY - 4);
        dragPanel.Width = ClientSize.Width - 25;
        inputBox.Location = new Point(10, bottomAreaY + 2);
        inputBox.Width = ClientSize.Width - 25;
        sendButton.Location = new Point(ClientSize.Width - 95, ClientSize.Height - 40);
        enterSendCheckBox.Location = new Point(10, ClientSize.Height - 32);
    }

    private void DragPanel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            dragging = true;
            dragStartY = Cursor.Position.Y;
            inputStartHeight = inputBox.Height;
        }
    }

    private void DragPanel_MouseMove(object? sender, MouseEventArgs e)
    {
        if (dragging)
        {
            int delta = Cursor.Position.Y - dragStartY;
            int newHeight = inputStartHeight - delta;
            if (newHeight < 40) newHeight = 40;
            if (newHeight > ClientSize.Height - 200) newHeight = ClientSize.Height - 200;
            inputBox.Height = newHeight;
            LayoutComponents();
        }
    }

    private void DragPanel_MouseUp(object? sender, MouseEventArgs e)
    {
        dragging = false;
    }

    private async void InputBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            if (enterSendCheckBox.Checked)
            {
                if (!e.Shift)
                {
                    e.SuppressKeyPress = true;
                    await SendMessage();
                }
            }
        }
    }

    private async Task StartHost(int port)
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            cts = new CancellationTokenSource();
            while (!cts.Token.IsCancellationRequested)
            {
                var tcpClient = await listener.AcceptTcpClientAsync(cts.Token);
                _ = Task.Run(() => HandleNewClient(tcpClient));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Invoke(() => MessageBox.Show($"监听失败: {ex.Message}"));
        }
    }

    private async Task HandleNewClient(TcpClient tcpClient)
    {
        try
        {
            using (tcpClient)
            {
                var netStream = tcpClient.GetStream();
                var clientReader = new StreamReader(netStream);
                var clientWriter = new StreamWriter(netStream) { AutoFlush = true };

                string? line = await clientReader.ReadLineAsync();
                if (line is null || !line.StartsWith("PASS:"))
                {
                    tcpClient.Close();
                    return;
                }
                string pass = line.Substring(5);
                if (!string.IsNullOrEmpty(password) && pass != password)
                {
                    await clientWriter.WriteLineAsync("WRONG_PASS");
                    tcpClient.Close();
                    return;
                }
                await clientWriter.WriteLineAsync("OK");

                string remoteIp = ((IPEndPoint)tcpClient.Client.RemoteEndPoint!).Address.ToString();
                try
                {
                    string? ipLine = await clientReader.ReadLineAsync();
                    if (ipLine != null && ipLine.StartsWith("MYIP:"))
                        remoteIp = ipLine.Substring(5);
                }
                catch { }

                var clientInfo = new ClientInfo
                {
                    TcpClient = tcpClient,
                    Stream = netStream,
                    Reader = clientReader,
                    Writer = clientWriter,
                    Ip = remoteIp
                };

                lock (connectedClients)
                {
                    connectedClients.Add(clientInfo);
                }

                Invoke(() => AppendSystemMessage($"{remoteIp} 加入了聊天"));
                BroadcastPlainMessage($"JOIN:{remoteIp}", clientInfo);

                await ReadClientMessages(clientInfo);
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            var remoteIp = ((IPEndPoint?)tcpClient.Client.RemoteEndPoint)?.Address.ToString() ?? "";
            lock (connectedClients)
            {
                connectedClients.RemoveAll(c => c.TcpClient == tcpClient);
            }
            if (!string.IsNullOrEmpty(remoteIp))
            {
                Invoke(() => AppendSystemMessage($"{remoteIp} 离开了聊天"));
                BroadcastPlainMessage($"LEAVE:{remoteIp}", null);
            }
        }
    }

    private async Task ReadClientMessages(ClientInfo clientInfo)
    {
        if (clientInfo.Reader is null) return;
        try
        {
            while (true)
            {
                string? line = await clientInfo.Reader.ReadLineAsync();
                if (line is null) break;

                if (line.StartsWith("MSG:"))
                {
                    string content = line.Substring(4);
                    string displayContent;
                    try
                    {
                        if (string.IsNullOrEmpty(password))
                            displayContent = content;
                        else
                            displayContent = ChatCrypto.Decrypt(content, password);
                        displayContent = MessageEscaper.Unescape(displayContent);
                    }
                    catch
                    {
                        continue;
                    }

                    BroadcastEncryptedOrPlain($"MSG:{clientInfo.Ip}:false:{content}", clientInfo);
                    Invoke(() => AppendRemoteMessage(clientInfo.Ip, displayContent));
                }
            }
        }
        catch (IOException) { }
        catch (Exception) { }
    }

    private void BroadcastPlainMessage(string message, ClientInfo? excludeClient)
    {
        List<ClientInfo> clients;
        lock (connectedClients)
        {
            clients = connectedClients.ToList();
        }
        foreach (var client in clients)
        {
            if (excludeClient is not null && client == excludeClient) continue;
            try
            {
                lock (client.WriteLock)
                {
                    client.Writer?.WriteLine(message);
                }
            }
            catch (Exception) { }
        }
    }

    private void BroadcastEncryptedOrPlain(string message, ClientInfo? excludeClient)
    {
        List<ClientInfo> clients;
        lock (connectedClients)
        {
            clients = connectedClients.ToList();
        }
        foreach (var client in clients)
        {
            if (excludeClient is not null && client == excludeClient) continue;
            try
            {
                if (string.IsNullOrEmpty(password))
                {
                    lock (client.WriteLock)
                    {
                        client.Writer?.WriteLine(message);
                    }
                }
                else
                {
                    string encrypted = ChatCrypto.Encrypt(message, password);
                    lock (client.WriteLock)
                    {
                        client.Writer?.WriteLine($"ENC:{encrypted}");
                    }
                }
            }
            catch (Exception) { }
        }
    }

    private async Task SendMessage()
    {
        string text = inputBox.Text;
        if (string.IsNullOrEmpty(text)) return;
        inputBox.Clear();

        string escapedText = MessageEscaper.Escape(text);

        if (isHost)
        {
            Invoke(() => AppendSelfMessage(text));
            string payload = $"MSG:{localIp}:true:{escapedText}";
            BroadcastEncryptedOrPlain(payload, null);
        }
        else
        {
            try
            {
                if (writer is not null)
                {
                    if (string.IsNullOrEmpty(password))
                        await writer.WriteLineAsync($"MSG:{escapedText}");
                    else
                    {
                        string encrypted = ChatCrypto.Encrypt(escapedText, password);
                        await writer.WriteLineAsync($"MSG:{encrypted}");
                    }
                    Invoke(() => AppendSelfMessage(text));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送失败: {ex.Message}");
            }
        }
    }

    private async Task ReceiveMessages(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (reader is null) break;
                string? line = await reader.ReadLineAsync();
                if (line is null) break;

                if (line.StartsWith("ENC:"))
                {
                    if (string.IsNullOrEmpty(password)) continue;
                    string encrypted = line.Substring(4);
                    try
                    {
                        string decrypted = ChatCrypto.Decrypt(encrypted, password);
                        ProcessDecryptedMessage(decrypted);
                    }
                    catch
                    {
                        // 解密失败忽略
                    }
                }
                else if (line.StartsWith("MSG:"))
                {
                    if (!string.IsNullOrEmpty(password)) continue;
                    ProcessDecryptedMessage(line);
                }
                else if (line.StartsWith("JOIN:"))
                {
                    string ip = line.Substring(5);
                    Invoke(() => AppendSystemMessage($"{ip} 加入了聊天"));
                }
                else if (line.StartsWith("LEAVE:"))
                {
                    string ip = line.Substring(6);
                    Invoke(() => AppendSystemMessage($"{ip} 离开了聊天"));
                }
                else if (line == "KICKED")
                {
                    Invoke(() =>
                    {
                        MessageBox.Show("你已被踢出聊天", "提示");
                        Close();
                    });
                    break;
                }
                else if (line == "SERVER_STOP")
                {
                    Invoke(() =>
                    {
                        AppendSystemMessage("聊天已结束");
                    });
                    break;
                }
            }
        }
        catch (IOException) { }
        catch (OperationCanceledException) { }
        catch (Exception) { }
        finally
        {
            Invoke(() =>
            {
                if (!IsDisposed)
                    AppendSystemMessage("连接已断开");
            });
        }
    }

    private void ProcessDecryptedMessage(string raw)
    {
        if (raw.StartsWith("MSG:"))
        {
            string payload = raw.Substring(4);
            string[] parts = payload.Split(':', 3);
            if (parts.Length == 3)
            {
                string senderIp = parts[0];
                bool isSenderHost = parts[1] == "true";
                string content = parts[2];
                content = MessageEscaper.Unescape(content);
                Invoke(() =>
                {
                    if (isSenderHost)
                        AppendRemoteMessage("发起人", content);
                    else
                        AppendRemoteMessage(senderIp, content);
                });
            }
        }
    }

    private void AppendSelfMessage(string content)
    {
        if (InvokeRequired)
        {
            Invoke(() => AppendSelfMessage(content));
            return;
        }

        chatHistory.SelectionStart = chatHistory.TextLength;
        chatHistory.SelectionLength = 0;
        chatHistory.SelectionAlignment = HorizontalAlignment.Right;
        chatHistory.SelectionFont = new Font("微软雅黑", 8, FontStyle.Regular);
        chatHistory.SelectionColor = Color.Gray;
        chatHistory.SelectedText = "自己\n";

        chatHistory.SelectionFont = new Font("微软雅黑", 10, FontStyle.Regular);
        chatHistory.SelectionColor = Color.Green;
        chatHistory.SelectedText = $"{content}\n\n";

        chatHistory.ScrollToCaret();
    }

    private void AppendRemoteMessage(string sender, string content)
    {
        if (InvokeRequired)
        {
            Invoke(() => AppendRemoteMessage(sender, content));
            return;
        }

        chatHistory.SelectionStart = chatHistory.TextLength;
        chatHistory.SelectionLength = 0;
        chatHistory.SelectionAlignment = HorizontalAlignment.Left;
        chatHistory.SelectionFont = new Font("微软雅黑", 8, FontStyle.Regular);
        chatHistory.SelectionColor = Color.Gray;
        chatHistory.SelectedText = $"{sender}\n";

        chatHistory.SelectionFont = new Font("微软雅黑", 10, FontStyle.Regular);
        chatHistory.SelectionColor = Color.Blue;
        chatHistory.SelectedText = $"{content}\n\n";

        chatHistory.ScrollToCaret();
    }

    private void AppendSystemMessage(string msg)
    {
        if (InvokeRequired)
        {
            Invoke(() => AppendSystemMessage(msg));
            return;
        }

        chatHistory.SelectionStart = chatHistory.TextLength;
        chatHistory.SelectionLength = 0;
        chatHistory.SelectionAlignment = HorizontalAlignment.Center;
        chatHistory.SelectionFont = new Font("微软雅黑", 9, FontStyle.Regular);
        chatHistory.SelectionColor = Color.Gray;
        chatHistory.SelectedText = $"{msg}\n\n";

        chatHistory.ScrollToCaret();
    }

    private void ManageButton_Click(object? sender, EventArgs e)
    {
        if (!isHost) return;

        var manageForm = new FormManage(
            connectedClients,
            clientToKick =>
            {
                try
                {
                    lock (clientToKick.WriteLock)
                    {
                        clientToKick.Writer?.WriteLine("KICKED");
                    }
                    clientToKick.TcpClient?.Close();
                }
                catch (Exception) { }

                lock (connectedClients)
                {
                    connectedClients.Remove(clientToKick);
                }
                Invoke(() => AppendSystemMessage($"{clientToKick.Ip} 已被踢出"));
                BroadcastPlainMessage($"LEAVE:{clientToKick.Ip}", null);
            });

        manageForm.ShowDialog(this);
    }

    private void ReturnOrDisbandButton_Click(object? sender, EventArgs e)
    {
        if (isHost)
        {
            var result = MessageBox.Show(
                "是否解散聊天",
                "确认",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (result == DialogResult.Yes)
            {
                BroadcastPlainMessage("SERVER_STOP", null);
                Close();
            }
        }
        else
        {
            var result = MessageBox.Show(
                "确定要返回主页吗？",
                "确认",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                Close();
            }
        }
    }

    private void FormChat_FormClosing(object? sender, FormClosingEventArgs e)
    {
        cts?.Cancel();
        if (isHost)
        {
            listener?.Stop();
            BroadcastPlainMessage("SERVER_STOP", null);
            lock (connectedClients)
            {
                foreach (var client in connectedClients)
                {
                    try
                    {
                        lock (client.WriteLock)
                        {
                            client.Writer?.WriteLine("SERVER_STOP");
                        }
                        client.TcpClient?.Close();
                    }
                    catch (Exception) { }
                }
                connectedClients.Clear();
            }
        }
        else
        {
            try
            {
                writer?.WriteLine("QUIT");
                client?.Close();
            }
            catch (Exception) { }
        }

        mainForm?.Show();
    }
}

public class ClientInfo
{
    public TcpClient TcpClient { get; set; } = null!;
    public NetworkStream Stream { get; set; } = null!;
    public StreamReader Reader { get; set; } = null!;
    public StreamWriter Writer { get; set; } = null!;
    public string Ip { get; set; } = string.Empty;
    public readonly object WriteLock = new();
}

public class FormManage : Form
{
    private FlowLayoutPanel membersPanel;

    public FormManage(
        List<ClientInfo> clients,
        Action<ClientInfo> onKickClient)
    {
        Text = "管理聊天";
        Size = new Size(420, 350);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var membersLabel = new Label
        {
            Text = "踢出成员",
            Location = new Point(20, 20),
            AutoSize = true,
            Font = new Font("微软雅黑", 10, FontStyle.Bold)
        };

        membersPanel = new FlowLayoutPanel
        {
            Location = new Point(20, 50),
            Size = new Size(370, 240),
            AutoScroll = true,
            BorderStyle = BorderStyle.FixedSingle,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        foreach (var client in clients)
        {
            var panel = new Panel { Width = 340, Height = 30 };
            var ipLabel = new Label
            {
                Text = client.Ip,
                AutoSize = true,
                Location = new Point(5, 6),
                Font = new Font("微软雅黑", 9)
            };
            var kickBtn = new Button
            {
                Text = "踢出",
                Size = new Size(50, 23),
                Location = new Point(280, 3),
                Font = new Font("微软雅黑", 8)
            };
            kickBtn.Click += (s, e) =>
            {
                onKickClient(client);
                membersPanel.Controls.Remove(panel);
            };
            panel.Controls.Add(ipLabel);
            panel.Controls.Add(kickBtn);
            membersPanel.Controls.Add(panel);
        }

        Controls.Add(membersLabel);
        Controls.Add(membersPanel);
    }
}