using System;
using System.Windows;
using System.Drawing;
using System.Runtime.InteropServices;

namespace CopyTop
{
    public partial class MainWindow : Window
    {
        private static readonly string RegAutostartName = "CopyTop";
        private System.Windows.Forms.MenuItem _OnTop;
        private System.Threading.Mutex _Mutex;

        public MainWindow()
        {
            var _MutexCreated_ = false;

            try
            {
                _Mutex = new System.Threading.Mutex(true, RegAutostartName, out _MutexCreated_);
                Application.Current.Exit += (sender, e) => {
                    if (_Mutex != null)
                    {
                        if (_Mutex.WaitOne(1000))
                            _Mutex.ReleaseMutex();

                        _Mutex.Close();
                        _Mutex = null;
                    }
                };
            }
            catch
            {
                _MutexCreated_ = false;
            }

            if (!_MutexCreated_)
            {
                Application.Current.Shutdown();
            }
            else
            {
                _Mutex.WaitOne();

                CreateNotifyIcon(CreateNotifyMenu());
                CreateTimer();
            }
        }

        private void CreateTimer()
        {
            var _Check_ = new System.Threading.Timer((e) =>
            {
                var _WindowHandle_ = Native.FindWindow("OperationStatusWindow", null);
                var _WindowStyleEx_ = Native.GetWindowLong(_WindowHandle_, Native.GWL_EXSTYLE);

                if (_OnTop.Checked)
                {
                    if ((_WindowStyleEx_ & Native.WS_EX_TOPMOST) != Native.WS_EX_TOPMOST)
                    {
                        Native.SetForegroundWindow(_WindowHandle_);
                        Native.BringWindowToTop(_WindowHandle_);
                        Native.SetWindowPos(_WindowHandle_, Native.HWND_TOPMOST, 0, 0, 0, 0, Native.TOPMOST_FLAGS);
                    }
                }
                else
                {
                    if ((_WindowStyleEx_ & Native.WS_EX_TOPMOST) == Native.WS_EX_TOPMOST)
                    {
                        Native.SetForegroundWindow(_WindowHandle_);
                        Native.BringWindowToTop(_WindowHandle_);
                        Native.SetWindowPos(_WindowHandle_, Native.HWND_NOTOPMOST, 0, 0, 0, 0, Native.TOPMOST_FLAGS);
                    }
                }
            }, null, 500, 500);

            Application.Current.Exit += (sender, e) => { _Check_.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite); };
        }

        private System.Windows.Forms.ContextMenu CreateNotifyMenu()
        {
            var _trayMenu_ = new System.Windows.Forms.ContextMenu();
            _OnTop = _trayMenu_.MenuItems.Add("On Top", (sender, e) => { (sender as System.Windows.Forms.MenuItem).Checked = !(sender as System.Windows.Forms.MenuItem).Checked; });
            var _AutoStart_ = _trayMenu_.MenuItems.Add("Autostart", (sender, e) => { (sender as System.Windows.Forms.MenuItem).Checked = !(sender as System.Windows.Forms.MenuItem).Checked; IsAutostart = (sender as System.Windows.Forms.MenuItem).Checked; });
            _trayMenu_.MenuItems.Add("Exit", (sender, e) => { Application.Current.Shutdown(); });

            _OnTop.Checked = true;
            _AutoStart_.Checked = IsAutostart;

            return _trayMenu_;
        }

        private void CreateNotifyIcon(System.Windows.Forms.ContextMenu Menu)
        {
            var _TrayIcon_ = new System.Windows.Forms.NotifyIcon() { Visible = true, ContextMenu = Menu, Text = "CopyTop", };
            _TrayIcon_.Icon = new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/CopyTop;component/Copy.ico")).Stream);

            Application.Current.Exit += (sender, e) => { _TrayIcon_.Visible = false; _TrayIcon_.Dispose(); };
        }

        private static string RegRead(string KeyName)
        {
            using (var _CurrentUser_ = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64))
            using (var _SubKey_ = _CurrentUser_.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (_SubKey_ == null)
                    return string.Empty;

                try
                {
                    return (string)_SubKey_.GetValue(KeyName);
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        private static bool RegWrite(string KeyName, string Value)
        {
            using (var _CurrentUser_ = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64))
            using (var _SubKey_ = _CurrentUser_.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (_SubKey_ == null)
                    return false;

                try
                {
                    _SubKey_.SetValue(KeyName, Value);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static bool RegDelete(string KeyName)
        {
            using (var _CurrentUser_ = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64))
            using (var _SubKey_ = _CurrentUser_.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (_SubKey_ == null)
                    return false;

                try
                {
                    _SubKey_.DeleteValue(KeyName);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static bool IsAutostart
        {
            get
            {
                return !string.IsNullOrEmpty(RegRead(RegAutostartName));
            }
            set
            {
                if (IsAutostart != value)
                {
                    if (value)
                        RegWrite(RegAutostartName, $"\"{System.Reflection.Assembly.GetExecutingAssembly().Location}\"");
                    else
                        RegDelete(RegAutostartName);
                }
            }
        }
    }

    internal static class Native
    {
        public static readonly int GWL_EXSTYLE = (-20);
        public static readonly uint WS_EX_TOPMOST = 0x0008;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public static readonly uint SWP_NOSIZE = 0x0001;
        public static readonly uint SWP_NOMOVE = 0x0002;
        public static readonly uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32", EntryPoint = "BringWindowToTop")]
        public static extern bool BringWindowToTop(IntPtr wHandle);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}
