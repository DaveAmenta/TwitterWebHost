using mshtml;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Controls;

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

        public static ResultInterface QueryService<ServiceID, InterfaceID, ResultInterface>(this object self)
        {
            return (ResultInterface)((IServiceProvider)self).QueryService(typeof(ServiceID).GUID, typeof(InterfaceID).GUID);
        }
    }

    public static class WebBrowserExtensions
    {
        // Wrap the action in a class because the event handler must be an instance function
        class NewWindowWrapper
        {
            Func<string, bool> NewWindowHandler;

            public NewWindowWrapper(WebBrowser web, Func<string, bool> NewWindowHandler)
            {
                this.NewWindowHandler = NewWindowHandler;
                web.LoadCompleted += TryAttach;
            }

            void TryAttach(object sender, System.Windows.Navigation.NavigationEventArgs e)
            {
                var web = ((WebBrowser)sender);
                web.LoadCompleted -= TryAttach;
                // This QS call needs to happenon the Document, but it finds the Trident instance which is preserved across navigation.
                web.Document.QueryService<
                    SHDocVw.IWebBrowserApp, 
                    SHDocVw.IWebBrowser2,
                    SHDocVw.DWebBrowserEvents_Event>().NewWindow += WebBrowser_NewWindow;
            }

            void WebBrowser_NewWindow(string URL, int Flags, string TargetFrameName, ref object PostData, string Headers, ref bool Processed)
            {
                Debug.WriteLine(string.Format("WebBrowser_NewWindow: {0}", URL));
                Processed = NewWindowHandler(URL);
            }
        }

        public static void AddNewWindowHandler(this WebBrowser web, Func<string, bool> OpenNewWindowHandler)
        {
            new NewWindowWrapper(web, OpenNewWindowHandler);
        }

        public enum CompatabilityMode : uint
        {
            // http://msdn.microsoft.com/en-us/library/ie/ee330730(v=vs.85).aspx
            Default = IE7,
            IE7 = 0x1B58,
            IE11_Edge = 0x2AF9,
        }

        public static void SetCompatabilityMode(this WebBrowser web, CompatabilityMode mode)
        {
            Microsoft.Win32.Registry.SetValue
                (@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION",
                System.IO.Path.GetFileName(Environment.GetCommandLineArgs()[0]),
                (uint)mode,
                Microsoft.Win32.RegistryValueKind.DWord);
        }

        public static void AddScript(this WebBrowser web, string scriptText, string Title = "")
        {
            var doc = (HTMLDocument)web.Document;
            HTMLHeadElement head = (HTMLHeadElement)doc.getElementsByTagName("head").item(0);
            foreach (HTMLScriptElement scr in doc.getElementsByTagName("script"))
            {
                if (scr.title == Title)
                {
                    head.removeChild((IHTMLDOMNode)scr);
                    break;
                }
            }

            var script = (HTMLScriptElement)doc.createElement("script");
            script.type = "text/javascript";
            script.title = Title;
            script.innerText = scriptText;

            head.appendChild((IHTMLDOMNode)script);
        }

        public static void AddStyleSheet(this WebBrowser web, string cssText, string Title = "")
        {
            var doc = (HTMLDocument)web.Document;

            foreach (IHTMLStyleSheet existingStyle in doc.styleSheets)
            {
                if (existingStyle.title == Title)
                {
                    existingStyle.cssText = cssText;
                    return;
                }
            }

            var style = doc.createStyleSheet();
            style.title = Title;
            style.cssText = cssText;
        }

        public static void SuppressScriptErrors(this WebBrowser wb)
        {
            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            var objComWebBrowser = fiComWebBrowser.GetValue(wb);
            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { true });
        }
    }
}
