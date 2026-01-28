using System;
using System.Collections.Generic;
using System.ServiceModel;
using WCFServices.Contracts;
using Common.Models;
using log4net;
using CoreWCF;

namespace WCFServices.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = CoreWCF.ConcurrencyMode.Multiple)]
    public class MesService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MesService));
        
        // In-memory storage for demonstration - in real implementation, this would be a database
        private static Dictionary<string, EquipmentStatus> equipmentStatuses = new Dictionary<string, EquipmentStatus>();
        private static Dictionary<string, ProductionData> productionData = new Dictionary<string, ProductionData>();
        private static Dictionary<string, List<AlarmInfo>> alarms = new Dictionary<string, List<AlarmInfo>>();
        
        static MesService()
        {
            // Initialize some sample data
            equipmentStatuses["EQP1"] = new EquipmentStatus
            {
                EquipmentID = "EQP1",
                Status = "Running",
                DataPoints = new Dictionary<string, object>
                {
                    { "Temperature", 25.5 },
                    { "Pressure", 1.2 },
                    { "FlowRate", 100.0 }
                }
            };
            
            equipmentStatuses["EQP2"] = new EquipmentStatus
            {
                EquipmentID = "EQP2",
                Status = "Idle",
                DataPoints = new Dictionary<string, object>
                {
                    { "Temperature", 22.0 },
                    { "Status", "Ready" }
                }
            };
        }
        
        public EquipmentStatus GetEquipmentStatus(string equipmentId)
        {
            log.Info($"GetEquipmentStatus called for {equipmentId}");
            
            if (equipmentStatuses.ContainsKey(equipmentId))
            {
                var status = equipmentStatuses[equipmentId];
                status.LastUpdate = DateTime.Now;
                return status;
            }
            
            log.Warn($"Equipment {equipmentId} not found");
            return new EquipmentStatus
            {
                EquipmentID = equipmentId,
                Status = "Unknown"
            };
        }
        
        public bool SetEquipmentStatus(string equipmentId, string status)
        {
            log.Info($"SetEquipmentStatus called for {equipmentId} with status {status}");
            
            if (equipmentStatuses.ContainsKey(equipmentId))
            {
                equipmentStatuses[equipmentId].Status = status;
                equipmentStatuses[equipmentId].LastUpdate = DateTime.Now;
            }
            else
            {
                equipmentStatuses[equipmentId] = new EquipmentStatus
                {
                    EquipmentID = equipmentId,
                    Status = status,
                    LastUpdate = DateTime.Now
                };
            }
            
            return true;
        }
        
        public ProductionData GetProductionData(string batchId)
        {
            log.Info($"GetProductionData called for batch {batchId}");
            
            if (productionData.ContainsKey(batchId))
            {
                return productionData[batchId];
            }
            
            log.Warn($"Production data for batch {batchId} not found");
            return new ProductionData
            {
                BatchID = batchId
            };
        }
        
        public bool UpdateProductionData(ProductionData productionDataParam)
        {
            log.Info($"UpdateProductionData called for batch {productionDataParam.BatchID}");
            
            productionData[productionDataParam.BatchID] = productionDataParam;
            return true;
        }
        
        public string ProcessEquipmentMessage(EquipmentMessage message)
        {
            log.Info($"ProcessEquipmentMessage called for {message.EquipmentID}: {message.MessageContent}");
            
            // Process the equipment message based on its type
            switch (message.MessageType)
            {
                case "HSMS":
                    // Handle HSMS message
                    ProcessHsmsMessage(message);
                    break;
                case "OPC_DATA":
                    // Handle OPC data message
                    ProcessOpcDataMessage(message);
                    break;
                case "ALARM":
                    // Handle alarm message
                    ProcessAlarmMessage(message);
                    break;
                default:
                    log.Info($"Unknown message type: {message.MessageType}");
                    break;
            }
            
            return "Message processed successfully";
        }
        
        private void ProcessHsmsMessage(EquipmentMessage message)
        {
            log.Info($"Processing HSMS message from {message.EquipmentID}");
            
            // Update equipment status based on HSMS message
            if (!equipmentStatuses.ContainsKey(message.EquipmentID))
            {
                equipmentStatuses[message.EquipmentID] = new EquipmentStatus
                {
                    EquipmentID = message.EquipmentID,
                    Status = "Connected"
                };
            }
            
            equipmentStatuses[message.EquipmentID].LastUpdate = DateTime.Now;
            
            // Parse message content and update data points
            if (message.Properties.ContainsKey("Data"))
            {
                foreach (var kvp in (Dictionary<string, object>)message.Properties["Data"])
                {
                    equipmentStatuses[message.EquipmentID].DataPoints[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void ProcessOpcDataMessage(EquipmentMessage message)
        {
            log.Info($"Processing OPC data message from {message.EquipmentID}");
            
            // Extract tag name and value from message
            if (message.Properties.ContainsKey("TagName") && message.Properties.ContainsKey("Value"))
            {
                var tagName = message.Properties["TagName"].ToString();
                var value = message.Properties["Value"];
                
                if (!equipmentStatuses.ContainsKey(message.EquipmentID))
                {
                    equipmentStatuses[message.EquipmentID] = new EquipmentStatus
                    {
                        EquipmentID = message.EquipmentID,
                        Status = "Connected"
                    };
                }
                
                equipmentStatuses[message.EquipmentID].DataPoints[tagName] = value;
                equipmentStatuses[message.EquipmentID].LastUpdate = DateTime.Now;
            }
        }
        
        private void ProcessAlarmMessage(EquipmentMessage message)
        {
            log.Info($"Processing alarm message from {message.EquipmentID}");
            
            // Create alarm based on message
            var alarm = new AlarmInfo
            {
                AlarmID = Guid.NewGuid().ToString(),
                EquipmentID = message.EquipmentID,
                AlarmCode = "ALM001",
                Description = message.MessageContent,
                Timestamp = DateTime.Now,
                Acknowledged = false
            };
            
            if (!alarms.ContainsKey(message.EquipmentID))
            {
                alarms[message.EquipmentID] = new List<AlarmInfo>();
            }
            
            alarms[message.EquipmentID].Add(alarm);
        }
        
        public bool SendAlarm(string equipmentId, string alarmCode, string description)
        {
            log.Info($"SendAlarm called for {equipmentId}, code: {alarmCode}, description: {description}");
            
            var alarm = new AlarmInfo
            {
                AlarmID = Guid.NewGuid().ToString(),
                EquipmentID = equipmentId,
                AlarmCode = alarmCode,
                Description = description,
                Timestamp = DateTime.Now,
                Acknowledged = false
            };
            
            if (!alarms.ContainsKey(equipmentId))
            {
                alarms[equipmentId] = new List<AlarmInfo>();
            }
            
            alarms[equipmentId].Add(alarm);
            
            return true;
        }
        
        public string[] GetActiveAlarms(string equipmentId)
        {
            log.Info($"GetActiveAlarms called for {equipmentId}");
            
            if (alarms.ContainsKey(equipmentId))
            {
                var activeAlarms = alarms[equipmentId].FindAll(a => !a.Acknowledged);
                var alarmIds = new string[activeAlarms.Count];
                
                for (int i = 0; i < activeAlarms.Count; i++)
                {
                    alarmIds[i] = activeAlarms[i].AlarmID;
                }
                
                return alarmIds;
            }
            
            return new string[0];
        }
        
        public bool AcknowledgeAlarm(string alarmId)
        {
            log.Info($"AcknowledgeAlarm called for alarm {alarmId}");
            
            foreach (var equipmentAlarms in alarms.Values)
            {
                var alarm = equipmentAlarms.Find(a => a.AlarmID == alarmId);
                if (alarm != null)
                {
                    alarm.Acknowledged = true;
                    return true;
                }
            }
            
            return false;
        }
    }
    
    internal class AlarmInfo
    {
        public string AlarmID { get; set; }
        public string EquipmentID { get; set; }
        public string AlarmCode { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Acknowledged { get; set; }
    }
}