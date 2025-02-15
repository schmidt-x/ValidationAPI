using System;
using System.Reflection;
using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ValidationAPI.Data;
using ValidationAPI.Data.TypeHandlers;
using ValidationAPI.Features;
using ValidationAPI.Infra;

namespace ValidationAPI;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		
		Log.Logger = new LoggerConfiguration()
			.ReadFrom.Configuration(builder.Configuration)
			.CreateLogger();
		
		try
		{
			builder.Services.AddSerilog(Log.Logger, true);

			builder.Services.AddNpgsql();
			DefaultTypeMap.MatchNamesWithUnderscores = true;
			
			builder.Services.AddScoped<IRepositoryContext, RepositoryContext>();
			SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
			
			builder.Services.AddFeatures();
			
			builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
			
			builder.Services.AddHttpContextAccessor();
			
			builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
			
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();
			
			builder.Services.AddConnectionStrings();
			builder.Services.AddAuthOptions();
			
			builder.Services.AddFluentMigrator();
			
			builder.Services.AddCookieAuthentication();
			builder.Services.AddAuthorization();

			builder.Services.ConfigureJsonOptions();
			
			var app = builder.Build();

			app.RunMigrations();
			
			app.UseSerilogRequestLogging();
			
			app.UseExceptionHandler(_ => {});
			
			if (!app.Environment.IsDevelopment())
			{
				app.UseHttpsRedirection();
			}
			
			app.UseSwagger();
			app.UseSwaggerUI();
			
			app.UseAuthentication();
			app.UseAuthorization();
			
			app.MapEndpoints();

			app.Run();
		}
		catch (Exception ex)
		{
			Log.Fatal(ex, "Application terminated unexpectedly.");
		}
		finally
		{
			Log.CloseAndFlush();
		}
	}
}
