-- Top queries by total time (B-09.1). Requires pg_stat_statements.
--   docker compose exec -T postgres psql -U todo -d tododb < scripts/pg-stat-top.sql

SELECT
  ROUND(total_exec_time::numeric, 2) AS total_ms,
  calls,
  ROUND(mean_exec_time::numeric, 2) AS mean_ms,
  LEFT(query, 120) AS query
FROM pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 10;
