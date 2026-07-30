-- B-10.3 — stats for one user from the aggregated view (parameter: @UserId).
SELECT "UserId",
       "Total",
       "Active",
       "Completed"
FROM v_todo_stats_by_user
WHERE "UserId" = @UserId;
