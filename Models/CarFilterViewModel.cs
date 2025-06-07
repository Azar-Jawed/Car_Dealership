namespace Car_Dealership.Models
{
    public class CarFilterViewModel
    {
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public decimal? PriceFrom { get; set; }
        public decimal? PriceTo { get; set; }

        public List<Car> Cars { get; set; } = new List<Car>();
    }
}
