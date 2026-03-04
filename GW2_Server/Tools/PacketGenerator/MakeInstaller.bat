@echo off
REM 가상환경 활성화
call env\Scripts\activate.bat

REM 프로젝트 루트로 이동
pushd %~dp0

REM PacketGenerator.exe 생성 (standalone, onefile)
env\Scripts\python.exe -m nuitka --standalone --onefile --include-data-dir=Templates=Templates PacketGenerator.py
REM PacketGenerator.exe
IF ERRORLEVEL 1 (
    echo Nuitka 빌드 실패
    PAUSE
    EXIT /B 1
)

XCOPY /Y PacketGenerator.exe "../../Common/Protobuf/bin"
XCOPY /Y /E /I Templates "../../Common/Protobuf/bin/Templates"  ← 추가

PAUSE