using System.Collections.Generic;
using Car_Dealership.Models;

namespace Car_Dealership.Models
{
    public class ReviewStatsViewModel
    {
        // Автомобиль с наибольшим количеством отзывов
        public Car MostReviewed { get; set; }
        // Автомобиль с наивысшим средним рейтингом (при ≥ 3 отзывах)
        public Car HighestRated { get; set; }
        // Топ-5 машин по количеству отзывов
        public List<Car> TopByCount { get; set; }
        // Топ-5 машин по среднему рейтингу (при ≥ 3 отзывах)
        public List<Car> TopByRating { get; set; }
        // Распределение оценок: ключ = рейтинг, значение = число отзывов
        public List<RatingBucket> RatingDistribution { get; set; }
        // самый «сильный» автомобиль по сумме всех звёзд
        /// <summary>
        /// Автомобиль, у которого встречается самая высокая оценка в отзывах
        /// </summary>
        public Car BestByHighestRating { get; set; }


    }

    // Вспомогательный класс для распределения оценок
    public class RatingBucket
    {
        public int Rating { get; set; }
        public int Count { get; set; }
    }
}
