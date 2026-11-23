using System.Diagnostics;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace TechHRMS.Desktop;

public sealed class MainForm : Form
{
    private readonly WebView2 browser = new() { Dock = DockStyle.Fill };
    private readonly ToolStripStatusLabel status = new("Đang kết nối TechHRMS…") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripButton retry = new("Tải lại") { AccessibleName = "Tải lại TechHRMS" };
    private readonly Label errorPanel = new()
    {
        Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Visible = false,
        BackColor = Color.FromArgb(247, 250, 249), ForeColor = Color.FromArgb(24, 46, 55),
        Font = new Font("Segoe UI", 12), Padding = new Padding(40)
    };
    private Uri? website;
    private bool initializing;

    public MainForm()
    {
        Text = "TechHRMS | Không gian làm việc";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(850, 600);
        Icon = SystemIcons.Application;
        var bar = new StatusStrip { BackColor = Color.White, SizingGrip = false };
        var openInBrowser = new ToolStripButton("Mở trên trình duyệt") { AccessibleName = "Mở TechHRMS trong trình duyệt mặc định" };
        var runtimeHelp = new ToolStripButton("WebView2 Runtime") { ToolTipText = "Trang tải Microsoft Edge WebView2 Runtime" };
        retry.Click += async (_, _) => { if (browser.CoreWebView2 is null) await InitializeBrowserAsync(); else NavigateHome(); };
        openInBrowser.Click += (_, _) => { if (website is not null) OpenExternal(website.AbsoluteUri); };
        runtimeHelp.Click += (_, _) => OpenExternal("https://developer.microsoft.com/microsoft-edge/webview2/");
        bar.Items.AddRange([status, retry, openInBrowser, runtimeHelp]);
        Controls.Add(browser);
        Controls.Add(errorPanel);
        Controls.Add(bar);
        Shown += async (_, _) => await InitializeBrowserAsync();
    }

    private async Task InitializeBrowserAsync()
    {
        if (initializing || IsDisposed) return;
        initializing = true;
        retry.Enabled = false;
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "desktop-settings.json");
            using var config = JsonDocument.Parse(await File.ReadAllTextAsync(path));
            var configured = config.RootElement.GetProperty("websiteUrl").GetString();
            if (!Uri.TryCreate(configured, UriKind.Absolute, out website)
                || (website.Scheme != Uri.UriSchemeHttps && !(website.IsLoopback && website.Scheme == Uri.UriSchemeHttp))
                || !string.IsNullOrEmpty(website.UserInfo))
                throw new InvalidOperationException("websiteUrl phải là địa chỉ HTTPS hợp lệ (hoặc HTTP localhost để phát triển).");

            // Program Files is read-only for normal users. Store cookies/cache under their own profile.
            var userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TechHRMS", "WebView2");
            Directory.CreateDirectory(userData);
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userData);
            await browser.EnsureCoreWebView2Async(environment);
            browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            browser.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            browser.CoreWebView2.NavigationStarting += (_, e) =>
            {
                if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri)) { e.Cancel = true; return; }
                if (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                {
                    if (uri.GetLeftPart(UriPartial.Authority) != website.GetLeftPart(UriPartial.Authority))
                    {
                        e.Cancel = true;
                        if (e.IsUserInitiated) OpenExternal(uri.AbsoluteUri);
                        return;
                    }
                }
                else { e.Cancel = true; return; }
                status.Text = "Đang tải… Lần truy cập đầu tiên có thể cần chờ máy chủ khởi động.";
            };
            browser.CoreWebView2.NavigationCompleted += (_, e) =>
            {
                retry.Enabled = true;
                if (e.IsSuccess)
                {
                    errorPanel.Visible = false;
                    status.Text = "TechHRMS • Dữ liệu dùng chung với website";
                }
                else ShowError("Chưa tải được website.\n\nKiểm tra Internet, chờ một lúc rồi nhấn Tải lại.\nBạn cũng có thể mở cùng website trên trình duyệt.\n\nMã kết nối: " + e.WebErrorStatus);
            };
            browser.CoreWebView2.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                if (e.IsUserInitiated) OpenExternal(e.Uri);
            };
            browser.CoreWebView2.ProcessFailed += (_, _) =>
                ShowError("Thành phần hiển thị vừa dừng.\nHãy đóng TechHRMS rồi mở lại ứng dụng.");
            NavigateHome();
        }
        catch (Exception e)
        {
            ShowError("Chưa khởi động được TechHRMS.\n\nNếu máy chưa có Microsoft Edge WebView2 Runtime, dùng nút WebView2 Runtime bên dưới để cài đặt.\n\nChi tiết: " + e.Message);
        }
        finally { initializing = false; retry.Enabled = true; }
    }

    private void NavigateHome()
    {
        errorPanel.Visible = false;
        if (website is not null && browser.CoreWebView2 is not null)
            browser.CoreWebView2.Navigate(website.AbsoluteUri);
    }

    private void ShowError(string message)
    {
        errorPanel.Text = message;
        errorPanel.Visible = true;
        errorPanel.BringToFront();
        status.Text = "Chưa kết nối • Có thể thử lại";
    }

    private void OpenExternal(string target)
    {
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && !(uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp))) return;
        try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception e) { MessageBox.Show(this, "Không mở được trình duyệt: " + e.Message, "TechHRMS"); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) browser.Dispose();
        base.Dispose(disposing);
    }
}
