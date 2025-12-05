# Complete CIM (Computer Integrated Manufacturing) System

A comprehensive Computer Integrated Manufacturing (CIM) system that provides device monitoring, communication protocol simulation, business processing, and message passing functionality.

## System Overview

This CIM system consists of four main components that work together to provide a complete industrial automation solution:

1. **CIMMonitor** - Monitoring System
2. **HsmsSimulator** - HSMS Communication Simulator
3. **WCFServices** - WCF Services
4. **TibcoTibrvService** - TIBCO Service

## Architecture

### 1. CIMMonitor - Monitoring System
- Windows Forms application for real-time equipment monitoring
- Supports HSMS protocol communication with semiconductor equipment
- Monitors KepServer (OPC protocol) for industrial data acquisition
- Reads configuration from `HsmsConfig.xml` and `KepServerConfig.xml`
- Integrates with TIBCO for enterprise messaging
- Uses log4net for comprehensive logging

### 2. HsmsSimulator - HSMS Communication Simulator
- Complete HSMS communication simulator supporting SECS/GEM protocol
- Provides both server and client implementations
- Features message visualization and debugging tools
- Includes quick commands for common SECS/GEM operations
- Supports heartbeat monitoring for connection status

### 3. WCFServices - WCF Services
- Business logic processing service (MesBusinessService)
- TIBCO message integration for enterprise communication
- Data persistence layer supporting Oracle and SQL Server
- Complete IMesService interface implementation
- XML to TIBCO message conversion and processing

### 4. TibcoTibrvService - TIBCO Service
- TIBCO Rendezvous integration layer
- Data access layer for database operations
- WCF service integration for service communication
- XML message processing and routing

## Configuration Files

### HsmsConfig.xml
Located in `CIMSystem/CIMMonitor/Config/`
```xml
<?xml version="1.0" encoding="utf-8"?>
<HsmsConfig>
  <Devices>
    <Device ID="EQP1" Name="Equipment1" IPAddress="127.0.0.1" Port="5000" Role="Server" />
    <Device ID="EQP2" Name="Equipment2" IPAddress="127.0.0.1" Port="5001" Role="Client" />
    <Device ID="EQP3" Name="Equipment3" IPAddress="192.168.1.100" Port="5002" Role="Server" />
  </Devices>
  <Settings>
    <Timeout>30000</Timeout>
    <RetryCount>3</RetryCount>
    <ConnectionInterval>5000</ConnectionInterval>
  </Settings>
</HsmsConfig>
```

### KepServerConfig.xml
Located in `CIMSystem/CIMMonitor/Config/`
```xml
<?xml version="1.0" encoding="utf-8"?>
<KepServerConfig>
  <Servers>
    <Server ID="KEPSERVER1" Name="KepServerInstance1" HostName="localhost" Port="49320">
      <Tags>
        <Tag Name="Temperature" Path="Channel1.Device1.Temperature" DataType="Float" ScanRate="1000" />
        <Tag Name="Pressure" Path="Channel1.Device1.Pressure" DataType="Float" ScanRate="1000" />
        <Tag Name="FlowRate" Path="Channel1.Device1.FlowRate" DataType="Float" ScanRate="1000" />
        <Tag Name="Status" Path="Channel1.Device1.Status" DataType="String" ScanRate="500" />
      </Tags>
    </Server>
    <Server ID="KEPSERVER2" Name="KepServerInstance2" HostName="192.168.1.50" Port="49320">
      <Tags>
        <Tag Name="BatchID" Path="Channel2.Device1.BatchID" DataType="String" ScanRate="2000" />
        <Tag Name="LotID" Path="Channel2.Device1.LotID" DataType="String" ScanRate="2000" />
        <Tag Name="RecipeID" Path="Channel2.Device1.RecipeID" DataType="String" ScanRate="2000" />
      </Tags>
    </Server>
  </Servers>
  <Settings>
    <ConnectionTimeout>10000</ConnectionTimeout>
    <ReconnectInterval>5000</ReconnectInterval>
    <MaxRetries>5</MaxRetries>
  </Settings>
</KepServerConfig>
```

## Features

### Device Monitoring
- Real-time monitoring of equipment status
- Support for multiple device types (HSMS, OPC, KepServer)
- Configurable device settings via XML configuration
- Status visualization with online/offline indicators

### Communication Protocols
- **HSMS/SECS-GEM Support**: Complete implementation of semiconductor equipment communication standards
- **OPC Integration**: Support for OPC-based industrial communication
- **TIBCO Messaging**: Enterprise message bus integration

### Business Processing
- Equipment status tracking
- Batch/lot information management
- Alarm management and processing
- Quality data tracking
- Production data monitoring

### Message Handling
- XML message conversion to TIBCO format
- Message routing and processing
- Error handling and logging
- Asynchronous message processing

## Running the System

1. **Start the WCF Service**:
   ```bash
   cd CIMSystem/WCFServices
   dotnet run
   ```

2. **Start the CIM Monitor** (requires Windows Forms runtime):
   ```bash
   cd CIMSystem/CIMMonitor
   # Run the Windows Forms application
   ```

3. **Start the HSMS Simulator** (requires Windows Forms runtime):
   ```bash
   cd CIMSystem/HsmsSimulator
   # Run the Windows Forms application
   ```

Or use the startup script:
```bash
./start_cim_system.sh
```

## Project Structure

```
CIMSystem/
├── CIMMonitor/
│   ├── Config/
│   │   ├── HsmsConfig.xml
│   │   └── KepServerConfig.xml
│   ├── Forms/
│   │   └── MainForm.cs
│   ├── Services/
│   │   ├── HsmsService.cs
│   │   ├── KepServerService.cs
│   │   └── TibcoService.cs
│   ├── Models/
│   ├── Properties/
│   ├── Program.cs
│   └── App.config
├── HsmsSimulator/
│   ├── Config/
│   ├── Forms/
│   │   └── HsmsSimulatorForm.cs
│   ├── Services/
│   ├── Models/
│   ├── Program.cs
│   └── App.config
├── WCFServices/
│   ├── Services/
│   │   └── MesService.cs
│   ├── DataAccess/
│   ├── Models/
│   ├── Contracts/
│   │   └── IMesService.cs
│   └── ServiceHost.cs
├── TibcoTibrvService/
│   ├── Models/
│   └── Services/
│   │   └── TibcoRendezvousService.cs
└── Common/
    ├── Models/
    │   └── ConfigModels.cs
    └── Utilities/
```

## Dependencies

The system uses the following key technologies:
- .NET Framework/.NET Core
- Windows Forms (for GUI components)
- WCF (for service communication)
- log4net (for logging)
- TIBCO Rendezvous (for messaging - simulated in this implementation)
- OPC libraries (simulated in this implementation)

## Security Considerations

- All network communications should be secured in production
- Proper authentication and authorization should be implemented
- Sensitive configuration data should be encrypted
- Regular security updates should be applied

## Performance Considerations

- The system is designed to handle multiple concurrent connections
- Message queuing is used to prevent blocking operations
- Connection pooling can be implemented for database operations
- Caching mechanisms can be added for frequently accessed data

## Troubleshooting

- Check log files in the Logs directory for error messages
- Verify network connectivity between components
- Ensure all configuration files are properly formatted
- Confirm that required services are running

## License

This project is created for demonstration purposes as part of a CIM system implementation.