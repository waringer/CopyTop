using System;

namespace CopyTop
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var _App_ = new App();
            _App_.InitializeComponent();
            _App_.Run();
        }
    }
}
