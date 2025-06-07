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
    [Authorize]
    public class PurchaseController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PurchaseController(ApplicationDbContext db) => _db = db;

        // POST /Purchase/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int carId)
        {
            // только клиент
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null || user.AccessLevel != 3)
                return Forbid();

            var purchase = new Purchase
            {
                CarId = carId,
                UserId = user.Id,
                PurchasedAt = DateTime.UtcNow
            };
            _db.Purchases.Add(purchase);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Car");
        }

        // GET /Purchase/Index   — для админа (1) и менеджера (2)
        [Authorize(Roles = "1,2")]
        public async Task<IActionResult> Index()
        {
            var list = await _db.Purchases
                               .Include(p => p.Car)
                               .Include(p => p.User)
                               .OrderByDescending(p => p.PurchasedAt)
                               .ToListAsync();
            return View(list);
        }


        // GET: /Purchase/Edit/5 — только для админа
        [Authorize(Roles = "1")]
        public async Task<IActionResult> Edit(int id)
        {
            var purchase = await _db.Purchases
                                    .Include(p => p.Car)
                                    .Include(p => p.User)
                                    .FirstOrDefaultAsync(p => p.Id == id);
            if (purchase == null) return NotFound();
            return View(purchase);
        }

        // POST: /Purchase/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PurchasedAt")] Purchase edited)
        {
            if (id != edited.Id) return BadRequest();

            var purchase = await _db.Purchases.FindAsync(id);
            if (purchase == null) return NotFound();

            purchase.PurchasedAt = edited.PurchasedAt;

            try
            {
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Не удалось сохранить изменения.");
                return View(purchase);
            }
        }

        
        /// Отчёт: сколько каждой модели продано за последние X месяцев.        
        [Authorize(Roles = "1,2")]  // только админ
        public async Task<IActionResult> SoldByModelReport(int months = 3)
        {
            // пороговая дата
            var since = DateTime.UtcNow.AddMonths(-months);

            // группируем покупки по модели
            var report = await _db.Purchases
                .Include(p => p.Car)
                .Where(p => p.PurchasedAt >= since)
                .GroupBy(p => p.Car.Model)
                .Select(g => new SoldByModelViewModel
                {
                    Model = g.Key,
                    Count = g.Count(),
                    TotalRevenue = g.Sum(x => x.Car.Price)
                })
                .OrderByDescending(r => r.Count)
                .ToListAsync();

            ViewData["Months"] = months;
            return View(report);
        }

        [Authorize(Roles = "1,2")]
        public async Task<IActionResult> RevenueByManufacturer()
        {
            // группируем абсолютно по всем покупкам
            var report = await _db.Purchases
                .Include(p => p.Car)
                .GroupBy(p => p.Car.Manufacturer)
                .Select(g => new RevenueByManufacturerViewModel
                {
                    Manufacturer = g.Key,
                    TotalRevenue = g.Sum(p => p.Car.Price),
                    Count = g.Count()
                })
                .OrderByDescending(r => r.TotalRevenue)
                .ToListAsync();

            return View(report);
        }

        // GET: /Purchase/SalesTrend?start=2024-01&end=2024-12
        [Authorize(Roles = "1,2")]
        public async Task<IActionResult> SalesTrend(string start, string end)
        {
            // парсим YYYY-MM в DateTime
            DateTime? from = null, to = null;
            if (!string.IsNullOrEmpty(start))
                from = DateTime.ParseExact(start + "-01", "yyyy-MM-dd", null);
            if (!string.IsNullOrEmpty(end))
            {
                var tmp = DateTime.ParseExact(end + "-01", "yyyy-MM-dd", null);
                // до конца месяца
                to = tmp.AddMonths(1).AddSeconds(-1);
            }

            // фильтруем по дате перед группировкой
            var query = _db.Purchases.Include(p => p.Car).AsQueryable();
            if (from.HasValue) query = query.Where(p => p.PurchasedAt >= from.Value);
            if (to.HasValue) query = query.Where(p => p.PurchasedAt <= to.Value);

            var monthly = await query
                .GroupBy(p => new { p.PurchasedAt.Year, p.PurchasedAt.Month })
                .Select(g => new MonthlySalesViewModel
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    TotalRevenue = g.Sum(p => p.Car.Price)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            // чтобы подставить выбранные значения в форму
            ViewBag.Start = start;
            ViewBag.End = end;

            return View(monthly);
        }


    }
}
