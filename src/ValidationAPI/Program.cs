using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
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

			const string documentName = "v1";
			builder.Services.AddOpenApi(documentName);
			
			builder.Services.AddConnectionStrings();
			
			var app = builder.Build();

			app.UseSerilogRequestLogging();
			
			app.UseHttpsRedirection();
			
			if (app.Environment.IsDevelopment())
			{
				app.MapOpenApi();
				app.UseSwaggerUI(o => o.SwaggerEndpoint($"/openapi/{documentName}.json", "ValidationAPI"));
			}
			
			app.MapEndpoints();

			app.Run();
		}
		catch (Exception ex)
		{
			Log.Fatal(ex, "Application terminated unexpectedly");
		}
		finally
		{
			Log.CloseAndFlush();
		}
	}
}
