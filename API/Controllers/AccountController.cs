using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        public AccountController(DataContext context,ITokenService tokenService)
        {
            _tokenService = tokenService;
            _context=context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDtos>> Register(RegisterDtos registerDtos)
        {

           if(await UserExist(registerDtos.Username)) return BadRequest("Username is already in use") ;
           using var hmac=new HMACSHA512();

           var user=new AppUser
           {
            UserName=registerDtos.Username.ToLower(),
            PasswordHash=hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDtos.Password)),
            PasswordSalt=hmac.Key
           };

           _context.Users.Add(user);
           await _context.SaveChangesAsync();
            return new UserDtos
            {
                Username=user.UserName,
                Token=_tokenService.CreateToken(user)
            };
           

        }


        [HttpPost("login")]
        public async Task<ActionResult<UserDtos>> Login(LoginDtos loginDtos)
        {
            var user=await _context.Users
            .SingleOrDefaultAsync(x=>x.UserName==loginDtos.Username);

            if(user == null) return Unauthorized("Invalid Username");

            using var hmac=new HMACSHA512(user.PasswordSalt);
            
            var computedHash=hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDtos.Password));
            
            for(int i=0; i<computedHash.Length!; i++)
            {
               if(computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
            }
            return new UserDtos
            {
                Username=user.UserName,
                Token=_tokenService.CreateToken(user)
            };
           
        }   
        private async Task<bool> UserExist(string username)
        {
            return await _context.Users.AnyAsync(x=> x.UserName==username.ToLower());

        }
       
    }
}