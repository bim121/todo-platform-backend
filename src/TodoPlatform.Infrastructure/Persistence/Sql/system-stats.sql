-- B-10.7 — tenant-agnostic admin aggregates (JOIN users ← todos).
SELECT
    COUNT(DISTINCT u."Id")::int AS "TotalUsers",
    COUNT(t."Id")::int AS "TotalTodos",
    COALESCE(
        ROUND(COUNT(t."Id")::numeric / NULLIF(COUNT(DISTINCT u."Id"), 0), 2),
        0
    ) AS "AvgTodosPerUser"
FROM users u
LEFT JOIN todos t ON t."UserId" = u."Id";
