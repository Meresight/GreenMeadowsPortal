using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers.ApiControllers
{
    [Route("api/user")]
    [ApiController]
    [Authorize]
    public class UserApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public UserApiController(
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService)
        {
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: api/user/getCurrentUser
        [HttpGet("getCurrentUser")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var unreadNotifications = await _notificationService.GetUnreadCountAsync(user.Id);

            return Ok(new
            {
                id = user.Id,
                firstName = user.FirstName,
                lastName = user.LastName,
                fullName = $"{user.FirstName} {user.LastName}",
                email = user.Email,
                role = roles.FirstOrDefault() ?? "User",
                profileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                notificationCount = unreadNotifications
            });
        }
    }
}