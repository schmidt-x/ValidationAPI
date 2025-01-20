using System.Text.Json.Serialization;
using FluentMigrator.Runner;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using ValidationAPI.Common.Options;
using ValidationAPI.Domain.Enums;

namespace ValidationAPI.Infra;

// ReSharper disable once InconsistentNaming
public static class IServiceCollectionExtensions
{
	public static IServiceCollection AddConnectionStrings(this IServiceCollection services)
	{
		services
			.AddOptions<ConnectionStringsOptions>()
			.BindConfiguration(ConnectionStringsOptions.Section)
			.Validate(opts => !string.IsNullOrEmpty(opts.Postgres), "Connection string is required.")
			.ValidateOnStart();
		
		return services;
	}
	
	public static IServiceCollection AddAuthOptions(this IServiceCollection services)
	{
		services.AddOptions<AuthOptions>().BindConfiguration(AuthOptions.Section);
		return services;
	}
	
	public static IServiceCollection AddFluentMigrator(this IServiceCollection services)
	{
		return services
			.AddFluentMigratorCore()
			.ConfigureRunner(rb =>
			{
				rb.AddPostgres()
					.WithGlobalConnectionString(sp => sp.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.Postgres)
					.ScanIn(typeof(Program).Assembly).For.Migrations();
			})
			.AddLogging(lb => lb.AddFluentMigratorConsole());
	}
	
	public static IServiceCollection AddCookieAuthentication(this IServiceCollection services)
	{
		services
			.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
			.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opts =>
			{
				opts.Events.OnRedirectToLogin = rc =>
				{
					rc.Response.StatusCode = StatusCodes.Status401Unauthorized;
					return Task.CompletedTask;
				};
				
				opts.Events.OnRedirectToAccessDenied = rc =>
				{
					rc.Response.StatusCode = StatusCodes.Status403Forbidden;
					return Task.CompletedTask;
				};
			});
		
		return services;
	}
	
	public static IServiceCollection AddNpgsql(this IServiceCollection services)
	{
		return services.AddSingleton<NpgsqlDataSource>(sp =>
			 NpgsqlDataSource.Create(sp.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.Postgres));
	}
	
	public static IServiceCollection ConfigureJsonOptions(this IServiceCollection services)
	{
		return services
			.ConfigureHttpJsonOptions(opts =>
			{
				opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter<PropertyType>());
				opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter<RuleType>());
			})
			.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(opts =>
			{
				opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<PropertyType>());
				opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<RuleType>());
			});
	}
}
