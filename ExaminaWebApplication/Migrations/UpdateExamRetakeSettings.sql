-- 更新现有考试的重考和练习设置
-- 这个脚本将为所有现有考试启用重考和练习功能

-- 更新ImportedExams表，启用重考和练习功能
UPDATE ImportedExams 
SET 
    AllowRetake = 1,           -- 允许重考
    AllowPractice = 1,         -- 允许练习
    MaxRetakeCount = 3         -- 最大重考次数设为3次
WHERE 
    AllowRetake = 0            -- 只更新当前不允许重考的考试
    OR AllowPractice = 0       -- 或不允许练习的考试
    OR MaxRetakeCount = 0;     -- 或重考次数为0的考试

-- 显示更新结果
SELECT 
    Id,
    Name,
    AllowRetake,
    AllowPractice,
    MaxRetakeCount,
    Status,
    IsEnabled
FROM ImportedExams
WHERE IsEnabled = 1
ORDER BY Id;

-- 输出更新统计
SELECT 
    COUNT(*) as TotalExams,
    SUM(CASE WHEN AllowRetake = 1 THEN 1 ELSE 0 END) as ExamsWithRetake,
    SUM(CASE WHEN AllowPractice = 1 THEN 1 ELSE 0 END) as ExamsWithPractice,
    AVG(CAST(MaxRetakeCount as FLOAT)) as AvgMaxRetakeCount
FROM ImportedExams
WHERE IsEnabled = 1;
