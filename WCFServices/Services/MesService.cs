using CoreWCF;
using Microsoft.Extensions.Logging;
using WCFServices.Models;

namespace WCFServices.Services
{
    public class MesService : IMesService
    {
        private readonly IMesBusinessService _businessService;
        private readonly ILogger<MesService> _logger;

        public MesService(IMesBusinessService businessService, ILogger<MesService> logger)
        {
            _businessService = businessService;
            _logger = logger;
        }

        public async Task<MesResponse> ProcessMessageAsync(MesMessage message)
        {
            try
            {
                _logger.LogInformation($"Processing message via WCF: {message.MessageId}, Type: {message.MessageType}");
                
                var response = await _businessService.ProcessTibcoMessageAsync(message);
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message via WCF: {message.MessageId}");
                
                return new MesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    MessageId = message.MessageId,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public async Task<EquipmentInfo> GetEquipmentStatusAsync(string equipmentId)
        {
            try
            {
                _logger.LogInformation($"Getting equipment status for: {equipmentId}");
                
                return await _businessService.GetEquipmentStatusAsync(equipmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting equipment status for: {equipmentId}");
                throw;
            }
        }

        public async Task<LotInfo> GetLotInfoAsync(string lotId)
        {
            try
            {
                _logger.LogInformation($"Getting lot info for: {lotId}");
                
                return await _businessService.GetLotInfoAsync(lotId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting lot info for: {lotId}");
                throw;
            }
        }

        public async Task<UpdateResult> UpdateEquipmentStatusAsync(string equipmentId, string status, string lotId)
        {
            try
            {
                _logger.LogInformation($"Updating equipment status for: {equipmentId}, Status: {status}, Lot: {lotId}");
                
                return await _businessService.UpdateEquipmentStatusAsync(equipmentId, status, lotId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating equipment status for: {equipmentId}");
                throw;
            }
        }

        public async Task<List<AlarmInfo>> GetAlarmsAsync(string equipmentId, DateTime startTime, DateTime endTime)
        {
            try
            {
                _logger.LogInformation($"Getting alarms for equipment: {equipmentId}, Time Range: {startTime} to {endTime}");
                
                return await _businessService.GetAlarmsAsync(equipmentId, startTime, endTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting alarms for equipment: {equipmentId}");
                throw;
            }
        }

        public async Task<List<LotTrackingInfo>> GetLotTrackingAsync(string lotId)
        {
            try
            {
                _logger.LogInformation($"Getting lot tracking for: {lotId}");
                
                return await _businessService.GetLotTrackingAsync(lotId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting lot tracking for: {lotId}");
                throw;
            }
        }

        public async Task<QualityDataInfo> GetQualityDataAsync(string lotId)
        {
            try
            {
                _logger.LogInformation($"Getting quality data for: {lotId}");
                
                return await _businessService.GetQualityDataAsync(lotId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting quality data for: {lotId}");
                throw;
            }
        }

        public async Task<SendResult> SendResponseMessageAsync(ResponseMessage response)
        {
            try
            {
                _logger.LogInformation($"Sending response message to subject: {response.Subject}");
                
                return await _businessService.SendResponseMessageAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending response message to subject: {response.Subject}");
                throw;
            }
        }
    }
}