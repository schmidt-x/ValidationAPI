namespace ValidationAPI.Features.Infra;

/// <summary>
/// This class is intended to be derived from by other classes that implement specific request handling logic.
/// All the derived non-abstract classes will be registered in DI as scoped services.
/// </summary>
public abstract class RequestHandlerBase;