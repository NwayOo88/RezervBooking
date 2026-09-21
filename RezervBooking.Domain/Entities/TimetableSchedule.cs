namespace RezervBooking.Domain.Entities
{
    public class TimetableSchedule
    {
        public long Id { get; set; }

        public long BusinessId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string InstructorName { get; set; } = string.Empty;

        public DateTime StartTimeUtc { get; set; }

        public DateTime EndTimeUtc { get; set; }

        public int Capacity { get; set; } // for avaliable slots
    }
}
