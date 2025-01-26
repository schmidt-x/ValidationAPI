using System.Collections.Generic;
using ValidationAPI.Common.Models;

namespace ValidationAPI.Requests;

public record CreateEndpointRequest(string Endpoint, string? Description, Dictionary<string, PropertyRequest> Properties);