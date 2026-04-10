using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;

public class SetupWizard : Form
{
    private Panel pnlWelcome, pnlDirectory, pnlProgress, pnlFinish;
    private TextBox txtPath;
    private ProgressBar progressBar;
    private string installPath;

    public SetupWizard()
    {
        this.Text = "GRIMDARK Stratagem Generator Setup";
        this.Size = new Size(500, 350);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;

        installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GRIMDARK Stratagem Generator");

        InitWelcomePanel();
        InitDirectoryPanel();
        InitProgressPanel();
        InitFinishPanel();

        this.Controls.Add(pnlWelcome);
    }

    private void InitWelcomePanel()
    {
        pnlWelcome = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        
        PictureBox pic = new PictureBox { ImageLocation = "wh40k_logo.png", SizeMode = PictureBoxSizeMode.Zoom, Location = new Point(20, 150), Size = new Size(150, 100) };
        try { pnlWelcome.Controls.Add(pic); } catch {} // Optional image rendering if ran unpacked

        pnlWelcome.Controls.Add(new Label { Text = "Welcome to the Setup Wizard", Font = new Font("Segoe UI", 16, FontStyle.Regular), Location = new Point(20, 20), AutoSize = true, ForeColor = Color.FromArgb(0, 51, 153) });
        pnlWelcome.Controls.Add(new Label { Text = "This wizard will guide you through the digital installation\nof the GRIMDARK Stratagem Generator v2.0.", Font = new Font("Segoe UI", 10, FontStyle.Regular), Location = new Point(20, 70), AutoSize = true });
        
        Button btnNext = new Button { Text = "Next >", Location = new Point(380, 270), Size = new Size(80, 25), BackColor = SystemColors.Control };
        btnNext.Click += (s, e) => { this.Controls.Remove(pnlWelcome); this.Controls.Add(pnlDirectory); };
        pnlWelcome.Controls.Add(btnNext);
    }

    private void InitDirectoryPanel()
    {
        pnlDirectory = new Panel { Dock = DockStyle.Fill };
        pnlDirectory.Controls.Add(new Label { Text = "Select Installation Directory", Font = new Font("Segoe UI", 12, FontStyle.Regular), Location = new Point(20, 20), AutoSize = true });
        pnlDirectory.Controls.Add(new Label { Text = "Setup will install the Stratagem Generator into the following folder.\nTo install in a different folder, click Browse and select another folder.", Font = new Font("Segoe UI", 9, FontStyle.Regular), Location = new Point(20, 55), AutoSize = true });

        txtPath = new TextBox { Text = installPath, Location = new Point(20, 100), Width = 350, ReadOnly = true };
        pnlDirectory.Controls.Add(txtPath);

        Button btnBrowse = new Button { Text = "Browse...", Location = new Point(380, 98), Size = new Size(80, 25) };
        btnBrowse.Click += (s, e) => {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog()) {
                if (fbd.ShowDialog() == DialogResult.OK) {
                    installPath = Path.Combine(fbd.SelectedPath, "GRIMDARK Stratagem Generator");
                    txtPath.Text = installPath;
                }
            }
        };
        pnlDirectory.Controls.Add(btnBrowse);

        Button btnInstall = new Button { Text = "Install", Location = new Point(380, 270), Size = new Size(80, 25) };
        btnInstall.Click += (s, e) => { this.Controls.Remove(pnlDirectory); this.Controls.Add(pnlProgress); PerformInstall(); };
        pnlDirectory.Controls.Add(btnInstall);
    }

    private void InitProgressPanel()
    {
        pnlProgress = new Panel { Dock = DockStyle.Fill };
        pnlProgress.Controls.Add(new Label { Text = "Installing...", Font = new Font("Segoe UI", 12, FontStyle.Regular), Location = new Point(20, 20), AutoSize = true });
        progressBar = new ProgressBar { Location = new Point(20, 70), Width = 440, Style = ProgressBarStyle.Marquee };
        pnlProgress.Controls.Add(progressBar);
    }

    private void InitFinishPanel()
    {
        pnlFinish = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        pnlFinish.Controls.Add(new Label { Text = "Installation Complete!", Font = new Font("Segoe UI", 16, FontStyle.Regular), Location = new Point(20, 20), AutoSize = true, ForeColor = Color.FromArgb(0, 153, 51) });
        pnlFinish.Controls.Add(new Label { Text = "The Stratagem Generator has been successfully installed.\nA shortcut has been created on your Desktop.", Font = new Font("Segoe UI", 10, FontStyle.Regular), Location = new Point(20, 70), AutoSize = true });

        Button btnFinish = new Button { Text = "Finish", Location = new Point(380, 270), Size = new Size(80, 25), BackColor = SystemColors.Control };
        btnFinish.Click += (s, e) => { this.Close(); };
        pnlFinish.Controls.Add(btnFinish);
    }

    private void PerformInstall()
    {
        System.Threading.ThreadPool.QueueUserWorkItem(_ => {
            try {
                if (!Directory.Exists(installPath)) Directory.CreateDirectory(installPath);

                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("package.zip")) {
                    if (stream != null) {
                        string tempZip = Path.Combine(Path.GetTempPath(), "package.zip");
                        using (FileStream fileStream = new FileStream(tempZip, FileMode.Create)) {
                            stream.CopyTo(fileStream);
                        }
                        
                        ZipFile.ExtractToDirectory(tempZip, installPath);
                        File.Delete(tempZip);
                    } else {
                        MessageBox.Show("Payload error: Archive package missing from installer build.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                CreateShortcut();

                this.Invoke((MethodInvoker)delegate {
                    this.Controls.Remove(pnlProgress);
                    this.Controls.Add(pnlFinish);
                });
            } catch (Exception ex) {
                MessageBox.Show("Installation failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Invoke((MethodInvoker)delegate { this.Close(); });
            }
        });
    }

    private void CreateShortcut()
    {
        try {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string shortcutLocation = Path.Combine(desktop, "Stratagems Generator v2.lnk");
            string targetPath = Path.Combine(installPath, "index.html");

            Type t = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(t);
            var shortcut = shell.CreateShortcut(shortcutLocation);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = installPath;
            shortcut.IconLocation = Path.Combine(installPath, "icon.ico");
            shortcut.Save();
        } catch { }
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new SetupWizard());
    }
}
