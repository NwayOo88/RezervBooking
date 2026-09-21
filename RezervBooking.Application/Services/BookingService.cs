using Microsoft.EntityFrameworkCore;
using RezervBooking.Application.Abstractions;
using RezervBooking.Application.Common;
using RezervBooking.Application.Models;
using RezervBooking.Domain.Entities;
using RezervBooking.Domain.Enums;

namespace RezervBooking.Application.Services
{
    public class BookingService
    {
        private readonly IBookingDbContext _db;
        private readonly IDistributedLockService _locks;
        private readonly IClock _clock;

        public BookingService(
            IBookingDbContext db,
            IDistributedLockService locks,
            IClock clock)
        {
            _db = db;
            _locks = locks;
            _clock = clock;
        }

        public async Task<BookingResult> BookAsync(CreateBookingRequest request, CancellationToken cancellationToken)
        {
            await using var scheduleLock = await AcquireLockAsync($"booking:schedule:{request.ScheduleId}", cancellationToken);

            await using var userLock = await AcquireLockAsync($"booking:user:{request.UserId}", cancellationToken);

            await using var transaction = await _db.BeginTransactionAsync(cancellationToken);

            var schedule = await _db.TimetableSchedules.SingleOrDefaultAsync(x => x.Id == request.ScheduleId, cancellationToken);

            if (schedule == null)
                throw new BusinessRuleException("SCHEDULE_NOT_FOUND", "Schedule was not found.", 404);

            var package = await _db.CustomerPackages.SingleOrDefaultAsync(x => x.Id == request.CustomerPackageId, cancellationToken);

            if (package == null)
                throw new BusinessRuleException("PACKAGE_NOT_FOUND", "Customer package was not found.", 404);

            if (package.UserId != request.UserId)
                throw new BusinessRuleException("PACKAGE_NOT_OWNED", "The package does not belong to the user.");

            if (package.ExpiryDateUtc <= _clock.UtcNow)
                throw new BusinessRuleException("PACKAGE_EXPIRED", "The selected package has expired.");

            if (schedule.StartTimeUtc <= _clock.UtcNow)
                throw new BusinessRuleException("SCHEDULE_ALREADY_STARTED", "This timetable schedule has already started.", 400);

            if (package.RemainingCredits < 1)
                throw new BusinessRuleException("INSUFFICIENT_CREDITS", "The package has no available credits.");

            if (package.BusinessId != schedule.BusinessId)
                throw new BusinessRuleException("PACKAGE_BUSINESS_MISMATCH", "The package cannot be used for this business.");

            var alreadyBooked = await _db.Bookings.AnyAsync
                                (x => x.UserId == request.UserId &&
                                x.ScheduleId == request.ScheduleId &&
                                x.Status == BookingStatus.Booked, cancellationToken);

            if (alreadyBooked)
                throw new BusinessRuleException("ALREADY_BOOKED", "The user has already booked this class.");

            var overlappingBooking = await (
                    from existingBooking in _db.Bookings
                    join existingSchedule
                        in _db.TimetableSchedules
                        on existingBooking.ScheduleId
                        equals existingSchedule.Id
                    where
                        existingBooking.UserId == request.UserId &&
                        existingBooking.Status ==
                            BookingStatus.Booked &&
                        existingSchedule.StartTimeUtc
                            < schedule.EndTimeUtc &&
                        existingSchedule.EndTimeUtc
                            > schedule.StartTimeUtc
                    select existingBooking.Id
                ).AnyAsync(cancellationToken);

            if (overlappingBooking)
                throw new BusinessRuleException("OVERLAPPING_BOOKING", "The user already has another class during this time.");

            var bookingCount = await _db.Bookings.CountAsync(x => x.ScheduleId == request.ScheduleId && x.Status == BookingStatus.Booked, cancellationToken);

            if (bookingCount >= schedule.Capacity)
                throw new BusinessRuleException("SCHEDULE_FULL", "The class is full. You may join the waitlist.");

            package.RemainingCredits--;

            var booking = new Booking
            {
                UserId = request.UserId,
                ScheduleId = request.ScheduleId,
                CustomerPackageId = request.CustomerPackageId,
                Status = BookingStatus.Booked,
                BookedAtUtc = _clock.UtcNow
            };

            _db.Bookings.Add(booking);

            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new BookingResult(booking.Id, package.RemainingCredits);
        }

        private async Task<IAsyncDisposable> AcquireLockAsync(string key, CancellationToken cancellationToken)
        {
            var result = await _locks.TryAcquireAsync(key, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), cancellationToken);

            if (result == null)
                throw new BusinessRuleException("BOOKING_BUSY", "Another booking operation is currently in progress.");

            return result;
        }

        public async Task<CancelBookingResult> CancelAsync(CancelBookingRequest request, CancellationToken cancellationToken)
        {
            var bookingInfo = await _db.Bookings.AsNoTracking().Where(x => x.Id == request.BookingId)
                            .Select(x => new
                            {
                                x.UserId,
                                x.ScheduleId
                            })
                            .SingleOrDefaultAsync(cancellationToken);

            if (bookingInfo == null)
                throw new BusinessRuleException("BOOKING_NOT_FOUND", "Booking was not found.", 404);

            if (bookingInfo.UserId != request.UserId)
                throw new BusinessRuleException("BOOKING_NOT_OWNED", "This booking does not belong to the user.");

            await using var scheduleLock = await AcquireLockAsync($"booking:schedule:{bookingInfo.ScheduleId}", cancellationToken);

            await using var userLock = await AcquireLockAsync($"booking:user:{request.UserId}", cancellationToken);

            await using var transaction = await _db.BeginTransactionAsync(cancellationToken);

            var booking = await _db.Bookings.SingleAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking.Status == BookingStatus.Cancelled)
                throw new BusinessRuleException("BOOKING_ALREADY_CANCELLED", "Booking has already been cancelled.");

            var schedule = await _db.TimetableSchedules.SingleAsync(x => x.Id == booking.ScheduleId, cancellationToken);

            var package = await _db.CustomerPackages.SingleAsync(x => x.Id == booking.CustomerPackageId && x.UserId == booking.UserId, cancellationToken);

            if (package == null)
                throw new BusinessRuleException("PACKAGE_NOT_OWNED", "The booking package does not belong to the booking user.", 400);

            var timeUntilClass = schedule.StartTimeUtc - _clock.UtcNow;

            var refund = timeUntilClass > TimeSpan.FromHours(4);

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAtUtc = _clock.UtcNow;

            if (refund)
            {
                package.RemainingCredits += 1;
                booking.CreditRefunded = true;
            }
            else
            {
                booking.CreditRefunded = false;
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAtUtc = _clock.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            var promotedUserId = await PromoteNextWaitlistAsync(schedule, request.UserId, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new CancelBookingResult(booking.Id, refund, promotedUserId);
        }

        private async Task<long?> PromoteNextWaitlistAsync(TimetableSchedule schedule, long currentlyLockedUserId, CancellationToken cancellationToken)
        {
            if (schedule.EndTimeUtc <= _clock.UtcNow)
            {
                var expiredEntries = await _db.WaitlistEntries.Where(x => x.ScheduleId == schedule.Id && x.Status == WaitlistStatus.Waiting).ToListAsync(cancellationToken);

                foreach (var item in expiredEntries)
                    item.Status = WaitlistStatus.Expired;

                return null;
            }

            var waitingEntries = await _db.WaitlistEntries.Where(x => x.ScheduleId == schedule.Id && x.Status == WaitlistStatus.Waiting)
                                .OrderBy(x => x.JoinedAtUtc)
                                .ThenBy(x => x.Id)
                                .ToListAsync(cancellationToken);

            foreach (var entry in waitingEntries)
            {
                IAsyncDisposable? candidateLock = null;

                if (entry.UserId != currentlyLockedUserId)
                {
                    candidateLock = await AcquireLockAsync($"booking:user:{entry.UserId}", cancellationToken);
                }

                try
                {
                    var package = await _db.CustomerPackages.SingleOrDefaultAsync(x =>
                                    x.Id == entry.CustomerPackageId &&
                                    x.UserId == entry.UserId, cancellationToken);

                    if (package == null || package.BusinessId != schedule.BusinessId || package.ExpiryDateUtc <= _clock.UtcNow || package.RemainingCredits < 1)
                    {
                        entry.Status = WaitlistStatus.Skipped;
                        continue;
                    }

                    var overlap = await (
                            from booking in _db.Bookings
                            join otherSchedule
                                in _db.TimetableSchedules
                                on booking.ScheduleId
                                equals otherSchedule.Id
                            where
                                booking.UserId ==
                                    entry.UserId &&
                                booking.Status ==
                                    BookingStatus.Booked &&
                                otherSchedule.StartTimeUtc
                                    < schedule.EndTimeUtc &&
                                otherSchedule.EndTimeUtc
                                    > schedule.StartTimeUtc
                            select booking.Id
                        ).AnyAsync(cancellationToken);

                    if (overlap)
                    {
                        entry.Status = WaitlistStatus.Skipped;
                        continue;
                    }

                    package.RemainingCredits--;

                    entry.Status = WaitlistStatus.Promoted;

                    entry.PromotedAtUtc = _clock.UtcNow;

                    _db.Bookings.Add(new Booking
                    {
                        UserId = entry.UserId,
                        ScheduleId = schedule.Id,
                        CustomerPackageId = package.Id,
                        Status = BookingStatus.Booked,
                        BookedAtUtc = _clock.UtcNow
                    });

                    return entry.UserId;
                }
                finally
                {
                    if (candidateLock != null)
                        await candidateLock.DisposeAsync();
                }
            }

            return null;
        }
    }
}
