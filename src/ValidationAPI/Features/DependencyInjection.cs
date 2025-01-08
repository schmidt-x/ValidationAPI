using Microsoft.Extensions.DependencyInjection;
using ValidationAPI.Features.Auth;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features;

public static class DependencyInjection
{
	public static IServiceCollection AddFeatures(this IServiceCollection services)
	{
		services.AddRequestHandlersFromExecutingAssembly();
		
		services.AddAuthServices();
		
		return services;
	}
}