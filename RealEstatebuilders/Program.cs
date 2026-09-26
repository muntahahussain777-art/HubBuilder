using HubBuilders.Model;
using System;
using System.Threading;
using System.Windows.Forms;

namespace HubBuilders
{
    internal static class Program
    {
        private static bool _activationChecked = false;

        [STAThread]
        private static void Main()
        {
            // single instance (machine-wide)
            using (var mtx = new Mutex(true, @"Global\RealEstatebuilders.ReceiptSuite", out bool isNew))
            {
                if (!isNew)
                {
                    // already running — second launch ko quietly exit
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                AppSafety.InstallGlobalHandlers();

                try
                {
                    if (!_activationChecked)
                    {
                        _activationChecked = true; // ensure one-time per process
                        if (!ActivationStore.IsActivated())
                        {
                            using (var act = new ActivationForm())
                            {
                                if (act.ShowDialog() != DialogResult.OK)
                                    return;
                            }
                        }
                    }

                    Application.Run(new LoginForm()); // apna start form
                }
                catch (Exception ex)
                {
                    AppSafety.ShowSoft(ex);
                }
            }
        }
    }
}
