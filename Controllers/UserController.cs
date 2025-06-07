using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Car_Dealership.Models;
using Car_Dealership.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Car_Dealership.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Метод для отображения страницы регистрации
        public IActionResult Register()
        {
            return View();
        }

        // Обработка регистрации
        [HttpPost]
        public async Task<IActionResult> Register(User model)
        {
            if (ModelState.IsValid)
            {
                // Устанавливаем уровень доступа 3 для клиента при регистрации
                model.AccessLevel = 3;

                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                // Перенаправление на страницу логина
                return RedirectToAction("Login");
            }

            // Если модель невалидна, возвращаем на страницу регистрации с ошибками
            return View(model);
        }

        // Метод для отображения страницы логина
        public IActionResult Login()
        {
            return View();
        }

        // Обработка логина
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username && u.Password == model.Password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Неправильный логин или пароль.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.AccessLevel.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // Перенаправление на нужную страницу
            return RedirectToAction("Index", "Car");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Перенаправление на страницу логина после выхода
            return RedirectToAction("Login", "User");
        }

        [Authorize(Roles = "1")] // Только для администратора
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    Role = u.AccessLevel == 1 ? "Администратор" : (u.AccessLevel == 2 ? "Менеджер" : "Клиент")
                })
                .ToListAsync();

            return View(users);  // Возвращаем пользователей в представление
        }

        [Authorize(Roles = "1")] // Только администратор
        public async Task<IActionResult> ChangeRole(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Устанавливаем новую роль (для примера переключаем роль между "Менеджер" и "Клиент")
            if (user.AccessLevel == 2)
            {
                user.AccessLevel = 3; // Меняем роль с менеджера на клиента
            }
            else if (user.AccessLevel == 3)
            {
                user.AccessLevel = 2; // Меняем роль с клиента на менеджера
            }

            // Сохраняем изменения в базе данных
            _context.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ManageUsers)); // После изменения роли перенаправляем обратно на список пользователей
        }

        [Authorize(Roles = "1")] // Только для администратора
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(); // Если пользователь не найден
            }

            _context.Users.Remove(user); // Удаляем пользователя
            await _context.SaveChangesAsync(); // Сохраняем изменения

            return RedirectToAction(nameof(ManageUsers)); // После удаления перенаправляем на страницу управления пользователями
        }
    }
}
