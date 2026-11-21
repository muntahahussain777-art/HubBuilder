using System;
using System.Threading;
using System.Windows.Forms;

namespace HubBuilders
{
    /// <summary>
    /// App-wide crash shield. Does not change business logic —
    /// only catches unexpected exceptions so the client never sees a hard crash.
    /// </summary>
    public static class AppSafety
    {
        private static int _showing;

        public static void InstallGlobalHandlers()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException -= OnUiThreadException;
            Application.ThreadException += OnUiThreadException;
            AppDomain.CurrentDomain.UnhandledException -= OnDomainUnhandled;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandled;
        }

        private static void OnUiThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ShowSoft(e.Exception);
        }

        private static void OnDomainUnhandled(object sender, UnhandledExceptionEventArgs e)
        {
            ShowSoft(e.ExceptionObject as Exception);
        }

        /// <summary>Test hook — MessageBox band (headless MainClass.SQL tests).</summary>
        public static bool SuppressUi { get; set; }

        public static void ShowSoft(Exception ex)
        {
            if (SuppressUi) return;
            if (Interlocked.Exchange(ref _showing, 1) == 1) return;
            try
            {
                MessageBox.Show(
                    "Something went wrong. Please try again.\n\nIf it continues, restart the app.",
                    "Hub Builders",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch
            {
                // last resort — never throw from safety net
            }
            finally
            {
                Interlocked.Exchange(ref _showing, 0);
            }
        }

        /// <summary>Run action; on failure show soft message and return false. Success path unchanged.</summary>
        public static bool Run(Action action)
        {
            if (action == null) return true;
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                ShowSoft(ex);
                return false;
            }
        }

        /// <summary>Run func; on failure return fallback. Success path unchanged.</summary>
        public static T Run<T>(Func<T> func, T fallback = default(T))
        {
            if (func == null) return fallback;
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                ShowSoft(ex);
                return fallback;
            }
        }

        public static void SafeClose(System.Data.SQLite.SQLiteConnection con)
        {
            try
            {
                if (con != null && con.State != System.Data.ConnectionState.Closed)
                    con.Close();
            }
            catch { }
        }
    }
}
