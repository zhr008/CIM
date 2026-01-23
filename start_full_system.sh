#!/bin/bash

echo "==========================================="
echo "启动完整的CIM监控系统"
echo "数据流向: PLC/HSMS → KepServerEX → CIMMonitor → TibcoTibrvService → WCFServices → ORACLE"
echo "==========================================="

# 设置工作目录
WORKSPACE="/workspace"

# 启动顺序：
# 1. 启动WCF服务（最底层）
# 2. 启动TibcoTibrvService
# 3. 启动CIMMonitor
# 4. 启动HSMS模拟器（可选）

echo "步骤 1: 启动WCF服务..."
cd "$WORKSPACE/WCFServices"
dotnet run &
WCF_PID=$!

sleep 3

echo "步骤 2: 启动TibcoTibrvService..."
cd "$WORKSPACE/TibcoTibrvService"
dotnet run &
TIBCO_PID=$!

sleep 3

echo "步骤 3: 启动CIMMonitor..."
cd "$WORKSPACE/CIMMonitor"
dotnet run &
CIM_PID=$!

sleep 3

echo "步骤 4: 启动HSMS模拟器（可选）..."
cd "$WORKSPACE/HsmsSimulator"
dotnet run &
HSMS_PID=$!

echo "==========================================="
echo "所有服务已启动！"
echo "WCF服务 PID: $WCF_PID"
echo "TibcoTibrvService PID: $TIBCO_PID"
echo "CIMMonitor PID: $CIM_PID"
echo "HSMS模拟器 PID: $HSMS_PID"
echo ""
echo "系统数据流向:"
echo "1. PLC → KepServerEX → CIMMonitor → TibcoTibrvService → WCFServices → ORACLE"
echo "2. PCL → HSMS → CIMMonitor → TibcoTibrvService → WCFServices → ORACLE"
echo ""
echo "要停止所有服务，请运行: kill $WCF_PID $TIBCO_PID $CIM_PID $HSMS_PID"
echo "==========================================="

# 等待用户输入停止命令
echo "按 Ctrl+C 停止所有服务..."
trap "echo '停止所有服务...'; kill $WCF_PID $TIBCO_PID $CIM_PID $HSMS_PID; exit" INT TERM

# 保持脚本运行
wait