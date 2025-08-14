@echo off
echo PowerPoint评分稳定性测试
echo ========================

echo 正在编译项目...
dotnet build BenchSuite.Console --configuration Release

if %ERRORLEVEL% NEQ 0 (
    echo 编译失败！
    pause
    exit /b 1
)

echo.
echo 编译成功！
echo.

echo 请确保以下文件存在：
echo 1. 试卷文件（JSON格式）
echo 2. PowerPoint文件（.pptx格式）
echo.

echo 运行稳定性测试...
echo 注意：程序将自动执行30次评分以测试稳定性
echo.

cd BenchSuite.Console\bin\Release\net9.0-windows
BenchSuite.Console.exe

pause
