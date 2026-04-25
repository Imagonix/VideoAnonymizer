using System.Diagnostics;
using System.Windows.Forms;

var root = AppContext.BaseDirectory;
var hostPath = Path.Combine(root, "bin", "VideoAnonymizer.StandaloneHost.exe");

if (!File.Exists(hostPath))
{
    MessageBox.Show(
        $"The application host was not found:{Environment.NewLine}{hostPath}",
        "VideoAnonymizer",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error);
    return 1;
}

Process.Start(new ProcessStartInfo
{
    FileName = hostPath,
    WorkingDirectory = Path.GetDirectoryName(hostPath)!,
    UseShellExecute = true
});

return 0;
