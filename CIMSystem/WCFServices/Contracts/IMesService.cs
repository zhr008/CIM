using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using Common.Models;

namespace WCFServices.Contracts
{
    [ServiceContract]
    public interface IMesService
    {
        [OperationContract]
        EquipmentStatus GetEquipmentStatus(string equipmentId);
        
        [OperationContract]
        bool SetEquipmentStatus(string equipmentId, string status);
        
        [OperationContract]
        ProductionData GetProductionData(string batchId);
        
        [OperationContract]
        bool UpdateProductionData(ProductionData productionData);
        
        [OperationContract]
        string ProcessEquipmentMessage(EquipmentMessage message);
        
        [OperationContract]
        bool SendAlarm(string equipmentId, string alarmCode, string description);
        
        [OperationContract]
        string[] GetActiveAlarms(string equipmentId);
        
        [OperationContract]
        bool AcknowledgeAlarm(string alarmId);
    }
    
    [DataContract]
    public class ServiceResponse
    {
        [DataMember]
        public bool Success { get; set; }
        
        [DataMember]
        public string Message { get; set; }
        
        [DataMember]
        public object Data { get; set; }
        
        public ServiceResponse()
        {
            Success = false;
            Message = string.Empty;
            Data = null;
        }
        
        public ServiceResponse(bool success, string message, object data = null)
        {
            Success = success;
            Message = message;
            Data = data;
        }
    }
}