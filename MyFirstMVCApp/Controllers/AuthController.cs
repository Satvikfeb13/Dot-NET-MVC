using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyFirstMVCApp.DTO;
using MyFirstMVCApp.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MyFirstMVCApp.Controllers
{
    public class AuthController(AppDbContext _context) : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Registration()
        {
            return View();
        }
        public async Task<IActionResult> LoginUser(UserDto dto)
        {
            if(dto is null)
            {
                ViewBag.Message = "Please provide email and password";
                return View("Login");
            }

             var exist = await _context.Users.FirstOrDefaultAsync(x=>x.Email == dto.Email);
            if (exist == null) {
                ViewBag.Messaage = "Email does not exist";
                return View("Login");
            }

            var passwordHasher = new PasswordHasher<string>();

            var result = passwordHasher.VerifyHashedPassword(
                dto.Email,
                exist.Password,
                dto.Password
            );

            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Message = "Invalid credentials";
                return View("Login");
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                exist.Password = PasswordHashing(dto);
                _context.Users.Update(exist);
                await _context.SaveChangesAsync();
            }
            var token = GenerateJwtToken(dto);

            Response.Cookies.Append("jwt_Token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(1)
            });
            return RedirectToAction("Index","Dashboard");
        }
        public IActionResult LoginToRegister()
        {
            return RedirectToAction("Registration");
        }

        public IActionResult RegisterToLogin()
        {
            return RedirectToAction("Login");
        }

        private string PasswordHashing(UserDto userDto)
        {
            var hasher = new PasswordHasher<string>();
            return hasher.HashPassword(userDto.Email, userDto.Password);
        }

        [HttpPost]
        public async Task<IActionResult> RegisterUser(UserDto userDto)
        {
            if (!ModelState.IsValid)
            {
                return View("Registration", userDto);
            }

            var data = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == userDto.Email);

            if (data != null)
            {
                ViewBag.Message = "Email already exists.";
                return View("Registration", userDto);
            }

            var user = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                Password = PasswordHashing(userDto)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Registration Successful.";

            return RedirectToAction("Login");
        }

        private string GenerateJwtToken(UserDto dto)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("961a72a90b319317c4953969408614319e7b8a4f07ee95f2a490cd90dd504fb6");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.Name, dto.Email)
        }),

                Issuer = "Satvik-Client",
                Audience = "Satvik-Backend",

                Expires = DateTime.UtcNow.AddHours(1),

                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt_Token");
            return RedirectToAction("Login");
        }
    }


}
