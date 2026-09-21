-- ============================================================
-- RezervBooking - Clean Sample Seed Data
-- Purpose: reproducible assessment/demo data only.
-- This does NOT include manual testing history.
-- ============================================================

SET FOREIGN_KEY_CHECKS = 0;

DELETE FROM WaitlistEntries;
DELETE FROM Bookings;
DELETE FROM CustomerPackages;
DELETE FROM TimetableSchedules;
DELETE FROM PackagePlans;
DELETE FROM Users;
DELETE FROM Businesses;

ALTER TABLE WaitlistEntries AUTO_INCREMENT = 1;
ALTER TABLE Bookings AUTO_INCREMENT = 1;
ALTER TABLE CustomerPackages AUTO_INCREMENT = 1;
ALTER TABLE TimetableSchedules AUTO_INCREMENT = 1;
ALTER TABLE PackagePlans AUTO_INCREMENT = 1;
ALTER TABLE Users AUTO_INCREMENT = 1;
ALTER TABLE Businesses AUTO_INCREMENT = 1;

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- Businesses
-- ============================================================
INSERT INTO Businesses (Id, Name) VALUES
(1, 'Yoga'),
(2, 'Fitness');

-- ============================================================
-- Users
-- ============================================================
INSERT INTO Users (Id, Name, Email) VALUES
(1,  'Test User 1',  'user1@rezerv.test'),
(2,  'Test User 2',  'user2@rezerv.test'),
(3,  'Test User 3',  'user3@rezerv.test'),
(4,  'Test User 4',  'user4@rezerv.test'),
(5,  'Test User 5',  'user5@rezerv.test'),
(6,  'Test User 6',  'user6@rezerv.test'),
(7,  'Test User 7',  'user7@rezerv.test'),
(8,  'Test User 8',  'user8@rezerv.test'),
(9,  'Test User 9',  'user9@rezerv.test'),
(10, 'Test User 10', 'user10@rezerv.test');

-- ============================================================
-- Package Plans
-- ============================================================
INSERT INTO PackagePlans
    (Id, BusinessId, Name, TotalCredits, ValidityDays, IsActive)
VALUES
(1, 2, 'Fitness 5 Credits', 5, 30, 1),
(2, 1, 'Yoga 10 Credits',  10, 30, 1);

-- ============================================================
-- Customer Packages
--
-- User 9  -> expired package
-- User 10 -> zero-credit package
-- RemainingCredits already reflects the pre-seeded bookings.
-- ============================================================
INSERT INTO CustomerPackages
    (Id, UserId, PackagePlanId, BusinessId, TotalCredits,
     RemainingCredits, ExpiryDateUtc, PurchasedAtUtc)
VALUES
(1,  1, 1, 2, 5, 4,
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 DAY),
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY)),
(2,  2, 1, 2, 5, 5,
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 DAY),
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY)),
(3,  3, 1, 2, 5, 5,
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 DAY),
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY)),
(4,  4, 1, 2, 5, 5,
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 DAY),
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY)),
(5,  5, 2, 1, 10, 9,
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 DAY),
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY)),
(6,  6, 2, 1, 10, 9,
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 DAY),
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY)),
(7,  7, 2, 1, 10, 10,
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 DAY),
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY)),
(8,  8, 2, 1, 10, 10,
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 DAY),
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY)),
(9,  9, 1, 2, 5, 2,
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY),
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 40 DAY)),
(10, 10, 1, 2, 5, 0,
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 DAY),
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY));

-- ============================================================
-- Timetable Schedules
--
-- Schedule 1 and 2 overlap.
-- Schedule 5 is intentionally full.
-- Schedule 8 is intentionally in the past.
-- ============================================================
INSERT INTO TimetableSchedules
    (Id, BusinessId, ClassName, InstructorName,
     StartTimeUtc, EndTimeUtc, Capacity)
VALUES
(1, 2, 'Fitness Fundamentals', 'John Doe',
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 24 HOUR),
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 25 HOUR), 10),

(2, 2, 'Strength Training', 'Emily Smith',
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 1470 MINUTE),
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 1530 MINUTE), 10),

(3, 1, 'Yoga Beginner', 'Anna Brown',
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 27 HOUR),
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 28 HOUR), 8),

(4, 2, 'Pilates', 'David Lee',
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 HOUR),
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 31 HOUR), 10),

(5, 1, 'Full Yoga Class', 'Sarah Kim',
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 32 HOUR),
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 33 HOUR), 2),

(6, 2, 'Morning Fitness', 'May Lin',
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 48 HOUR),
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 49 HOUR), 10),

(7, 2, 'Power Fitness', 'Chris Wong',
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 50 HOUR),
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 51 HOUR), 12),

(8, 2, 'Past Class', 'James Tan',
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 3 HOUR),
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 2 HOUR), 8),

(9, 1, 'Advanced Yoga', 'Susan Lim',
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 72 HOUR),
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 73 HOUR), 6),

(10, 1, 'Evening Yoga', 'Jane Doe',
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 74 HOUR),
    DATE_ADD(UTC_TIMESTAMP(), INTERVAL 75 HOUR), 6);

-- ============================================================
-- Existing Bookings
--
-- Booking 1: overlap and cancellation-refund scenario.
-- Bookings 2 and 3: make Schedule 5 full.
-- ============================================================
INSERT INTO Bookings
    (Id, UserId, ScheduleId, CustomerPackageId, Status,
     BookedAtUtc, CancelledAtUtc, CreditRefunded)
VALUES
(1, 1, 1, 1, 'Booked',
    UTC_TIMESTAMP(), NULL, 0),
(2, 5, 5, 5, 'Booked',
    UTC_TIMESTAMP(), NULL, 0),
(3, 6, 5, 6, 'Booked',
    UTC_TIMESTAMP(), NULL, 0);

-- ============================================================
-- Waitlist
--
-- Schedule 5 is full.
-- User 7 = FIFO position 1
-- User 8 = FIFO position 2
-- No credit is deducted while waiting.
-- ============================================================
INSERT INTO WaitlistEntries
    (Id, UserId, ScheduleId, CustomerPackageId,
     JoinedAtUtc, PromotedAtUtc, Status)
VALUES
(1, 7, 5, 7,
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 10 MINUTE),
    NULL, 'Waiting'),
(2, 8, 5, 8,
    DATE_SUB(UTC_TIMESTAMP(), INTERVAL 5 MINUTE),
    NULL, 'Waiting');

-- ============================================================
-- Quick Test Guide
-- ============================================================
--
-- Successful booking:
--   UserId = 2, ScheduleId = 4, CustomerPackageId = 2
--
-- Expired package:
--   UserId = 9, ScheduleId = 4, CustomerPackageId = 9
--   Expected: PACKAGE_EXPIRED
--
-- Zero credits:
--   UserId = 10, ScheduleId = 4, CustomerPackageId = 10
--   Expected: INSUFFICIENT_CREDITS
--
-- Wrong business:
--   UserId = 2, ScheduleId = 3, CustomerPackageId = 2
--   Schedule 3 = Yoga / Business 1
--   CustomerPackage 2 = Fitness / Business 2
--   Expected: PACKAGE_BUSINESS_MISMATCH
--
-- Overlapping booking:
--   UserId = 1, ScheduleId = 2, CustomerPackageId = 1
--   User 1 already has Schedule 1.
--   Expected: OVERLAPPING_BOOKING
--
-- Past schedule:
--   UserId = 2, ScheduleId = 8, CustomerPackageId = 2
--   Expected: SCHEDULE_ALREADY_STARTED
--
-- Full schedule:
--   ScheduleId = 5, Capacity = 2
--   Already booked by Users 5 and 6.
--
-- Waitlist FIFO:
--   ScheduleId = 5
--   User 7 = position 1
--   User 8 = position 2
--
-- Cancellation refund + promotion:
--   Cancel BookingId = 2 as UserId = 5
--   Expected:
--     CustomerPackage 5 gets +1 refund
--     User 7 is promoted first
--     CustomerPackage 7 gets -1 on promotion
-- ============================================================
