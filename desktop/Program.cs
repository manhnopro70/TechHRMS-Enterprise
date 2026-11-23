namespace TechHRMS.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var mutex = new Mutex(true, @"Local\TechHRMS.Desktop", out var firstInstance);
        if (!firstInstance)
        {
            MessageBox.Show("TechHRMS đang mở. Hãy chọn cửa sổ TechHRMS trên thanh tác vụ.",
                "TechHRMS", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Application.Run(new MainForm());
    }
}
