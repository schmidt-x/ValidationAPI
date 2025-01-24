using System;
using ValidationAPI.Domain.Entities;

namespace ValidationAPI.Domain.Models;

public record EndpointExpandedResponse(
	string Name, string Description, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt, PropertyResponse[] Properties);

public static class EndpointExtensions
{
	public static EndpointExpandedResponse ToResponse(this Endpoint endpoint, PropertyResponse[] properties)
		=> new(endpoint.Name, endpoint.Description ?? string.Empty, endpoint.CreatedAt, endpoint.ModifiedAt, properties);
}
