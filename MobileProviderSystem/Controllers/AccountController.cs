﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileProviderSystem.Data;
using MobileProviderSystem.Models.Entities;
using MobileProviderSystem.Models.ViewModels;

namespace MobileProviderSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly MobileProviderContext _context;
        
        public AccountController(MobileProviderContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public IActionResult Registration()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> Registration(RegistrationModel model)
        {
            IActionResult result = View();
            try
            {
                if (ModelState.IsValid)
                {
                    Account account = await _context.Accounts.FirstOrDefaultAsync(u => u.Login == model.Login);
                    if (account == null)
                    {
                        account = new Account()
                        {
                            Login = model.Login, Password = model.Password
                        };
                        Role userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Client");
                        if (userRole != null)
                            account.Role = userRole;
 
                        _context.Accounts.Add(account);
                        if (await _context.SaveChangesAsync() != 0)
                        {
                            User user = new User()
                            {
                                Email = model.Email,
                                PhoneNumber = model.PhoneNumber,
                                Fio = model.Fio,
                                Account = account
                            };
                            _context.Users.Add(user);

                            if (await _context.SaveChangesAsync() != 0)
                            {
                                await Authenticate(account);
                                
                                result =  RedirectToAction("Authorization", "Account");
                            }
                            else
                            {
                                throw new Exception();
                            }
                        
                        }
                        else
                        {
                            throw new Exception();
                        }
                    
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }
            catch (Exception)
            {
                result = RedirectToAction("ErrorPage", "Error",
                    new {
                        TypeError = "Ошибка регистрации в системе" ,
                        Message = "Не удалось зарегистрировать аккаунт. Скорее всего такой аккаунт уже существует."
                    });
            }

            return result;
        }
        
        [HttpGet]
        public IActionResult Authorization()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> Authorization(AuthorizationModel model)
        {
            if (ModelState.IsValid)
            {
                Account? user = _context.Accounts
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.Login == model.Login && u.Password == model.Password);
                if (user != null)
                {
                    await Authenticate(user);
 
                    return RedirectToAction("MainMenu", "Home");
                }
                else
                {
                    return RedirectToAction("ErrorPage", "Error",
                        new {
                            TypeError = "Ошибка авторизации в системе" ,
                            Message = "Не удалось авторизоваться в системе. Проверьте введённые данные."
                        });
                }
            }
            return View(model);
        }
        
        private async Task Authenticate(Account user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Login),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role.RoleName)
            };
            
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }
    }
}