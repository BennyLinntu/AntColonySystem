using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AntColonyWorkbench
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new MainForm();
            string demo = GetArgument(args, "--demo");
            string run = GetArgument(args, "--run");
            string tsp = args.FirstOrDefault(a => a.EndsWith(".tsp", StringComparison.OrdinalIgnoreCase));

            if (demo != null || tsp != null)
            {
                string path = tsp ?? (demo.Length > 0 ? demo : FindDemoFile());
                if (path != null && File.Exists(path))
                {
                    int iterations = 0;
                    if (run != null) int.TryParse(run, out iterations);
                    form.LoadDemo(path, iterations);
                }
            }

            Application.Run(form);
        }

        private static string GetArgument(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i + 1 < args.Length ? args[i + 1] : string.Empty;
                if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                    return args[i].Substring(name.Length + 1);
            }
            return null;
        }

        private static string FindDemoFile()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
            {
                string candidate = Path.Combine(dir, "samples", "berlin52.tsp");
                if (File.Exists(candidate)) return candidate;
                dir = Directory.GetParent(dir)?.FullName;
            }
            return null;
        }
    }
}

