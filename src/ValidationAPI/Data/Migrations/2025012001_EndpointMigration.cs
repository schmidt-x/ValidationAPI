﻿using FluentMigrator;

namespace ValidationAPI.Data.Migrations;

[Migration(20250120_01)]
public class EndpointMigration : Migration
{
	public override void Up()
	{
		const string sql = """
			CREATE TYPE propertytype AS ENUM ('Int', 'Float', 'String', 'DateTime', 'DateOnly', 'TimeOnly');

			CREATE TYPE ruletype AS ENUM (
				'Less', 'More', 'LessOrEqual', 'MoreOrEqual', 'Equal', 'NotEqual', 'Between', 'Outside', 'Regex', 'Email');

			CREATE TYPE rulevaluetype AS ENUM ('Int', 'Float', 'String', 'Range');

			CREATE TABLE endpoints (
				id              SERIAL      PRIMARY KEY,
				name            TEXT        NOT NULL,
				normalized_name TEXT        NOT NULL,
				description     TEXT,
				created_at      TIMESTAMPTZ NOT NULL,
				modified_at     TIMESTAMPTZ NOT NULL,
				user_id         UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,

				UNIQUE (normalized_name, user_id)
			);

			CREATE TABLE properties (
				id          SERIAL       PRIMARY KEY,
				name        TEXT         NOT NULL,
				type        PROPERTYTYPE NOT NULL,
				is_optional BOOL         NOT NULL,
				created_at  TIMESTAMPTZ  NOT NULL,
				modified_at TIMESTAMPTZ  NOT NULL,
				endpoint_id SERIAL       REFERENCES endpoints(id) ON DELETE CASCADE,

				UNIQUE (name, endpoint_id)
			);

			CREATE TABLE rules (
				id              SERIAL   PRIMARY KEY,
				name            TEXT     NOT NULL,
				normalized_name TEXT     NOT NULL,
				type            RULETYPE NOT NULL,
				value           TEXT     NOT NULL,
				value_type      RULEVALUETYPE NOT NULL,
				raw_value       TEXT,
				extra_info      TEXT,
				is_relative     BOOL NOT NULL,
				error_message   TEXT,
				property_id     SERIAL REFERENCES properties(id) ON DELETE CASCADE,
				endpoint_id     SERIAL REFERENCES endpoints(id)  ON DELETE CASCADE,

				UNIQUE (normalized_name, endpoint_id)
			);
			""";
		
		Execute.Sql(sql);
	}

	public override void Down()
	{
		const string sql = """
			DROP TABLE rules, properties, endpoints;
			DROP TYPE propertytype, ruletype, rulevaluetype;
			""";
		
		Execute.Sql(sql);
	}
}