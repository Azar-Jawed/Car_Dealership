using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Car_Dealership.Models;
using Car_Dealership.Services;



namespace Car_Dealership.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ReviewController(ApplicationDbContext db)
        {
            _db = db;
        }

        /*----------------------------------------------------------
         * 1.  СПИСОК ОТЗЫВОВ ПО КОНКРЕТНОЙ МАШИНЕ
         *     (доступен только администратору и менеджеру)
         *---------------------------------------------------------*/
        [HttpGet]
        public async Task<IActionResult> Index(int carId)
        {
            
            var reviews = await _db.Reviews
                                   .Where(r => r.CarId == carId)
                                   .Include(r => r.User)             // чтобы вывести имя автора
                                   .OrderByDescending(r => r.CreatedAt)
                                   .ToListAsync();

            ViewBag.Car = await _db.Cars.FindAsync(carId);
            return View(reviews);
        }

        /*----------------------------------------------------------
         * 2.  ФОРМА СОЗДАНИЯ ОТЗЫВА  (Client == AccessLevel 3)
         *---------------------------------------------------------*/
        [HttpGet]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> Create(int carId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.AccessLevel != 3 ||
                !await _db.Purchases.AnyAsync(p => p.CarId == carId && p.UserId == user.Id))
            {
                return Forbid();
            }

            // подгружаем машину
            var car = await _db.Cars.FindAsync(carId);
            if (car == null) return NotFound();

            // сохраняем её в ViewBag
            ViewBag.CarName = $"{car.Manufacturer} {car.Model}";

            // отдаем форму
            return View(new Review { CarId = carId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> Create(Review model)
        {
            // 0. проверяем, клиент ли
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null || currentUser.AccessLevel != 3)
                return Forbid();

            // 1. проверяем, купил ли он эту машину
            bool bought = await _db.Purchases
                                   .AnyAsync(p => p.CarId == model.CarId && p.UserId == currentUser.Id);
            if (!bought)
                return Forbid();

            // 2. Валидация модели...
            if (!ModelState.IsValid)
            {
                // собрать ошибки и вернуть форму
                return View(model);
            }

            // 3. добавляем мета и сохраняем
            model.UserId = currentUser.Id;
            model.CreatedAt = DateTime.Now;
            _db.Reviews.Add(model);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Car");
        }

        /*----------------------------------------------------------
         * 3.  УДАЛЕНИЕ ОТЗЫВА
         *     админ/менеджер    
         *     клиент‑автор      
         *---------------------------------------------------------*/
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _db.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Forbid();

            bool isStaff = currentUser.AccessLevel <= 2;
            bool isAuthor = review.UserId == currentUser.Id;
            if (!isStaff && !isAuthor) return Forbid();

            int carId = review.CarId;
            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();

            // тоже возвращаем к списку машин
            return RedirectToAction("Index", "Car");
        }

        /*----------------------------------------------------------
         * 4.  ВСПОМОГАТЕЛЬНЫЙ МЕТОД
         *---------------------------------------------------------*/
        private async Task<User?> GetCurrentUserAsync()
        {
            string? username = User.Identity?.Name;
            return username == null
                ? null
                : await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
        }
        
        [Authorize]
        public async Task<IActionResult> Stats()
        {
            // Подтянем все машины вместе с отзывами
            var cars = await _db.Cars
            .Include(c => c.Reviews)
            .ToListAsync();

            // Соберём статистику
            var model = new ReviewStatsViewModel
            {
                MostReviewed = cars
            .OrderByDescending(c => c.Reviews.Count)
            .FirstOrDefault(),
                HighestRated = cars
            .Where(c => c.Reviews.Count >= 3)
            .OrderByDescending(c => c.Reviews.Average(r => r.Rating))
            .FirstOrDefault(),
                TopByCount = cars
            .OrderByDescending(c => c.Reviews.Count)
            .Take(5)
            .ToList(),
                TopByRating = cars
            .Where(c => c.Reviews.Count >= 3)
            .OrderByDescending(c => c.Reviews.Average(r => r.Rating))
            .Take(5)
            .ToList(),
                RatingDistribution = cars
            .SelectMany(c => c.Reviews)
            .GroupBy(r => r.Rating)
            .Select(g => new RatingBucket { Rating = g.Key, Count = g.Count() })
            .OrderBy(b => b.Rating)
            .ToList()
            };

            model.BestByHighestRating = cars
            .Where(c => c.Reviews.Any())
            .OrderByDescending(c => c.Reviews.Max(r => r.Rating))
            .FirstOrDefault();

            return View(model);
        }
    }
}
