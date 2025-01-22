using ValidationAPI.Domain.Entities;

namespace ValidationAPI.Domain.Models;

public record EndpointResponse(string Name, PropertyResponse[] Properties);

public static class EndpointExtensions
{
	public static EndpointResponse ToResponse(this Endpoint endpoint, PropertyResponse[] properties)
		=> new(endpoint.Name, properties);
}
