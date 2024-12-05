using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using tupadportal.Data;
using tupadportal.Models;
using Microsoft.AspNetCore.Identity;
using tupadportal.ViewModels;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;

namespace tupadportal.Controllers
{
    [Authorize]
    public class ApplicantsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await _userManager.Users.Include(u => u.Address).FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
        }

        public ApplicantsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Centralized method to get the list of ID Types
        private List<SelectListItem> GetIdTypeList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Voters Id", Text = "Voters Id" },
                new SelectListItem { Value = "Postal Id", Text = "Postal Id" },
                new SelectListItem { Value = "Unified Multi Purpose ID", Text = "Unified Multi Purpose ID" },
                new SelectListItem { Value = "PhilHealth Id", Text = "PhilHealth Id" },
                new SelectListItem { Value = "Pag-IBIG ID", Text = "Pag-IBIG ID" },
                new SelectListItem { Value = "PhilSys Id", Text = "PhilSys Id" },
                new SelectListItem { Value = "Senior Citizens ID", Text = "Senior Citizens ID" },
                new SelectListItem { Value = "Passport", Text = "Passport" },
                new SelectListItem { Value = "ID Card by LTO", Text = "ID Card by LTO" },
                new SelectListItem { Value = "Barangay ID", Text = "Barangay ID" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };
        }

        // GET: Applicants
        public async Task<IActionResult> Index(int? batchId)
        {
            var currentUser = await GetCurrentUserAsync();

            // Fetch the list of available batches for the dropdown based on the current user's barangay address
            var batches = await _context.Batches
                .Where(b => b.AddressId == currentUser.Address.Add_Id)
                .Select(b => new
                {
                    b.BatId,
                    b.Slot, // Include the Slot property
                    DisplayName = b.BatchName + " (" + _context.Applicants.Count(a => a.BatchId == b.BatId) + "/" + b.Slot + " slots)"
                })
                .ToListAsync();
            ViewBag.BatchId = new SelectList(batches, "BatId", "DisplayName", batchId);

            // Check if all batches are full
            var allBatchesFull = batches.All(b => _context.Applicants.Count(a => a.BatchId == b.BatId) >= b.Slot);

            ViewBag.AllBatchesFull = allBatchesFull;

            // Query applicants based on the logged-in user's address
            var applicantsQuery = _context.Applicants
                .Include(a => a.Address)
                .Include(a => a.Batch)
                .AsQueryable();

            // Apply batch filter if batchId is provided
            if (batchId.HasValue)
            {
                applicantsQuery = applicantsQuery.Where(a => a.BatchId == batchId.Value);
            }

            if (currentUser?.Address != null)
            {
                applicantsQuery = applicantsQuery.Where(a => a.AddressId == currentUser.Address.Add_Id);
            }

            var applicants = await applicantsQuery.ToListAsync();
            return View(applicants);
        }




        // GET: Applicants/GetBatchDetails
        public async Task<IActionResult> GetBatchDetails()
        {
            var currentUser = await GetCurrentUserAsync();

            var batches = await _context.Batches
                .Where(b => b.AddressId == currentUser.Address.Add_Id)
                .Select(b => new BatchDetailsViewModel
                {
                    BatId = b.BatId,
                    BatchName = b.BatchName,
                    ApplicantCount = _context.Applicants.Count(a => a.BatchId == b.BatId),
                    Slot = b.Slot
                })
                .ToListAsync();

            return PartialView("_BatchDetailsPartial", batches);
        }






        // GET: Applicants/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Applicants == null)
            {
                return NotFound();
            }

            var applicants = await _context.Applicants
                .Include(a => a.Address)
                .Include(a => a.Batch)
                .FirstOrDefaultAsync(m => m.ApplicantId == id);
            if (applicants == null)
            {
                return NotFound();
            }

            return View(applicants);
        }

        // GET: Applicants/Create
        public IActionResult Create()
        {
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay");
            ViewData["BatchId"] = new SelectList(_context.Batches, "BatId", "BatId");
            ViewData["IdTypeList"] = GetIdTypeList(); // Populate IdType dropdown
            return View();
        }

        // POST: Applicants/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ApplicantId,FirstName,MiddleName,LastName,ExtensionName,Birthdate,Barangay,Municipality,IdType,IdNumber,ContactNo,BankAccountType,BankAccountNo,BeneficiaryType,Occupation,OccupationSpecify,Sex,CivilStatus,Age,MonthlyIncome,Dependent,InterestedInSkillsTraining,SkillsTrainingNeeded,Approved,DateTime,BatchId,AddressId")] Applicants applicants)
        {
            if (ModelState.IsValid)
            {
                _context.Add(applicants);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", applicants.AddressId);
            ViewData["BatchId"] = new SelectList(_context.Batches, "BatId", "BatId", applicants.BatchId);
            ViewData["IdTypeList"] = GetIdTypeList(); // Re-populate IdType dropdown in case of error
            return View(applicants);
        }

        // GET: Applicants/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Applicants == null)
            {
                return NotFound();
            }

            var applicants = await _context.Applicants.FindAsync(id);
            if (applicants == null)
            {
                return NotFound();
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", applicants.AddressId);
            ViewData["BatchId"] = new SelectList(_context.Batches, "BatId", "BatId", applicants.BatchId);
            ViewData["IdTypeList"] = GetIdTypeList(); // Populate IdType dropdown
            return View(applicants);
        }

        // POST: Applicants/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ApplicantId,FirstName,MiddleName,LastName,ExtensionName,Birthdate,Barangay,Municipality,IdType,IdNumber,ContactNo,BankAccountType,BankAccountNo,BeneficiaryType,Occupation,OccupationSpecify,Sex,CivilStatus,Age,MonthlyIncome,Dependent,InterestedInSkillsTraining,SkillsTrainingNeeded,Approved,DateTime,BatchId,AddressId")] Applicants applicants)
        {
            if (id != applicants.ApplicantId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(applicants);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplicantsExists(applicants.ApplicantId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", applicants.AddressId);
            ViewData["BatchId"] = new SelectList(_context.Batches, "BatId", "BatId", applicants.BatchId);
            ViewData["IdTypeList"] = GetIdTypeList(); // Re-populate IdType dropdown in case of error
            return View(applicants);
        }

        // GET: Applicants/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Applicants == null)
            {
                return NotFound();
            }

            var applicants = await _context.Applicants
                .Include(a => a.Address)
                .Include(a => a.Batch)
                .FirstOrDefaultAsync(m => m.ApplicantId == id);
            if (applicants == null)
            {
                return NotFound();
            }

            return View(applicants);
        }

        // POST: Applicants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Applicants == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Applicants'  is null.");
            }
            var applicants = await _context.Applicants.FindAsync(id);
            if (applicants != null)
            {
                _context.Applicants.Remove(applicants);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Applicants/LoadCreateStep
        public IActionResult LoadCreateStep(int step)
        {
            var userId = User.Identity.Name; // Assuming User.Identity.Name holds the logged-in user's ID or username
            var user = _context.Users
                .Include(u => u.Address) // Include the Address navigation property
                .FirstOrDefault(u => u.UserName == userId);

            if (user == null || user.Address == null)
            {
                // Handle case where user or user's address is not found
                return NotFound();
            }

            switch (step)
            {
                case 1:
                    return PartialView("_CreateStep1Partial");
                case 2:
                    KeepTempData();
                    // Set the AddressId as the selected value
                    ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", user.AddressId);
                    ViewData["IdTypeList"] = GetIdTypeList(); // Populate IdType dropdown
                    var modelStep2 = new Step2ViewModel
                    {
                        Municipality = user.Address.Municipality, // Fill with user's Municipality
                        Barangay = user.Address.Barangay, // Automatically set logged-in user's Barangay
                        AddressId = user.AddressId // Pre-select the user's AddressId
                    };
                    return PartialView("_CreateStep2Partial", modelStep2);
                case 3:
                    KeepTempData();
                    return PartialView("_CreateStep3Partial");
                case 4:
                    KeepTempData();
                    return PartialView("_CreateStep4Partial");
                case 5:
                    KeepTempData();
                    var userAddressId = user.Address.Add_Id;

                    // Fetch the list of batches for the dropdown based on the user's barangay address
                    var batches = _context.Batches
                        .Where(b => b.AddressId == userAddressId)
                        .Select(b => new
                        {
                            b.BatId,
                            DisplayName = b.BatchName + " (" + _context.Applicants.Count(a => a.BatchId == b.BatId) + "/" + b.Slot + " slots)",
                            IsFull = _context.Applicants.Count(a => a.BatchId == b.BatId) >= b.Slot
                        })
                        .ToList();

                    // Create the SelectList with items properly marked as disabled if IsFull
                    ViewBag.BatchId = new SelectList(
                        batches.Select(b => new SelectListItem
                        {
                            Value = b.BatId.ToString(),
                            Text = b.DisplayName,
                            Disabled = b.IsFull
                        }),
                        "Value",
                        "Text",
                        "Disabled"
                    );

                    return PartialView("_CreateStep5Partial");
                default:
                    return PartialView("_CreateStep1Partial");
            }
        }





        // POST: Applicants/SubmitCreateStep1
        [HttpPost]
        public IActionResult SubmitCreateStep1(Step1ViewModel model)
        {
            if (ModelState.IsValid)
            {
                TempData["Step1Data"] = JsonConvert.SerializeObject(model);
                return Json(new { success = true });
            }
            else
            {
                return PartialView("_CreateStep1Partial", model);
            }
        }

        // POST: Applicants/SubmitCreateStep2
        [HttpPost]
        public IActionResult SubmitCreateStep2(Step2ViewModel model)
        {
            KeepTempData();

            // Repopulate the AddressId dropdown after form submission
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", model.AddressId);
            ViewData["IdTypeList"] = GetIdTypeList(); // Re-populate IdType dropdown

            if (ModelState.IsValid)
            {
                TempData["Step2Data"] = JsonConvert.SerializeObject(model);
                return Json(new { success = true });
            }

            return PartialView("_CreateStep2Partial", model);
        }

        // POST: Applicants/SubmitCreateStep3
        [HttpPost]
        public IActionResult SubmitCreateStep3(Step3ViewModel model)
        {
            KeepTempData();
            if (ModelState.IsValid)
            {
                TempData["Step3Data"] = JsonConvert.SerializeObject(model);
                return Json(new { success = true });
            }
            else
            {
                return PartialView("_CreateStep3Partial", model);
            }
        }

        [HttpPost]
        public IActionResult SubmitCreateStep4(Step4ViewModel model)
        {
            KeepTempData();
            if (ModelState.IsValid)
            {
                TempData["Step4Data"] = JsonConvert.SerializeObject(model);
                return Json(new { success = true });
            }
            else
            {
                return PartialView("_CreateStep4Partial", model);
            }
        }


        // POST: Applicants/SubmitCreateStep5
        [HttpPost]
        public async Task<IActionResult> SubmitCreateStep5(Step5ViewModel model)
        {
            KeepTempData();

            // Get current user's address
            var currentUser = await GetCurrentUserAsync();
            var userAddressId = currentUser?.Address?.Add_Id;

            if (userAddressId == null)
            {
                return NotFound("User address not found.");
            }

            // Fetch the list of batches for the dropdown based on the user's barangay address
            var batches = await _context.Batches
                .Where(b => b.AddressId == userAddressId)
                .Select(b => new
                {
                    b.BatId,
                    DisplayName = b.BatchName + " (" + _context.Applicants.Count(a => a.BatchId == b.BatId) + "/" + b.Slot + " slots)",
                    IsFull = _context.Applicants.Count(a => a.BatchId == b.BatId) >= b.Slot
                })
                .ToListAsync();
            ViewData["BatchId"] = new SelectList(batches, "BatId", "DisplayName", model.BatchId);

            // Check if the selected batch is full
            var selectedBatch = batches.FirstOrDefault(b => b.BatId == model.BatchId);
            if (selectedBatch == null || selectedBatch.IsFull)
            {
                ModelState.AddModelError("BatchId", "The batch is full.");
                return PartialView("_CreateStep5Partial", model);
            }

            if (ModelState.IsValid)
            {
                // Retrieve data from TempData
                var step1Data = JsonConvert.DeserializeObject<Step1ViewModel>((string)TempData["Step1Data"]);
                var step2Data = JsonConvert.DeserializeObject<Step2ViewModel>((string)TempData["Step2Data"]);
                var step3Data = JsonConvert.DeserializeObject<Step3ViewModel>((string)TempData["Step3Data"]);
                var step4Data = JsonConvert.DeserializeObject<Step4ViewModel>((string)TempData["Step4Data"]);

                // Combine data into the Applicants model
                var applicants = new Applicants
                {
                    // Step 1 data
                    FirstName = step1Data.FirstName,
                    MiddleName = step1Data.MiddleName,
                    LastName = step1Data.LastName,
                    ExtensionName = step1Data.ExtensionName,
                    Birthdate = step1Data.Birthdate,
                    Sex = step1Data.Sex,
                    CivilStatus = step1Data.CivilStatus,
                    Age = CalculateAge(step1Data.Birthdate),

                    // Step 2 data
                    Barangay = step2Data.Barangay,
                    Municipality = step2Data.Municipality,
                    ContactNo = step2Data.ContactNo,
                    IdType = step2Data.IdType,
                    IdNumber = step2Data.IdNumber,
                    AddressId = step2Data.AddressId,

                    // Step 3 data
                    Occupation = step3Data.Occupation,
                    OccupationSpecify = step3Data.OccupationSpecify,
                    MonthlyIncome = step3Data.MonthlyIncome ?? 0,
                    BeneficiaryType = step3Data.BeneficiaryType,
                    Dependent = step3Data.Dependent,

                    // Step 4 data
                    BankAccountType = step4Data.BankAccountType,
                    /*BankAccountNo = step4Data.BankAccountNo,*/

                    // Step 5 data
                    InterestedInSkillsTraining = model.InterestedInSkillsTraining,
                    SkillsTrainingNeeded = model.SkillsTrainingNeeded,
                    BatchId = model.BatchId,

                    Approved = false,
                    DateTime = DateTime.Now
                };

                // Save to database
                _context.Add(applicants);
                await _context.SaveChangesAsync();

                // Clear TempData
                TempData.Clear();

                // Set success message
                TempData["SuccessMessage"] = "Applicant successfully created!";

                return Json(new { success = true });
            }
            else
            {
                return PartialView("_CreateStep5Partial", model);
            }
        }






        private void KeepTempData()
        {
            TempData.Keep("Step1Data");
            TempData.Keep("Step2Data");
            TempData.Keep("Step3Data");
            TempData.Keep("Step4Data");
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }

        // Load Edit Step
        public async Task<IActionResult> LoadEditStep(int step, int applicantId)
        {
            KeepTempData();
            var userId = User.Identity.Name; // Assuming User.Identity.Name holds the logged-in user's ID or username
            var user = await _context.Users
                .Include(u => u.Address) // Include the Address navigation property
                .FirstOrDefaultAsync(u => u.UserName == userId);

            if (user == null || user.Address == null)
            {
                // Handle case where user or user's address is not found
                return NotFound();
            }

            var userAddressId = user.Address.Add_Id;

            switch (step)
            {
                case 1:
                    var applicant1 = await _context.Applicants.FindAsync(applicantId);
                    var model1 = new Step1ViewModel
                    {
                        ApplicantId = applicant1.ApplicantId,
                        FirstName = applicant1.FirstName,
                        MiddleName = applicant1.MiddleName,
                        LastName = applicant1.LastName,
                        ExtensionName = applicant1.ExtensionName,
                        Birthdate = applicant1.Birthdate,
                        Sex = applicant1.Sex,
                        CivilStatus = applicant1.CivilStatus
                    };
                    // Store ApplicantId in TempData
                    TempData["ApplicantId"] = applicant1.ApplicantId;
                    TempData.Keep("ApplicantId");
                    return PartialView("_EditStep1Partial", model1);

                case 2:
                    KeepTempData();
                    var applicant2 = await _context.Applicants.FindAsync(applicantId);
                    ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", applicant2.AddressId);
                    ViewData["IdTypeList"] = GetIdTypeList(); // Populate IdType dropdown
                    var model2 = new Step2ViewModel
                    {
                        ApplicantId = applicant2.ApplicantId,
                        Barangay = applicant2.Barangay,
                        Municipality = applicant2.Municipality,
                        ContactNo = applicant2.ContactNo,
                        IdType = applicant2.IdType,
                        IdNumber = applicant2.IdNumber,
                        AddressId = applicant2.AddressId
                    };
                    return PartialView("_EditStep2Partial", model2);

                case 3:
                    KeepTempData();
                    var applicant3 = await _context.Applicants.FindAsync(applicantId);
                    var model3 = new Step3ViewModel
                    {
                        ApplicantId = applicant3.ApplicantId,
                        Occupation = applicant3.Occupation,
                        OccupationSpecify = applicant3.OccupationSpecify,
                        MonthlyIncome = applicant3.MonthlyIncome,
                        BeneficiaryType = applicant3.BeneficiaryType,
                        Dependent = applicant3.Dependent
                    };
                    return PartialView("_EditStep3Partial", model3);

                case 4:
                    KeepTempData();
                    var applicant4 = await _context.Applicants.FindAsync(applicantId);
                    var model4 = new Step4ViewModel
                    {
                        ApplicantId = applicant4.ApplicantId,
                        BankAccountType = applicant4.BankAccountType,
                        BankAccountNo = applicant4.BankAccountNo
                    };
                    return PartialView("_EditStep4Partial", model4);

                case 5:
                    KeepTempData();
                    var applicant5 = await _context.Applicants.FindAsync(applicantId);

                    // Fetch the list of batches for the dropdown based on the user's barangay address
                    var batches = await _context.Batches
                        .Where(b => b.AddressId == userAddressId)
                        .Select(b => new
                        {
                            b.BatId,
                            DisplayName = b.BatchName + " (" + _context.Applicants.Count(a => a.BatchId == b.BatId) + "/" + b.Slot + " slots)"
                        })
                        .ToListAsync();
                    ViewData["BatchId"] = new SelectList(batches, "BatId", "DisplayName", applicant5.BatchId);

                    var model5 = new Step5ViewModel
                    {
                        ApplicantId = applicant5.ApplicantId,
                        InterestedInSkillsTraining = applicant5.InterestedInSkillsTraining,
                        SkillsTrainingNeeded = applicant5.SkillsTrainingNeeded,
                        BatchId = applicant5.BatchId
                    };
                    return PartialView("_EditStep5Partial", model5);

                default:
                    return PartialView("_EditStep1Partial");
            }
        }


        // Submit Edit Step 1
        [HttpPost]
        public IActionResult SubmitEditStep1(Step1ViewModel model)
        {
            if (ModelState.IsValid)
            {
                TempData["Step1Data"] = JsonConvert.SerializeObject(model);
                TempData["ApplicantId"] = model.ApplicantId;
                return Json(new { success = true });
            }
            else
            {
                return PartialView("_EditStep1Partial", model);
            }
        }

        // Submit Edit Step 2
        [HttpPost]
        public IActionResult SubmitEditStep2(Step2ViewModel model)
        {
            KeepTempData();
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", model.AddressId);
            ViewData["IdTypeList"] = GetIdTypeList(); // Re-populate IdType dropdown

            if (ModelState.IsValid)
            {
                TempData["Step2Data"] = JsonConvert.SerializeObject(model);
                TempData["ApplicantId"] = model.ApplicantId;
                return Json(new { success = true });
            }
            else
            {
                return PartialView("_EditStep2Partial", model);
            }
        }

        // Submit Edit Step 3
        [HttpPost]
        public IActionResult SubmitEditStep3(Step3ViewModel model)
        {
            KeepTempData();

            if (ModelState.IsValid)
            {
                TempData["Step3Data"] = JsonConvert.SerializeObject(model);
                TempData["ApplicantId"] = model.ApplicantId;
                return Json(new { success = true });
            }
            else
            {
                return PartialView("_EditStep3Partial", model);
            }
        }

        // Submit Edit Step 4
        [HttpPost]
        public IActionResult SubmitEditStep4(Step4ViewModel model)
        {
            KeepTempData();

            if (ModelState.IsValid)
            {
                TempData["Step4Data"] = JsonConvert.SerializeObject(model);
                TempData["ApplicantId"] = model.ApplicantId;
                return Json(new { success = true });
            }
            else
            {
                return PartialView("_EditStep4Partial", model);
            }
        }

        // Submit Edit Step 5
        [HttpPost]
        public async Task<IActionResult> SubmitEditStep5(Step5ViewModel model)
        {
            KeepTempData();

            // Get current user's address
            var userId = User.Identity.Name; // Assuming User.Identity.Name holds the logged-in user's ID or username
            var user = await _context.Users
                .Include(u => u.Address) // Include the Address navigation property
                .FirstOrDefaultAsync(u => u.UserName == userId);

            if (user == null || user.Address == null)
            {
                // Handle case where user or user's address is not found
                return NotFound();
            }

            var userAddressId = user.Address.Add_Id;

            // Fetch the list of batches for the dropdown based on the user's barangay address
            var batches = await _context.Batches
                .Where(b => b.AddressId == userAddressId)
                .Select(b => new
                {
                    b.BatId,
                    DisplayName = b.BatchName + " (" + _context.Applicants.Count(a => a.BatchId == b.BatId) + "/" + b.Slot + " slots)"
                })
                .ToListAsync();
            ViewData["BatchId"] = new SelectList(batches, "BatId", "DisplayName", model.BatchId);

            if (ModelState.IsValid)
            {
                // Retrieve data from TempData
                var step1Data = JsonConvert.DeserializeObject<Step1ViewModel>((string)TempData["Step1Data"]);
                var step2Data = JsonConvert.DeserializeObject<Step2ViewModel>((string)TempData["Step2Data"]);
                var step3Data = JsonConvert.DeserializeObject<Step3ViewModel>((string)TempData["Step3Data"]);
                var step4Data = JsonConvert.DeserializeObject<Step4ViewModel>((string)TempData["Step4Data"]);

                var applicantId = (int)TempData["ApplicantId"];

                var applicant = await _context.Applicants.FindAsync(applicantId);

                if (applicant == null)
                {
                    return NotFound();
                }

                // Update applicant properties
                // Step 1 data
                applicant.FirstName = step1Data.FirstName;
                applicant.MiddleName = step1Data.MiddleName;
                applicant.LastName = step1Data.LastName;
                applicant.ExtensionName = step1Data.ExtensionName;
                applicant.Birthdate = step1Data.Birthdate;
                applicant.Sex = step1Data.Sex;
                applicant.CivilStatus = step1Data.CivilStatus;
                applicant.Age = CalculateAge(step1Data.Birthdate);

                // Step 2 data
                applicant.Barangay = step2Data.Barangay;
                applicant.Municipality = step2Data.Municipality;
                applicant.ContactNo = step2Data.ContactNo;
                applicant.IdType = step2Data.IdType;
                applicant.IdNumber = step2Data.IdNumber;
                applicant.AddressId = step2Data.AddressId;

                // Step 3 data
                applicant.Occupation = step3Data.Occupation;
                applicant.OccupationSpecify = step3Data.OccupationSpecify;
                applicant.MonthlyIncome = step3Data.MonthlyIncome ?? 0;
                applicant.BeneficiaryType = step3Data.BeneficiaryType;
                applicant.Dependent = step3Data.Dependent;

                // Step 4 data
                applicant.BankAccountType = step4Data.BankAccountType;
                /*applicant.BankAccountNo = step4Data.BankAccountNo;*/

                // Step 5 data
                applicant.InterestedInSkillsTraining = model.InterestedInSkillsTraining;
                applicant.SkillsTrainingNeeded = model.SkillsTrainingNeeded;
                applicant.BatchId = model.BatchId;

                try
                {
                    _context.Update(applicant);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplicantsExists(applicant.ApplicantId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // Clear TempData
                TempData.Clear();

                // Set success message
                TempData["SuccessMessage"] = "Applicant details successfully updated!";
                return Json(new { success = true });
            }
            else
            {
                return PartialView("_EditStep5Partial", model);
            }
        }


        private bool ApplicantsExists(int id)
        {
            return (_context.Applicants?.Any(e => e.ApplicantId == id)).GetValueOrDefault();
        }
    }
}
