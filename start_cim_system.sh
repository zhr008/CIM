#!/bin/bash

# CIM System Startup Script
# This script starts all components of the CIM system

echo "Starting CIM System..."
echo "======================="

# Create logs directory if it doesn't exist
mkdir -p CIMSystem/CIMMonitor/Logs

# Start the WCF service first
echo "Starting WCF MES Service..."
cd CIMSystem/WCFServices
dotnet run &
WCF_PID=$!
cd ../..

echo "WCF Service started with PID: $WCF_PID"

# Give the service a moment to start
sleep 3

# Start the CIM Monitor
echo "Starting CIM Monitor..."
cd CIMSystem/CIMMonitor
# In a real scenario, you would run the Windows Forms app here
# For this example, we'll just show the command that would be used
echo "CIM Monitor would be started here (requires Windows Forms runtime)"
cd ../..

# Start the HSMS Simulator
echo "Starting HSMS Simulator..."
cd CIMSystem/HsmsSimulator
# In a real scenario, you would run the Windows Forms app here
# For this example, we'll just show the command that would be used
echo "HSMS Simulator would be started here (requires Windows Forms runtime)"
cd ../..

echo "All CIM System components initiated"
echo "WCF Service PID: $WCF_PID"
echo "Access the WCF service at: http://localhost:8080/MesService"
echo ""
echo "To stop the system, run: kill $WCF_PID"