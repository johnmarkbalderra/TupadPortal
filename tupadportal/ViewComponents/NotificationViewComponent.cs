using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using tupadportal.Data;
using tupadportal.Models;

namespace tupadportal.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public NotificationViewComponent(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int unreadAnnouncementsCount = 0;

            if (User.Identity.IsAuthenticated && User.IsInRole("brgy"))
            {
                // Cast User to ClaimsPrincipal
                var claimsPrincipal = User as ClaimsPrincipal;

                // Check if the cast was successful
                if (claimsPrincipal != null)
                {
                    // Retrieve the user ID from the claims
                    var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

                    // Fetch the user from the database
                    var user = await _userManager.Users
                        .Include(u => u.Address)
                        .FirstOrDefaultAsync(u => u.Id == userId);

                    if (user?.AddressId != null)
                    {
                        // Get the count of unread announcements
                        unreadAnnouncementsCount = await _context.Announcements
                            .Where(a => a.AddressId == user.AddressId && !a.Read)
                            .CountAsync();
                    }
                }
                else
                {
                    // Handle the case where the cast failed
                    // You can log an error or handle it as per your application's needs
                }
            }

            return View(unreadAnnouncementsCount);
        }
    }
}
