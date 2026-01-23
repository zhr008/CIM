# CIM系统项目文档

## 项目概述

CIM (Computer Integrated Manufacturing) 系统是一个完整的工业自动化监控系统，提供设备监控、通信协议仿真、业务处理和消息传递功能。系统支持半导体制造设备的实时监控和数据采集，并通过多种协议实现与MES系统的集成。

## 系统架构

### 整体架构图
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   CIMMonitor    │    │  HsmsSimulator   │    │  WCFServices    │
│                 │◄──►│                  │◄──►│                 │
│  - Windows Forms│    │  - HSMS/SECS-GEM │    │  - WCF Services │
│  - Device       │    │  - Client/Server │    │  - Oracle DB    │
│    Monitoring   │    │  - Message Vis.  │    │  - Business     │
└─────────────────┘    └──────────────────┘    │    Logic       │
                                              └─────────────────┘
                                                     ▲
                                                     │
                                      ┌─────────────────────────┐
                                      │   TibcoTibrvService     │
                                      │                         │
                                      │  - TIBCO Rendezvous     │
                                      │  - XML Message Processing│
                                      └─────────────────────────┘
```

### 组件说明

#### 1. CIMMonitor - 监控系统
- **类型**: Windows Forms 应用程序
- **功能**:
  - 设备状态实时监控
  - 支持 HSMS 协议和 KepServer (OPC 协议)
  - 通过 HsmsConfig.xml 和 KepServerConfig.xml 进行配置
  - 集成 TIBCO 消息功能
  - 使用 log4net 进行日志记录
- **主要模块**:
  - 设备监控表单 (DeviceMonitorForm)
  - KepServer 监控表单 (KepServerMonitorForm)
  - 报警管理表单 (AlarmManagerForm)
  - 生产数据表单 (ProductionDataForm)
  - TIBCO 消息表单 (TibcoMessageForm)

#### 2. HsmsSimulator - HSMS通信模拟器
- **类型**: Windows Forms 应用程序
- **功能**:
  - 完整的 HSMS 通信模拟器
  - 支持 SECS/GEM 协议
  - 提供客户端和服务器实现
  - 消息可视化功能
  - 预定义命令集和心跳监控
- **主要模块**:
  - HsmsServer - 服务器端实现
  - HsmsClient - 客户端实现
  - SecsSessionManager - SECS会话管理
  - 消息模型和工具类

#### 3. WCFServices - WCF服务层
- **类型**: WCF 服务
- **功能**:
  - TIBCO 消息服务集成
  - 业务逻辑处理
  - 数据持久化 (Oracle, SQL Server)
  - IMesService 接口实现
- **主要模块**:
  - IMesService - 服务契约接口
  - MesBusinessService - 业务逻辑处理
  - OracleDataAccess - Oracle数据库访问
  - TibcoMessageService - TIBCO消息处理

#### 4. TibcoTibrvService - TIBCO服务
- **类型**: 控制台应用程序
- **功能**:
  - TIBCO Rendezvous 集成
  - 数据访问层
  - 与 WCF 服务集成
  - XML 消息处理
- **主要模块**:
  - SimpleTibrvService - 简化的TIBCO服务实现

## 业务流程

### 1. 设备监控流程
```
设备 → HSMS/OPC → CIMMonitor → TIBCO → WCFServices → 数据库
```

### 2. 消息处理流程
```
设备消息 → HsmsSimulator → CIMMonitor → TIBCO → WCFServices → 业务处理 → 响应
```

### 3. 报警处理流程
```
设备报警 → CIMMonitor → TIBCO → WCFServices → 数据库记录 → 报警管理界面
```

## 关键技术栈

- **编程语言**: C#
- **框架**: .NET 8, Windows Forms, WCF (CoreWCF), ASP.NET Core
- **通信协议**: 
  - HSMS/SECS-GEM (半导体设备通信标准)
  - OPC (工业自动化数据交换)
  - TIBCO Rendezvous (企业消息总线)
- **数据库**: Oracle (模拟实现)
- **日志**: log4net
- **序列化**: JSON, XML

## 配置文件

### 1. HsmsConfig.xml
定义 HSMS 设备的配置，包括客户端/服务器角色和连接设置

### 2. KepServerConfig.xml
配置 OPC/KepServer 连接参数和数据映射

## 主要功能模块

### 设备管理
- 实时监控设备状态
- 支持多种设备类型 (HSMS, OPC, KepServer)
- 配置管理
- 在线/离线状态指示

### 数据采集
- HSMS/SECS-GEM 协议数据采集
- OPC 数据采集
- 历史数据存储

### 生产管理
- 批次/工单信息管理
- 生产数据跟踪
- 良率分析

### 报警管理
- 设备报警监控
- 报警级别分类
- 报警历史记录

### 消息处理
- XML 消息转换为 TIBCO 格式
- 消息路由和处理
- 异步消息处理

## 数据模型

### 设备相关
- EquipmentStatus - 设备状态
- Device - 设备信息
- DeviceConfig - 设备配置

### 生产相关
- ProductionData - 生产数据
- ProductionOrder - 生产订单
- LotTracking - 批次追踪

### 报警相关
- Alarm - 报警信息
- AlarmLog - 报警日志

### 消息相关
- TibcoMessage - TIBCO消息
- EquipmentMessage - 设备消息
- MessageLogEntry - 消息日志

## 安全考虑

- 所有网络通信设计为可安全加密
- 实现认证和授权模式
- 敏感配置数据安全处理
- 详细的日志记录和审计

## 性能考虑

- 系统设计支持多并发连接
- 消息队列防止阻塞操作
- 可扩展的连接池机制
- 缓存机制优化频繁访问数据

## 错误处理和日志

- 全面的错误处理机制
- 使用 log4net 进行详细日志记录
- 异常管理和恢复机制
- 线程安全的操作

## 部署方式

系统可以通过 `start_cim_system.sh` 脚本启动，该脚本负责协调启动各个组件。

## 开发环境要求

- .NET 8 SDK
- Visual Studio 或 VS Code
- Oracle 客户端 (用于数据库连接)
- TIBCO Rendezvous 库 (生产环境)

## 测试策略

- 单元测试覆盖核心业务逻辑
- 集成测试验证各组件间通信
- 端到端测试确保完整业务流程
- 性能测试验证系统负载能力

## 维护指南

- 日志文件位于指定目录
- 配置文件热更新支持
- 服务健康监控
- 定期备份策略

## 扩展性设计

- 模块化架构便于扩展
- 插件化设计支持新协议
- 微服务架构准备
- 云原生部署兼容

## 质量保证

- 遵循编码标准
- 全面的错误处理
- 资源管理和清理
- 线程安全操作
- 职责分离和模块化

此CIM系统为半导体和其他制造业提供了完整的自动化监控解决方案，支持现代工业4.0标准，具有高可用性、可扩展性和可靠性。