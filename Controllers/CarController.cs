using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Car_Dealership.Models;
using Car_Dealership.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;


namespace Car_Dealership.Controllers
{
    [Authorize]  // Защищаем весь контроллер аутентификацией
    public class CarController : Controller
    {
        private readonly ApplicationDbContext _context;



        public CarController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cars
        public async Task<IActionResult> Index(string? manufacturer, string? model, int? yearFrom, int? yearTo, decimal? priceFrom, decimal? priceTo)
        {
            var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == User.Identity.Name);

            // Формируем базовый запрос с учётом уровня доступа
            var query = _context.Cars
                .Include(c => c.Reviews)
                .Where(c => c.AccessLevel >= user.AccessLevel);

            // Применяем фильтры
            if (!string.IsNullOrEmpty(manufacturer))
                query = query.Where(c => c.Manufacturer.Contains(manufacturer));

            if (!string.IsNullOrEmpty(model))
                query = query.Where(c => c.Model.Contains(model));

            if (yearFrom.HasValue)
                query = query.Where(c => c.ProductionYear >= yearFrom.Value);

            if (yearTo.HasValue)
                query = query.Where(c => c.ProductionYear <= yearTo.Value);

            if (priceFrom.HasValue)
                query = query.Where(c => c.Price >= priceFrom.Value);

            if (priceTo.HasValue)
                query = query.Where(c => c.Price <= priceTo.Value);

            // Возвращаем результат через модель фильтра
            var result = new CarFilterViewModel
            {
                Manufacturer = manufacturer,
                Model = model,
                YearFrom = yearFrom,
                YearTo = yearTo,
                PriceFrom = priceFrom,
                PriceTo = priceTo,
                Cars = await query.ToListAsync()
            };

            if (user.AccessLevel == 3)
            {
                ViewBag.PurchasedCarIds = await _context.Purchases
                    .Where(p => p.UserId == user.Id)
                    .Select(p => p.CarId)
                    .ToListAsync();
            }
            else
            {
                ViewBag.PurchasedCarIds = new List<int>();
            }

            return View(result);
        }

        // GET: Cars/Create
        [Authorize(Roles = "1")] // Только администратор может добавлять
        public IActionResult Create()
        {
            return View();
        }

        // POST: Cars/Create
        [HttpPost]
        [Authorize(Roles = "1")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Manufacturer,Model,HorsePower,ProductionYear,Price,AccessLevel")] Car car)
        {
            if (ModelState.IsValid)
            {
                _context.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(car);
        }

        // GET: Cars/Edit/5
        [Authorize(Roles = "1")] // Только администратор может редактировать
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars.FindAsync(id);
            if (car == null)
            {
                return NotFound();
            }
            return View(car);
        }

        // POST: Cars/Edit/5
        [HttpPost]
        [Authorize(Roles = "1")] // Только администратор может редактировать
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Manufacturer,Model,HorsePower,ProductionYear,Price,AccessLevel")] Car car)
        {
            if (id != car.Id)
            {
                return NotFound();
            }

            // Проверяем, чтобы уровень доступа не был равен 0
            if (car.AccessLevel == 0)
            {
                ModelState.AddModelError("AccessLevel", "Уровень доступа не может быть равен 0.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(car);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarExists(car.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(car);
        }

        // GET: Cars/Delete/5
        [Authorize(Roles = "1,2")] // Администратор и менеджер могут удалять
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var car = _context.Cars.Find(id);
            if (car == null) return NotFound();
            return View(car);
        }

        // POST: Cars/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "1,2")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var car = _context.Cars.Find(id);
            _context.Cars.Remove(car);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "2")]  // только менеджер (роль "2")
        public async Task<IActionResult> SetAccessLevel(int id, int level)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();

            // разрешаем только уровни 2 и 3
            if (level < 2 || level > 3) return Forbid();

            car.AccessLevel = level;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// GET /Car/ExportPriceListPdf
        /// Экспортирует текущий список автомобилей (с фильтрами доступа и поиска) в PDF.
        /// </summary>
        [Authorize(Roles = "1,2")] // админ и менеджер
        public async Task<IActionResult> ExportPriceListPdf(
            string? manufacturer,
            string? model,
            int? yearFrom,
            int? yearTo,
            decimal? priceFrom,
            decimal? priceTo)
        {
            // получаем текущего пользователя
            var user = await _context.Users
                         .FirstOrDefaultAsync(u => u.Username == User.Identity.Name);

            // базовый запрос с учётом уровня доступа
            var query = _context.Cars
                .Include(c => c.Reviews)
                .Where(c => c.AccessLevel >= user.AccessLevel);

            // повторяем те же фильтры, что и в Index
            if (!string.IsNullOrEmpty(manufacturer))
                query = query.Where(c => c.Manufacturer.Contains(manufacturer));
            if (!string.IsNullOrEmpty(model))
                query = query.Where(c => c.Model.Contains(model));
            if (yearFrom.HasValue)
                query = query.Where(c => c.ProductionYear >= yearFrom.Value);
            if (yearTo.HasValue)
                query = query.Where(c => c.ProductionYear <= yearTo.Value);
            if (priceFrom.HasValue)
                query = query.Where(c => c.Price >= priceFrom.Value);
            if (priceTo.HasValue)
                query = query.Where(c => c.Price <= priceTo.Value);

            var cars = await query.ToListAsync();

            // возвращаем PDF по представлению
            return new ViewAsPdf("_PriceList", cars)
            {
                FileName = "PriceList.pdf"
            };
        }


    }
}
