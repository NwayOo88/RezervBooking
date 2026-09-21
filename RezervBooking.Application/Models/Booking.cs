namespace RezervBooking.Application.Models
{
    public record CreateBookingRequest(
    long UserId,
    long ScheduleId,
    long CustomerPackageId);

    public record BookingResult(
        long BookingId,
        int RemainingCredits);
}

