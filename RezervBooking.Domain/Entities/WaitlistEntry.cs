using RezervBooking.Domain.Enums;

namespace RezervBooking.Domain.Entities
{
    public class WaitlistEntry
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public long ScheduleId { get; set; }

        public long CustomerPackageId { get; set; } //user gets promoted, package loose the credit

        public DateTime JoinedAtUtc { get; set; }

        public DateTime? PromotedAtUtc { get; set; }

        public WaitlistStatus Status { get; set; }
    }
}
