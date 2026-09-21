namespace RezervBooking.Application.Models
{
    public record PurchasePackageRequest(
        long UserId,
        long PackageId);
}