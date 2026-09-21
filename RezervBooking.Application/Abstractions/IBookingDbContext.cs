using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RezervBooking.Domain.Entities;

namespace RezervBooking.Application.Abstractions
{
    public interface IBookingDbContext
    {
        DbSet<Business> Businesses { get; }

        DbSet<User> Users { get; }

        DbSet<PackagePlan> PackagePlans { get; }

        DbSet<CustomerPackage> CustomerPackages { get; }

        DbSet<TimetableSchedule> TimetableSchedules { get; }

        DbSet<Booking> Bookings { get; }

        DbSet<WaitlistEntry> WaitlistEntries { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }
}
