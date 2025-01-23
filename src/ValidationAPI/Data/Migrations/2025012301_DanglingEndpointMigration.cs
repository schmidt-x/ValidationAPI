using FluentMigrator;

namespace ValidationAPI.Data.Migrations;

[Migration(20250123_01)]
public class DanglingEndpointMigration : Migration
{
	public override void Up()
	{
		const string query = """
			CREATE OR REPLACE FUNCTION delete_dangling_endpoint()
			RETURNS TRIGGER AS
			$$
			BEGIN
				-- Check if any properties still reference the endpoint
				IF NOT EXISTS (SELECT 1 FROM properties WHERE endpoint_id = OLD.endpoint_id) 
				THEN
					-- Delete 'dangling' endpoint
					DELETE FROM endpoints WHERE id = OLD.endpoint_id;
				END IF;
				RETURN OLD;
			END;
			$$
			LANGUAGE plpgsql;
			
			CREATE TRIGGER delete_dangling_endpoint
			AFTER DELETE ON properties
			FOR EACH ROW
			EXECUTE FUNCTION delete_dangling_endpoint();
			""";
		
		Execute.Sql(query);
	}

	public override void Down()
	{
		const string query = """
			DROP TRIGGER delete_dangling_endpoint ON properties;
			DROP FUNCTION delete_dangling_endpoint();
			""";
		
		Execute.Sql(query);
	}
}