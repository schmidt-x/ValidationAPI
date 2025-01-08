using System;
using System.Security;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ValidationAPI.Common.Services;

public class CurrentUser : IUser
{
	private readonly IHttpContextAccessor _ctxAccessor;

	public CurrentUser(IHttpContextAccessor ctxAccessor)
	{
		_ctxAccessor = ctxAccessor;
	}
	
	public Guid Id()
	{
		var user = _ctxAccessor.HttpContext!.User;
		ThrowIfNotAuthenticated(user);
		
		var rawId = user.FindFirstValue(ClaimTypes.NameIdentifier)
			?? throw new SecurityException($"Claim '{nameof(ClaimTypes.NameIdentifier)}' is not present.");
		
		return Guid.TryParse(rawId, out var id)
			? id
			: throw new SecurityException($"Claim '{nameof(ClaimTypes.NameIdentifier)}' is not valid Guid value.");
	}
	
	private static void ThrowIfNotAuthenticated(ClaimsPrincipal user)
	{
		if (user.Identity is not { IsAuthenticated: true })
		{
			throw new InvalidOperationException("User is not authenticated.");
		}
	}
}