using FluentEmail.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using tupadportal.Models;
using tupadportal.Data;

namespace tupadportal.Controllers
{
    [Authorize(Roles = "admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IFluentEmail _fluentEmail;
        private readonly ApplicationDbContext _context;

        // Single constructor with all required dependencies

        public UserController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IFluentEmail fluentEmail, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _fluentEmail = fluentEmail;
            _context = context; // Inject context
        }

        public async Task<IActionResult> Index(string searchString)
        {
            // Use _context.Users directly and include the Address data
            IQueryable<ApplicationUser> usersQuery = _context.Users.Include(u => u.Address); // No casting needed here

            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(s => s.UserName.Contains(searchString));
            }

            var userList = await usersQuery.ToListAsync();

            // Get roles for each user
            foreach (var user in userList)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                user.Role = userRoles.FirstOrDefault();
            }

            ViewData["CurrentFilter"] = searchString;

            return View(userList);
        }


        [HttpPost]
        public async Task<IActionResult> ToggleUserActivation(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Toggle the user's active status
                user.Active = !user.Active;
                await _userManager.UpdateAsync(user);

                // Send email notification if user is activated and in "brgy" role
                if (user.Active)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    if (userRoles.Contains("brgy"))
                    {
                        string loginUrl = "https://localhost:7231/Identity/Account/Login"; // Adjust your login URL here

                        await _fluentEmail
                            .To(user.Email)
                            .Subject("Account Activation - TUPAD Portal")
                            .Body($"Hello {user.UserName},\n\nYour account has been activated by the Admin. You can now access your TUPAD Portal account.\n\n" +
                                  $"Please click the following link to log in to your account:\n{loginUrl}\n\n" +
                                  $"Thank you,\nTUPAD Portal Team")
                            .SendAsync();
                    }
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetails(string id)
        {
            var user = await _userManager.Users
                .Include(u => u.Address) // Include related address for barangay info
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return PartialView("_UserDetailsPartial", user); // Return the partial view with user data
        }



        public async Task<IActionResult> ActivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.Active = true;
                await _userManager.UpdateAsync(user);

                // Check if the user role is 'brgy'
                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Contains("brgy"))
                {
                    // Construct the login URL
                    string loginUrl = "https://localhost:7231/Identity/Account/Login"; // Replace with your actual login page URL

                    // Send email notification to the user with a link to the login page
                    await _fluentEmail
                        .To(user.Email)
                        .Subject("Account Activation - TUPAD Portal")
                        .Body($"Hello {user.UserName},\n\nYour account has been activated by the Admin. You can now access your TUPAD Portal account.\n\n" +
                              $"Please click the following link to log in to your account:\n{loginUrl}\n\n" +
                              $"Thank you,\nTUPAD Portal Team")
                        .SendAsync();
                }
            }
            return RedirectToAction("Index");
        }


        public async Task<IActionResult> DeactivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.Active = false;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ChangeUserRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains("admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "admin");
                await _userManager.AddToRoleAsync(user, "brgy");
                user.Role = "brgy";
            }
            else if (currentRoles.Contains("brgy"))
            {
                await _userManager.RemoveFromRoleAsync(user, "brgy");
                await _userManager.AddToRoleAsync(user, "admin");
                user.Role = "admin";
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction("Index");
        }
    }
}
