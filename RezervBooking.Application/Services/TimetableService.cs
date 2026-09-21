using Microsoft.EntityFrameworkCore;
using RezervBooking.Application.Abstractions;
using RezervBooking.Domain.Enums;

namespace RezervBooking.Application.Services
{
    public class TimetableService
    {
        private readonly IBookingDbContext _db;

        public TimetableService(IBookingDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<TimetableDto>> GetAsync(long? businessId, DateOnly? date, CancellationToken cancellationToken)
        {
            var query = _db.TimetableSchedules.AsNoTracking().AsQueryable();

            if (businessId.HasValue)
            {
                query = query.Where(x => x.BusinessId == businessId.Value);
            }

            if (date.HasValue)
            {
                var from = date.Value.ToDateTime(TimeOnly.MinValue);

                var to = from.AddDays(1);

                query = query.Where(x => x.StartTimeUtc >= from && x.StartTimeUtc < to);
            }

            return await query
                .OrderBy(x => x.StartTimeUtc)
                .Select(x => new TimetableDto(
                    x.Id,
                    x.ClassName,
                    x.InstructorName,
                    x.StartTimeUtc,
                    x.EndTimeUtc,

                    _db.Bookings.Count(b =>
                        b.ScheduleId == x.Id &&
                        b.Status == BookingStatus.Booked),

                    x.Capacity,

                    x.Capacity -
                    _db.Bookings.Count(b =>
                        b.ScheduleId == x.Id &&
                        b.Status == BookingStatus.Booked),

                    x.BusinessId))
                .ToListAsync(cancellationToken);
        }
    }

    public record TimetableDto(
        long ScheduleId,
        string ClassName,
        string InstructorName,
        DateTime StartTime,
        DateTime EndTime,
        int AttendanceCount,
        int AvailableSlots,
        int RemainingSlots,
        long BusinessId);
}
