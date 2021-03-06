using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;

        private readonly IConfiguration _config;
        public AuthController(IAuthRepository repo, IConfiguration config)
        {
            this._repo = repo;
            this._config = config;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegistration)
        {
            // validate request 
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            userForRegistration.Username = userForRegistration.Username.ToLower();

            if (await _repo.UserExists(userForRegistration.Username))
            {
                return BadRequest("Username already Exists");
            }

            var userToCreate = new User
            {
                Username = userForRegistration.Username
            };

            var createdUser = await _repo.Register(userToCreate, userForRegistration.Password);

            return StatusCode(201);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            var userFromRepo = await _repo.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);

            if (userFromRepo == null)
            {
                return Unauthorized();
            }

            // Building token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userFromRepo.ID.ToString()),
                new Claim(ClaimTypes.Name, userFromRepo.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes
                        (_config.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new
            {
                token = tokenHandler.WriteToken(token)
            });
        }
    }
}