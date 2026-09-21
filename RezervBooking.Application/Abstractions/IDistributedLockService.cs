namespace RezervBooking.Application.Abstractions
{
    public interface IDistributedLockService
    {
        Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan waitTime, TimeSpan expiry, CancellationToken cancellationToken = default);
    }
}
