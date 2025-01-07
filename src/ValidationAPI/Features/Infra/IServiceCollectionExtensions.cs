using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ValidationAPI.Features.Infra;

// ReSharper disable once InconsistentNaming 
public static class IServiceCollectionExtensions
{
	public static IServiceCollection AddRequestHandlersFromExecutingAssembly(this IServiceCollection services)
	{
		var types = Assembly
			.GetExecutingAssembly()
			.GetExportedTypes()
			.Where(t => t.IsSubclassOf(typeof(RequestHandlerBase)) && t is { IsClass: true, IsAbstract: false });
		
		foreach (var type in types)
		{
			services.AddScoped(type);
		}
		
		return services;
	}
}