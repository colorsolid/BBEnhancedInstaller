using System.Drawing.Text;
using System.IO;
using System.Reflection;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        string installPath;
        string backupsPath;
        string gameFilesPath = GetRelativeFilePath("GAME FILES", "dvdroot_ps4");
        bool? overwriteBackups = null;

        var installedFiles = new List<string>();

        if (args.Length > 0 && Path.Exists(args[0]))
        {
            installPath = Path.GetFullPath(args[0]);
        }
        else
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "Select the mod destination.",
                UseDescriptionForTitle = true,
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                installPath = dialog.SelectedPath;
            }
            else
            {
                Console.WriteLine("No path selected");
                return;
            }
        }

        Prompts();

        void Prompts()
        {
            string txtPath = Path.Combine(installPath, "bbe.txt");
            backupsPath = Path.Join(installPath, "_BBE_backup");
            DialogResult result;
            if (Path.Exists(txtPath))
            {
                result = MessageBox.Show(
                    "Mod found in selected folder. Remove mod and restore backups?",
                    "Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly
                );

                if (result == DialogResult.Yes)
                {
                    RemoveFiles();
                    bool backupsRestored = RestoreBackups();
                    MessageBox.Show($"Mod removed, backups {(backupsRestored ? "" : "could not be ")}restored.");
                    return;
                }

                result = MessageBox.Show(
                    "Ovewrite existing mod installation?",
                    "Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly
                );

                if (result != DialogResult.Yes)
                {
                    return;
                }
                Console.WriteLine("Overwriting mod if exists");

                result = MessageBox.Show(
                    "Overwrite backups?",
                    "Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly
                );

                overwriteBackups = result == DialogResult.Yes;
                if (overwriteBackups == true)
                {
                    Console.WriteLine("Overwriting backups");
                }
                else
                {
                    Console.WriteLine("Not overwriting backups");
                }
            }

            result = MessageBox.Show(
                $"Install to \"{installPath}\"?{(overwriteBackups != false ? "\nBackups will be created." : "\nExisting backups will be preserved.")}",
                "Final Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.DefaultDesktopOnly
            );

            if (result != DialogResult.Yes)
            {
                Console.WriteLine("Installation cancelled.");
                return;
            }

            try
            {
                Install();
                MessageBox.Show("Installation complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                MessageBox.Show($"Error occurred during installation:\n\n{ex}");
            }
        }

        void Install()
        {
            string[] allFiles = Directory.GetFiles(gameFilesPath, "*.*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(gameFilesPath, f)).ToArray();

            foreach (string file in allFiles)
            {
                CopyFile(file, gameFilesPath, installPath);
                installedFiles.Add(file);
            }
            string filesTxtPath = Path.Combine(installPath, "bbe.txt");
            string text = string.Join("\n", installedFiles);
            text = "# This file is needed for the uninstaller. Do not delete.\n" + text;
            File.WriteAllText(filesTxtPath, text);
        }

        bool RestoreBackups()
        {
            if (!Directory.Exists(backupsPath))
            {
                string err = $"Missing: \"{backupsPath}\"\nBackups folder not found. Skipping restore.";
                MessageBox.Show(err);
                Console.WriteLine(err);
                return false;
            }
            string[] allFiles = Directory.GetFiles(backupsPath, "*.*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(backupsPath, f)).ToArray();

            foreach (string file in allFiles)
            {
                CopyFile(file, backupsPath, installPath);
            }
            Directory.Delete(backupsPath, true);
            return true;
        }

        void RemoveFiles()
        {
            string filesTxtPath = Path.Combine(installPath, "bbe.txt");
            if (!Path.Exists(filesTxtPath))
            {
                return;
            }
            string txt = File.ReadAllText(filesTxtPath);
            string[] lines = txt.Split("\n").Where(l => !l.StartsWith("#")).ToArray();
            foreach (string line in lines)
            {
                string removePath = Path.Combine(installPath, line);
                Console.WriteLine($"Removing \"{removePath}\"");
                File.Delete(removePath);
            }
            File.Delete(filesTxtPath);
        }

        void CopyFile(string fileRelativePath, string sourceRoot, string destinationRoot)
        {
            string sourcePath = Path.GetFullPath(Path.Join(sourceRoot, fileRelativePath));
            string destinationPath = Path.GetFullPath(Path.Join(destinationRoot, fileRelativePath));
            if (File.Exists(destinationPath) && overwriteBackups != false)
            {
                string backupPath = Path.Join(backupsPath, fileRelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                Console.WriteLine($"Backing Up \"{destinationPath}\"  --->  \"{backupPath}\"");
                File.Copy(destinationPath, backupPath, true);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            Console.WriteLine($"Copying \"{sourcePath}\"  --->  \"{destinationPath}\"");
            File.Copy(sourcePath, destinationPath, true);
        }
    }

    static string GetRelativeFilePath(params string[] pathParts)
    {
        return GetRelativeFilePath(false, pathParts);
    }

    static string GetRelativeFilePath(bool shouldNotify = true, params string[] pathParts)
    {
        bool exists = false;
        string path = Path.Combine(new[] { Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) }.Concat(pathParts).ToArray()!);
        exists = Path.Exists(path);
        if (!exists)
        {
            throw new Exception("\"GAME FILES\" path not found");
        }
        return path;
    }
}
