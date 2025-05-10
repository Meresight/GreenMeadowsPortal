using GreenMeadowsPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers
{
    public class UserListController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserListController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();

                var userList = users.Select(u => new {
                    Id = u.Id,
                    Email = u.Email,
                    UserName = u.UserName,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Status = u.Status
                }).ToList();

                ViewBag.Users = userList;
                ViewBag.UserCount = userList.Count;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }
    }
}