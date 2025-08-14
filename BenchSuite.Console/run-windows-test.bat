@echo off
echo ========================================
echo BenchSuite Windows 测试启动脚本
echo ========================================
echo.

REM 检查是否存在编译后的程序
if not exist "bin\Debug\net8.0\BenchSuite.Console.exe" (
    echo 错误: 找不到编译后的程序文件
    echo 请先编译 BenchSuite.Console 项目
    echo.
    pause
    exit /b 1
)

REM 检查是否存在测试数据文件
if not exist "TestData\windows-test-exam.json" (
    echo 错误: 找不到Windows测试数据文件
    echo 请确保 TestData\windows-test-exam.json 文件存在
    echo.
    pause
    exit /b 1
)

echo 正在启动Windows系统操作测试...
echo 测试将执行30轮评分以验证稳定性
echo.

REM 运行Windows测试
"bin\Debug\net8.0\BenchSuite.Console.exe" windows "TestData\windows-test-exam.json"

echo.
echo 测试完成！
pause
