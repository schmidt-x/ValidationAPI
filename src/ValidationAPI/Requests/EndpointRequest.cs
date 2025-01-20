using System.Collections.Generic;
using ValidationAPI.Common.Models;

namespace ValidationAPI.Requests;

public record EndpointRequest(string Endpoint, Dictionary<string, PropertyRequest> Properties);