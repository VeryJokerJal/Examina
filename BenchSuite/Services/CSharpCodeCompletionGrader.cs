using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using BenchSuite.Models;

namespace BenchSuite.Services;

/// <summary>
/// C#代码补全评分器 - 基于NotImplementedException的填空检测
/// </summary>
public static class CSharpCodeCompletionGrader
{
    /// <summary>
    /// 找到模板中所有 throw new NotImplementedException() 的位置，并记录前后锚点语句
    /// </summary>
    /// <param name="templateCode">模板代码</param>
    /// <returns>填空描述符列表</returns>
    public static List<BlankDescriptor> FindBlanks(string templateCode)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(templateCode);
        SyntaxNode root = tree.GetRoot();
        
        List<ThrowStatementSyntax> throws = root.DescendantNodes()
            .OfType<ThrowStatementSyntax>()
            .Where(t =>
            {
                if (t.Expression is ObjectCreationExpressionSyntax oce)
                {
                    if (oce.Type is IdentifierNameSyntax id)
                    {
                        return id.Identifier.Text == "NotImplementedException";
                    }
                }
                return false;
            })
            .ToList();

        List<BlankDescriptor> blanks = [];
        
        foreach (ThrowStatementSyntax t in throws)
        {
            BaseMethodDeclarationSyntax? method = t.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
            string name = method switch
            {
                MethodDeclarationSyntax m => m.Identifier.Text,
                ConstructorDeclarationSyntax c => c.Identifier.Text,
                _ => "unknown"
            };

            int idx = -1;
            StatementSyntax? prev = null;
            StatementSyntax? next = null;

            if (method is MethodDeclarationSyntax md && md.Body != null)
            {
                SyntaxList<StatementSyntax> stmts = md.Body.Statements;
                StatementSyntax? containing = stmts.FirstOrDefault(s => s.Span.Contains(t.Span));
                if (containing != null)
                {
                    idx = stmts.IndexOf(containing);
                    if (idx - 1 >= 0)
                    {
                        prev = stmts[idx - 1];
                    }

                    if (idx + 1 < stmts.Count)
                    {
                        next = stmts[idx + 1];
                    }
                }
            }
            else if (method is ConstructorDeclarationSyntax cd && cd.Body != null)
            {
                SyntaxList<StatementSyntax> stmts = cd.Body.Statements;
                StatementSyntax? containing = stmts.FirstOrDefault(s => s.Span.Contains(t.Span));
                if (containing != null)
                {
                    idx = stmts.IndexOf(containing);
                    if (idx - 1 >= 0)
                    {
                        prev = stmts[idx - 1];
                    }

                    if (idx + 1 < stmts.Count)
                    {
                        next = stmts[idx + 1];
                    }
                }
            }

            blanks.Add(new BlankDescriptor
            {
                MethodName = name,
                StatementIndex = idx,
                ThrowNode = t,
                PrevStatement = prev,
                NextStatement = next
            });
        }

        return blanks;
    }

    /// <summary>
    /// 在学生代码中，按照方法名 + （prev/next）锚点定位并提取"替换块"——返回多个语句（可能为空）
    /// </summary>
    /// <param name="studentCode">学生代码</param>
    /// <param name="descriptor">填空描述符</param>
    /// <returns>学生语句列表</returns>
    public static List<StatementSyntax>? ExtractStudentStatements(string studentCode, BlankDescriptor descriptor)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(studentCode);
        SyntaxNode root = tree.GetRoot();

        // 找方法
        List<MethodDeclarationSyntax> candidateMethods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.Text == descriptor.MethodName)
            .ToList();

        if (!candidateMethods.Any())
        {
            // 也尝试构造函数
            List<ConstructorDeclarationSyntax> ctors = root.DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .Where(c => c.Identifier.Text == descriptor.MethodName)
                .ToList();
            if (ctors.Any())
            {
                ConstructorDeclarationSyntax ctor = ctors.First();
                return ExtractBetweenAnchors(ctor.Body?.Statements.ToList() ?? [], 
                    descriptor.PrevStatement as StatementSyntax, 
                    descriptor.NextStatement as StatementSyntax);
            }
            return null;
        }

        MethodDeclarationSyntax methodNode = candidateMethods.First();
        if (methodNode.Body == null)
        {
            return null;
        }

        List<StatementSyntax> studentStmts = methodNode.Body.Statements.ToList();

        // 如果模板有 prev/next，可以在学生语句中寻找等价语句作为锚点
        return ExtractBetweenAnchors(studentStmts, 
            descriptor.PrevStatement as StatementSyntax, 
            descriptor.NextStatement as StatementSyntax);
    }

    /// <summary>
    /// 根据 prev 和 next（可能为空）从 studentStatements 中提取区间
    /// </summary>
    /// <param name="studentStatements">学生语句列表</param>
    /// <param name="prev">前一个语句</param>
    /// <param name="next">后一个语句</param>
    /// <returns>提取的语句列表</returns>
    private static List<StatementSyntax>? ExtractBetweenAnchors(List<StatementSyntax> studentStatements, StatementSyntax? prev, StatementSyntax? next)
    {
        int startIndex = 0;
        int endIndex = studentStatements.Count - 1;

        int FindEquivalentIndex(StatementSyntax s)
        {
            for (int i = 0; i < studentStatements.Count; i++)
            {
                if (studentStatements[i].IsEquivalentTo(s, topLevel: false))
                {
                    return i;
                }
            }
            return -1;
        }

        if (prev != null)
        {
            int p = FindEquivalentIndex(prev);
            if (p >= 0)
            {
                startIndex = p + 1;
            }
        }

        if (next != null)
        {
            int n = FindEquivalentIndex(next);
            if (n >= 0)
            {
                endIndex = n - 1;
            }
        }

        return startIndex > endIndex ? [] : studentStatements.GetRange(startIndex, endIndex - startIndex + 1);
    }

    /// <summary>
    /// 比较教师期望（可能多行）与学生提取出的语句序列
    /// </summary>
    /// <param name="expectedBlockText">期望的代码块文本</param>
    /// <param name="studentStmts">学生语句列表</param>
    /// <returns>是否等价</returns>
    public static bool AreStatementsEquivalent(string expectedBlockText, List<StatementSyntax>? studentStmts)
    {
        if (studentStmts == null)
        {
            return false;
        }

        string wrapper = "{\n" + expectedBlockText + "\n}";
        try
        {
            StatementSyntax stmt = SyntaxFactory.ParseStatement(wrapper);
            if (stmt is not BlockSyntax block)
            {
                return false;
            }

            List<StatementSyntax> expectedList = block.Statements.ToList();

            if (expectedList.Count != studentStmts.Count)
            {
                return false;
            }

            for (int i = 0; i < expectedList.Count; i++)
            {
                if (!expectedList[i].IsEquivalentTo(studentStmts[i], topLevel: false))
                {
                    return false;
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 主评分方法（返回 FillBlankResult 列表）
    /// </summary>
    /// <param name="templateCode">模板代码</param>
    /// <param name="expectedImplementations">期望实现列表</param>
    /// <param name="studentCode">学生代码</param>
    /// <returns>填空结果列表</returns>
    public static List<FillBlankResult> Grade(string templateCode, List<string> expectedImplementations, string studentCode)
    {
        List<BlankDescriptor> blanks = FindBlanks(templateCode);
        List<FillBlankResult> results = [];

        for (int i = 0; i < blanks.Count; i++)
        {
            BlankDescriptor desc = blanks[i];
            string expectedText = i < expectedImplementations.Count ? expectedImplementations[i] : "";
            List<StatementSyntax>? studentStmts = ExtractStudentStatements(studentCode, desc);
            bool matched = AreStatementsEquivalent(expectedText, studentStmts);

            string studentText = studentStmts == null
                ? "(未找到对应方法或方法无块体)"
                : studentStmts.Count == 0 ? "(学生未填写任何语句)" : string.Join("\n", studentStmts.Select(s => s.ToFullString().Trim()));
            
            string message = matched ? "匹配成功" : "不匹配";
            if (studentStmts == null)
            {
                message += "（未找到 / 无法定位）";
            }
            else if (studentStmts.Count == 0)
            {
                message += "（学生没有填入代码）";
            }
            else
            {
                message += $"（匹配了 {studentStmts.Count} 行语句）";
            }

            results.Add(new FillBlankResult
            {
                BlankIndex = i,
                Descriptor = desc,
                Matched = matched,
                ExpectedText = expectedText,
                StudentText = studentText,
                Message = message,
                Score = matched ? 1 : 0 // 简单的0/1评分，可以根据需要调整
            });
        }

        return results;
    }
}
