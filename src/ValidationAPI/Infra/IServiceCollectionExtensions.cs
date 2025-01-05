using Microsoft.Extensions.DependencyInjection;
using ValidationAPI.Options;

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
}
