using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RezervBooking.Application.Abstractions;
using RezervBooking.Domain.Entities;
using RezervBooking.Domain.Enums;

namespace RezervBooking.Infrastructure.Persistence;

public class BookingDbContext : DbContext, IBookingDbContext
{
    public BookingDbContext(
        DbContextOptions<BookingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Business> Businesses => Set<Business>();

    public DbSet<User> Users => Set<User>();

    public DbSet<PackagePlan> PackagePlans => Set<PackagePlan>();

    public DbSet<CustomerPackage> CustomerPackages => Set<CustomerPackage>();

    public DbSet<TimetableSchedule> TimetableSchedules => Set<TimetableSchedule>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(x => x.Email)
                .IsUnique();
        });

        modelBuilder.Entity<PackagePlan>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.BusinessId);

            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasOne<Business>()
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerPackage>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.UserId);

            entity.HasIndex(x => new
            {
                x.UserId,
                x.BusinessId
            });

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PackagePlan>()
                .WithMany()
                .HasForeignKey(x => x.PackagePlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TimetableSchedule>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.BusinessId,
                x.StartTimeUtc
            });

            entity.Property(x => x.ClassName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.InstructorName)
                .HasMaxLength(150)
                .IsRequired();
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasIndex(x => new
            {
                x.ScheduleId,
                x.Status
            });

            entity.HasIndex(x => new
            {
                x.UserId,
                x.Status
            });

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<TimetableSchedule>()
                .WithMany()
                .HasForeignKey(x => x.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<CustomerPackage>()
                .WithMany()
                .HasForeignKey(x => x.CustomerPackageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WaitlistEntry>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasIndex(x => new
            {
                x.ScheduleId,
                x.Status,
                x.JoinedAtUtc
            });
        });
    }
}