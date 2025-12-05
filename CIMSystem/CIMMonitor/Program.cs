using System;
using System.IO;
using System.Windows.Forms;
using CIMMonitor.Forms;
using log4net;
using log4net.Config;

namespace CIMMonitor
{
    static class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        
        [STAThread]
        static void Main()
        {
            // Configure log4net
            XmlConfigurator.Configure();
            
            log.Info("CIM Monitor application starting...");
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                log.Error($"Unhandled exception in application: {ex.Message}", ex);
                MessageBox.Show($"An error occurred: {ex.Message}", "Application Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            log.Info("CIM Monitor application ended");
        }
    }
}