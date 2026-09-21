# RezervBooking

A simplified studio booking engine built for the Rezerv Backend Engineering Assessment.

The solution demonstrates package purchase, timetable browsing, class booking, cancellation/refund rules, FIFO waitlists, Redis caching, and Redis-based concurrency handling.

## Tech Stack

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- MySQL
- Redis
- Swagger / OpenAPI
- Clean Architecture style project separation

## Solution Structure

```text
RezervBooking/
│
├── RezervBooking.sln
├── README.md
├── .gitignore
│
├── database/
│   └── seed.sql
│
├── docs/
│   └── ERD.md
│
├── RezervBooking.Api/
├── RezervBooking.Application/
├── RezervBooking.Domain/
└── RezervBooking.Infrastructure/
```

## Architecture

### RezervBooking.Api

Responsible for the HTTP/API layer.

Main responsibilities:

- Controllers
- Swagger configuration
- API exception middleware
- Dependency injection
- Application startup

### RezervBooking.Application

Contains the use-case and business logic.

Main services:

- `PackageService`
- `TimetableService`
- `BookingService`
- `WaitlistService`

It also contains:

- Request/response models
- Database abstraction
- Cache abstraction
- Distributed lock abstraction
- Clock abstraction
- Business-rule exceptions

### RezervBooking.Domain

Contains the core domain entities and enums:

- `Business`
- `User`
- `PackagePlan`
- `CustomerPackage`
- `TimetableSchedule`
- `Booking`
- `WaitlistEntry`
- `BookingStatus`
- `WaitlistStatus`

### RezervBooking.Infrastructure

Contains implementations for external infrastructure:

- EF Core / MySQL
- Database migrations
- Redis caching
- Redis distributed locks
- System clock

## Business Rules

### Packages

- Each package belongs to a business.
- A customer package tracks total credits, remaining credits, purchase time, and expiry time.
- Expired packages cannot be used for booking.
- A customer must have at least 1 remaining credit to book.
- A package can only be used for schedules under the same business.
- A successful booking deducts 1 credit immediately.

### Booking

Before creating a booking, the service validates:

- Schedule exists.
- Schedule has not already started.
- Customer package exists.
- Customer package belongs to the requesting user.
- Customer package is not expired.
- Customer has sufficient remaining credits.
- Package business matches schedule business.
- User has not already booked the same schedule.
- User does not have another overlapping active booking.
- Schedule capacity has not been reached.

If all validations pass:

1. 1 credit is deducted from the selected customer package.
2. A booking is created.
3. The changes are saved in a database transaction.

### Cancellation

- If cancelled more than 4 hours before class start, 1 credit is refunded.
- If cancelled within 4 hours of class start, no credit is refunded.
- Exactly 4 hours before class start is treated as no refund.
- Refund is returned to the same `CustomerPackage` used by the booking.
- If a cancellation creates an available slot, the first valid waitlisted user can be promoted.

### Waitlist

- Users can join the waitlist when a schedule is full.
- Waitlist order is FIFO.
- Joining the waitlist does not deduct a credit.
- 1 credit is deducted only when a waitlisted user is successfully promoted to booked.
- Package validity, available credit, business match, and overlap rules are checked again during promotion.
- Invalid waitlist entries can be skipped so the next eligible user can be considered.
- If a class has ended, remaining waiting entries can be treated as expired without deducting credit.

## Concurrency Strategy

Redis distributed locks are used to protect booking operations from concurrent requests.

Typical lock keys:

```text
booking:schedule:{scheduleId}
booking:user:{userId}
```

Locks are acquired before the final capacity and credit checks.

Example:

```text
Schedule capacity = 5
Current bookings = 4

User A and User B both try to book the final slot.

Without locking:
- both requests could see 4 / 5
- both could create a booking
- final result could become 6 / 5

With Redis locking:
- User A acquires the schedule lock
- User A books the final slot
- User A releases the lock
- User B checks again and sees the schedule is full
- User B is rejected
```

MySQL remains the source of truth. Redis is used for distributed locking and caching.

## Redis Caching

Package catalogue data is cached in Redis to reduce repeated reads from MySQL.

Redis is not the authoritative data store. Persistent business data remains in MySQL.

## API Endpoints

### 1. Get Packages

```http
GET /api/packages
```

If the current implementation uses the optional package filter:

```http
GET /api/packages?packageId=1
```

### 2. Purchase Package

```http
POST /api/packages/purchase
```

Example:

```json
{
  "userId": 2,
  "packageId": 1
}
```

`packageId = 1` represents the seeded **Fitness 5 Credits** package.

Purchasing a package creates a new row in `CustomerPackages`.

### 3. Get Timetable

```http
GET /api/timetable
```

Supported filters:

```http
GET /api/timetable?businessId=1
GET /api/timetable?date=2026-09-22
```

### 4. Book Class

```http
POST /api/bookings
```

Example successful request using the seeded data:

```json
{
  "userId": 2,
  "scheduleId": 4,
  "customerPackageId": 2
}
```

In the seed data:

```text
CustomerPackageId 2
UserId             2
BusinessId         2 (Fitness)
RemainingCredits   5

ScheduleId         4
BusinessId         2 (Fitness)
Class              Pilates
```

The request should succeed if no other test data has changed the database.

### 5. Cancel Booking

```http
POST /api/bookings/cancel
```

Seeded cancellation/refund example:

```json
{
  "userId": 5,
  "bookingId": 2
}
```

Booking 2 belongs to User 5 and uses CustomerPackage 5.

The seeded class starts more than 4 hours in the future, so the expected behavior is:

- Booking becomes `Cancelled`.
- CustomerPackage 5 receives a 1-credit refund.
- The first eligible waitlisted user for that schedule can be promoted.

### 6. Join Waitlist

```http
POST /api/waitlist
```

The seed already contains a FIFO waitlist scenario for Schedule 5.

```text
Schedule 5
Capacity = 2

Booked:
- User 5
- User 6

Waiting:
1. User 7
2. User 8
```

No credit is deducted from Users 7 or 8 while they are only waiting.

## Seed Data

Sample data is provided in:

```text
database/seed.sql
```

The seed data contains:

- 10 users
- 2 businesses
- 2 package plans
- valid customer packages
- 1 expired customer package
- 1 zero-credit customer package
- 10 timetable schedules
- 1 past schedule
- overlapping schedules
- 1 intentionally full schedule
- existing bookings
- FIFO waitlist entries

### Seeded Businesses

```text
Id  Name
1   Yoga
2   Fitness
```

### Seeded Package Plans

```text
Id  BusinessId  Name                TotalCredits  ValidityDays
1   2           Fitness 5 Credits   5             30
2   1           Yoga 10 Credits     10            30
```

## Useful Seed Test Scenarios

### Successful booking

```json
{
  "userId": 2,
  "scheduleId": 4,
  "customerPackageId": 2
}
```

### Expired package

```json
{
  "userId": 9,
  "scheduleId": 4,
  "customerPackageId": 9
}
```

Expected:

```text
PACKAGE_EXPIRED
```

### Insufficient credits

```json
{
  "userId": 10,
  "scheduleId": 4,
  "customerPackageId": 10
}
```

Expected:

```text
INSUFFICIENT_CREDITS
```

### Wrong business

```json
{
  "userId": 2,
  "scheduleId": 3,
  "customerPackageId": 2
}
```

Schedule 3 belongs to Yoga while CustomerPackage 2 belongs to Fitness.

Expected:

```text
PACKAGE_BUSINESS_MISMATCH
```

### Overlapping schedule

```json
{
  "userId": 1,
  "scheduleId": 2,
  "customerPackageId": 1
}
```

User 1 already has the seeded booking for Schedule 1 and Schedule 2 overlaps it.

Expected:

```text
OVERLAPPING_BOOKING
```

### Past schedule

```json
{
  "userId": 2,
  "scheduleId": 8,
  "customerPackageId": 2
}
```

Expected:

```text
SCHEDULE_ALREADY_STARTED
```

### Full schedule

Schedule 5 has:

```text
Capacity = 2
Booked users = User 5, User 6
```

A new direct booking should be rejected as full.

### FIFO promotion

For Schedule 5:

```text
Waitlist position 1 = User 7
Waitlist position 2 = User 8
```

Cancelling Booking 2 for User 5 should free a slot.

Expected:

```text
User 5:
- booking cancelled
- +1 refund because cancellation is more than 4 hours before start

User 7:
- promoted first
- 1 credit deducted from CustomerPackage 7
```

## Local Setup

### Prerequisites

Install:

- .NET 8 SDK
- MySQL 8+
- Redis, or access to a hosted Redis service
- Visual Studio 2022 or another .NET-compatible IDE

### Configure Local Secrets

Do not commit real database or Redis credentials.

For local development, configure secrets for `RezervBooking.Api`.

In Visual Studio:

```text
Right-click RezervBooking.Api
-> Manage User Secrets
```

Example:

```json
{
  "ConnectionStrings": {
    "MySql": "Server=YOUR_HOST;Port=YOUR_PORT;Database=YOUR_DATABASE;User=YOUR_USER;Password=YOUR_PASSWORD;"
  },
  "Redis": {
    "ConnectionString": "YOUR_REDIS_HOST:YOUR_REDIS_PORT,user=default,password=YOUR_PASSWORD,abortConnect=false"
  }
}
```

For hosted Railway services used from a local machine, use the public TCP proxy host and port rather than Railway internal hostnames.

### Restore Packages

From the solution directory:

```bash
dotnet restore
```

### Apply Database Migrations

```bash
dotnet ef database update \
  --project RezervBooking.Infrastructure \
  --startup-project RezervBooking.Api
```

Visual Studio Package Manager Console equivalent:

```powershell
Update-Database -Project RezervBooking.Infrastructure -StartupProject RezervBooking.Api
```

### Load Sample Data

Run:

```text
database/seed.sql
```

against the configured MySQL database.

> The seed script is intended for development/demo use and clears the related sample tables before inserting clean sample data.

### Run the API

Start:

```text
RezervBooking.Api
```

using the HTTPS or HTTP launch profile.

Swagger should be available at:

```text
https://localhost:<port>/swagger
```

or:

```text
http://localhost:<port>/swagger
```

## Assumptions

- Date/time values are handled in UTC.
- A schedule cannot be newly booked after it has started.
- Exactly 4 hours before class start is treated as within the no-refund window.
- Waitlist membership does not reserve or deduct a package credit.
- Waitlisted users are revalidated before promotion.
- `CustomerPackageId` identifies the exact purchased package balance used for booking and refund.

## Tradeoffs

### Redis Distributed Lock

Redis distributed locking is used because the booking engine must support concurrency across multiple API instances.

For a production implementation, additional protections could include:

- Lock renewal for long operations
- Redis failover handling
- Database-level optimistic or pessimistic concurrency
- Idempotency keys for retry-safe booking requests

### Cache Strategy

Package catalogue data is relatively stable and is suitable for caching.

Booking capacity is treated as dynamic data and is checked against the database during booking rather than relying only on cached availability.

## Production Scaling

The solution could be extended with:

- Multiple stateless API instances behind a load balancer
- Managed MySQL with backups and replication
- Highly available Redis
- Authentication and authorization
- Structured logging
- Monitoring and distributed tracing
- Rate limiting
- Idempotency support
- Background processing for waitlist/class lifecycle cleanup
- Automated unit, integration, and concurrency tests
- CI/CD pipeline

## Database Schema / ERD

Database relationship documentation is available at:

```text
docs/ERD.md
```

## Notes

- Package purchase is mocked; no real payment gateway is required.
- Swagger can be used to test the required APIs.
- Database migrations are included in `RezervBooking.Infrastructure/Migrations`.
- Sample data is included in `database/seed.sql`.
