using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Migration.Common.Log;

namespace WikiImport
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            VersionInfo.PrintInfoMessage("Work Item Importer");

            try
            {
                var cmd = new ImportCommandLine(args);
                cmd.Run();
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Application stopped due to an unexpected exception", LogLevel.Critical);
            }
        }
    }
}
