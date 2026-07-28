-- B-09.2 — seed ~10k todos for EXPLAIN / load checks.
-- Usage:
--   docker compose exec -T postgres psql -U todo -d tododb < scripts/seed-load-test.sql
--
-- Idempotent for the load-test user; re-run deletes previous load-test rows for that user.

DO $$
DECLARE
  v_user_id uuid;
  v_existing int;
BEGIN
  SELECT "Id" INTO v_user_id FROM users WHERE "Email" = 'loadtest@example.com';

  IF v_user_id IS NULL THEN
    v_user_id := 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';
    INSERT INTO users ("Id", "Email", "PasswordHash", "Name")
    VALUES (
      v_user_id,
      'loadtest@example.com',
      -- placeholder hash (not used for Keycloak login)
      'load-test-hash',
      'Load Test User'
    );
  END IF;

  SELECT COUNT(*) INTO v_existing FROM todos WHERE "UserId" = v_user_id;
  IF v_existing >= 10000 THEN
    RAISE NOTICE 'Load-test user already has % todos — skip.', v_existing;
    RETURN;
  END IF;

  DELETE FROM todos WHERE "UserId" = v_user_id;

  INSERT INTO todos ("Id", "Title", "Completed", "UserId", "Status", "Priority")
  SELECT
    gen_random_uuid(),
    'Load todo #' || g,
    (g % 5 = 0), -- ~20% completed
    v_user_id,
    CASE
      WHEN g % 5 = 0 THEN 'Done'
      WHEN g % 3 = 0 THEN 'InProgress'
      ELSE 'Todo'
    END,
    CASE
      WHEN g % 7 = 0 THEN 'High'
      WHEN g % 4 = 0 THEN 'Low'
      ELSE 'Medium'
    END
  FROM generate_series(1, 10000) AS g;

  RAISE NOTICE 'Inserted 10000 todos for user % (loadtest@example.com)', v_user_id;
END $$;

-- Quick sanity
SELECT
  "UserId",
  COUNT(*) AS total,
  COUNT(*) FILTER (WHERE "Completed" = false) AS active
FROM todos
WHERE "UserId" = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'
GROUP BY "UserId";
