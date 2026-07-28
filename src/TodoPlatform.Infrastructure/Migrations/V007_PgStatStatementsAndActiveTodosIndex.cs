using FluentMigrator;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-09: pg_stat_statements extension + partial index for active todos (GetTodos ActiveOnly).
/// <para>
/// Base indexes already exist: <c>IX_todos_UserId</c> (V002), <c>IX_todos_UserId_Completed</c> (V003).
/// Prod tip: create large indexes with <c>CREATE INDEX CONCURRENTLY</c> outside a transaction.
/// </para>
/// </summary>
[Migration(7, "V007_PgStatStatementsAndActiveTodosIndex")]
public sealed class V007_PgStatStatementsAndActiveTodosIndex : Migration
{
    public override void Up()
    {
        // Requires shared_preload_libraries = pg_stat_statements (infra/postgres/postgresql.conf).
        Execute.Sql("""CREATE EXTENSION IF NOT EXISTS pg_stat_statements;""");

        // Partial index: hot path GET /api/todos?activeOnly=true → WHERE "UserId" = $1 AND "Completed" = false
        Execute.Sql(
            """
            CREATE INDEX IF NOT EXISTS "IX_todos_UserId_Active"
            ON todos ("UserId")
            WHERE "Completed" = false;
            """);
    }

    public override void Down()
    {
        Execute.Sql("""DROP INDEX IF EXISTS "IX_todos_UserId_Active";""");
        Execute.Sql("""DROP EXTENSION IF EXISTS pg_stat_statements;""");
    }
}
