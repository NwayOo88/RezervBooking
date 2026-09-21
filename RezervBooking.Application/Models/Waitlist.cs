namespace RezervBooking.Application.Models
{
    public record JoinWaitlistRequest(
    long UserId,
    long ScheduleId,
    long CustomerPackageId);

    public record WaitlistResult(
        long WaitlistId,
        int Position);
}