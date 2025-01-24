using System.Collections.Generic;
using ValidationAPI.Common.Models;

namespace ValidationAPI.Requests;

public record EndpointRequest(string Endpoint, string? Description, Dictionary<string, PropertyRequest> Properties);