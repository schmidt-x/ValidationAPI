using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;

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
}