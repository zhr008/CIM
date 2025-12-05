CIM System
========

A complete Computer Integrated Manufacturing (CIM) system that provides device monitoring, communication protocol simulation, business processing, and message passing functionality.

Components
----------

### 1. CIMMonitor - Monitoring System
- Windows Forms application
- Device monitoring with HSMS protocol and KepServer (OPC protocol) support
- Configuration via HsmsConfig.xml and KepServerConfig.xml
- TIBCO message integration
- log4net logging

### 2. HsmsSimulator - HSMS Communication Simulator
- Complete HSMS communication simulator
- SECS/GEM protocol support
- Client and server implementations
- Message visualization

### 3. WCFServices - WCF Services
- TIBCO message service integration
- Business logic processing
- Data persistence (Oracle, SQL Server)
- IMesService interface implementation

### 4. TibcoTibrvService - TIBCO Service
- TIBCO Rendezvous integration
- Data access layer
- WCF service integration
- XML message processing

Directory Structure
-------------------
```
CIMSystem/
├── CIMMonitor/
│   ├── Config/
│   ├── Forms/
│   ├── Services/
│   ├── Models/
│   └── Properties/
├── HsmsSimulator/
│   ├── Config/
│   ├── Forms/
│   ├── Services/
│   └── Models/
├── WCFServices/
│   ├── Services/
│   ├── DataAccess/
│   ├── Models/
│   └── Contracts/
├── TibcoTibrvService/
│   ├── Models/
│   └── Services/
└── Common/
    ├── Models/
    └── Utilities/
```