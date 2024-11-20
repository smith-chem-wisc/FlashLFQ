using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Util
{
    public class Misc
    {
        // Does the same thing as Process.Start() except it works on .NET Core
        // hello
        public static void StartProcess(string path)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(path)
            {
                UseShellExecute = true
            };
            p.Start();
        }
    }
}
