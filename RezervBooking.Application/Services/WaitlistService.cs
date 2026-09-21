using Microsoft.EntityFrameworkCore;
using RezervBooking.Application.Abstractions;
using RezervBooking.Application.Common;
using RezervBooking.Application.Models;
using RezervBooking.Domain.Entities;
using RezervBooking.Domain.Enums;

namespace RezervBooking.Application.Services
{
    public class WaitlistService
    {
        private readonly IBookingDbContext _db;
        private readonly IDistributedLockService _locks;
        private readonly IClock _clock;

        public WaitlistService(
            IBookingDbContext db,
            IDistributedLockService locks,
            IClock clock)
        {
            _db = db;
            _locks = locks;
            _clock = clock;
        }

        public async Task<WaitlistResult> JoinAsync(JoinWaitlistRequest request, CancellationToken cancellationToken)
        {
            await using var scheduleLock = await AcquireAsync($"booking:schedule:{request.ScheduleId}", cancellationToken);

            await using var userLock = await AcquireAsync($"booking:user:{request.UserId}", cancellationToken);

            await using var transaction = await _db.BeginTransactionAsync(cancellationToken);

            var schedule = await _db.TimetableSchedules.SingleOrDefaultAsync(x => x.Id == request.ScheduleId, cancellationToken);

            if (schedule == null)
                throw new BusinessRuleException("SCHEDULE_NOT_FOUND", "Schedule was not found.", 404);

            var package = await _db.CustomerPackages.SingleOrDefaultAsync(x => x.Id == request.CustomerPackageId && x.UserId == request.UserId, cancellationToken);

            if (package == null)
                throw new BusinessRuleException("PACKAGE_NOT_FOUND", "Package was not found.", 404);

            if (package.BusinessId != schedule.BusinessId)
                throw new BusinessRuleException("PACKAGE_BUSINESS_MISMATCH", "The package cannot be used for this business.");

            if (package.ExpiryDateUtc <= _clock.UtcNow)
                throw new BusinessRuleException("PACKAGE_EXPIRED", "The selected package has expired.");

            if (schedule.StartTimeUtc <= _clock.UtcNow)
                throw new BusinessRuleException("SCHEDULE_ALREADY_STARTED", "This timetable schedule has already started.", 400);

            if (package.RemainingCredits < 1)
                throw new BusinessRuleException("INSUFFICIENT_CREDITS", "The package has no available credits.");

            var booked = await _db.Bookings.AnyAsync(x =>
                        x.UserId == request.UserId &&
                        x.ScheduleId == request.ScheduleId &&
                        x.Status == BookingStatus.Booked, cancellationToken);

            if (booked)
                throw new BusinessRuleException("ALREADY_BOOKED", "The user is already booked.");

            var alreadyWaiting = await _db.WaitlistEntries.AnyAsync(x =>
                                x.UserId == request.UserId &&
                                x.ScheduleId == request.ScheduleId &&
                                x.Status == WaitlistStatus.Waiting, cancellationToken);

            if (alreadyWaiting)
                throw new BusinessRuleException("ALREADY_WAITLISTED", "The user is already on the waitlist.");

            var bookingCount = await _db.Bookings.CountAsync(x =>
                                x.ScheduleId == request.ScheduleId &&
                                x.Status == BookingStatus.Booked, cancellationToken);

            if (bookingCount < schedule.Capacity)
                throw new BusinessRuleException("SLOTS_AVAILABLE", "Slots are available. Book the class instead.");

            var position = await _db.WaitlistEntries.CountAsync(x =>
                            x.ScheduleId == request.ScheduleId &&
                            x.Status == WaitlistStatus.Waiting, cancellationToken) + 1;

            var entry = new WaitlistEntry
            {
                UserId = request.UserId,
                ScheduleId = request.ScheduleId,
                CustomerPackageId = request.CustomerPackageId,
                JoinedAtUtc = _clock.UtcNow,
                Status = WaitlistStatus.Waiting
            };

            _db.WaitlistEntries.Add(entry);

            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new WaitlistResult(entry.Id, position);
        }

        private async Task<IAsyncDisposable> AcquireAsync(string key, CancellationToken cancellationToken)
        {
            return await _locks.TryAcquireAsync(
                       key,
                       TimeSpan.FromSeconds(5),
                       TimeSpan.FromSeconds(30),
                       cancellationToken)
                   ?? throw new BusinessRuleException("BOOKING_BUSY", "Another booking operation is currently in progress.");
        }
    }

}
