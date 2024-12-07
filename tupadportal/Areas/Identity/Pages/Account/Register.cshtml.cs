using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using FluentEmail.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FluentEmail.Smtp;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;
using tupadportal.Data;
using tupadportal.Models;

namespace tupadportal.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IFluentEmail _fluentEmail;
        private readonly ApplicationDbContext _context;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IFluentEmail fluentEmail,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _fluentEmail = fluentEmail;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public List<SelectListItem> Barangays { get; set; }

        public class InputModel
        {
            [Required]
            public string FirstName { get; set; }

            [Required]
            public string LastName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [RegularExpression(@"^\d{11}$", ErrorMessage = "The phone number must be exactly 11 digits.")]
            [Display(Name = "Phone")]
            public string Phone { get; set; }

            [Required]
            public string Position { get; set; }

            [Required]
            public string Barangay { get; set; }

            [Required]
            public string Municipality { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var sender = new SmtpSender(() => new SmtpClient("smtp.gmail.com")
            {
                UseDefaultCredentials = false,
                Port = 587, // Use 465 if SSL is required
                Credentials = new NetworkCredential("markbaldera11@gmail.com", "tgdz svld akxu vaax"),
                EnableSsl = true, // True for SSL, or use the appropriate setting based on your email provider
            });

            services
                .AddFluentEmail("markbaldera11@gmail.com")
                .AddRazorRenderer();
        }


        private void InitializeBarangays()
        {
            Barangays = new List<SelectListItem>
            {
                                new SelectListItem { Value = "San Agustin", Text = "San Agustin" },
new SelectListItem { Value = "San Antonio 1", Text = "San Antonio 1" },
new SelectListItem { Value = "San Antonio 2", Text = "San Antonio 2" },
new SelectListItem { Value = "San Bartolome", Text = "San Bartolome" },
new SelectListItem { Value = "San Buenaventura", Text = "San Buenaventura" },
new SelectListItem { Value = "San Crispin", Text = "San Crispin" },
new SelectListItem { Value = "San Diego", Text = "San Diego" },
new SelectListItem { Value = "San Francisco (Calihan)", Text = "San Francisco (Calihan)" },
new SelectListItem { Value = "San Gabriel", Text = "San Gabriel" },
new SelectListItem { Value = "San Gregorio", Text = "San Gregorio" },
new SelectListItem { Value = "San Ignacio", Text = "San Ignacio" },
new SelectListItem { Value = "San Isidro", Text = "San Isidro" },
new SelectListItem { Value = "San Joaquin", Text = "San Joaquin" },
new SelectListItem { Value = "San Jose", Text = "San Jose" },
new SelectListItem { Value = "San Juan", Text = "San Juan" },
new SelectListItem { Value = "San Lorenzo", Text = "San Lorenzo" },
new SelectListItem { Value = "San Lucas 1", Text = "San Lucas 1" },
new SelectListItem { Value = "San Lucas 2", Text = "San Lucas 2" },
new SelectListItem { Value = "San Marcos", Text = "San Marcos" },
new SelectListItem { Value = "San Mateo", Text = "San Mateo" },
new SelectListItem { Value = "San Miguel", Text = "San Miguel" },
new SelectListItem { Value = "San Nicolas", Text = "San Nicolas" },
new SelectListItem { Value = "San Pedro", Text = "San Pedro" },
new SelectListItem { Value = "San Rafael", Text = "San Rafael" },
new SelectListItem { Value = "San Roque", Text = "San Roque" },
new SelectListItem { Value = "San Vicente", Text = "San Vicente" },
new SelectListItem { Value = "Santa Ana", Text = "Santa Ana" },
new SelectListItem { Value = "Santa Catalina", Text = "Santa Catalina" },
new SelectListItem { Value = "Santa Cruz", Text = "Santa Cruz" },
new SelectListItem { Value = "Santa Elena", Text = "Santa Elena" },
new SelectListItem { Value = "Santa Filomena", Text = "Santa Filomena" },
new SelectListItem { Value = "Santa Isabel", Text = "Santa Isabel" },
new SelectListItem { Value = "Santa Maria", Text = "Santa Maria" },
new SelectListItem { Value = "Santo Angel", Text = "Santo Angel" },
new SelectListItem { Value = "Santo Cristo", Text = "Santo Cristo" },
new SelectListItem { Value = "Santo Niño", Text = "Santo Niño" },
new SelectListItem { Value = "Soledad", Text = "Soledad" },
new SelectListItem { Value = "Atisan", Text = "Atisan" },
new SelectListItem { Value = "Bagong Pook", Text = "Bagong Pook" },
new SelectListItem { Value = "Bautista", Text = "Bautista" },
new SelectListItem { Value = "Concepcion", Text = "Concepcion" },
new SelectListItem { Value = "Del Remedio", Text = "Del Remedio" },
new SelectListItem { Value = "Dolores", Text = "Dolores" },
new SelectListItem { Value = "Macabanban", Text = "Macabanban" },
new SelectListItem { Value = "Magampon", Text = "Magampon" },
new SelectListItem { Value = "Malamig", Text = "Malamig" },
new SelectListItem { Value = "Malinaw", Text = "Malinaw" },
new SelectListItem { Value = "Putol", Text = "Putol" },
new SelectListItem { Value = "San Cristobal", Text = "San Cristobal" },
new SelectListItem { Value = "Santiago 1", Text = "Santiago 1" },
new SelectListItem { Value = "Santiago 2", Text = "Santiago 2" },
new SelectListItem { Value = "Santisimo Rosario", Text = "Santisimo Rosario" },
new SelectListItem { Value = "Sibara", Text = "Sibara" },
new SelectListItem { Value = "Sinipian", Text = "Sinipian" },
new SelectListItem { Value = "Talahib", Text = "Talahib" },
new SelectListItem { Value = "Tatlong Maria", Text = "Tatlong Maria" },
new SelectListItem { Value = "Tiguihan", Text = "Tiguihan" },
new SelectListItem { Value = "Villa Agoncillo", Text = "Villa Agoncillo" },
new SelectListItem { Value = "Villa Española", Text = "Villa Española" },
new SelectListItem { Value = "Villa Navarro", Text = "Villa Navarro" },
new SelectListItem { Value = "Villa Prinza", Text = "Villa Prinza" },
new SelectListItem { Value = "Villarica", Text = "Villarica" },
new SelectListItem { Value = "Vista Hermosa", Text = "Vista Hermosa" },
new SelectListItem { Value = "Palakpakin", Text = "Palakpakin" },
new SelectListItem { Value = "Malabansang Sto Tomas", Text = "Malabansang Sto Tomas" },
new SelectListItem { Value = "San Mateo Ext.", Text = "San Mateo Ext." },
new SelectListItem { Value = "San Francisco Norte", Text = "San Francisco Norte" },
new SelectListItem { Value = "San Isidro Norte", Text = "San Isidro Norte" },
new SelectListItem { Value = "San Vicente Norte", Text = "San Vicente Norte" },
new SelectListItem { Value = "San Roque Norte", Text = "San Roque Norte" },
new SelectListItem { Value = "Sto Niño Norte", Text = "Sto Niño Norte" },
new SelectListItem { Value = "Atisan Norte", Text = "Atisan Norte" },
new SelectListItem { Value = "Del Remedio Norte", Text = "Del Remedio Norte" },
new SelectListItem { Value = "Sinipian Norte", Text = "Sinipian Norte" },
new SelectListItem { Value = "San Lucas 2 Norte", Text = "San Lucas 2 Norte" },
new SelectListItem { Value = "Sta Catalina Norte", Text = "Sta Catalina Norte" },
new SelectListItem { Value = "Santiago Norte", Text = "Santiago Norte" },
new SelectListItem { Value = "Sta Maria Norte", Text = "Sta Maria Norte" },
new SelectListItem { Value = "San Pablo Norte", Text = "San Pablo Norte" },
new SelectListItem { Value = "Malinaw Norte", Text = "Malinaw Norte" }
            };
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            InitializeBarangays();
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            Input = new InputModel
            {
                Municipality = "San Pablo City" // Default value
            };
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            InitializeBarangays();
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            Input.Municipality = "San Pablo City"; // Ensure Municipality is set

            if (ModelState.IsValid)
            {
                var user = CreateUser();
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.Phone = Input.Phone;
                user.Position = Input.Position;
                user.CreatedAt = DateTime.UtcNow;
                user.Active = false;
                user.AddressId = await GetOrCreateAddressIdAsync(Input.Barangay, Input.Municipality);

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "brgy");
                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code },
                        protocol: Request.Scheme);

                    var response = await _fluentEmail
                        .To(Input.Email)
                        .Subject("Confirm your email")
                        .UsingTemplateFromFile(Path.Combine(Directory.GetCurrentDirectory(), "/EmailConfirmation.cshtml"),
                            new { Name = Input.FirstName, ConfirmationLink = callbackUrl })
                        .SendAsync();

                    if (response.Successful)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email });
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Error sending confirmation email.");
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }


        private async Task<int> GetOrCreateAddressIdAsync(string barangay, string municipality)
        {
            var address = await _context.Addresses.FirstOrDefaultAsync(a =>
                a.Barangay == barangay && a.Municipality == municipality);

            if (address == null)
            {
                address = new Address
                {
                    Barangay = barangay,
                    Municipality = municipality,
                    DateTime = DateTime.UtcNow
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();
            }

            return address.Add_Id;
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The UserManager doesn't support email operations.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
