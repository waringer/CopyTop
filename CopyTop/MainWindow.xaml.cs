using System;
using System.Windows;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Windows.Input;

namespace CopyTop
{
    public partial class MainWindow : Window
    {
        #region Form
        private static readonly Type MyType = typeof(MainWindow);
        private static readonly DependencyProperty ProductNameProperty = DependencyProperty.Register("ProductName", typeof(string), MyType, new FrameworkPropertyMetadata(string.Empty, null));
        private static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(string), MyType, new FrameworkPropertyMetadata(string.Empty, null));
        private static readonly DependencyProperty CopyrightProperty = DependencyProperty.Register("Copyright", typeof(string), MyType, new FrameworkPropertyMetadata(string.Empty, null));
        private static readonly DependencyProperty CompanyNameProperty = DependencyProperty.Register("CompanyName", typeof(string), MyType, new FrameworkPropertyMetadata(string.Empty, null));
        private static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), MyType, new FrameworkPropertyMetadata(string.Empty, null));
        public static readonly RoutedCommand OkCommand = new RoutedCommand("OkCommand", MyType);

        private string _Info = string.Empty;

        public string Info
        {
            get
            {
                return _Info;
            }
            set
            {
                _Info = value;

                if (!string.IsNullOrEmpty(_Info))
                    SetValue(DescriptionProperty, $"{AssemblyDescription}\n{_Info.Trim()}");
                else
                    SetValue(DescriptionProperty, AssemblyDescription);
            }
        }

        #region Assembly Attribute Accessors
        public static string AssemblyTitle
        {
            get
            {
                var _Attributes_ = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (_Attributes_.Length > 0)
                {
                    var _TitleAttribute_ = (AssemblyTitleAttribute)_Attributes_[0];
                    if (!string.IsNullOrEmpty(_TitleAttribute_.Title))
                        return _TitleAttribute_.Title;
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public static string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public static string AssemblyDescription
        {
            get
            {
                var _Attributes_ = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (_Attributes_.Length == 0)
                    return "*** n/a ***";

#if DEBUG
                return $"DEBUG VERSION\n.net Version : {Environment.Version}\n\n{((AssemblyDescriptionAttribute)_Attributes_[0]).Description}";
#else
                return ((AssemblyDescriptionAttribute)_Attributes_[0]).Description;
#endif
            }
        }

        public static string AssemblyProduct
        {
            get
            {
                var _Attributes_ = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (_Attributes_.Length == 0)
                    return "*** n/a ***";

                return ((AssemblyProductAttribute)_Attributes_[0]).Product;
            }
        }

        public static string AssemblyCopyright
        {
            get
            {
                var _Attributes_ = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (_Attributes_.Length == 0)
                    return "*** n/a ***";

                return ((AssemblyCopyrightAttribute)_Attributes_[0]).Copyright;
            }
        }

        public static string AssemblyCompany
        {
            get
            {
                var _Attributes_ = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (_Attributes_.Length == 0)
                    return "*** n/a ***";

                return ((AssemblyCompanyAttribute)_Attributes_[0]).Company;
            }
        }
        #endregion

        private void OKCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
        }
        #endregion

        private System.Windows.Forms.MenuItem _OnTop;
        private bool _AllowClose;

        public MainWindow()
        {
            InitializeComponent();

            SetValue(ProductNameProperty, AssemblyProduct);
            SetValue(VersionProperty, $"Version {AssemblyVersion}");
            SetValue(CopyrightProperty, AssemblyCopyright);
            SetValue(CompanyNameProperty, AssemblyCompany);
            SetValue(DescriptionProperty, AssemblyDescription);

            CommandBindings.Add(new CommandBinding(OkCommand, OKCommand_Executed));

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            if (!CreateMutex())
                Application.Current.Shutdown();
            else
            {
                CreateNotifyIcon(CreateNotifyMenu());
                CreateTimer();
            }

            _AllowClose = false;
            Closing += (sender, e) => { e.Cancel = !_AllowClose; Visibility = Visibility.Hidden; };

            Info = $@"Licence : BSD 3-clause license

Copyright (c) 2016, Holger Wolff <waringer@gmail.com>
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

 - Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 - Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
 - Neither the name of the creator nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

Icon by Recep Kütük: https://www.iconfinder.com/icons/728927/copy_data_document_documents_files_paper_sheet_icon";
        }

        private static bool CreateMutex()
        {
            var _MutexCreated_ = false;

            try
            {
                var _Mutex_ = new System.Threading.Mutex(true, System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(@"\", string.Empty), out _MutexCreated_);
                if (_MutexCreated_)
                {
                    Application.Current.Exit += (sender, e) =>
                    {
                        if (_Mutex_ != null)
                        {
                            if (_Mutex_.WaitOne(1000))
                                _Mutex_.ReleaseMutex();

                            _Mutex_.Close();
                            _Mutex_ = null;
                        }
                    };

                    _Mutex_.WaitOne();
                }

                return _MutexCreated_;
            }
            catch
            {
                return false;
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
            var _AutoStart_ = _trayMenu_.MenuItems.Add("Autostart", (sender, e) => { (sender as System.Windows.Forms.MenuItem).Checked = !(sender as System.Windows.Forms.MenuItem).Checked; RegistryTools.IsAutostart = (sender as System.Windows.Forms.MenuItem).Checked; });
            _trayMenu_.MenuItems.Add("Exit", (sender, e) => { _AllowClose = true; Application.Current.Shutdown(); });

            _OnTop.Checked = true;
            _AutoStart_.Checked = RegistryTools.IsAutostart;

            return _trayMenu_;
        }

        private void CreateNotifyIcon(System.Windows.Forms.ContextMenu Menu)
        {
            var _TrayIcon_ = new System.Windows.Forms.NotifyIcon() { Visible = true, ContextMenu = Menu, Text = "CopyTop", };
            _TrayIcon_.Icon = new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/CopyTop;component/Copy.ico")).Stream);
            _TrayIcon_.DoubleClick += (sender, e) => { Visibility = Visibility.Visible; };

            Application.Current.Exit += (sender, e) => { _TrayIcon_.Visible = false; _TrayIcon_.Dispose(); };
        }
    }

    internal static class RegistryTools
    {
        private static readonly string RegAutostartName = "CopyTop";
        private static readonly string RegPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        private static T UseReg<T>(Func<Microsoft.Win32.RegistryKey, T> Worker)
        {
            using (var _CurrentUser_ = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64))
            using (var _SubKey_ = _CurrentUser_.OpenSubKey(RegPath, true))
            {
                if (_SubKey_ == null)
                    return default(T);

                try
                {
                    return Worker(_SubKey_);
                }
                catch
                {
                    return default(T);
                }
            }
        }

        private static string RegRead(string KeyName)
        {
            return UseReg((SubKey) =>
            {
                return (string)SubKey.GetValue(KeyName);
            });
        }

        private static bool RegWrite(string KeyName, string Value)
        {
            return UseReg((SubKey) =>
            {
                SubKey.SetValue(KeyName, Value);
                return true;
            });
        }

        private static bool RegDelete(string KeyName)
        {
            return UseReg((SubKey) =>
            {
                SubKey.DeleteValue(KeyName);
                return true;
            });
        }

        public static bool IsAutostart
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
