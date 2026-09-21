namespace RezervBooking.Application.Models
{
    public record CancelBookingRequest(
        long UserId,
        long BookingId);

    public record CancelBookingResult(
        long BookingId,
        bool CreditRefunded,
        long? PromotedUserId);
}