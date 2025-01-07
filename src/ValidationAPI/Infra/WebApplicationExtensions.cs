using System;
using System.Linq;
using System.Reflection;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ValidationAPI.Infra;

public static class WebApplicationExtensions
{
	public static WebApplication MapEndpoints(this WebApplication app)
	{
		var assembly = Assembly.GetExecutingAssembly();
		
		var endpointGroupTypes = assembly
			.GetExportedTypes()
			.Where(type => type.IsSubclassOf(typeof(EndpointGroupBase)));
		
		foreach (var type in endpointGroupTypes)
		{
			if (Activator.CreateInstance(type) is EndpointGroupBase instance)
			{
				instance.Map(app);
			}
		}
		
		return app;
	}
	
	public static WebApplication RunMigrations(this WebApplication app)
	{
		using var scope = app.Services.CreateScope();
		var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
		runner.MigrateUp();
		return app;
	}
}