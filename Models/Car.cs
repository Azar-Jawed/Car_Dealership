using System.ComponentModel.DataAnnotations;

namespace Car_Dealership.Models
{
    public class Car
    {
        public int Id { get; set; }

        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public int HorsePower { get; set; }
        public int ProductionYear { get; set; }
        [Range(1, double.MaxValue, ErrorMessage = "Поле {0} должно быть числом.")]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Уровень доступа не может быть равен 0.")]
        public int AccessLevel { get; set; }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

    }
}
