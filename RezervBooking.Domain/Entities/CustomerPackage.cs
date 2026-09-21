using System;
using System.Collections.Generic;
using System.Linq;
namespace RezervBooking.Domain.Entities
{
    public class CustomerPackage
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public long PackagePlanId { get; set; }

        public long BusinessId { get; set; }

        public int TotalCredits { get; set; }

        public int RemainingCredits { get; set; }

        public DateTime ExpiryDateUtc { get; set; }

        public DateTime PurchasedAtUtc { get; set; }
    }
}
