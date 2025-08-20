using System;
using System.Threading.Tasks;
using Examina.Services;
using Examina.ViewModels;
using Microsoft.Extensions.Logging;

namespace Examina.Views;

/// <summary>
/// ExamToolbarWindow 组件测试示例
/// </summary>
public static class ExamToolbarWindowTestExample
{
    /// <summary>
    /// 创建基本的考试工具栏测试
    /// </summary>
    /// <returns>配置好的考试工具栏窗口</returns>
    public static ExamToolbarWindow CreateBasicExamToolbar(
        IAuthenticationService authenticationService,
        ILogger<ExamToolbarViewModel> viewModelLogger,
        ILogger<ExamToolbarWindow> windowLogger)
    {
        // 创建ViewModel
        ExamToolbarViewModel viewModel = new ExamToolbarViewModel(authenticationService, viewModelLogger);
        
        // 创建屏幕预留服务
        ScreenReservationService screenService = new ScreenReservationService();
        
        // 创建窗口
        ExamToolbarWindow toolbarWindow = new ExamToolbarWindow(viewModel, screenService, windowLogger);

        return toolbarWindow;
    }

    /// <summary>
    /// 创建模拟考试工具栏测试
    /// </summary>
    public static async Task<ExamToolbarWindow> CreateMockExamToolbarAsync(
        IAuthenticationService authenticationService,
        IStudentMockExamService mockExamService,
        ILogger<ExamToolbarViewModel> viewModelLogger,
        ILogger<ExamToolbarWindow> windowLogger,
        ILogger<ExamToolbarService> serviceLogger)
    {
        // 创建ViewModel
        ExamToolbarViewModel viewModel = new ExamToolbarViewModel(authenticationService, viewModelLogger);
        
        // 创建考试服务（这里需要其他服务的实例，暂时传null）
        ExamToolbarService examService = new ExamToolbarService(
            null!, // IStudentExamService
            mockExamService,
            null!, // IStudentComprehensiveTrainingService
            authenticationService,
            serviceLogger);
        
        // 创建屏幕预留服务
        ScreenReservationService screenService = new ScreenReservationService();
        
        // 创建窗口
        ExamToolbarWindow toolbarWindow = new ExamToolbarWindow(viewModel, screenService, windowLogger);

        // 设置模拟考试信息
        viewModel.SetExamInfo(ExamType.MockExam, 1, "模拟考试测试", 50, 7200); // 2小时

        return toolbarWindow;
    }

    /// <summary>
    /// 创建综合实训工具栏测试
    /// </summary>
    public static ExamToolbarWindow CreateComprehensiveTrainingToolbar(
        IAuthenticationService authenticationService,
        IStudentComprehensiveTrainingService trainingService,
        ILogger<ExamToolbarViewModel> viewModelLogger,
        ILogger<ExamToolbarWindow> windowLogger,
        ILogger<ExamToolbarService> serviceLogger)
    {
        // 创建ViewModel
        ExamToolbarViewModel viewModel = new ExamToolbarViewModel(authenticationService, viewModelLogger);
        
        // 创建考试服务
        ExamToolbarService examService = new ExamToolbarService(
            null!, // IStudentExamService
            null!, // IStudentMockExamService
            trainingService,
            authenticationService,
            serviceLogger);
        
        // 创建屏幕预留服务
        ScreenReservationService screenService = new ScreenReservationService();
        
        // 创建窗口
        ExamToolbarWindow toolbarWindow = new ExamToolbarWindow(viewModel, screenService, windowLogger);

        // 设置综合实训信息
        viewModel.SetExamInfo(ExamType.ComprehensiveTraining, 1, "综合实训测试", 30, 7200); // 2小时

        return toolbarWindow;
    }

    /// <summary>
    /// 测试考试工具栏功能
    /// </summary>
    public static async Task TestExamToolbarFunctionalityAsync(ExamToolbarWindow toolbarWindow)
    {
        Console.WriteLine("开始测试考试工具栏功能...");

        try
        {
            // 测试显示工具栏
            Console.WriteLine("1. 测试显示工具栏");
            toolbarWindow.Show();
            await Task.Delay(1000);

            // 测试开始考试
            Console.WriteLine("2. 测试开始考试");
            toolbarWindow.StartExam(ExamType.MockExam, 1, "测试考试", 20, 60); // 1分钟测试
            await Task.Delay(2000);

            // 测试停止考试
            Console.WriteLine("3. 测试停止考试");
            toolbarWindow.StopExam();
            await Task.Delay(1000);

            Console.WriteLine("考试工具栏功能测试完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试过程中发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试倒计时功能
    /// </summary>
    public static async Task TestCountdownFunctionalityAsync(ExamToolbarViewModel viewModel)
    {
        Console.WriteLine("开始测试倒计时功能...");

        try
        {
            // 设置一个短时间的倒计时进行测试
            Console.WriteLine("1. 开始10秒倒计时测试");
            viewModel.StartCountdown(10);

            // 监听状态变化
            bool autoSubmitted = false;
            viewModel.ExamAutoSubmitted += (sender, e) =>
            {
                autoSubmitted = true;
                Console.WriteLine("检测到自动提交事件");
            };

            // 等待倒计时结束
            while (viewModel.RemainingTimeSeconds > 0 && !autoSubmitted)
            {
                Console.WriteLine($"剩余时间: {viewModel.FormattedRemainingTime}");
                await Task.Delay(1000);
            }

            if (autoSubmitted)
            {
                Console.WriteLine("倒计时功能测试成功：自动提交已触发");
            }
            else
            {
                Console.WriteLine("倒计时功能测试完成");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"倒计时测试过程中发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试错误处理功能
    /// </summary>
    public static async Task TestErrorHandlingAsync()
    {
        Console.WriteLine("开始测试错误处理功能...");

        try
        {
            // 创建错误处理器
            ILogger<ExamErrorHandler> logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ExamErrorHandler>.Instance;
            ExamErrorHandler errorHandler = new ExamErrorHandler(logger);

            // 测试网络连接检查
            Console.WriteLine("1. 测试网络连接检查");
            bool networkAvailable = await errorHandler.CheckNetworkConnectivityAsync();
            Console.WriteLine($"网络连接状态: {(networkAvailable ? "可用" : "不可用")}");

            // 测试网络错误处理
            Console.WriteLine("2. 测试网络错误处理");
            Exception networkException = new System.Net.Http.HttpRequestException("网络请求失败");
            bool canRetry = await errorHandler.HandleNetworkErrorAsync(networkException, "测试操作");
            Console.WriteLine($"网络错误处理结果: {(canRetry ? "可重试" : "不可重试")}");

            // 测试考试提交错误处理
            Console.WriteLine("3. 测试考试提交错误处理");
            Exception submitException = new TimeoutException("请求超时");
            ExamSubmitErrorResult submitResult = await errorHandler.HandleExamSubmitErrorAsync(submitException, "MockExam", 1);
            Console.WriteLine($"提交错误处理结果: {submitResult.ErrorMessage}, 可重试: {submitResult.IsRetryable}");

            Console.WriteLine("错误处理功能测试完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误处理测试过程中发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建完整的测试场景
    /// </summary>
    public static async Task<ExamToolbarWindow> CreateCompleteTestScenarioAsync(
        IAuthenticationService authenticationService,
        IStudentMockExamService mockExamService,
        ILogger<ExamToolbarViewModel> viewModelLogger,
        ILogger<ExamToolbarWindow> windowLogger,
        ILogger<ExamToolbarService> serviceLogger)
    {
        Console.WriteLine("创建完整的考试工具栏测试场景...");

        // 创建考试工具栏
        ExamToolbarWindow toolbarWindow = await CreateMockExamToolbarAsync(
            authenticationService, mockExamService, viewModelLogger, windowLogger, serviceLogger);

        // 设置事件处理
        toolbarWindow.ExamAutoSubmitted += (sender, e) =>
        {
            Console.WriteLine("考试自动提交事件触发");
        };

        toolbarWindow.ExamManualSubmitted += (sender, e) =>
        {
            Console.WriteLine("考试手动提交事件触发");
        };

        toolbarWindow.ViewQuestionsRequested += (sender, e) =>
        {
            Console.WriteLine("查看题目请求事件触发");
        };

        // 执行功能测试
        await TestExamToolbarFunctionalityAsync(toolbarWindow);

        Console.WriteLine("完整测试场景创建完成！");
        return toolbarWindow;
    }

    /// <summary>
    /// 演示考试工具栏的完整生命周期
    /// </summary>
    public static async Task DemonstrateExamLifecycleAsync(ExamToolbarWindow toolbarWindow)
    {
        Console.WriteLine("演示考试工具栏的完整生命周期...");

        try
        {
            // 1. 显示工具栏
            Console.WriteLine("步骤1: 显示考试工具栏");
            toolbarWindow.Show();
            await Task.Delay(1000);

            // 2. 开始考试
            Console.WriteLine("步骤2: 开始模拟考试");
            toolbarWindow.StartExam(ExamType.MockExam, 1, "生命周期演示考试", 25, 30); // 30秒测试
            await Task.Delay(2000);

            // 3. 模拟考试进行中
            Console.WriteLine("步骤3: 考试进行中...");
            await Task.Delay(10000); // 等待10秒

            // 4. 手动停止考试
            Console.WriteLine("步骤4: 手动停止考试");
            toolbarWindow.StopExam();
            await Task.Delay(1000);

            // 5. 隐藏工具栏
            Console.WriteLine("步骤5: 隐藏考试工具栏");
            toolbarWindow.Hide();

            Console.WriteLine("考试生命周期演示完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生命周期演示过程中发生错误: {ex.Message}");
        }
    }
}
