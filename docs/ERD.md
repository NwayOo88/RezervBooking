# Database ERD

This document describes the core database relationships for the RezervBooking assessment project.

```mermaid
erDiagram
    Businesses ||--o{ PackagePlans : has
    Businesses ||--o{ CustomerPackages : owns
    Businesses ||--o{ TimetableSchedules : has

    Users ||--o{ CustomerPackages : purchases
    PackagePlans ||--o{ CustomerPackages : creates

    Users ||--o{ Bookings : makes
    TimetableSchedules ||--o{ Bookings : receives
    CustomerPackages ||--o{ Bookings : uses

    Users ||--o{ WaitlistEntries : joins
    TimetableSchedules ||--o{ WaitlistEntries : has
    CustomerPackages ||--o{ WaitlistEntries : uses

    Businesses {
        bigint Id PK
        string Name
    }

    Users {
        bigint Id PK
        string Name
        string Email
    }

    PackagePlans {
        bigint Id PK
        bigint BusinessId FK
        string Name
        int TotalCredits
        int ValidityDays
        bool IsActive
    }

    CustomerPackages {
        bigint Id PK
        bigint UserId FK
        bigint PackagePlanId FK
        bigint BusinessId FK
        int TotalCredits
        int RemainingCredits
        datetime ExpiryDateUtc
        datetime PurchasedAtUtc
    }

    TimetableSchedules {
        bigint Id PK
        bigint BusinessId FK
        string ClassName
        string InstructorName
        datetime StartTimeUtc
        datetime EndTimeUtc
        int Capacity
    }

    Bookings {
        bigint Id PK
        bigint UserId FK
        bigint ScheduleId FK
        bigint CustomerPackageId FK
        string Status
        datetime BookedAtUtc
        datetime CancelledAtUtc
        bool CreditRefunded
    }

    WaitlistEntries {
        bigint Id PK
        bigint UserId FK
        bigint ScheduleId FK
        bigint CustomerPackageId FK
        datetime JoinedAtUtc
        datetime PromotedAtUtc
        string Status
    }
```

## Relationship Summary

- One **Business** can have many **PackagePlans**.
- One **Business** can have many **TimetableSchedules**.
- One **User** can own many **CustomerPackages**.
- One **PackagePlan** can be purchased many times, creating many **CustomerPackages**.
- One **CustomerPackage** belongs to one **Business** and tracks the user's remaining credits.
- One **User** can create many **Bookings**.
- One **TimetableSchedule** can have many **Bookings**, subject to its capacity.
- Each **Booking** uses one **CustomerPackage**.
- One **User** can have many **WaitlistEntries**.
- One **TimetableSchedule** can have many **WaitlistEntries** ordered FIFO by `JoinedAtUtc`.
- A **WaitlistEntry** references the **CustomerPackage** that will be revalidated and charged if the user is promoted.

## Important Business Constraints

- `CustomerPackage.BusinessId` must match `TimetableSchedule.BusinessId` when booking.
- `CustomerPackage.RemainingCredits` must be at least 1 for a successful booking or waitlist promotion.
- Active booking count must not exceed `TimetableSchedule.Capacity`.
- Overlapping active bookings for the same user are not allowed.
- Waitlist promotion follows FIFO order.
- Credits are deducted when booking succeeds or when a waitlisted user is promoted.
- Cancellation more than 4 hours before class start refunds one credit.
