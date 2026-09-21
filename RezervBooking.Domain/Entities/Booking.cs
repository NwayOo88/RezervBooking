using RezervBooking.Domain.Enums;

namespace RezervBooking.Domain.Entities
{
    public class Booking
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public long ScheduleId { get; set; }

        public long CustomerPackageId { get; set; }

        public BookingStatus Status { get; set; }

        public DateTime BookedAtUtc { get; set; }

        public DateTime? CancelledAtUtc { get; set; }

        public bool CreditRefunded { get; set; }
    }
}
