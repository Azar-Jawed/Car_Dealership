using System.ComponentModel.DataAnnotations;

namespace Car_Dealership.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Display(Name = "Автомобиль")]
        public int CarId { get; set; }
        
        public Car? Car { get; set; }


        public int? UserId { get; set; }
        public User? User { get; set; }

        [Range(1, 5)]
        [Display(Name = "Оценка")]
        public byte Rating { get; set; }

        [Required, MaxLength(1000)]
        [Display(Name = "Текст отзыва")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Дата")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
