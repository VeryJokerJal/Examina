-- 测试小数分值功能的SQL脚本
-- 用于验证数据库迁移和小数分值的正确性

-- 1. 检查数据库表结构是否正确更新
DESCRIBE SimplifiedQuestions;
DESCRIBE ExamQuestions;
DESCRIBE ExamSubjects;
DESCRIBE Exams;
DESCRIBE PracticeQuestions;

-- 2. 测试插入小数分值数据
-- 测试 SimplifiedQuestions 表
INSERT INTO SimplifiedQuestions (
    SubjectId, OperationType, Score, OperationConfig, Title, Description, 
    QuestionType, IsEnabled, CreatedAt
) VALUES (
    1, 'TestOperation', 15.5, '{}', '测试小数分值题目', '测试描述', 
    1, 1, NOW()
);

-- 测试 ExamQuestions 表
INSERT INTO ExamQuestions (
    ExamId, ExamSubjectId, QuestionNumber, Title, Content, QuestionType, 
    Score, DifficultyLevel, EstimatedMinutes, SortOrder, IsRequired, 
    IsEnabled, CreatedAt
) VALUES (
    1, 1, 999, '测试小数分值题目', '测试内容', 1, 
    12.75, 3, 10, 999, 1, 
    1, NOW()
);

-- 测试 ExamSubjects 表
INSERT INTO ExamSubjects (
    ExamId, SubjectType, SubjectName, Description, Score, DurationMinutes, 
    SortOrder, IsRequired, IsEnabled, Weight, CreatedAt
) VALUES (
    1, 1, '测试小数分值科目', '测试描述', 25.25, 30, 
    999, 1, 1, 1.0, NOW()
);

-- 测试 Exams 表
INSERT INTO Exams (
    Name, Description, ExamType, Status, TotalScore, DurationMinutes, 
    PassingScore, IsEnabled, CreatedAt, CreatedBy
) VALUES (
    '测试小数分值试卷', '测试描述', 1, 1, 150.75, 120, 
    90.5, 1, NOW(), 1
);

-- 测试 PracticeQuestions 表
INSERT INTO PracticeQuestions (
    PracticeId, Title, Content, OperationType, Score, DifficultyLevel, 
    IsEnabled, CreatedAt
) VALUES (
    1, '测试小数分值练习题', '测试内容', 'TestOperation', 8.25, 2, 
    1, NOW()
);

-- 3. 查询验证数据是否正确插入
SELECT 'SimplifiedQuestions' as TableName, Id, Score FROM SimplifiedQuestions WHERE OperationType = 'TestOperation';
SELECT 'ExamQuestions' as TableName, Id, Score FROM ExamQuestions WHERE QuestionNumber = 999;
SELECT 'ExamSubjects' as TableName, Id, Score FROM ExamSubjects WHERE SubjectName = '测试小数分值科目';
SELECT 'Exams' as TableName, Id, TotalScore, PassingScore FROM Exams WHERE Name = '测试小数分值试卷';
SELECT 'PracticeQuestions' as TableName, Id, Score FROM PracticeQuestions WHERE Title = '测试小数分值练习题';

-- 4. 测试小数精度
-- 插入边界值测试
INSERT INTO SimplifiedQuestions (
    SubjectId, OperationType, Score, OperationConfig, Title, Description, 
    QuestionType, IsEnabled, CreatedAt
) VALUES 
(1, 'MinScore', 0.1, '{}', '最小分值测试', '测试描述', 1, 1, NOW()),
(1, 'MaxScore', 100.0, '{}', '最大分值测试', '测试描述', 1, 1, NOW()),
(1, 'PrecisionTest', 99.99, '{}', '精度测试', '测试描述', 1, 1, NOW());

-- 查询边界值测试结果
SELECT 'Boundary Test' as TestType, OperationType, Score 
FROM SimplifiedQuestions 
WHERE OperationType IN ('MinScore', 'MaxScore', 'PrecisionTest');

-- 5. 测试分值计算
-- 计算总分测试
SELECT 
    'Score Calculation Test' as TestType,
    SUM(Score) as TotalScore,
    AVG(Score) as AverageScore,
    MIN(Score) as MinScore,
    MAX(Score) as MaxScore
FROM SimplifiedQuestions 
WHERE OperationType LIKE 'Test%';

-- 6. 清理测试数据
DELETE FROM SimplifiedQuestions WHERE OperationType LIKE 'Test%' OR OperationType IN ('MinScore', 'MaxScore', 'PrecisionTest');
DELETE FROM ExamQuestions WHERE QuestionNumber = 999;
DELETE FROM ExamSubjects WHERE SubjectName = '测试小数分值科目';
DELETE FROM Exams WHERE Name = '测试小数分值试卷';
DELETE FROM PracticeQuestions WHERE Title = '测试小数分值练习题';

-- 7. 验证清理完成
SELECT 'Cleanup Verification' as TestType, COUNT(*) as RemainingTestRecords
FROM (
    SELECT 1 FROM SimplifiedQuestions WHERE OperationType LIKE 'Test%' OR OperationType IN ('MinScore', 'MaxScore', 'PrecisionTest')
    UNION ALL
    SELECT 1 FROM ExamQuestions WHERE QuestionNumber = 999
    UNION ALL
    SELECT 1 FROM ExamSubjects WHERE SubjectName = '测试小数分值科目'
    UNION ALL
    SELECT 1 FROM Exams WHERE Name = '测试小数分值试卷'
    UNION ALL
    SELECT 1 FROM PracticeQuestions WHERE Title = '测试小数分值练习题'
) as test_records;

-- 8. 显示测试完成信息
SELECT 
    '小数分值功能测试完成' as Message,
    NOW() as TestCompletedAt,
    'decimal(5,2) for questions, decimal(6,2) for exams' as ScoreFormat;
