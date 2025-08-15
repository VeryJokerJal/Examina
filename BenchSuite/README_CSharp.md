# C#编程题打分功能说明

## 概述

BenchSuite中的C#编程题打分功能提供了对C#代码的自动化评分能力，支持三种不同的评分模式，能够全面评估学生的编程能力和代码质量。

## 功能特性

### 支持的评分模式（与ExamLab完全对应）

#### 1. 代码补全模式 (CodeCompletion)
- **原理**: 基于`NotImplementedException`的填空检测
- **适用场景**: 填空题、代码补全练习
- **技术实现**: 使用Roslyn语法分析器进行代码结构比较
- **评分标准**: 每个填空位置独立评分，支持多行代码填空

**示例**:
```csharp
// 模板代码
public int Add(int a, int b)
{
    // TODO: 实现加法
    throw new NotImplementedException();
}

// 学生代码
public int Add(int a, int b)
{
    return a + b;
}

// 期望实现
"return a + b;"
```

#### 2. 调试纠错模式 (Debugging)
- **原理**: 检测和验证代码错误修复
- **适用场景**: 调试能力评估、错误识别和修复
- **技术实现**: 对比错误代码和修复代码，验证错误是否正确修复
- **评分标准**: 根据正确修复的错误数量计算得分

**特性**:
- 语法错误检测
- 编译错误分析
- 逻辑错误识别
- 修复验证和评分

**示例**:
```csharp
// 包含错误的代码
public int Add(int a, int b)
{
    return a - b; // 错误：应该是加法
}

// 学生修复后的代码
public int Add(int a, int b)
{
    return a + b; // 修复：改为正确的加法
}

// 期望发现的错误
["减法错误"]
```

#### 3. 编写实现模式 (Implementation)
- **原理**: 完整实现指定功能并通过测试验证
- **适用场景**: 完整功能实现、算法编程、项目开发
- **技术实现**: 编译检查 + 单元测试验证
- **评分标准**: 编译成功 + 测试通过率

**特性**:
- 编译正确性验证
- 功能完整性测试
- 性能和质量评估
- 综合能力评价

**示例**:
```csharp
// 学生实现的完整代码
public class Calculator
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
}

// 测试代码
public class CalculatorTests
{
    [Test]
    public void TestAdd()
    {
        var calc = new Calculator();
        if (calc.Add(2, 3) != 5)
            throw new Exception("Add test failed");
    }
}
```

## 核心组件

### 1. CSharpScoringService
主要的评分服务类，提供统一的评分接口。

```csharp
public async Task<CSharpScoringResult> ScoreCodeAsync(
    string templateCode, 
    string studentCode, 
    List<string> expectedImplementations, 
    CSharpScoringMode mode)
```

### 2. CSharpCodeCompletionGrader
代码补全模式的核心评分器，负责：
- 查找模板中的`NotImplementedException`位置
- 提取学生代码中对应的实现
- 使用语法树比较代码等价性

### 3. CSharpCompilationChecker
编译检查模式的核心组件，负责：
- 动态编译C#代码
- 收集编译错误和警告
- 生成可执行程序集

### 4. CSharpDebuggingGrader
调试纠错模式的核心组件，负责：
- 分析代码中的各种错误
- 比较修复前后的代码差异
- 验证错误修复的正确性
- 生成详细的调试报告

### 5. CSharpUnitTestRunner
单元测试执行组件，负责：
- 合并学生代码和测试代码
- 动态执行测试用例
- 收集测试结果和统计信息

## 使用示例

### 基础使用
```csharp
var service = new CSharpScoringService();

// 代码补全模式
var result = await service.ScoreCodeAsync(
    templateCode, 
    studentCode, 
    expectedImplementations, 
    CSharpScoringMode.CodeCompletion);

Console.WriteLine($"得分: {result.AchievedScore}/{result.TotalScore}");
```

### 完整评分流程
```csharp
// 1. 代码补全评分
var completionResult = await service.ScoreCodeAsync(
    template, studentCode, expected, CSharpScoringMode.CodeCompletion);

// 2. 调试纠错评分
var debuggingResult = await service.ScoreCodeAsync(
    buggyCode, fixedCode, expectedErrors, CSharpScoringMode.Debugging);

// 3. 编写实现评分
var implementationResult = await service.ScoreCodeAsync(
    "", studentCode, [testCode], CSharpScoringMode.Implementation);

// 综合评分
decimal totalScore = completionResult.AchievedScore +
                   debuggingResult.AchievedScore +
                   implementationResult.AchievedScore;
```

## 评分结果模型

### CSharpScoringResult
```csharp
public class CSharpScoringResult
{
    public CSharpScoringMode Mode { get; set; }
    public decimal TotalScore { get; set; }
    public decimal AchievedScore { get; set; }
    public decimal ScoreRate { get; set; }
    public bool IsSuccess { get; set; }
    public string Details { get; set; }
    
    // 模式特定结果
    public List<FillBlankResult> FillBlankResults { get; set; }
    public CompilationResult? CompilationResult { get; set; }
    public UnitTestResult? UnitTestResult { get; set; }
    public DebuggingResult? DebuggingResult { get; set; }
}
```

### 填空结果 (FillBlankResult)
```csharp
public class FillBlankResult
{
    public int BlankIndex { get; set; }
    public BlankDescriptor Descriptor { get; set; }
    public bool Matched { get; set; }
    public string ExpectedText { get; set; }
    public string StudentText { get; set; }
    public string Message { get; set; }
}
```

### 编译结果 (CompilationResult)
```csharp
public class CompilationResult
{
    public bool IsSuccess { get; set; }
    public List<CompilationError> Errors { get; set; }
    public List<CompilationWarning> Warnings { get; set; }
    public byte[]? AssemblyBytes { get; set; }
    public long CompilationTimeMs { get; set; }
}
```

### 调试结果 (DebuggingResult)
```csharp
public class DebuggingResult
{
    public bool IsSuccess { get; set; }
    public int TotalErrors { get; set; }
    public int FixedErrors { get; set; }
    public int RemainingErrors { get; set; }
    public List<ErrorDetectionResult> ErrorDetections { get; set; }
    public List<FixVerificationResult> FixVerifications { get; set; }
    public long DebuggingTimeMs { get; set; }
}
```

### 单元测试结果 (UnitTestResult)
```csharp
public class UnitTestResult
{
    public bool IsSuccess { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public List<TestCaseResult> TestCaseResults { get; set; }
    public long ExecutionTimeMs { get; set; }
}
```

## 技术特点

### 1. 基于Roslyn的语法分析
- 精确的代码结构分析
- 语义等价性比较
- 支持复杂的代码模式匹配

### 2. 动态编译和执行
- 实时编译验证
- 内存中程序集加载
- 安全的代码执行环境

### 3. 灵活的测试框架支持
- 支持多种测试属性
- 自定义测试模板生成
- 详细的测试执行报告

### 4. 完善的错误处理
- 详细的错误信息收集
- 异常安全的代码执行
- 超时保护机制

## 最佳实践

### 1. 代码补全模式
- 使用清晰的注释标记填空位置
- 提供合理的前后文锚点
- 期望实现应该简洁明确

### 2. 编译检查模式
- 提供必要的using语句
- 确保引用的程序集可用
- 考虑不同.NET版本的兼容性

### 3. 单元测试模式
- 编写全面的测试用例
- 包含边界条件测试
- 提供清晰的错误消息

## 扩展性

系统设计具有良好的扩展性，支持：
- 自定义评分算法
- 新的测试框架集成
- 额外的代码质量检查
- 性能测试集成

## 依赖项

- Microsoft.CodeAnalysis.CSharp (4.8.0)
- Microsoft.CodeAnalysis.CSharp.Scripting (4.8.0)
- .NET 9.0 或更高版本

## 注意事项

1. **安全性**: 动态编译和执行代码存在安全风险，建议在隔离环境中运行
2. **性能**: 编译过程可能耗时较长，建议设置合理的超时时间
3. **内存**: 动态加载的程序集可能导致内存泄漏，注意及时清理
4. **兼容性**: 不同.NET版本可能存在API差异，需要适当调整

## 示例项目

参考`BenchSuite/Tests/CSharpScoringDemo.cs`获取完整的使用示例和演示代码。
