# CIMMonitor - KepServer 监控系统

这是一个简化的 KepServer 监控系统，用于监控工业自动化系统中的 PLC 数据。

## 项目结构

```
CIMMonitor/
├── Models/
│   └── KepServerModels.cs          # KepServer 数据模型定义
├── Services/
│   └── KepServerMonitoringService.cs # KepServer 监控服务
├── Example/
│   └── KepServerExample.cs         # 使用示例
├── Forms/
│   └── MainForm.cs                 # 主窗体
├── Config/
│   └── KepServerConfig.xml         # KepServer 配置文件
├── Program.cs                      # 程序入口
└── CIMMonitor.csproj               # 项目文件
```

## 功能特点

- **标准 KepServer EX 配置**: 支持标准的 XML 配置格式
- **分层架构**: 通道(Channel) -> 设备(Device) -> 标签组(TagGroup) -> 标签(Tag)
- **实时监控**: 监控标签值的变化
- **事件处理**: 提供数据变更事件回调
- **灵活配置**: 支持多种数据类型和独立扫描周期

## 配置说明

XML 配置文件遵循 KepServer EX 标准格式：

```xml
<Project xmlns="http://www.kepware.com/schemas/kepserverexproject" Version="6.12.0.0" ProjectID="{...}">
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
                <Tag Name="Start_Button" Address="00001" DataType="BOOLEAN" ScanRate="500" Description="启动按钮"/>
                <Tag Name="Stop_Button" Address="00002" DataType="BOOLEAN" ScanRate="500" Description="停止按钮"/>
              </Tags>
            </TagGroup>
            
            <TagGroup Name="Word_Area">
              <Tags>
                <Tag Name="Temperature" Address="40001" DataType="WORD" ScanRate="1000" Description="温度传感器值"/>
                <Tag Name="Pressure" Address="40002" DataType="WORD" ScanRate="1000" Description="压力传感器值"/>
              </Tags>
            </TagGroup>
          </TagGroups>
        </Device>
      </Devices>
    </Channel>
  </Channels>
</Project>
```

## 使用方法

1. 配置 KepServerConfig.xml 文件
2. 实例化 KepServerMonitoringService
3. 订阅 DataChanged 事件
4. 调用 InitializeAsync 和 StartMonitoringAsync 方法开始监控

## 标签属性说明

| 属性名 | 数据类型 | 说明 | 示例 |
|--------|----------|------|------|
| `Name` | String | 标签唯一标识符 | `Motor_Run` |
| `Address` | String | PLC 寄存器地址 | `DB1.DBX0.0` |
| `DataType` | Enum | OPC 数据类型 | `BOOLEAN`, `WORD`, `FLOAT` |
| `AccessRights` | Enum | 读写权限 | `ReadOnly`, `ReadWrite` |
| `ScanRate` | Integer | 独立扫描周期(ms) | `500` |
| `Description` | String | 标签描述 | `电机运行反馈信号` |