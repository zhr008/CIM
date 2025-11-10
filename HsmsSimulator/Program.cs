using System;
using System.Windows.Forms;

namespace HsmsSimulator
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 设置应用程序样式
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            // 启动主窗体
            Application.Run(new MainForm());
        }
    }
}
