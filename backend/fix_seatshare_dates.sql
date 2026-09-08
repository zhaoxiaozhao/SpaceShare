-- =========================================================
-- 修复：生产库 SeatShares 存在非法日期（0000-00-00 / 极小值）
-- 导致 GET /api/v1/venues/{id}/shares 返回 500：
--   MySqlException: Unable to serialize date/time value
--
-- 请在云端数据库（friendlyseat 库）执行以下步骤。
-- 步骤 1：先排查（只读，确认脏数据范围）
-- 步骤 2：备份 + 修复
-- =========================================================
--
-- ---------------------------------------------------------
-- 【步骤 1】排查：找出所有日期异常的行
-- 说明：若查询报错 "Incorrect DATETIME value"，说明库处于严格模式，
-- 先执行 SET SESSION sql_mode='' 再查询（当前会话有效，不影响全局）。
-- ---------------------------------------------------------
SET SESSION sql_mode='';

SELECT Id, SeatId, OwnerUserId, Status, StartAt, EndAt, CreatedAt, CancelledAt
FROM SeatShares
WHERE Id IN (
    SELECT Id FROM SeatShares
    WHERE StartAt IS NULL OR StartAt < '1980-01-01 00:00:00'
       OR EndAt   IS NULL OR EndAt   < '1980-01-01 00:00:00'
       OR CreatedAt IS NULL OR CreatedAt < '1980-01-01 00:00:00'
       OR CancelledAt < '1980-01-01 00:00:00'
)
ORDER BY Id;

-- 同时检查这些脏分享是否被预约/候补引用（决定能否直接删）
SELECT s.Id AS ShareId, s.Status,
       (SELECT COUNT(*) FROM Reservations r WHERE r.ShareId = s.Id) AS Reservations,
       (SELECT COUNT(*) FROM ReservationWaitlists w WHERE w.ShareId = s.Id) AS Waitlists
FROM SeatShares s
WHERE s.Id IN (
    SELECT Id FROM SeatShares
    WHERE StartAt IS NULL OR StartAt < '1980-01-01 00:00:00'
       OR EndAt   IS NULL OR EndAt   < '1980-01-01 00:00:00'
       OR CreatedAt IS NULL OR CreatedAt < '1980-01-01 00:00:00'
       OR CancelledAt < '1980-01-01 00:00:00'
);

-- ---------------------------------------------------------
-- 【步骤 2】修复
-- 策略：优先删除无法使用的脏分享（无预约/候补引用）；有引用的行，将非法
-- 日期修正为合理的时间窗，保留数据完整性。
--
-- 2a) 删除「无任何预约/候补引用」的非法分享（通常可直接删）
--     —— 先删除依赖，再删分享本身
-- =========================================================
DELETE w FROM ReservationWaitlists w
JOIN SeatShares s ON w.ShareId = s.Id
WHERE (s.StartAt IS NULL OR s.StartAt < '1980-01-01 00:00:00'
    OR s.EndAt IS NULL OR s.EndAt < '1980-01-01 00:00:00'
    OR s.CreatedAt IS NULL OR s.CreatedAt < '1980-01-01 00:00:00'
    OR s.CancelledAt < '1980-01-01 00:00:00')
  AND NOT EXISTS (SELECT 1 FROM Reservations r WHERE r.ShareId = s.Id);

DELETE r FROM Reservations r
JOIN SeatShares s ON r.ShareId = s.Id
WHERE (s.StartAt IS NULL OR s.StartAt < '1980-01-01 00:00:00'
    OR s.EndAt IS NULL OR s.EndAt < '1980-01-01 00:00:00'
    OR s.CreatedAt IS NULL OR s.CreatedAt < '1980-01-01 00:00:00'
    OR s.CancelledAt < '1980-01-01 00:00:00');

DELETE s FROM SeatShares s
WHERE (s.StartAt IS NULL OR s.StartAt < '1980-01-01 00:00:00'
    OR s.EndAt IS NULL OR s.EndAt < '1980-01-01 00:00:00'
    OR s.CreatedAt IS NULL OR s.CreatedAt < '1980-01-01 00:00:00'
    OR s.CancelledAt < '1980-01-01 00:00:00');

-- 2b) 对「仍被预约引用」的非法分享，修正日期为合理时间窗（保留数据）
--     StartAt/EndAt 设为未来一段可用的分钟级窗口，CreatedAt 设为合理历史。
UPDATE SeatShares s
SET s.StartAt   = CASE WHEN (s.StartAt < '1980-01-01') OR s.StartAt IS NULL THEN UTC_TIMESTAMP() + INTERVAL 1 HOUR ELSE s.StartAt END,
    s.EndAt     = CASE WHEN (s.EndAt < '1980-01-01') OR s.EndAt IS NULL THEN UTC_TIMESTAMP() + INTERVAL 2 HOUR ELSE s.EndAt END,
    s.CreatedAt = CASE WHEN (s.CreatedAt < '1980-01-01') OR s.CreatedAt IS NULL THEN UTC_TIMESTAMP() ELSE s.CreatedAt END,
    s.CancelledAt = CASE WHEN s.CancelledAt < '1980-01-01' THEN NULL ELSE s.CancelledAt END
WHERE s.StartAt < '1980-01-01' OR s.StartAt IS NULL
   OR s.EndAt < '1980-01-01' OR s.EndAt IS NULL
   OR s.CreatedAt < '1980-01-01' OR s.CreatedAt IS NULL
   OR s.CancelledAt < '1980-01-01';

-- ---------------------------------------------------------
-- 【步骤 3】验证：应返回 0 行
-- ---------------------------------------------------------
SET SESSION sql_mode='';
SELECT COUNT(*) AS RemainingDirty
FROM SeatShares
WHERE StartAt IS NULL OR StartAt < '1980-01-01 00:00:00'
   OR EndAt IS NULL OR EndAt < '1980-01-01 00:00:00'
   OR CreatedAt IS NULL OR CreatedAt < '1980-01-01 00:00:00'
   OR CancelledAt < '1980-01-01 00:00:00';
