using FluentMigrator;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-10.2 — aggregated read model for per-user todo counts (consumed by Dapper).
/// Plan named this V006; that number was already used for processed_messages → V008.
/// Columns are quoted PascalCase to match FluentMigrator table definitions.
/// </summary>
[Migration(8, "V008_CreateTodoStatsView")]
public sealed class V008_CreateTodoStatsView : Migration
{
    public override void Up()
    {
        Execute.Sql(
            """
            CREATE OR REPLACE VIEW v_todo_stats_by_user AS
            SELECT
                "UserId",
                COUNT(*)::int AS "Total",
                COUNT(*) FILTER (WHERE NOT "Completed")::int AS "Active",
                COUNT(*) FILTER (WHERE "Completed")::int AS "Completed"
            FROM todos
            GROUP BY "UserId";
            """);

        // App role owns the schema in local Docker; GRANT documents the intent for split roles.
        Execute.Sql("""GRANT SELECT ON v_todo_stats_by_user TO CURRENT_USER;""");
    }

    public override void Down()
    {
        Execute.Sql("""DROP VIEW IF EXISTS v_todo_stats_by_user;""");
    }
}
