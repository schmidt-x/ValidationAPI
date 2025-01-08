using Microsoft.Extensions.DependencyInjection;
using ValidationAPI.Features.Auth.Services;

namespace ValidationAPI.Features.Auth;

public static class DependencyInjection
{
	public static IServiceCollection AddAuthServices(this IServiceCollection services)
	{
		services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
		services.AddSingleton<IAuthSchemeProvider, CookieAuthSchemeProvider>();
		
		return services;
	}
}