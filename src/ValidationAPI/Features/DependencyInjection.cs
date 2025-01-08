using Microsoft.Extensions.DependencyInjection;
using ValidationAPI.Common.Services;
using ValidationAPI.Features.Auth;
using ValidationAPI.Features.Infra;

namespace ValidationAPI.Features;

public static class DependencyInjection
{
	public static IServiceCollection AddFeatures(this IServiceCollection services)
	{
		services.AddRequestHandlersFromExecutingAssembly();
		
		services.AddAuthServices();
		
		// common services
		services.AddSingleton<IUser, CurrentUser>();
		
		return services;
	}
}