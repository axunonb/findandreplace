using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FindAndReplace.App
{
    internal class Tools
    {
        public static void LaunchBrowser(string url)
        {
            const int CO_E_APPNOTFOUND = unchecked((int) 0x800401F5);

            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
                Verb = "open"
            };

            try
            {
                // don't use WaitForExit for the process
                using var p = Process.Start(psi);
            }
            catch (Win32Exception w32Ex) when (w32Ex.NativeErrorCode == CO_E_APPNOTFOUND)
            {
                // Try to open Microsoft Edge or the Internet Explorer fallback
                try
                {
                    psi.FileName = "msedge.exe";
                    psi.Arguments = url;
                    using var p = Process.Start(psi);
                }
                catch
                {
                    psi.FileName = "IExplore.exe";
                    psi.Arguments = url;
                    using var p = Process.Start(psi);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Looks like you don't have a web browser installed or configured correctly. (Error: " + ex.Message + ")");
            }
        }
    }
}
