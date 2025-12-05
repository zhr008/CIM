using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using WCFServices.Services;
using log4net;

namespace WCFServices
{
    class ServiceHost
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ServiceHost));
        private System.ServiceModel.ServiceHost mesServiceHost;
        
        static void Main(string[] args)
        {
            // Configure log4net
            log4net.Config.XmlConfigurator.Configure();
            
            log.Info("WCF MES Service starting...");
            
            var service = new ServiceHost();
            service.Start();
            
            Console.WriteLine("MES Service is running...");
            Console.WriteLine("Press any key to stop the service...");
            Console.ReadKey();
            
            service.Stop();
            
            log.Info("WCF MES Service stopped");
        }
        
        public void Start()
        {
            try
            {
                // Create service instance
                var serviceInstance = new MesService();
                
                // Define service URI
                Uri baseAddress = new Uri("http://localhost:8080/MesService");
                
                // Create service host
                mesServiceHost = new System.ServiceModel.ServiceHost(serviceInstance, baseAddress);
                
                // Add service endpoint
                var binding = new BasicHttpBinding();
                binding.MaxReceivedMessageSize = 2147483647; // Max size
                binding.ReaderQuotas.MaxArrayLength = 2147483647;
                binding.ReaderQuotas.MaxStringContentLength = 2147483647;
                
                mesServiceHost.AddServiceEndpoint(
                    typeof(WCFServices.Contracts.IMesService),
                    binding,
                    "");
                
                // Enable metadata exchange
                ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.HttpGetEnabled = true;
                mesServiceHost.Description.Behaviors.Add(smb);
                
                // Open the service
                mesServiceHost.Open();
                
                log.Info("MES Service started successfully");
                Console.WriteLine("MES Service started successfully at " + baseAddress);
            }
            catch (Exception ex)
            {
                log.Error($"Error starting MES Service: {ex.Message}", ex);
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        public void Stop()
        {
            try
            {
                if (mesServiceHost != null)
                {
                    mesServiceHost.Close();
                    log.Info("MES Service stopped");
                    Console.WriteLine("MES Service stopped");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error stopping MES Service: {ex.Message}", ex);
                Console.WriteLine($"Error stopping service: {ex.Message}");
            }
        }
    }
}