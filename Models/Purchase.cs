using System;

namespace Car_Dealership.Models
{
    public class Purchase
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public Car Car { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public DateTime PurchasedAt { get; set; }

    }
}
