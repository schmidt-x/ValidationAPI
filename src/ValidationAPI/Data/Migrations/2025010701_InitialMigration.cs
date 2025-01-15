using FluentMigrator;

namespace ValidationAPI.Data.Migrations;

[Migration(20250107_01)]
public class InitialMigration : Migration
{
	public override void Up()
	{
		const string sql = """
			CREATE TABLE IF NOT EXISTS users (
				id                  UUID PRIMARY KEY,
				email               TEXT NOT NULL,
				normalized_email    TEXT NOT NULL UNIQUE,
				username            TEXT NOT NULL,
				normalized_username TEXT NOT NULL UNIQUE,
				password_hash       TEXT NOT NULL,
				is_confirmed        BOOLEAN,
				created_at          TIMESTAMPTZ NOT NULL,
				modified_at         TIMESTAMPTZ NOT NULL
			)
			""";
		
		Execute.Sql(sql);
	}

	public override void Down()
	{
		const string sql = "DROP TABLE IF EXISTS users";
		Execute.Sql(sql);
	}
}