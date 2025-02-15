using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using ValidationAPI.Common.Models;
using static ValidationAPI.Domain.Constants.ErrorCodes;

namespace ValidationAPI.Infra;

public class GlobalExceptionHandler : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception ex, CancellationToken ct)
	{
		var response = httpContext.Response;
			
		switch (ex)
		{
			case NotImplementedException:
				response.StatusCode = StatusCodes.Status501NotImplemented;
				await response.WriteAsJsonAsync(new ErrorDetail(NOT_IMPLEMENTED_FAILURE, "Feature is currently not implemented."), ct);
				break;
			
			default:
				response.StatusCode = StatusCodes.Status500InternalServerError;
				await response.WriteAsJsonAsync(new ErrorDetail(INTERNAL_SERVER_FAILURE, "Something went wrong."), ct);
				break;
		}
		
		return true;
	}
}