using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TwitterWebHost.Properties;

namespace TwitterWebHost
{
    public partial class MainWindow : Window 
    {
        public MainWindow()
        {
            // Needs to happen before bringing up IE.
            webTwitter.SetCompatabilityMode(WebBrowserExtensions.CompatabilityMode.IE11_Edge);

            InitializeComponent();

            webTwitter.ObjectForScripting = new ExternalScript();

            // Save window position across launches.
            this.TrackPosition(() =>
            {
                return Settings.Default.MainWindowPosition;
            }, (settingData) =>
            {
                Settings.Default.MainWindowPosition = settingData;
            });

            Closed += (_, __) =>
            {
                Settings.Default.Save();
            };

            Loaded += (_, __) =>
            {
                // Open links in the default browser.
                webTwitter.AddNewWindowHandler(URL =>
                {
                    if (URL.StartsWith("http://") || URL.StartsWith("https://"))
                    {
                        Process.Start(URL);
                        return true; // handled
                    }
                    return false;
                });

                webTwitter.LoadCompleted += (_w, e) =>
                {
                    if (!Debugger.IsAttached)
                    {
                        webTwitter.SuppressScriptErrors();
                    }

                    Debug.WriteLine("Navigated: " + e.Uri);
                    InjectPageOverride(webTwitter);
                };

                webTwitter.Navigate("http://twitter.com/");
            };

            WatchDiskForChanges();
        }

        FileSystemWatcher fsw = null;
        void WatchDiskForChanges()
        {
            if (Debugger.IsAttached)
            {
                // Assume under VS and debug bin\Debug path, redirect to editor files.
                Environment.CurrentDirectory = Path.GetFullPath(Environment.CurrentDirectory + @"..\..\..\");
            }
            // For debugging, reload automatically.
            Debug.WriteLine("Watching " + Environment.CurrentDirectory);
            fsw = new FileSystemWatcher();
            fsw.Path = Environment.CurrentDirectory;
            fsw.IncludeSubdirectories = true;
            fsw.NotifyFilter = NotifyFilters.LastWrite;
            fsw.Filter = "*.*";
            fsw.Changed += (_f, __f) => InjectPageOverride(webTwitter);
            fsw.EnableRaisingEvents = true;
        }

        private void InjectPageOverride(WebBrowser wb)
        {
            Debug.WriteLine("Injecting third party accessories.");

            var script = RetryIO(() => { return File.ReadAllText("Profiles\\Default\\Default.js"); });
            var css = RetryIO(() => { return File.ReadAllText("Profiles\\Default\\Default.css"); });

            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (webTwitter.Document == null) return;

                wb.AddScript(script, "_i_js");
                wb.AddStyleSheet(css, "_i_css");
            }));
        }

        T RetryIO<T>(Func<T> action, int tries = 3)
        {
            for (int i = 0; i < tries; ++i)
            {
                try
                {
                    return action();
                }
                catch (IOException ex)
                {
                    Debug.WriteLine("RetryIO Exception: " + ex.Message);
                    System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(100));
                }
            }
            return default(T);
        }
    }
}
