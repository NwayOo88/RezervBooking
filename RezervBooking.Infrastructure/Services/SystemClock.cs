using RezervBooking.Application.Abstractions;

namespace RezervBooking.Infrastructure.Services
{
    public class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
