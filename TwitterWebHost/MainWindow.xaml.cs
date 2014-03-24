using Microsoft.Win32;
using mshtml;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TwitterWebHost
{
    public static class COM
    {
        [ComImport]
        [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IServiceProvider
        {
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object QueryService(ref Guid guidService, ref Guid riid);
        }

        public static object QueryService(this IServiceProvider svc, Guid service, Guid riid)
        {
            return svc.QueryService(ref service, ref riid);
        }
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            InitializeWebBrowser();

            Loaded += (_, __) =>
            {
                var args = Environment.GetCommandLineArgs();
                int refresh = 30;
                if (args.Length > 1)
                {
                    Navigate(args[1]);
                    if (args.Length > 2)
                    {
                        refresh = int.Parse(args[2]);
                    }
                }
                else
                {
                    Navigate("http://twitter.com/");
                }

                StartRefreshTimer(TimeSpan.FromSeconds(refresh));
            };
        }

        bool NewWindowHandlerAttached = false;
        void AttachNewWindowHandler(WebBrowser wb)
        {
            if (NewWindowHandlerAttached) { return; }
            // This QS call needs to happenon the Document, but it finds the Trident instance which is preserved across navigation.
            var web2 = ((COM.IServiceProvider)wb.Document).
                QueryService(typeof(SHDocVw.IWebBrowserApp).GUID, 
                             typeof(SHDocVw.IWebBrowser2).GUID);
            ((SHDocVw.DWebBrowserEvents_Event)web2).NewWindow += WebBrowser_NewWindow;
            NewWindowHandlerAttached = true;
        }

        void WebBrowser_NewWindow(string URL, int Flags, string TargetFrameName, ref object PostData, string Headers, ref bool Processed)
        {
            // Open in default browser.
            Debug.WriteLine(URL);
            Process.Start(URL); // Safe?
            Processed = true;
        }

        void FreezeBrowserVisuals()
        {
            imgTwitter.Source = CopyHwndVisualToImageSource(webTwitter);
            webTwitter.Visibility = Visibility.Hidden;
            imgTwitter.Visibility = Visibility.Visible;
        }

        void UnFreezeBrowserVisuals()
        {
            webTwitter.Visibility = Visibility.Visible;
            imgTwitter.Visibility = Visibility.Hidden;
        }

        void InitializeWebBrowser()
        {
            EnsureWebBrowserCompatabilityEmulationMode();

            webTwitter.LoadCompleted += (_, e) =>
            {
                Debug.WriteLine("Navigated: " + e.Uri);
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    UnFreezeBrowserVisuals();
                    Title = ((HTMLDocument)webTwitter.Document).title;
                    AttachNewWindowHandler(webTwitter);

                    InjectPageOverride(webTwitter);
                }));
            };
        }

        private void Navigate(string uri)
        {
            webTwitter.Navigate(uri); //, null, null, "User-Agent: Mozilla/5.0 (Linux; Android 4.1.1; Galaxy Nexus Build/JRO03C) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1025.166 Mobile Safari/535.19");
        }

        private void InjectPageOverride(WebBrowser wb)
        {
            HTMLDocument doc = (HTMLDocument)wb.Document;
            var style = doc.createStyleSheet();
            style.title = Environment.CurrentDirectory.GetHashCode().ToString(); // heheh
            style.cssText = @"

.dashboard {
    display: none;
}

.content-main {
    width: 100%;
}

.wrapper {
    width: 100%;
    padding: 0;
}

";
            /*
            style.cssText = @"
                #main_content
                {
                    background-color: black; 
                    color: white; 
                }

                .timeline .conversation-tweet
                {
                    background-color: black; 
                    color: white; 
                }


                *
                {
                    background-color: black; 
                    color: white; 
                }

                #brand_bar { 
                    display: none; 
                }
"; */
            /*
            style.cssText = @"
* {
    filter: invert(100%); }
";
            */

        }

        void StartRefreshTimer(TimeSpan ts)
        {
            Debug.WriteLine(string.Format("Refreshing every {0}ms", ts.TotalMilliseconds));
            Timer t = new Timer(ts.TotalMilliseconds);
            t.AutoReset = true;
            t.Elapsed += (_, __) =>
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    OnTick();
                }));
            };
            t.Start();
        }

        void OnTick()
        {
            var Document = ((HTMLDocument)webTwitter.Document);
            if (Document == null) return;

            var DocElement = ((IHTMLElement2)Document.documentElement);
            if (DocElement == null) return;

            if (Document.title.Contains("Compose")) return; // Composing new tweet
            if (Document.title.Contains("Reply")) return; // reply tweet
            Debug.WriteLine("Scroll Position: " + DocElement.scrollTop);
            if (DocElement.scrollTop > 0) return; // Not looking for new tweets

            RefreshNoflicker();
        }

        void RefreshNoflicker()
        {
            // Flip to the image, navigate and then flip back when the navigation completes or on failure.
            // TODO: a watchdog pattern may be necessary here.
            FreezeBrowserVisuals();
            try
            {
                Navigate(webTwitter.Source.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to refresh: " + ex.Message);
                UnFreezeBrowserVisuals();
            }
        }

        ImageSource CopyHwndVisualToImageSource(Visual v)
        {
            // Copy the web browser into an image which we display while the refresh
            // is occuring, which avoids flicker of elements on the page.
            Rect bounds = VisualTreeHelper.GetDescendantBounds(v);
            System.Windows.Point ptWindow = (webTwitter.PointToScreen(bounds.TopLeft));
            // Desktop DPI scale
            var source = PresentationSource.FromVisual(Application.Current.MainWindow);
            int Width = (int)(bounds.Width * source.CompositionTarget.TransformToDevice.M11);
            int Height = (int)(bounds.Height * source.CompositionTarget.TransformToDevice.M22);
            // Copy the control into a bitmap.
            Bitmap bmpImage = new Bitmap(Width, Height);
            Graphics.FromImage(bmpImage).CopyFromScreen((int)ptWindow.X, (int)ptWindow.Y, 0, 0, bmpImage.Size);
            // Convert to image source
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bmpImage.GetHbitmap(),
                IntPtr.Zero,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(bmpImage.Size.Width, bmpImage.Size.Height));
        }

        void EnsureWebBrowserCompatabilityEmulationMode()
        {
            Registry.SetValue
                (@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION",
                System.IO.Path.GetFileName(Environment.GetCommandLineArgs()[0]),
                0x2AF9, // IE11 Edge
                RegistryValueKind.DWord);
        }
    }
}
