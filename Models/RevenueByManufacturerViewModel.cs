namespace Car_Dealership.Models
{
    public class RevenueByManufacturerViewModel
    {
        // Название бренда / производителя
        public string Manufacturer { get; set; }
        // Суммарная выручка по этому бренду
        public decimal TotalRevenue { get; set; }
        // (опционально) число проданных машин
        public int Count { get; set; }
    }
}
