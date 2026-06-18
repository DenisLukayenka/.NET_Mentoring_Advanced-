namespace Scheduler.BusinessLogic.Abstractions;

public interface IHandler<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

public interface IHandler<in TRequest>
{
    Task HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
