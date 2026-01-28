using System.ServiceModel;

namespace WCFServices.Contracts
{
    /// <summary>
    /// CIM服务契约接口
    /// </summary>
    [ServiceContract]
    public interface ICimService
    {
        /// <summary>
        /// 处理CIM请求
        /// </summary>
        /// <param name="requestData">请求数据</param>
        /// <returns>响应数据</returns>
        [OperationContract]
        string ProcessCimRequest(string requestData);

        /// <summary>
        /// 设备状态更新
        /// </summary>
        /// <param name="equipmentId">设备ID</param>
        /// <param name="status">设备状态</param>
        /// <returns>处理结果</returns>
        [OperationContract]
        string UpdateEquipmentStatus(string equipmentId, string status);

        /// <summary>
        /// 获取设备信息
        /// </summary>
        /// <param name="equipmentId">设备ID</param>
        /// <returns>设备信息</returns>
        [OperationContract]
        string GetEquipmentInfo(string equipmentId);
    }
}