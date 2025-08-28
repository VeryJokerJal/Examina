@echo off
echo ========================================
echo BenchSuite 相对路径处理功能测试
echo ========================================
echo.

REM 设置测试环境路径
set TEST_BASE_PATH=D:\BenchSuite_RelativePathTest

REM 检查是否存在编译后的程序
if not exist "bin\Debug\net9.0-windows\BenchSuite.Console.exe" (
    echo 错误: 找不到编译后的程序文件
    echo 请先编译 BenchSuite.Console 项目
    echo.
    pause
    exit /b 1
)

REM 检查是否存在测试数据文件
if not exist "TestData\relative-path-test.json" (
    echo 错误: 找不到相对路径测试数据文件
    echo 请确保 TestData\relative-path-test.json 文件存在
    echo.
    pause
    exit /b 1
)

echo 正在创建测试环境...
mkdir "%TEST_BASE_PATH%\TestFiles\Backup" 2>nul
echo 测试文件内容 > "%TEST_BASE_PATH%\TestFiles\source.txt"
echo 要删除的文件 > "%TEST_BASE_PATH%\TestFiles\test-file.txt"
echo 测试环境创建完成: %TEST_BASE_PATH%
echo.

echo ========================================
echo 测试1: 不使用基础路径（默认行为）
echo ========================================
echo 运行命令: BenchSuite.Console.exe windows TestData\relative-path-test.json
echo.
"bin\Debug\net9.0-windows\BenchSuite.Console.exe" windows "TestData\relative-path-test.json"

echo.
echo ========================================
echo 测试2: 使用基础路径（--base-path）
echo ========================================
echo 运行命令: BenchSuite.Console.exe windows TestData\relative-path-test.json --base-path "%TEST_BASE_PATH%"
echo.
"bin\Debug\net9.0-windows\BenchSuite.Console.exe" windows "TestData\relative-path-test.json" --base-path "%TEST_BASE_PATH%"

echo.
echo ========================================
echo 测试3: 使用基础路径（-bp 简写）
echo ========================================
echo 运行命令: BenchSuite.Console.exe windows TestData\relative-path-test.json -bp "%TEST_BASE_PATH%"
echo.
"bin\Debug\net9.0-windows\BenchSuite.Console.exe" windows "TestData\relative-path-test.json" -bp "%TEST_BASE_PATH%"

echo.
echo ========================================
echo 清理测试环境
echo ========================================
if exist "%TEST_BASE_PATH%" (
    rmdir /s /q "%TEST_BASE_PATH%"
    echo 测试环境已清理: %TEST_BASE_PATH%
) else (
    echo 测试环境不存在，无需清理
)

echo.
echo ========================================
echo 测试完成！
echo ========================================
echo.
echo 测试说明:
echo 1. 测试1应该显示路径解析为 C:\TestFiles\... 格式
echo 2. 测试2和3应该显示路径解析为 %TEST_BASE_PATH%\TestFiles\... 格式
echo 3. 使用基础路径时，相对路径应该正确组合为绝对路径
echo.
pause
