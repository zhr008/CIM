# CIMMonitor 双架构支持文档

## 概述
本项目已增强以同时支持两种工业自动化架构：
1. **方案1**：PLC → 上位机 → CIM(SEMI/SECS)
2. **方案2**：PLC → KepServer → CIM(SEMI/SECS)

## 架构详情

### 方案1：PLC → 上位机 → CIM(SEMI/SECS)
- **架构流程**：PLC →（原生协议）→ 上位机 →（SECS/GEM协议）→ CIM系统
- **特点**：上位机作为核心转换节点，直接通过PLC原生协议（如西门子S7、三菱MC、Modbus、OPC UA等）采集数据
- **实现**：在上位机中集成SECS/GEM协议栈，将PLC数据转换为符合SEMI标准的SECS消息
- **通信**：上位机与CIM系统通过HSMS（E37）或SECS-I（E4）通信

### 方案2：PLC → KepServer → CIM(SEMI/SECS)
- **架构流程**：PLC →（OPC）→ KepServer →（OPC）→ SECS转换软件 →（SECS/GEM）→ CIM系统
- **特点**：KepServer作为OPC中间件，将不同品牌PLC数据统一为OPC标准接口
- **实现**：需要额外的SECS/GEM转换层（如CIMConnect或TransSECS/OPC）将OPC数据映射为SECS协议
- **优势**：实现PLC协议与SECS协议的解耦

## 核心组件

### 1. 统一架构配置 (`ArchitectureConfig.cs`)
- 定义了两种架构的配置模型
- 支持动态切换架构类型
- 包含PLC直连和KepServer两种配置选项

### 2. 统一数据服务 (`UnifiedDataService.cs`)
- 实现了 `IUnifiedDataService` 接口
- 支持两种架构的数据采集和转换
- 提供统一的事件接口供UI层使用
- 支持架构间动态切换

### 3. 设备监控界面增强
- 添加了架构选择下拉框
- 添加了架构切换按钮
- 显示当前架构状态
- 实时反馈架构切换结果

## 配置文件

### ArchitectureConfig.xml
位于 `Config/ArchitectureConfig.xml`，包含：
- 当前架构类型设置
- 直接PLC连接配置（方案1）
- KepServer连接配置（方案2）
- SECS/GEM协议配置
- 全局设置

## 使用方法

### 1. 启动应用
- 应用启动时会自动加载架构配置
- 默认使用KepServer架构（方案2）

### 2. 切换架构
- 在设备监控界面中使用架构选择下拉框选择目标架构
- 点击"切换架构"按钮执行切换操作
- 系统会自动停止当前架构服务并启动目标架构服务

### 3. 配置修改
- 修改 `Config/ArchitectureConfig.xml` 文件来调整架构配置
- 支持添加多个PLC设备和KepServer连接
- 可配置数据映射关系和SECS变量转换

## 技术实现细节

### 统一数据服务工作原理
1. 根据当前架构类型初始化相应的服务（`DirectPlcService` 或 `KepServerMonitoringService`）
2. 监听数据变更和连接状态事件
3. 提供统一的事件接口给UI层
4. 支持运行时架构切换，无需重启应用

### 数据流处理
- **方案1**：PLC数据 → DirectPLC服务 → 数据映射 → SECS消息 → CIM系统
- **方案2**：PLC数据 → KepServer → OPC数据 → SECS转换 → CIM系统

### 事件处理
- 统一事件模型适配两种架构的数据变更事件
- 连接状态变更事件统一处理
- 错误处理和日志记录统一管理

## 优势

1. **灵活性**：可根据现场环境选择最适合的架构方案
2. **可扩展性**：易于添加新的PLC协议类型
3. **兼容性**：保持与现有系统的兼容性
4. **易维护性**：统一的配置和管理界面
5. **无缝切换**：支持运行时架构切换，无需停机

## 注意事项

1. 切换架构时会短暂中断数据采集服务
2. 确保配置文件中的IP地址和端口设置正确
3. 不同PLC品牌可能需要安装相应的驱动程序
4. SECS/GEM协议配置需与CIM系统要求匹配