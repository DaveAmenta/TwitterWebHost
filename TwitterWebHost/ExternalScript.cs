using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TwitterWebHost
{
    [ComVisibleAttribute(true)]
    public class ExternalScript
    {
        public void log(string msg)
        {
            Debug.WriteLine("JS console: " + msg);
        }
    }
}
