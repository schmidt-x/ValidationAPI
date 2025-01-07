using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ValidationAPI.Common.Options;

namespace ValidationAPI.Infra;

// ReSharper disable once InconsistentNaming
public static class IServiceCollectionExtensions
{
	public static IServiceCollection AddConnectionStrings(this IServiceCollection services)
	{
		services
			.AddOptions<ConnectionStringsOptions>()
			.BindConfiguration(ConnectionStringsOptions.Section)
			.Validate(opts => !string.IsNullOrEmpty(opts.Postgres), "Connection string is required.")
			.ValidateOnStart();
		
		return services;
	}
	
	public static IServiceCollection AddFluentMigrator(this IServiceCollection services)
	{
		return services
			.AddFluentMigratorCore()
			.ConfigureRunner(rb =>
			{
				rb.AddPostgres()
					.WithGlobalConnectionString(sp => sp.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.Postgres)
					.ScanIn(typeof(Program).Assembly).For.Migrations();
			})
			.AddLogging(lb => lb.AddFluentMigratorConsole());
	}
}
