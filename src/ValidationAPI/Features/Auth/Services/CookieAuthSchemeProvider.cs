using Microsoft.AspNetCore.Authentication.Cookies;

namespace ValidationAPI.Features.Auth.Services;

public class CookieAuthSchemeProvider : IAuthSchemeProvider
{
	public string Scheme => CookieAuthenticationDefaults.AuthenticationScheme;
}