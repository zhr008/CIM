# KepServer EX 标准配置架构重构

## 概述
本次重构工作旨在将 CIMMonitor 系统中的 KepServer 配置架构升级为符合 KepServer EX 标准的 XML 配置格式。

## 标准 KepServer EX XML 配置格式

### 配置文件结构
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Project xmlns="http://www.kepware.com/schemas/kepserverexproject"
         Version="6.12.0.0"
         ProjectID="{A1B2C3D4-E5F6-7890-1234-567890ABCDEF}">
  
  <Properties>
    <Property Name="ProjectName" Value="Assembly_Line_Config"/>
    <Property Name="Description" Value="装配线 PLC 数据采集配置"/>
  </Properties>
  
  <Channels>
    <Channel Name="ModbusTCP" Driver="Modbus TCP/IP Ethernet">
      <Properties>
        <Property Name="NetworkAdapter" Value="0.0.0.0"/>
        <Property Name="Port" Value="502"/>
      </Properties>
      
      <Devices>
        <Device Name="DX010_Device">
          <Properties>
            <Property Name="DeviceID" Value="1"/>
            <Property Name="IP Address" Value="192.168.1.50"/>
          </Properties>
          
          <TagGroups>
            <TagGroup Name="Bit_Area">
              <Tags>
                <Tag Name="Start_Button" Address="00001" DataType="BOOLEAN"/>
                <Tag Name="Stop_Button" Address="00002" DataType="BOOLEAN"/>
              </Tags>
            </TagGroup>
            
            <TagGroup Name="Word_Area">
              <Tags>
                <Tag Name="Temperature" Address="40001" DataType="WORD" 
                     Description="温度传感器值"/>
                <Tag Name="Pressure" Address="40002" DataType="WORD" 
                     Description="压力传感器值"/>
              </Tags>
            </TagGroup>
          </TagGroups>
        </Device>
      </Devices>
    </Channel>
  </Channels>
</Project>
```

### 标签属性说明

| 属性名            | 数据类型    | 说明         | 示例                         |
|------------------|-------------|--------------|------------------------------|
| `Name`           | String      | 标签唯一标识符| `Motor_Run`                  |
| `Address`        | String      | PLC 寄存器地址| `DB1.DBX0.0`                 |
| `DataType`       | Enum        | OPC 数据类型  | `BOOLEAN`, `WORD`, `FLOAT`   |
| `AccessRights`   | Enum        | 读写权限      | `ReadOnly`, `ReadWrite`      |
| `ScanRate`       | Integer     | 独立扫描周期(ms)| `500`                       |
| `Description`    | String      | 标签描述      | `电机运行反馈信号`              |

## 重构后的架构组件

### 1. 模型层 (Models/KepServerModels.cs)

重新设计了符合 KepServer EX 标准的模型结构：

- **KepServerProject**: 项目根节点
- **Channel**: 通道配置
- **Device**: 设备配置
- **TagGroup**: 标签组配置
- **Tag**: 标签配置

### 2. 服务层 (Services/KepServerMonitoringService.cs)

实现了对标准 KepServer EX XML 配置的支持：

- 支持标准 XML 序列化和反序列化
- 提供多层级配置访问接口
- 实现标签监控和事件处理机制
- 支持多种数据类型和扫描周期配置

### 3. 配置文件 (Config/KepServerConfig.xml)

采用标准 KepServer EX XML 格式，支持：

- 通道配置
- 设备配置
- 标签组管理
- 标签属性配置

## 主要改进点

1. **标准化配置格式**：采用 KepServer EX 官方标准 XML 格式
2. **灵活的标签管理**：支持多种数据类型和独立扫描周期
3. **模块化架构**：清晰的分层架构便于维护和扩展
4. **事件驱动机制**：支持 OPC DA 事件触发和处理
5. **实时监控**：提供标签值变更监控功能

## 使用示例

参见 `Example/KepServerExample.cs` 文件，展示了如何使用重构后的服务。

## 配置验证

系统能够正确解析和处理标准 KepServer EX XML 配置文件，包括：
- 项目属性解析
- 通道配置加载
- 设备配置加载
- 标签组和标签配置加载
- 数据类型和扫描周期设置