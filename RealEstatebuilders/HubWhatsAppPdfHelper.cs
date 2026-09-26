using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace HubBuilders
{
    /// <summary>WhatsApp Desktop: PDF clipboard + auto-paste (URL file attach support nahi).</summary>
    internal static class HubWhatsAppPdfHelper
    {
        public static string WhatsAppOutFolder()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RealEstateBuilders", "WhatsAppOut");
            try { Directory.CreateDirectory(dir); } catch { }
            return dir;
        }

        public static string SavePdfForWhatsApp(byte[] pdfBytes, string fileBase)
        {
            if (pdfBytes == null || pdfBytes.Length == 0) return "";
            string safe = MakeFileSafe(fileBase);
            string path = Path.Combine(WhatsAppOutFolder(),
                safe + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
            File.WriteAllBytes(path, pdfBytes);
            return Path.GetFullPath(path);
        }

        public static bool PrepareClipboardForManualPaste(string pdfPath)
        {
            return CopyPdfToClipboardRobust(pdfPath);
        }

        /// <summary>Open chat + paste PDF. Clipboard paste se pehle copy hota hai.</summary>
        public static bool SendPdfToWhatsApp(string pdfPath, string phoneDigits, string message)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
                return false;
            if (string.IsNullOrWhiteSpace(phoneDigits))
                return false;

            pdfPath = Path.GetFullPath(pdfPath);
            EnsureWhatsAppDesktopRunning();
            OpenWhatsAppChat(phoneDigits, message);

            if (TryAttachPdfToWhatsApp(pdfPath))
                return true;

            CopyPdfToClipboardRobust(pdfPath);
            TryFocusWhatsAppWindow();
            return false;
        }

        public static void OpenExplorerWithPdf(string pdfPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath)) return;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "/select,\"" + Path.GetFullPath(pdfPath) + "\"",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        public static string NormalizePhoneToDigits(string input)
        {
            try
            {
                var digits = new string((input ?? "").Where(char.IsDigit).ToArray());
                if (digits.StartsWith("03") && digits.Length == 11)
                    digits = "92" + digits.Substring(1);
                return digits;
            }
            catch { return ""; }
        }

        private static bool CopyPdfToClipboardRobust(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
                return false;

            string full = Path.GetFullPath(pdfPath);
            for (int i = 0; i < 12; i++)
            {
                try
                {
                    var files = new StringCollection();
                    files.Add(full);
                    var data = new DataObject();
                    data.SetFileDropList(files);
                    Clipboard.Clear();
                    Clipboard.SetDataObject(data, true, 5, 200);

                    if (Clipboard.ContainsFileDropList())
                    {
                        var got = Clipboard.GetFileDropList();
                        if (got != null && got.Count > 0
                            && string.Equals(Path.GetFullPath(got[0]), full, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
                catch { }
                Thread.Sleep(120);
            }
            return false;
        }

        private static bool TryAttachPdfToWhatsApp(string pdfPath)
        {
            try
            {
                IntPtr hwnd = WaitForWhatsAppWindow(18000);
                if (hwnd == IntPtr.Zero)
                    return false;

                FocusWindowRobust(hwnd);
                Thread.Sleep(400);
                ClickWhatsAppComposeArea(hwnd);
                Thread.Sleep(350);

                if (!CopyPdfToClipboardRobust(pdfPath))
                    return false;

                FocusWindowRobust(hwnd);
                Thread.Sleep(200);
                SendKeyTap(VK_END);
                Thread.Sleep(80);
                SendKeyTap(VK_RETURN);
                Thread.Sleep(250);
                SendCtrlV();

                Thread.Sleep(900);
                SendCtrlV();
                Thread.Sleep(400);
                return true;
            }
            catch { return false; }
        }

        private static IntPtr WaitForWhatsAppWindow(int timeoutMs)
        {
            var sw = Stopwatch.StartNew();
            IntPtr hwnd = IntPtr.Zero;
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                hwnd = FindWhatsAppMainWindow();
                if (hwnd != IntPtr.Zero)
                {
                    Thread.Sleep(1200);
                    return hwnd;
                }
                Thread.Sleep(350);
            }
            return FindWhatsAppMainWindow();
        }

        private static void EnsureWhatsAppDesktopRunning()
        {
            if (FindWhatsAppMainWindow() != IntPtr.Zero)
                return;

            string[] paths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WhatsApp", "WhatsApp.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WhatsApp", "WhatsApp.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WhatsApp", "WhatsApp.exe")
            };

            foreach (string p in paths)
            {
                try
                {
                    if (!File.Exists(p)) continue;
                    Process.Start(new ProcessStartInfo { FileName = p, UseShellExecute = true });
                    Thread.Sleep(1500);
                    return;
                }
                catch { }
            }
        }

        private static void OpenWhatsAppChat(string digitsE164, string message)
        {
            try
            {
                string encoded = Uri.EscapeDataString(message ?? "");
                if (!TryOpenUrl("whatsapp://send?phone=" + digitsE164 + "&text=" + encoded))
                    TryOpenUrl("https://wa.me/" + digitsE164 + "?text=" + encoded);
            }
            catch { }
        }

        private static bool TryOpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                return true;
            }
            catch { return false; }
        }

        private static void TryFocusWhatsAppWindow()
        {
            IntPtr hwnd = FindWhatsAppMainWindow();
            if (hwnd != IntPtr.Zero)
                FocusWindowRobust(hwnd);
        }

        private static void FocusWindowRobust(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;

            uint targetThread = GetWindowThreadProcessId(hwnd, out _);
            uint currentThread = GetCurrentThreadId();
            bool attached = false;
            try
            {
                if (targetThread != currentThread)
                    attached = AttachThreadInput(currentThread, targetThread, true);

                AllowSetForegroundWindow(GetProcessIdFromHwnd(hwnd));
                if (IsIconic(hwnd))
                    ShowWindow(hwnd, SW_RESTORE);
                SetForegroundWindow(hwnd);
                BringWindowToTop(hwnd);
            }
            finally
            {
                if (attached)
                    AttachThreadInput(currentThread, targetThread, false);
            }
        }

        private static uint GetProcessIdFromHwnd(IntPtr hwnd)
        {
            GetWindowThreadProcessId(hwnd, out uint pid);
            return pid;
        }

        private static void ClickWhatsAppComposeArea(IntPtr hwnd)
        {
            try
            {
                if (!GetWindowRect(hwnd, out RECT rect))
                    return;

                int x = rect.Left + (rect.Right - rect.Left) * 3 / 5;
                int y = rect.Bottom - Math.Max(70, (rect.Bottom - rect.Top) / 8);
                SetCursorPos(x, y);
                Thread.Sleep(80);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(40);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            }
            catch { }
        }

        private static void SendKeyTap(ushort vk)
        {
            INPUT[] inputs =
            {
                KeyEvent(vk, false),
                KeyEvent(vk, true)
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void SendCtrlV()
        {
            INPUT[] inputs =
            {
                KeyEvent(VK_CONTROL, false),
                KeyEvent(VK_V, false),
                KeyEvent(VK_V, true),
                KeyEvent(VK_CONTROL, true)
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static INPUT KeyEvent(ushort vk, bool keyUp)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vk,
                        dwFlags = keyUp ? KEYEVENTF_KEYUP : 0u
                    }
                }
            };
        }

        private static IntPtr FindWhatsAppMainWindow()
        {
            IntPtr best = IntPtr.Zero;
            try
            {
                string[] names = { "WhatsApp", "whatsapp" };
                foreach (var name in names)
                {
                    foreach (var p in Process.GetProcessesByName(name))
                    {
                        try
                        {
                            if (p.MainWindowHandle == IntPtr.Zero) continue;
                            string title = (p.MainWindowTitle ?? "").Trim();
                            if (title.Length == 0) continue;
                            best = p.MainWindowHandle;
                            if (title.IndexOf("WhatsApp", StringComparison.OrdinalIgnoreCase) >= 0)
                                return best;
                        }
                        catch { }
                    }
                }

                foreach (var p in Process.GetProcessesByName("ApplicationFrameHost"))
                {
                    try
                    {
                        if (p.MainWindowHandle == IntPtr.Zero) continue;
                        string title = (p.MainWindowTitle ?? "").Trim();
                        if (title.IndexOf("WhatsApp", StringComparison.OrdinalIgnoreCase) >= 0)
                            return p.MainWindowHandle;
                    }
                    catch { }
                }
            }
            catch { }
            return best;
        }

        private static string MakeFileSafe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Receipt";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            name = name.Trim();
            return name.Length > 0 ? name : "Receipt";
        }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const ushort VK_END = 0x23;
        private const ushort VK_RETURN = 0x0D;
        private const ushort VK_CONTROL = 0x11;
        private const ushort VK_V = 0x56;
        private const int SW_RESTORE = 9;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(uint dwProcessId);
    }
}
