using Microsoft.AspNetCore.Builder;

namespace ValidationAPI.Infra;

public abstract class EndpointGroupBase
{
	public abstract void Map(WebApplication app);
}