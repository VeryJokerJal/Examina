using System;
using System.Threading.Tasks;

namespace Examina.Tests;

/// <summary>
/// 运行考试模式修复测试的程序
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        await ExamModeFixTest.RunAllTestsAsync();
    }
}
