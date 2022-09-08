using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Runtime.InteropServices;
using System.Web;
using Microsoft.Win32;

namespace FacebookWidget
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var Params = base.CreateParams;
                Params.ExStyle |= 0x80;
                return Params;
            }
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern Int32 GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT Rect);

        string id, cookie, position, xOffset, yOffset, response, msgNum = "0";
        int phase = 1, boxWidth = 0, boxHeight = 0;
        bool retrievalError = false;

        void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            int xOs = Convert.ToInt32(xOffset), yOs = Convert.ToInt32(yOffset);
            int resWidth = Screen.PrimaryScreen.Bounds.Width, resHeight = Screen.PrimaryScreen.Bounds.Height;
            switch (position)
            {
                case "UpperLeft":
                    this.Left = xOs;
                    this.Top = yOs;
                    break;
                case "UpperRight":
                    this.Left = resWidth - this.Size.Width - xOs;
                    this.Top = yOs;
                    break;
                case "LowerLeft":
                    this.Left = xOs;
                    this.Top = resHeight - this.Size.Height - yOs;
                    break;
                case "LowerRight":
                    this.Left = resWidth - this.Size.Width - xOs;
                    this.Top = resHeight - this.Size.Height - yOs;
                    break;
                default:
                    MessageBox.Show("Unknown position type '" + position + "', the supported positions are 'UpperLeft', 'UpperRight', 'LowerLeft' or 'LowerRight'", "FacebookWidget", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                    break;
            }
        }

        void GetInformation()
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("accept-language", "en;q=0.9,vi;q=0.8,fr-FR;q=0.7,fr;q=0.6,en-US;q=0.5");
                client.Headers.Add("cache-control", "max-age=0");
                client.Headers.Add("cookie", cookie);
                client.Headers.Add("sec-ch-ua", "\"Chromium\";v=\"104\", \" Not A;Brand\";v=\"99\", \"Google Chrome\";v=\"104\"");
                client.Headers.Add("sec-ch-ua-mobile", "?0");
                client.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
                client.Headers.Add("sec-fetch-dest", "document");
                client.Headers.Add("sec-fetch-mode", "navigate");
                client.Headers.Add("sec-fetch-site", "none");
                client.Headers.Add("sec-fetch-user", "?1");
                client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36");
                if (phase == 1)
                {
                    Stream data = client.OpenRead(String.IsNullOrEmpty(cookie) ? ("https://mbasic.facebook.com/" + id) : ("https://mbasic.facebook.com/" + id + "?v=timeline"));
                    StreamReader reader = new StreamReader(data);
                    string s = reader.ReadToEnd();
                    response = s;
                    var html = new HtmlAgilityPack.HtmlDocument();
                    html.LoadHtml(s);
                    if (!String.IsNullOrEmpty(cookie))
                    {
                        var avatar = html.DocumentNode.SelectSingleNode("//div[contains(@class, 'acw')]").SelectSingleNode("//img[contains(@alt, 'profile picture')]").Attributes["src"].Value.Replace("&amp;", "&");
                        pictureBox1.Load(avatar);
                        var name = html.DocumentNode.SelectSingleNode("//span/div/span/strong").InnerText;
                        if (name.Contains("(")) name = name.Substring(0, name.IndexOf('(') - 1);
                        if (name.IndexOf('(') - 1 > 0) label1.Text = name.Substring(0, name.IndexOf('(') - 1);
                        else label1.Text = name;
                        try
                        {
                            var x = html.DocumentNode.SelectSingleNode("//a[contains(@href, 'photos/change/profile_picture')]").InnerText;
                            label1.Text = name + " (You)";
                        }
                        catch { }
                        var status = "Online";
                        try
                        {
                            if (System.Web.HttpUtility.HtmlDecode(html.DocumentNode.SelectSingleNode("//span/div/span/img").Attributes["aria-label"].Value).Contains(name)) status = "Offline";
                            label2.Text = status;
                        }
                        catch
                        {
                            label2.Text = "Offline";
                        }
                        IniFile Config = new IniFile(Application.StartupPath + "\\Config.ini");
                        if (label2.Text == "Online")
                        {
                            try
                            {
                                if (!String.IsNullOrEmpty(Config.Read("OnlineStatusColor", "Config"))) label2.ForeColor = Color.FromName(Config.Read("OnlineStatusColor", "Config"));
                            }
                            catch
                            {
                                MessageBox.Show("Unsupported color name '" + Config.Read("OnlineStatusColor", "Config") + "' in OnlineStatusColor", "FacebookWidget", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else label2.ForeColor = Color.FromName(Config.Read("StatusColor", "Config"));
                    }
                    else
                    {
                        var avatar = html.DocumentNode.SelectSingleNode("//img[contains(@alt, 'profile')]").Attributes["src"].Value.Replace("&amp;", "&");
                        pictureBox1.Load(avatar);
                        var name = html.DocumentNode.SelectSingleNode("//div[@id='cover-name-root']").InnerText;
                        label1.Text = name;
                        label2.Text = "Active status unavailable";
                    }
                    data.Close();
                    reader.Close();
                    if (!String.IsNullOrEmpty(cookie)) phase = 2;
                }
                else if (phase == 2)
                {
                    Stream data = client.OpenRead("https://mbasic.facebook.com/home.php");
                    StreamReader reader = new StreamReader(data);
                    string s = reader.ReadToEnd();
                    var html = new HtmlAgilityPack.HtmlDocument();
                    response = s;
                    html.LoadHtml(s);
                    var text = "";
                    try
                    {
                        text = html.DocumentNode.SelectSingleNode("//nav/a/strong[contains(text(), 'Messages')]//span").InnerText;
                        if (text.Contains("(") && text.Contains(")"))
                        {
                            text = html.DocumentNode.SelectSingleNode("//nav/a[contains(@href, '" + id + "') and contains(@aria-label, 'new message')]").InnerText;
                            if (text.Contains("(") && text.Contains(")"))
                            {
                                try
                                {
                                    var p = text.Substring(text.IndexOf("(") + 1);
                                    label2.Text = p.Substring(0, p.IndexOf(")")) + " unread message" + (p.Substring(0, p.IndexOf(")")) != "1" ? "s" : "");
                                    if (p.Substring(0, p.IndexOf(")")) != "0" && p.Substring(0, p.IndexOf(")")) != msgNum)
                                    {
                                        notifyIcon1.BalloonTipText = (label1.Text.Contains(" (You)") ? "You have " : (label1.Text + " send you ")) + p.Substring(0, p.IndexOf(")")) + " new message" + (p.Substring(0, p.IndexOf(")")) != "1" ? "s" : "") + ".";
                                        notifyIcon1.ShowBalloonTip(15000);
                                    }
                                    msgNum = p.Substring(0, p.IndexOf(")"));
                                }
                                catch
                                {
                                    label2.Text = "0 unread messages";
                                }
                            }
                            else label2.Text = "0 unread messages";
                        }
                        else label2.Text = "0 unread messages";
                    }
                    catch
                    {
                        label2.Text = "0 unread messages";
                    }
                    data.Close();
                    reader.Close();
                    phase = 1;
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(Application.StartupPath + "\\WidgetLog.log", "Response:\n" + response + "\nErrorInformation:\n" + ex.ToString());
                if (retrievalError == false) MessageBox.Show("Cannot retrieve the user data!\nPlease check again the Facebook User ID" + (String.IsNullOrEmpty(cookie) ? ", or provide a Facebook cookie for a better retrieving" : ", or provide another cookie and try again.") + ".\nIf you have checked it but the error still occurs, you can contact the developer at GitHub.com/NozakiYuu.\n\nError information:\n" + ex.ToString() + "\n\nTo view more information, open the WidgetLog.log file in the same folder as the executable.", "FacebookWidget", MessageBoxButtons.OK, MessageBoxIcon.Error);
                retrievalError = true;
                label2.Text = "Cannot retrieve data!";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (System.Diagnostics.Process.GetProcessesByName("FacebookWidget").Length > 1)
            {
                timer1.Enabled = false;
                timer2.Enabled = false;
                MessageBox.Show("Only 1 instance of FacebookWidget is allowed. End the currently running process first and try again.", "FacebookWidget", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            else
            {
                SystemEvents.DisplaySettingsChanged += new EventHandler(SystemEvents_DisplaySettingsChanged);
                if (File.Exists(Application.StartupPath + "\\Config.ini"))
                {
                    var Config = new IniFile(Application.StartupPath + "\\Config.ini");
                    id = Config.Read("ID", "Config");
                    cookie = Config.Read("Cookie", "Config");
                    this.Size = new Size(Convert.ToInt32(Config.Read("Width", "Config")), Convert.ToInt32(Config.Read("Height", "Config")));
                    position = Config.Read("Position", "Config");
                    xOffset = Config.Read("XOffset", "Config");
                    yOffset = Config.Read("YOffset", "Config");
                    int xOs = Convert.ToInt32(xOffset), yOs = Convert.ToInt32(yOffset);
                    int resWidth = Screen.PrimaryScreen.Bounds.Width, resHeight = Screen.PrimaryScreen.Bounds.Height;
                    switch (position)
                    {
                        case "UpperLeft":
                            this.Left = xOs;
                            this.Top = yOs;
                            break;
                        case "UpperRight":
                            this.Left = resWidth - this.Size.Width - xOs;
                            this.Top = yOs;
                            break;
                        case "LowerLeft":
                            this.Left = xOs;
                            this.Top = resHeight - this.Size.Height - yOs;
                            break;
                        case "LowerRight":
                            this.Left = resWidth - this.Size.Width - xOs;
                            this.Top = resHeight - this.Size.Height - yOs;
                            break;
                        default:
                            MessageBox.Show("Unknown position type '" + position + "', the supported positions are 'UpperLeft', 'UpperRight', 'LowerLeft' or 'LowerRight'", "FacebookWidget", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Application.Exit();
                            break;
                    }
                    try
                    {
                        this.Font = new System.Drawing.Font(Config.Read("FontName", "Config"), Convert.ToSingle(Convert.ToInt32(Config.Read("FontSize", "Config"))));
                    }
                    catch
                    {
                        MessageBox.Show("Cannot load the given font with the font name '" + Config.Read("FontName", "Config") + "' and size " + Config.Read("FontSize", "Config"), "FacebookWidget", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
                    }
                    try
                    {
                        label1.ForeColor = Color.FromName(Config.Read("NameColor", "Config"));
                    }
                    catch
                    {
                        MessageBox.Show("Unsupported color name '" + Config.Read("NameColor", "Config") + "' in NameColor", "FacebookWidget", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
                    }
                    try
                    {
                        label2.ForeColor = Color.FromName(Config.Read("StatusColor", "Config"));
                    }
                    catch
                    {
                        MessageBox.Show("Unsupported color name '" + Config.Read("StatusColor", "Config") + "' in StatusColor", "FacebookWidget", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
                    }
                    try
                    {
                        this.BackColor = Color.FromName(Config.Read("TransparencyKey", "Config"));
                        this.TransparencyKey = Color.FromName(Config.Read("TransparencyKey", "Config"));
                    }
                    catch
                    {
                        MessageBox.Show("Unsupported color name '" + Config.Read("TransparencyKey", "Config") + "' in TransparencyKey", "FacebookWidget", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
                    }
                    if (!String.IsNullOrEmpty(Config.Read("AdditionalStyle", "Config")))
                    {
                        switch (Config.Read("AdditionalStyle", "Config"))
                        {
                            case "Bold":
                                label1.Font = new Font(label1.Font.Name, label1.Font.Size, FontStyle.Bold);
                                label2.Font = new Font(label2.Font.Name, label2.Font.Size, FontStyle.Bold);
                                break;
                            case "Italic":
                                label1.Font = new Font(label1.Font.Name, label1.Font.Size, FontStyle.Italic);
                                label2.Font = new Font(label2.Font.Name, label2.Font.Size, FontStyle.Italic);
                                break;
                            default:
                                MessageBox.Show("Unknown style '" + Config.Read("AdditionalStyle", "Config") + "', the supported styles are 'Bold' or 'Italic'", "FacebookWidget", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Cannot find the config file!", "FacebookWidget", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }
                System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)(768 | 3072);
                GetInformation();
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
                tableLayoutPanel1.ColumnStyles[0].Width = pictureBox1.Height + 8;
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                notifyIcon1.ShowBalloonTip(5000);
            }
            boxWidth = this.Size.Width;
            boxHeight = this.Size.Height;
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GetInformation();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.Focus();
            this.BringToFront();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.facebook.com/" + id);
        }

        void Hover()
        {
            this.BackColor = Color.Gray;
        }

        void Leave()
        {
            var Config = new IniFile(Application.StartupPath + "\\Config.ini");
            this.BackColor = Color.FromName(Config.Read("TransparencyKey", "Config"));
        }

        private void pictureBox1_MouseHover(object sender, EventArgs e)
        {
            Hover();
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            Leave();
        }

        private void tableLayoutPanel1_MouseHover(object sender, EventArgs e)
        {
            Hover();
        }

        private void tableLayoutPanel1_MouseLeave(object sender, EventArgs e)
        {
            Leave();
        }

        private void tableLayoutPanel2_MouseHover(object sender, EventArgs e)
        {
            Hover();
        }

        private void tableLayoutPanel2_MouseLeave(object sender, EventArgs e)
        {
            Leave();
        }

        private void label1_MouseHover(object sender, EventArgs e)
        {
            Hover();
        }

        private void label1_MouseLeave(object sender, EventArgs e)
        {
            Leave();
        }

        private void label2_MouseHover(object sender, EventArgs e)
        {
            Hover();
        }

        private void label2_MouseLeave(object sender, EventArgs e)
        {
            Leave();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.facebook.com/" + id);
        }

        private void label2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.facebook.com/" + id);
        }

        private void tableLayoutPanel1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.facebook.com/" + id);
        }

        private void tableLayoutPanel2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.facebook.com/" + id);
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            IntPtr handle = GetForegroundWindow();
            if (handle != null)
            {
                RECT Rect = new RECT();
                if (GetWindowRect(handle, ref Rect))
                {
                    int width = Rect.right - Rect.left, height = Rect.bottom - Rect.top;
                    if (width == Screen.PrimaryScreen.Bounds.Width && height >= Screen.PrimaryScreen.Bounds.Height - boxHeight)
                    {
                        uint pid;
                        GetWindowThreadProcessId(handle, out pid);
                        System.Diagnostics.Process proc = System.Diagnostics.Process.GetProcessById(Convert.ToInt32(pid));
                        if (proc.ProcessName != "explorer")
                        {
                            timer2.Enabled = false;
                            this.Size = new Size(0, 0);
                        }
                        else
                        {
                            this.Size = new Size(boxWidth, boxHeight);
                            timer2.Enabled = true;
                        }
                    }
                    else
                    {
                        this.Size = new Size(boxWidth, boxHeight);
                        timer2.Enabled = true;
                    }
                }
            }
        }

        void HandleMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            HandleMouseDown(e);
        }

        private void label1_MouseDown(object sender, MouseEventArgs e)
        {
            HandleMouseDown(e);
        }

        private void label2_MouseDown(object sender, MouseEventArgs e)
        {
            HandleMouseDown(e);
        }

        private void tableLayoutPanel1_MouseDown(object sender, MouseEventArgs e)
        {
            HandleMouseDown(e);
        }
    }
}
