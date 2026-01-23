# CIMMonitor 系统架构文档

## 数据流向概述

本系统实现了两个主要的数据流向：

1. **PLC → KepServerEX → CIMMonitor → TibcoTibrvService → WCFServices → ORACLE**
2. **PCL → HSMS → CIMMonitor → TibcoTibrvService → WCFServices → ORACLE**

## 系统组件架构

```
┌─────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│   PLC Devices   │    │  KepServerEX     │    │   CIMMonitor     │
│                 │◄──►│  (OPC DA/UA)     │◄──►│                  │
│  • Siemens      │    │  • Modbus        │    │  • DataFlow      │
│  • Allen-Bradley│    │  • AB            │    │    Service       │
│  • Mitsubishi   │    │  •西门子         │    │  • KepServer     │
└─────────────────┘    │  • 三菱          │    │    Handler       │
                       │  • AB            │    │  • HSMS Device   │
                       └──────────────────┘    │    Manager       │
                                              └──────────────────┘
                                                      │
                                                      ▼
┌─────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│   PCL Devices   │    │     HSMS         │    │ TibcoTibrv       │
│                 │◄──►│  (Semiconductor │◄──►│ Service          │
│  • Semiconductor│    │   Equipment)     │    │                  │
│  • Factory      │    │                  │    │  • Integration   │
│  • Automation   │    │                  │    │    Service       │
└─────────────────┘    └──────────────────┘    └──────────────────┘
                                                      │
                                                      ▼
                                              ┌──────────────────┐
                                              │   WCFServices    │
                                              │                  │
                                              │  • MesService    │
                                              │  • Oracle Data   │
                                              │    Access        │
                                              └──────────────────┘
                                                      │
                                                      ▼
                                              ┌──────────────────┐
                                              │    Oracle DB     │
                                              │                  │
                                              │  • MES Tables    │
                                              │  • Production    │
                                              │    Data          │
                                              └──────────────────┘
```

## 主要组件说明

### 1. DataFlowService (新增)
- **位置**: CIMMonitor/Services/DataFlowService.cs
- **功能**: 统一处理两种数据流向
- **职责**:
  - 接收来自KepServerEX的数据变化事件
  - 接收来自HSMS设备的消息
  - 将数据转发到TibcoTibrvService
  - 提供统一的错误处理和日志记录

### 2. KepServerEX 模块
- **配置**: CIMMonitor/Config/KepServerConfig.xml
- **功能**: 
  - 从PLC设备采集数据
  - 支持多种协议 (OPC DA/UA, Modbus, Siemens, AB, Mitsubishi)
  - 提供标签管理和数据映射功能

### 3. HSMS 模块
- **配置**: CIMMonitor/Config/HsmsConfig.xml
- **功能**:
  - 与半导体设备通信
  - 支持客户端/服务端模式
  - 处理SECS/GEM协议消息

### 4. TibcoTibrvService
- **功能**:
  - 接收来自CIMMonitor的消息
  - 转发消息到WCF服务
  - 处理XML消息格式转换

### 5. WCFServices
- **功能**:
  - 提供MES服务接口
  - 数据验证和业务逻辑处理
  - Oracle数据库操作

### 6. Oracle 数据库
- **功能**:
  - 存储生产数据
  - 设备状态信息
  - 报警记录等

## 数据流向实现细节

### 方向1: PLC → KepServerEX → CIMMonitor → TibcoTibrvService → WCFServices → ORACLE

1. **PLC设备** 通过各种协议（OPC DA/UA、Modbus等）与KepServerEX通信
2. **KepServerEX** 采集PLC数据并提供OPC接口
3. **CIMMonitor** 中的KepServerMonitoringService监控数据变化
4. **DataFlowService** 捕获数据变化事件并通过TibcoIntegrationService转发
5. **TibcoTibrvService** 接收消息并调用WCF服务
6. **WCFServices** 处理业务逻辑并将数据存入Oracle

### 方向2: PCL → HSMS → CIMMonitor → TibcoTibrvService → WCFServices → ORACLE

1. **PCL设备**（半导体设备）通过HSMS协议与系统通信
2. **CIMMonitor** 中的HsmsDeviceManager管理设备连接和消息
3. **DataFlowService** 捕获HSMS消息并通过TibcoIntegrationService转发
4. **TibcoTibrvService** 接收消息并调用WCF服务
5. **WCFServices** 处理业务逻辑并将数据存入Oracle

## 关键改进点

1. **统一数据处理**: 通过DataFlowService统一处理两种数据流向
2. **解耦设计**: 各模块职责明确，松耦合设计
3. **错误处理**: 完善的错误处理和恢复机制
4. **可扩展性**: 易于添加新的数据源和目标系统
5. **日志记录**: 全链路日志追踪便于问题排查

## 启动顺序

1. 启动Oracle数据库
2. 启动WCFServices
3. 启动TibcoTibrvService
4. 启动CIMMonitor
5. 启动设备（PLC/HSMS）

使用 `/workspace/start_full_system.sh` 脚本可自动启动整个系统。