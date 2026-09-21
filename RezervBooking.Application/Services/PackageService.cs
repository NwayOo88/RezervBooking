using Microsoft.EntityFrameworkCore;
using RezervBooking.Application.Abstractions;
using RezervBooking.Application.Common;
using RezervBooking.Application.Models;
using RezervBooking.Domain.Entities;

namespace RezervBooking.Application.Services
{
    public class PackageService
    {
        private readonly IBookingDbContext _db;
        private readonly ICacheService _cache;
        private readonly IClock _clock;

        public PackageService(IBookingDbContext db, ICacheService cache, IClock clock)
        {
            _db = db;
            _cache = cache;
            _clock = clock;
        }

        public async Task<IReadOnlyList<PackageDto>> GetPackagesAsync(long? packageId, CancellationToken cancellationToken)
        {
            var cacheKey = packageId.HasValue ? $"packages:id:{packageId.Value}" : "packages:all";

            var cached = await _cache.GetAsync<List<PackageDto>>(cacheKey, cancellationToken);

            if (cached != null)
                return cached;

            var query = _db.PackagePlans.AsNoTracking().Where(x => x.IsActive);

            if (packageId.HasValue)
            {
                query = query.Where(x => x.Id == packageId.Value);
            }

            var packages = await query
                .Select(x => new PackageDto(
                    x.Id,
                    x.BusinessId,
                    x.Name,
                    x.TotalCredits,
                    x.ValidityDays))
                .ToListAsync(cancellationToken);

            await _cache.SetAsync(
                cacheKey,
                packages,
                TimeSpan.FromMinutes(5),
                cancellationToken);

            return packages;
        }

        public async Task<CustomerPackageDto> PurchaseAsync(PurchasePackageRequest request, CancellationToken cancellationToken)
        {
            var userExists = await _db.Users.AnyAsync(x => x.Id == request.UserId, cancellationToken);

            if (!userExists)
                throw new BusinessRuleException("USER_NOT_FOUND", "User was not found.", 404);

            var package = await _db.PackagePlans.SingleOrDefaultAsync(x => x.Id == request.PackageId && x.IsActive, cancellationToken);

            if (package == null)
                throw new BusinessRuleException("PACKAGE_NOT_FOUND", "Package was not found.", 404);

            var purchased = new CustomerPackage
            {
                UserId = request.UserId,
                PackagePlanId = package.Id,
                BusinessId = package.BusinessId,
                TotalCredits = package.TotalCredits,
                RemainingCredits = package.TotalCredits,
                PurchasedAtUtc = _clock.UtcNow,
                ExpiryDateUtc = _clock.UtcNow.AddDays(package.ValidityDays)
            };

            _db.CustomerPackages.Add(purchased);

            await _db.SaveChangesAsync(cancellationToken);

            return new CustomerPackageDto(
                purchased.Id,
                purchased.UserId,
                purchased.BusinessId,
                purchased.TotalCredits,
                purchased.RemainingCredits,
                purchased.ExpiryDateUtc);
        }
    }

    public record PackageDto(
        long Id,
        long BusinessId,
        string Name,
        int TotalCredits,
        int ValidityDays);

    public record CustomerPackageDto(
        long Id,
        long UserId,
        long BusinessId,
        int TotalCredits,
        int RemainingCredits,
        DateTime ExpiryDateUtc);
}
