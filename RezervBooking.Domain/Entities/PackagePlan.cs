namespace RezervBooking.Domain.Entities
{
    public class PackagePlan
    {
        public long Id { get; set; }

        public long BusinessId { get; set; }

        public string Name { get; set; }

        public int TotalCredits { get; set; }

        public int ValidityDays { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
