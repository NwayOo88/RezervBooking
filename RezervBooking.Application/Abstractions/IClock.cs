namespace RezervBooking.Application.Abstractions
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
