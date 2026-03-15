pushd %~dp0
protoc.exe -I=./ --cpp_out=./ ./Enum.proto
protoc.exe -I=./ --cpp_out=./ ./Struct.proto
protoc.exe -I=./ --cpp_out=./ ./Protocol.proto

protoc.exe -I=./ --csharp_out=./ ./Enum.proto
protoc.exe -I=./ --csharp_out=./ ./Struct.proto
protoc.exe -I=./ --csharp_out=./ ./Protocol.proto

GenPackets.exe --path=./Protocol.proto --output=ClientPacketHandler --recv=C_ --send=S_
GenPackets.exe --path=./Protocol.proto --output=ServerPacketHandler --recv=S_ --send=C_

IF ERRORLEVEL 1 PAUSE

XCOPY /Y Enum.pb.h "../../../GW2_Server"
XCOPY /Y Enum.pb.cc "../../../GW2_Server"
XCOPY /Y Struct.pb.h "../../../GW2_Server"
XCOPY /Y Struct.pb.cc "../../../GW2_Server"
XCOPY /Y Protocol.pb.h "../../../GW2_Server"
XCOPY /Y Protocol.pb.cc "../../../GW2_Server"
XCOPY /Y ClientPacketHandler.h "../../../GW2_Server"

PAUSE

XCOPY /Y Protocol.cs "../../../../GW2_Client/Assets/Scripts/Packet"
XCOPY /Y Enum.cs "../../../../GW2_Client/Assets/Scripts/Packet"
XCOPY /Y Struct.cs "../../../../GW2_Client/Assets/Scripts/Packet"
REM XCOPY /Y ServerPacketHandler.h "../../../GW2_Client"
XCOPY /Y ServerPacketHandler.cs "../../../../GW2_Client/Assets/Scripts/Packet"

REM DEL /Q /F *.pb.h
REM DEL /Q /F *.pb.cc
REM DEL /Q /F *.h

PAUSE