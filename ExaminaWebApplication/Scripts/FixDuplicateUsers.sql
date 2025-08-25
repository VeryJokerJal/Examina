-- 修复微信用户和手机号用户重复的问题
-- 此脚本将手机号用户的数据合并到对应的微信用户，然后删除重复的手机号用户

-- 1. 首先查看当前的重复用户情况
SELECT 
    u1.Id as WeChatUserId,
    u1.Username as WeChatUsername,
    u1.PhoneNumber as WeChatPhone,
    u1.WeChatOpenId,
    u2.Id as PhoneUserId,
    u2.Username as PhoneUsername,
    u2.PhoneNumber as PhonePhone,
    u2.WeChatOpenId as PhoneWeChatOpenId
FROM Users u1
INNER JOIN Users u2 ON u1.PhoneNumber IS NULL 
    AND u2.PhoneNumber IS NOT NULL 
    AND u1.WeChatOpenId IS NOT NULL 
    AND u2.WeChatOpenId IS NULL
    AND u1.Username LIKE 'wechat_%'
    AND u2.Username LIKE '考生%'
WHERE u1.IsActive = 1 AND u2.IsActive = 1;

-- 2. 更新微信用户的手机号（如果为空）
UPDATE u1 
SET u1.PhoneNumber = u2.PhoneNumber,
    u1.IsFirstLogin = 0  -- 确保标记为非首次登录
FROM Users u1
INNER JOIN Users u2 ON u1.PhoneNumber IS NULL 
    AND u2.PhoneNumber IS NOT NULL 
    AND u1.WeChatOpenId IS NOT NULL 
    AND u2.WeChatOpenId IS NULL
    AND u1.Username LIKE 'wechat_%'
    AND u2.Username LIKE '考生%'
WHERE u1.IsActive = 1 AND u2.IsActive = 1;

-- 3. 将手机号用户的相关数据迁移到微信用户（如果有的话）
-- 迁移用户会话
UPDATE UserSessions 
SET UserId = (
    SELECT u1.Id 
    FROM Users u1
    INNER JOIN Users u2 ON u1.PhoneNumber = u2.PhoneNumber
        AND u1.WeChatOpenId IS NOT NULL 
        AND u2.WeChatOpenId IS NULL
        AND u1.Username LIKE 'wechat_%'
        AND u2.Username LIKE '考生%'
    WHERE u2.Id = UserSessions.UserId
        AND u1.IsActive = 1 AND u2.IsActive = 1
)
WHERE UserId IN (
    SELECT u2.Id 
    FROM Users u1
    INNER JOIN Users u2 ON u1.PhoneNumber = u2.PhoneNumber
        AND u1.WeChatOpenId IS NOT NULL 
        AND u2.WeChatOpenId IS NULL
        AND u1.Username LIKE 'wechat_%'
        AND u2.Username LIKE '考生%'
    WHERE u1.IsActive = 1 AND u2.IsActive = 1
);

-- 4. 迁移设备绑定
UPDATE UserDevices 
SET UserId = (
    SELECT u1.Id 
    FROM Users u1
    INNER JOIN Users u2 ON u1.PhoneNumber = u2.PhoneNumber
        AND u1.WeChatOpenId IS NOT NULL 
        AND u2.WeChatOpenId IS NULL
        AND u1.Username LIKE 'wechat_%'
        AND u2.Username LIKE '考生%'
    WHERE u2.Id = UserDevices.UserId
        AND u1.IsActive = 1 AND u2.IsActive = 1
)
WHERE UserId IN (
    SELECT u2.Id 
    FROM Users u1
    INNER JOIN Users u2 ON u1.PhoneNumber = u2.PhoneNumber
        AND u1.WeChatOpenId IS NOT NULL 
        AND u2.WeChatOpenId IS NULL
        AND u1.Username LIKE 'wechat_%'
        AND u2.Username LIKE '考生%'
    WHERE u1.IsActive = 1 AND u2.IsActive = 1
);

-- 5. 软删除重复的手机号用户
UPDATE u2 
SET u2.IsActive = 0,
    u2.Username = u2.Username + '_DELETED_' + CAST(GETDATE() AS VARCHAR(20))
FROM Users u1
INNER JOIN Users u2 ON u1.PhoneNumber = u2.PhoneNumber
    AND u1.WeChatOpenId IS NOT NULL 
    AND u2.WeChatOpenId IS NULL
    AND u1.Username LIKE 'wechat_%'
    AND u2.Username LIKE '考生%'
WHERE u1.IsActive = 1 AND u2.IsActive = 1;

-- 6. 验证修复结果
SELECT 
    Id,
    Username,
    PhoneNumber,
    WeChatOpenId,
    IsFirstLogin,
    IsActive,
    CreatedAt
FROM Users 
WHERE (Username LIKE 'wechat_%' OR Username LIKE '考生%' OR Username LIKE '%DELETED%')
ORDER BY CreatedAt DESC;

-- 7. 检查是否还有重复的手机号
SELECT PhoneNumber, COUNT(*) as Count
FROM Users 
WHERE PhoneNumber IS NOT NULL AND IsActive = 1
GROUP BY PhoneNumber
HAVING COUNT(*) > 1;
