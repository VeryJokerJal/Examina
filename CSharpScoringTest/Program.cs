using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

Console.WriteLine("🎯 C#编程题打分系统演示");
Console.WriteLine("=".PadRight(50, '='));
Console.WriteLine();

// 演示代码补全模式
await DemoCodeCompletionAsync();

Console.WriteLine();
Console.WriteLine("演示完成，按任意键退出...");
Console.ReadKey();

/// <summary>
/// 演示代码补全模式
/// </summary>
static async Task DemoCodeCompletionAsync()
{
    Console.WriteLine("📝 演示代码补全模式");

    // 模板代码（包含NotImplementedException）
    string templateCode = @"
using System;

class Calculator
{
    public int Add(int a, int b)
    {
        Console.WriteLine(""计算中..."");
        // 填空1：实现加法
        throw new NotImplementedException();
    }
}";

    // 学生代码（已填写）
    string studentCode = @"
using System;

class Calculator
{
    public int Add(int a, int b)
    {
        Console.WriteLine(""计算中..."");
        int result = a + b;
        Console.WriteLine($""结果: {result}"");
        return result;
    }
}";

    // 期望的实现
    string expectedImplementation = @"int result = a + b;
Console.WriteLine($""结果: {result}"");
return result;";

    Console.WriteLine("模板代码:");
    Console.WriteLine(templateCode);
    Console.WriteLine();

    Console.WriteLine("学生代码:");
    Console.WriteLine(studentCode);
    Console.WriteLine();

    Console.WriteLine("期望实现:");
    Console.WriteLine(expectedImplementation);
    Console.WriteLine();

    // 简化的评分逻辑演示
    try
    {
        // 解析模板代码，查找NotImplementedException
        SyntaxTree templateTree = CSharpSyntaxTree.ParseText(templateCode);
        var templateRoot = templateTree.GetRoot();

        var throwStatements = templateRoot.DescendantNodes()
            .OfType<ThrowStatementSyntax>()
            .Where(t => t.Expression is ObjectCreationExpressionSyntax oce &&
                       oce.Type is IdentifierNameSyntax id &&
                       id.Identifier.Text == "NotImplementedException")
            .ToList();

        Console.WriteLine($"✅ 在模板中找到 {throwStatements.Count} 个填空位置");

        // 解析学生代码
        SyntaxTree studentTree = CSharpSyntaxTree.ParseText(studentCode);
        var studentRoot = studentTree.GetRoot();

        // 检查学生代码是否能编译
        var compilation = CSharpCompilation.Create("StudentCode")
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location))
            .AddSyntaxTrees(studentTree);

        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        if (errors.Any())
        {
            Console.WriteLine($"❌ 学生代码有 {errors.Count} 个编译错误:");
            foreach (var error in errors.Take(3))
            {
                Console.WriteLine($"   - {error.GetMessage()}");
            }
        }
        else
        {
            Console.WriteLine("✅ 学生代码编译成功");
        }

        // 简化的匹配检查
        bool hasAddMethod = studentRoot.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Any(m => m.Identifier.Text == "Add");

        bool hasReturnStatement = studentRoot.DescendantNodes()
            .OfType<ReturnStatementSyntax>()
            .Any();

        bool hasAddOperation = studentCode.Contains("a + b");

        Console.WriteLine($"✅ 包含Add方法: {hasAddMethod}");
        Console.WriteLine($"✅ 包含return语句: {hasReturnStatement}");
        Console.WriteLine($"✅ 包含加法操作: {hasAddOperation}");

        bool isCorrect = hasAddMethod && hasReturnStatement && hasAddOperation && !errors.Any();

        Console.WriteLine();
        Console.WriteLine($"🎯 评分结果: {(isCorrect ? "通过✅" : "未通过❌")}");
        Console.WriteLine($"   得分: {(isCorrect ? 1 : 0)}/1");
        Console.WriteLine($"   状态: {(isCorrect ? "代码补全正确" : "代码补全有误")}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ 评分过程中发生异常: {ex.Message}");
    }
}
