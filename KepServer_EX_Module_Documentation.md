# CIMMonitor项目 - KepServer EX模块文档

## 1. 模块概述

KepServer EX是CIMMonitor项目中的关键数据采集模块，负责连接多种工业协议设备，为上层应用提供统一的数据访问接口。该模块支持OPC DA/UA协议，能够适配西门子、Allen-Bradley、三菱和Modbus等多种工业设备。

### 1.1 主要功能
- 多协议设备集成
- 统一OPC数据访问接口
- 实时数据采集与监控
- 历史数据记录
- 报警事件管理
- 安全用户权限控制

### 1.2 支持的设备协议
- 西门子（S7-1500, S7-1200等）
- Allen-Bradley（ControlLogix系列）
- 三菱（Q系列PLC, 变频器等）
- Modbus（TCP/RTU）

## 2. 系统架构

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   上层应用      │◄──►│  KepServer EX   │◄──►│    工业设备     │
│ (MES, SCADA等)  │    │  (OPC Server)   │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                              │
                       ┌─────────────────┐
                       │   数据存储      │
                       │ (SQL Server等)  │
                       └─────────────────┘
```

## 3. 核心配置结构

### 3.1 项目配置结构

```xml
<Project>
├── Properties          # 项目元数据
├── Channels            # 通信通道集合
│   ├── Siemens_Production    # 西门子通道
│   ├── Modbus_Sensors        # Modbus通道
│   ├── AB_Robots             # AB通道
│   └── Mitsubishi_Drives     # 三菱通道
├── AdvancedTags        # 高级标签（计算、链接）
├── AdvancedPlugins     # 插件配置
│   ├── DataLogger      # 数据记录插件
│   ├── AlarmsEvents    # 报警事件插件
│   └── OPCUA           # OPC UA服务插件
├── UserManager         # 用户权限管理
└── Diagnostics         # 诊断配置
</Project>
```

### 3.2 通道配置详解

#### 3.2.1 西门子通道配置
```xml
<Channel Name="Siemens_Production" Driver="Siemens TCP/IP Ethernet">
    <Properties>
        <Property Name="NetworkAdapter" Value="192.168.10.100"/>
        <Property Name="Protocol" Value="ISO-on-TCP"/>
        <Property Name="ScanRate" Value="100"/>
        <Property Name="EnableDiagnostics" Value="True"/>
    </Properties>
</Channel>
```

#### 3.2.2 Modbus通道配置
```xml
<Channel Name="Modbus_Sensors" Driver="Modbus TCP/IP Ethernet">
    <Properties>
        <Property Name="NetworkAdapter" Value="192.168.20.100"/>
        <Property Name="Port" Value="502"/>
        <Property Name="ProtocolMode" Value="RTUoverTCP"/>
        <Property Name="ScanRate" Value="500"/>
    </Properties>
</Channel>
```

#### 3.2.3 Allen-Bradley通道配置
```xml
<Channel Name="AB_Robots" Driver="Allen-Bradley ControlLogix Ethernet">
    <Properties>
        <Property Name="NetworkAdapter" Value="192.168.30.100"/>
        <Property Name="Protocol" Value="EtherNet/IP"/>
        <Property Name="ScanRate" Value="50"/>
    </Properties>
</Channel>
```

#### 3.2.4 三菱通道配置
```xml
<Channel Name="Mitsubishi_Drives" Driver="Mitsubishi Ethernet">
    <Properties>
        <Property Name="NetworkAdapter" Value="192.168.40.100"/>
        <Property Name="Protocol" Value="MC Protocol"/>
        <Property Name="ScanRate" Value="200"/>
    </Properties>
</Channel>
```

## 4. 标签（Tag）系统

### 4.1 标签核心属性

| 属性名            | 数据类型    | 说明         | 示例                         |
| ---------------- | ------- | ---------- | -------------------------- |
| `Name`           | String  | 标签唯一标识符    | `Motor_Run`                |
| `Address`        | String  | PLC 寄存器地址  | `DB1.DBX0.0`               |
| `DataType`       | Enum    | OPC 数据类型   | `BOOLEAN`, `WORD`, `FLOAT` |
| `AccessRights`   | Enum    | 读写权限       | `ReadOnly`, `ReadWrite`    |
| `ScanRate`       | Integer | 独立扫描周期(ms) | `500`                      |
| `Description`    | String  | 标签描述       | `电机运行反馈信号`                 |

### 4.2 标签分类

#### 4.2.1 位信号组（布尔量）
```xml
<TagGroup Name="Bit_Control">
    <Tag Name="System_Run" Address="DB100.DBX0.0" DataType="BOOLEAN" AccessRights="ReadWrite">
        <Properties>
            <Property Name="Description" Value="系统运行总开关"/>
            <Property Name="TagAlias" Value="系统.运行状态"/>
        </Properties>
    </Tag>
</TagGroup>
```

#### 4.2.2 整型数据组（计数、状态码）
```xml
<TagGroup Name="Word_Counters">
    <Tag Name="Production_Count" Address="DB101.DBW0" DataType="WORD" AccessRights="ReadWrite">
        <Properties>
            <Property Name="Description" Value="日产量计数"/>
            <Property Name="ArchiveEnabled" Value="True"/>
            <Property Name="ArchiveMode" Value="Change"/>
        </Properties>
    </Tag>
</TagGroup>
```

#### 4.2.3 浮点数组（模拟量）
```xml
<TagGroup Name="Float_Analog">
    <Tag Name="Temperature_Zone1" Address="DB102.DBD0" DataType="FLOAT" AccessRights="ReadOnly">
        <Properties>
            <Property Name="Description" Value="1区温度"/>
            <Property Name="Units" Value="°C"/>
            <Property Name="Scaling" Value="Linear"/>
            <Property Name="RawLow" Value="0"/>
            <Property Name="RawHigh" Value="27648"/>
            <Property Name="ScaledLow" Value="0"/>
            <Property Name="ScaledHigh" Value="800"/>
            <Property Name="AlarmEnabled" Value="True"/>
            <Property Name="AlarmHighLimit" Value="750"/>
            <Property Name="AlarmLowLimit" Value="50"/>
        </Properties>
    </Tag>
</TagGroup>
```

### 4.3 高级标签

#### 4.3.1 计算标签
```xml
<CalculatedTag Name="Total_Production_Today" DataType="DINT" AccessRights="ReadOnly">
    <Properties>
        <Property Name="Expression" Value="[Siemens_Production.Assembly_Line_Main.Word_Counters.Production_Count]"/>
        <Property Name="ScanRate" Value="1000"/>
        <Property Name="Description" Value="今日总产量（计算）"/>
    </Properties>
</CalculatedTag>
```

#### 4.3.2 链接标签
```xml
<LinkTag Name="Redundant_Temperature" DataType="FLOAT" AccessRights="ReadOnly">
    <Properties>
        <Property Name="PrimarySource" Value="Siemens_Production.Assembly_Line_Main.Float_Analog.Temperature_Zone1"/>
        <Property Name="BackupSource" Value="Modbus_Sensors.Temp_Sensors_01.Temperature_Readings.Sensor_01"/>
        <Property Name="SwitchOnQuality" Value="True"/>
    </Properties>
</LinkTag>
```

## 5. 插件系统

### 5.1 数据记录插件（DataLogger）

用于历史数据存储和分析：

```xml
<LogGroup Name="Production_Hourly_Report">
    <Properties>
        <Property Name="TriggerType" Value="Interval"/>
        <Property Name="Interval" Value="3600000"/> <!-- 每小时 -->
        <Property Name="TableName" Value="ProductionData_Hourly"/>
    </Properties>
    <Tags>
        <Tag>Siemens_Production.Assembly_Line_Main.Word_Counters.Production_Count</Tag>
        <Tag>Siemens_Production.Assembly_Line_Main.Word_Counters.GoodParts_Count</Tag>
    </Tags>
</LogGroup>
```

### 5.2 报警事件插件（AlarmsEvents）

用于实时报警和通知：

```xml
<AlarmTag TagName="Siemens_Production.Assembly_Line_Main.Bit_Control.Emergency_Stop">
    <Properties>
        <Property Name="Condition" Value="True"/>
        <Property Name="Message" Value="急停按钮被触发！"/>
        <Property Name="Severity" Value="Critical"/>
        <Property Name="EnableEmail" Value="True"/>
    </Properties>
</AlarmTag>
```

### 5.3 OPC UA服务插件

提供现代OPC UA接口：

```xml
<Plugin Name="OPCUA" Enabled="True">
    <Properties>
        <Property Name="EndpointURL" Value="opc.tcp://0.0.0.0:49320"/>
        <Property Name="SecurityMode" Value="SignAndEncrypt"/>
        <Property Name="MaxSessionCount" Value="100"/>
    </Properties>
</Plugin>
```

## 6. 用户权限管理

```xml
<User Name="Administrator" Enabled="True">
    <Properties>
        <Property Name="Password" Value="encrypted_hash_12345"/>
        <Property Name="Role" Value="Administrator"/>
    </Properties>
</User>

<User Name="Operator" Enabled="True">
    <Permissions>
        <Permission Channel="Siemens_Production" Access="ReadOnly"/>
        <Permission Channel="Modbus_Sensors" Access="ReadOnly"/>
        <Permission Channel="AB_Robots" Access="Deny"/>
        <Permission Channel="Mitsubishi_Drives" Access="ReadWrite"/>
    </Permissions>
</User>
```

## 7. 设备适配详情

### 7.1 西门子设备适配

- **驱动**: Siemens TCP/IP Ethernet
- **协议**: ISO-on-TCP
- **特点**: 支持S7-1500/S7-1200系列PLC
- **示例配置**:
  ```xml
  <Device Name="Assembly_Line_Main">
      <Properties>
          <Property Name="IPAddress" Value="192.168.10.10"/>
          <Property Name="PLCModel" Value="S7-1500"/>
          <Property Name="Rack" Value="0"/>
          <Property Name="Slot" Value="1"/>
      </Properties>
  </Device>
  ```

### 7.2 Allen-Bradley设备适配

- **驱动**: Allen-Bradley ControlLogix Ethernet
- **协议**: EtherNet/IP
- **特点**: 支持ControlLogix系列控制器
- **示例配置**:
  ```xml
  <Device Name="Robot_ARM_01">
      <Properties>
          <Property Name="IPAddress" Value="192.168.30.10"/>
          <Property Name="CPUType" Value="ControlLogix5500"/>
      </Properties>
  </Device>
  ```

### 7.3 三菱设备适配

- **驱动**: Mitsubishi Ethernet
- **协议**: MC Protocol
- **特点**: 支持Q系列PLC和变频器
- **示例配置**:
  ```xml
  <Device Name="VFD_Conveyor_01">
      <Properties>
          <Property Name="IPAddress" Value="192.168.40.10"/>
          <Property Name="CPUType" Value="Q06UDV"/>
      </Properties>
  </Device>
  ```

### 7.4 Modbus设备适配

- **驱动**: Modbus TCP/IP Ethernet
- **协议**: RTUoverTCP
- **特点**: 支持温湿度传感器等设备
- **示例配置**:
  ```xml
  <Device Name="Temp_Sensors_01" DeviceID="1">
      <Properties>
          <Property Name="IPAddress" Value="192.168.20.10"/>
      </Properties>
  </Device>
  ```

## 8. 企业级特性

### 8.1 高可用性
- 冗余设备支持
- 自动故障切换
- 断线重连机制

### 8.2 性能优化
- 分层标签组织
- 扫描率自定义
- 批量数据传输

### 8.3 安全性
- 多级用户权限
- 数据加密传输
- 安全认证机制

### 8.4 可维护性
- 详细的诊断日志
- 远程监控能力
- 配置版本管理

## 9. 实施建议

1. **分阶段部署**: 先部署单一协议设备，再逐步增加其他协议设备
2. **性能调优**: 根据实际需求调整扫描率和数据更新频率
3. **安全配置**: 合理设置用户权限，避免过度授权
4. **监控告警**: 设置关键参数的报警阈值，确保及时响应异常情况

## 10. 维护要点

- 定期检查设备连接状态
- 监控系统资源使用情况
- 更新设备固件和驱动程序
- 备份配置文件以防止数据丢失