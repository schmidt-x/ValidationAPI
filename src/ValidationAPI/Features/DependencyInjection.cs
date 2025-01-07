using Microsoft.Extensions.DependencyInjection;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features;

public static class DependencyInjection
{
	public static IServiceCollection AddFeatures(this IServiceCollection services)
	{
		services.AddRequestHandlersFromExecutingAssembly();
		
		return services;
	}
}