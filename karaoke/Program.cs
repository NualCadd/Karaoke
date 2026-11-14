using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace karaoke
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            while (true)
            {
                karaoke.Helpers.UserSession.Clear();
                using (var login = new FrmLogin())
                {
                    if (login.ShowDialog() != DialogResult.OK)
                        return;
                } 

                DialogResult result;
                try
                {
                    using (var main = new FrmMain())
                    {
                        Application.Run(main);
                        result = main.DialogResult;
                    }
                }
                finally
                {
                    karaoke.Helpers.UserSession.Clear();
                }

                
                if (result != DialogResult.Retry)
                    return;
            }
        }
    }
}
