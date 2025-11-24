using AttendanceMonitoring.Data;
using AttendanceMonitoring.Models;
using AttendanceMonitoring.ViewModel;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering; // para sa SelectListItem
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using NuGet.DependencyResolver;
using System.Data;
using System.Diagnostics.Metrics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static NuGet.Packaging.PackagingConstants;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AttendanceMonitoring.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // disabled caching para kapag pinindot back button sa isang browser at naka logged out na eh hindi na babalik sa specific user dashboard
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        //fields         //type of class: generic class. class type parin sila
                                       //<> means generic parang placeholder. Sinasabi mo: "SignInManager for AppUser type"
                                                //variable
        private readonly SignInManager<AppUser> signInManager;
        private readonly UserManager<AppUser> userManager;
                         //Hindi generic kase walang <>
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment; //Accessing Static Files: Use WebRootPath to locate static files like images, CSS, or JavaScript stored in the wwwroot directory.

        //private readonly UserManager<IdentityUser> _userManager;

        // **Dependency Injection = How the service is provided** to your controller
        //constructor           //parameters
        //Dependency Injection
        //eto mismo yung parameter: signInManager, tapos object dn sya pero yung laman nya, example if yung parameter is name then ang object is 'Juan'
        public AdminController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, ApplicationDbContext context, IWebHostEnvironment environment)
        {
            // so you can use them in any method inside the controller.
            // eto nayung ininject sa conrstructor
            //These four lines assign the injected parameters to the class fields

            this.signInManager = signInManager;
            this.userManager = userManager;
            this.context = context;
            this.environment = environment;

            //this._userManager = _userManager;
        }

        //[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // disabled caching para kapag pinindot back button sa isang browser at naka logged out na eh hindi na babalik sa specific user dashboard
        //[Authorize(Roles = "Admin")]
        public IActionResult AdminHome()
        {
            return View();
        }

        public async Task<IActionResult> SubjectList()
        {
            var subjectList = await context.Subjects
                .OrderBy(s => s.SubjectDescription)
                .ToListAsync();

            return View(subjectList);
        }

        public async Task<IActionResult> GradeList()
        {
            var GradeList = await context.Grades
                .OrderBy(s => s.GradeLevel)
                .ToListAsync();

            return View(GradeList);
        }

        public async Task<IActionResult> AddSubject()
        {
            return PartialView("_AddSubjectPartial");
        }

        [HttpPost]
        [ValidateAntiForgeryToken] //protecting applications from CSRF attacks. //ValidateAntiForgeryToken is recommended for Form submission
        public async Task<IActionResult> AddSubject(SubjectViewModel model)
        {
            bool subjectDescriptionExisted = await context.Subjects.AnyAsync(s => s.SubjectDescription == model.SubjectDescription);
            
            if (subjectDescriptionExisted)
            {
                ModelState.AddModelError("SubjectDescription", "Subject Description is already existed!");
            }
            
            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            var Subject = new Subject()
            {
                SubjectDescription = model.SubjectDescription,
                Category = model.Category,
                CreatedAt = DateTime.Now
            };

            

            await context.Subjects.AddAsync(Subject);
            await context.SaveChangesAsync();
            
            return Json(new { success = true, message = "Subject Added Successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> EditSubject(int id)
        {
            var Subject = await context.Subjects.FindAsync(id);

            if(Subject == null)
            {
                return Json(new { success = false, error = "Subject Not Found!" });
            }

            var model = new EditSubjectViewModel()
            {
                SubjectDescription = Subject.SubjectDescription,
                Category = Subject.Category,

            };

            return PartialView("_EditSubjectPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubject(int id, EditSubjectViewModel model)
        {
            var EditSubject = await context.Subjects.FindAsync(id);

            if (EditSubject == null)
            {
                return Json(new { success = false, error = "Subject Not Found!" });
            }

            bool subjectDescriptionExisted = await context.Subjects.AnyAsync(s => s.SubjectDescription == model.SubjectDescription && s.Id != id);

            if (subjectDescriptionExisted)
            {
                ModelState.AddModelError("SubjectDescription", "Subject Description is already existed!");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            EditSubject.SubjectDescription = model.SubjectDescription;
            EditSubject.Category = model.Category;

            //if (model.Category != "TVL")
            //{
            //    EditSubject.TVLProgram = null;
            //}
            //else
            //{
            //    EditSubject.TVLProgram = model.TVLProgram;
            //}

            context.Subjects.Update(EditSubject);
            await context.SaveChangesAsync();

            return Json(new { success = true, message = "Subject Edited Successfully!" });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var DeleteSubject = await context.Subjects.FindAsync(id);

            if (DeleteSubject == null)
            {
                return Json(new { success = false, error = "Subject Not Found!" });
            }

            context.Subjects.Remove(DeleteSubject);
            await context.SaveChangesAsync();

            return Json(new { success = true, message = "Subject Deleted Successfully!" });
        }

        //public async Task<IActionResult> ManageSectionSubject()
        //{
        //    var assignedSubjectList = await context.SectionSubjects
        //                                           .OrderBy(a => a.SectionId)
        //                                           .ToListAsync();

        //    return View(assignedSubjectList);
        //}

        [HttpGet]
        public async Task<IActionResult> ManageSectionSubject(int id)
        {
            //Displays assigned subjects on an specific Grade and section
            var assignedSubjectList = await context.SectionSubjects
                                                   .Include(ss => ss.Subject) //para sa Razor page is maaccess yun SubjectDescription
                                                   .Where(ss => ss.SectionId == id) //Where SectionId (FK) in SectionSubjects Db is equals to int db
                                                   .ToListAsync();

            var GradeSection = await context.Sections
                .Include(g => g.Grade) // dahil sa Nav.Property/Lazy loading na nakadeclare sa Section.cs kaya gumana ang .Include
                .FirstOrDefaultAsync(g => g.Id == id); //Retrieve single data
                                                       //.TolistAsync(); //Retrieve all data

            var sectionWithSameGrade = await context.Sections
                                                    .Include(s => s.Grade)
                                                    .Where(s => s.GradesId == GradeSection.GradesId && s.Track == GradeSection.Track && s.TVLProgram == GradeSection.TVLProgram && s.Id != id)
                                                    //.Select(s => s.SectionName)
                                                    .ToListAsync();

            var model = new ManageSectionSubjectViewModel
            {
                Section = GradeSection,
                otherSectionWithSameGrade = sectionWithSameGrade,
                assignedList = assignedSubjectList
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
                                                   //id of section where to copy from(sang section galing)
                                                                           //Which sections to copy To(kaya nakaList)
        public async Task<IActionResult> CopySubjects(int sourceSectionId, List<int> targetSectionIds)
        {
            //Gets all subjects under the source section
            var sourceSubjects = await context.SectionSubjects
                                              .Where(ss => ss.SectionId == sourceSectionId)
                                              .ToListAsync();
            int copiedCount = 0;

            //Outer loop: Copy subject to each target sections
            foreach(var targetSectionId in targetSectionIds) //<- Where to Copy
            {
                //check if yung sections is existing
                var targetSection = await context.Sections.FindAsync(targetSectionId);
                //Inner Loop: Copying each subject
                foreach(var sourceSubject in sourceSubjects) //<- What to copy
                {
                    //Check if the subject is already exist on the target section
                    var exists = await context.SectionSubjects
                                              .AnyAsync(ss => ss.SectionId == targetSectionId && ss.SubjectId == sourceSubject.SubjectId);
                    //If subjec does not already exists, it adds it
                    if (!exists)
                    {
                        context.SectionSubjects.Add(new SectionSubject
                        {
                            SectionId = targetSectionId,
                            SubjectId = sourceSubject.SubjectId,
                        });
                        copiedCount++;
                    }
                }
            }

            await context.SaveChangesAsync();

            return Json(new { sucess = true, message = $"Successfully copied {copiedCount} subjects!" });
        }

        //[HttpGet]
        //public async Task<IActionResult> AssignSubject(string searchString)
        //{
        //    var subjectList = await context.Subjects
        //        .OrderBy(s => s.SubjectDescription)
        //        .Take(10)
        //        .ToListAsync();

        //    var model = new ManageSectionSubjectViewModel()
        //    {
        //        //SubjectDescription = subjectList.SubjectDesription //Ginagamit kapag single string value lang
        //        AvailableSubject = subjectList //Uses for list

        //    };

        //    if (!string.IsNullOrEmpty(searchString))
        //    {
        //        subjectList = await context.Subjects
        //                     .Where(s => s.SubjectDescription.Contains(searchString));

        //    }
        //    return PartialView("_AssignSubjectPartial", model);
        //}

        [HttpGet]
        public async Task<IActionResult> AssignSubject(int sectionId, int subjectId, string searchString, string category)
        {
            var assignedSubject = await context.SectionSubjects
                                               .Where(s => s.SectionId == sectionId) //This excluded the assigned subject by an specific section only
                                               .Select(s => s.SubjectId)
                                               .ToListAsync();

            //var availableSubject = await context.Subjects
            //                                    .Where(s => s.Category == "JHS" && !assignedSubject.Contains(s.Id))
            //                                    .ToListAsync();

            //Used for Dynamic filtereing. Hindi agad gagana yung query unless tinawag na yung ToList
            IQueryable<Subject> subjectQuery = context.Subjects
                                                      .Where(s => !assignedSubject.Contains(s.Id))
                                                      .OrderBy(s => s.SubjectDescription);

            if (!string.IsNullOrEmpty(searchString))
            {
                subjectQuery = subjectQuery.Where(s => s.SubjectDescription.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(category))
            {
                subjectQuery = subjectQuery.Where(c => c.Category == category);
            }

            var subjectList = await subjectQuery.Take(10).ToListAsync();


            var model = new AssignSubjectViewModel()
            {
                SectionId = sectionId,
                AvailableSubject = subjectList,
            };

            ViewData["searchString"] = searchString;
            ViewData["category"] = category;

            return PartialView("_AssignSubjectPartial", model);

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSubject(int sectionId, List<int> SelectedSubjects)
        {
            var section = await context.Sections.FindAsync(sectionId);

            if ( section == null)
            {
                return Json(new { success = false, errors = "Section Id Not Found!" });
            }

            if(SelectedSubjects == null || !SelectedSubjects.Any())
            {
                return Json(new { success = false, errors = "Please select atleast 1 subject" });
            }
            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            foreach(var subjectId in SelectedSubjects)
            {
                var subject = await context.Subjects.FindAsync(subjectId);
                if (subject != null)
                {
                    var assigned = new SectionSubject()
                    {
                        SectionId = sectionId,
                        SubjectId = subjectId,
                        CreatedAt = DateTime.Now

                    };
                    await context.SectionSubjects.AddAsync(assigned);
                }
                //if(subject != null)
                //{
                //    await context.SectionSubjects.AddAsync(new SectionSubject
                //    {
                //        SectionId = sectionId,
                //        SubjectId = subjectId
                //    });
                //}
            }
            await context.SaveChangesAsync();
            return Json(new { success = true, message = "Subject Assigned Successfully!" });
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveAssignedSubject(int id)
        {
            var subject = await context.SectionSubjects.FindAsync(id);

            if (subject == null)
            {
                return Json(new { success = true, error = "Id not Found!" });
            }

            context.SectionSubjects.Remove(subject);
            await context.SaveChangesAsync();

            return Json(new {success = true, message = "Subject Removed!"});
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AssignSubject()
        //{
        //    return Json(new { success = true, message = "Subject assigned successfully!" });
        //}
        //public async Task<IActionResult> GradeAndSectionList()
        //{
        //    var GradesSection = await context.AcademicClasses
        //        .OrderBy(s => s.GradeLevel)
        //        .ThenBy(s => s.SectionName)
        //        .ToListAsync();

        //    //var groupdSections = await context.AcademicClasses
        //    //    .GroupBy(g => g.GradeLevel) //group the class by gradeLevel
        //    //    .Select(group => new GradeAndSectionViewModel // transform each group into object
        //    //    {
        //    //        GradeLevel = group.Key, // the gradelevel (grouping key)
        //    //        SectionName = string.Join(", ",  //Join Section into one string and seperate them using comma
        //    //            group.Select(s => s.SectionName))//Get all section names in the group
        //    //    })
        //    //    .OrderBy(g => g.GradeLevel) //sort by grade level. Pag kakasunod 
        //    //    .ToListAsync(); //Execure query and return result into list


        //    return View(GradesSection);
        //}

        public IActionResult AddGrade()
        {
            return PartialView("_AddGradePartial");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGrade(GradeViewModel model)
        {
            bool gradeExisted = await context.Grades.AnyAsync(g => g.GradeLevel == model.GradeLevel);

            if (gradeExisted)
            {
                ModelState.AddModelError("GradeLevel", "Grade Level is already Existed!");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            var grade = new Grade()
            {
                GradeLevel = model.GradeLevel,
                Category = model.Category,
                CreatedAt = DateTime.Now
            };

            await context.Grades.AddAsync(grade);
            await context.SaveChangesAsync();

            return Json(new { success = true, message = "Grade Level Added Successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> EditGrade(int id)
        {
            var grade = await context.Grades.FindAsync(id);

            if (grade == null)
            {
                return Json(new { success = false, error = "Not Found" });// always gamitin ang json lalo na kapag ajax/modal. Standard para sa ajax ang json
            }

            var model = new GradeViewModel()
            {
                GradeLevel = grade.GradeLevel,
                Category = grade.Category

            };


            return PartialView("_EditGradePartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGrade(int id, GradeViewModel model)
        {
            var editGrade = await context.Grades.FindAsync(id);

            if(editGrade == null)
            {
                return Json(new { success = false, message = "Grade not found" });
            }

            bool GradeLevelExisted = await context.Grades.AnyAsync(g => g.GradeLevel == model.GradeLevel && g.Id != id);

            if (GradeLevelExisted)
            {
                ModelState.AddModelError("GradeLevel", "Grade Level already existed!");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            editGrade.GradeLevel = model.GradeLevel;
            editGrade.Category = model.Category;

            context.Grades.Update(editGrade);
            await context.SaveChangesAsync();

            return Json(new { success = true, message = "Grade Successfully Edited!" });

        }

        [HttpDelete]
        public async Task<IActionResult> DeleteGrade(int id)
        {
            var grade = await context.Grades.FindAsync(id);

            if(grade == null)
            {
                return Json(new { success = false, error = "Grade does not found" });
            }

            context.Grades.Remove(grade);
            await context.SaveChangesAsync();

            return Json(new { success = true, message = "Grade Successfully Deleted!" });

        }
        public async Task<IActionResult> SectionList()
        {
            var sectionList = await context.Sections
                .Include(g => g.Grade) // dahil sa Nav.Property/Lazy loading na nakadeclare sa Section.cs kaya gumana ang .Include
                .OrderBy(s => s.GradesId)
                .ToListAsync();

            return View(sectionList);
        }

        [HttpGet]
        public async Task<IActionResult> AddSection()
        {
            // With 'new' (pulls only what we need):
            var availableGrades = await context.Grades
                .Select(g => new { g.Id, g.GradeLevel }) //Only gets id and GradeLevel
                .ToListAsync();

                
            var model = new SectionViewModel
            {
                //this create a dropdown
                AvailableGrades = availableGrades.Select(g => new SelectListItem //SelectListItem design for creating dropdown list
                {
                    Value = g.Id.ToString(), // what gets sent to the server when selected
                    Text = $"Grade {g.GradeLevel}" // eto yung makikita ng user
                }).ToList()

            };

            return PartialView("_AddSectionPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSection(CreateSectionViewModel model)
        {    
            var sectionNames = model.SectionName
                .Split(',') //Divide string into an array
                .Select(s => s.Trim()) //remove extra spaces
                .Where(s => !string.IsNullOrEmpty(s))//remove empty entries
                .Distinct() //remove duplicates
                .ToList();

            if (!sectionNames.Any())
            {
                ModelState.AddModelError("SectionName", "Please provide atleast 1 section name");
            }

            //Check if Section is already Existed on a specific Grade level sa iinput na bagong section
            var sectionExisted = await context.Sections
                .Where(s => s.GradesId == model.GradesId && s.Track == model.Track
                        && sectionNames.Contains(s.SectionName))
                .Select(s => s.SectionName)
                .ToListAsync();

            if (sectionExisted.Any())
            {
                ModelState.AddModelError("SectionName", "Section Name is Already Existed");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                   kvp => kvp.Key,
                   kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            //var Section = new Section()
            //{
            //    GradesId = model.GradesId,
            //    SectionName = model.SectionName,
            //    Track = model.Track,
            //    CreatedAt = DateTime.Now
            //};

            //pag ganito ibig sabihin may data na multiple ang iinsert na data   
            var Sections = sectionNames.Select(name => new Section
            {
                GradesId = model.GradesId,
                SectionName = name,
                Track = model.Track,
                TVLProgram = model.TVLProgram,
                CreatedAt = DateTime.Now
            });

            await context.Sections.AddRangeAsync(Sections);
            await context.SaveChangesAsync();

            return Json(new { success = true, message = "Section Added Succesfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> EditSection(int id)
        {
            var section = await context.Sections.FindAsync(id);

            if(section == null)
            {
                return Json(new { success = false, message = "Section not found" });
            }

            // With 'new' (pulls only what we need):
            var availableGrades = await context.Grades
                .Select(g => new { g.Id, g.GradeLevel, g.Category }) //Only gets id and GradeLevel
                .ToListAsync();

            var model = new EditSectionViewModel()
            {
                AvailableGrades = availableGrades.Select(g => new SelectListItem
                {
                    Value = g.Id.ToString(),
                    Text = $"Grade {g.GradeLevel}"
                }).ToList(),

                GradesId = section.GradesId,
                SectionName = section.SectionName,
                Track = section.Track,
                TVLProgram = section.TVLProgram,

            };


            return PartialView("_EditSectionPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSection(int id, EditSectionViewModel model)
        {
            var editSection = await context.Sections.FindAsync(id);

            if (editSection == null)
            {
                return Json(new { success = false, message = "Section not found" });
            }

            //Gamitin ang FirstOrDefaultAsync If you need to do something with the existing section data. Pag kailangan mo ng actual data
            //var sectionExisted = await context.Sections
            //    .FirstOrDefaultAsync(s => s.GradesId == model.GradesId
            //            && s.SectionName == model.SectionName && s.Id != id);

            //Gamitin ang Any kapag more on validation checking lang
            var sectionExisted = await context.Sections
                .AnyAsync(s => s.GradesId == model.GradesId && s.Track == model.Track
                        && s.SectionName == model.SectionName && s.Id != id);        

            if (sectionExisted)
            {
                ModelState.AddModelError("SectionName", "Section Name is already Existed");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            editSection.GradesId = model.GradesId;
            editSection.SectionName = model.SectionName;
            editSection.Track = model.Track;
            editSection.TVLProgram = model.TVLProgram;

            context.Sections.Update(editSection);
            await context.SaveChangesAsync();

            return Json( new {success = true, message = "Section Edited Succcessfully!"});
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSection(int id)
        {
            var Section = await context.Sections.FindAsync(id);

            if (Section == null)
            {
                return Json(new { success = false, error = "Section does not exist!" });
            }

            context.Sections.Remove(Section);
            await context.SaveChangesAsync();

            return Json(new { success = true, message = "Section Successfully deleted!" });
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AddGradeAndSection(GradeAndSectionViewModel model)
        //{
        //    //bool gradeLevel = await context.AcademicClasses.AnyAsync(g => g.GradeLevel == model.GradeLevel);
        //    //if (gradeLevel)
        //    //{
        //    //    ModelState.AddModelError("GradeLevel", "Grade Level is already existed!");
        //    //}

        //    //divides section name input if the section has two or more entries
        //    //split section names   
        //    var sectionNames = model.SectionName
        //        .Split(',')//divides a string into an array
        //        .Select(s => s.Trim()) // remove extra spaces
        //        .Where(s => !string.IsNullOrEmpty(s))//remove empty entries
        //        .Distinct()//remove duplicates
        //        .ToList();

        //    if (!sectionNames.Any())
        //    {
        //        ModelState.AddModelError("SectionName", "Please provide atleast one section names");
        //        //return View(model);
        //    }

        //    //Check if Section is already Existed on a specific Grade level
        //    //LINQ Query Syntax
        //    //var sectionExisted = from s in context.AcademicClasses
        //    //                     where s.GradeLevel == model.GradeLevel && sectionNames.Contains(s.SectionName)
        //    //                     select s.SectionName;

        //    //LINQ Method Syntax
        //    //var sectionExisted = await context.AcademicClasses
        //    //    .Where(s => s.GradeLevel == model.GradeLevel
        //    //            && sectionNames.Contains(s.SectionName))
        //    //    .Select(s => s.SectionName)
        //    //    .ToListAsync();


        //    //if (sectionExisted.Any())
        //    //{
        //    //    ModelState.AddModelError("SectionName", "Section Name is Already Existed!");
        //    //}

        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState.ToDictionary(
        //                                   kvp => kvp.Key,
        //                                   kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
        //                               );

        //        return Json(new { success = false, errors = errors });
        //    }

        //    //LINQ using loop //shortcut to na naka forloop
        //    //pa ganito ibig sabihin may data na multiple ang iinsert na data   
        //    //var GradeSection = sectionNames.Select(name => new GradeLevel
        //    //{
        //    //    GradeLevel = model.GradeLevel,
        //    //    SectionName = name, // yung name is represent mismo ng section names kase naka array na sya dahil by batch ang add. Kase kapag ang gamit is model.SectionName is string sya and isang variable lang
        //    //    CreatedAt = DateTime.Now
        //    //});

        //    //await context.AcademicClasses.AddRangeAsync(GradeSection); //AddRangeAsync ang ginamit kase by batch ang iadd, means mulitiple data
        //    //await context.SaveChangesAsync();

        //    //return Json(new { success = true, message = "Grade and Section Added!" });
        //}

        //[HttpGet]
        //public async Task<IActionResult> EditGradeAndSection(int id)
        //{
        //    var GradeSection = await context.AcademicClasses.FindAsync(id);

        //    if(GradeSection == null)
        //    {
        //        return Json(new { success = false, error = "Grade and Section does not exist!" });
        //    }

        //    var model = new EditGradeAndSectionViewModel()
        //    {
        //        GradeLevel = GradeSection.GradeLevel,
        //        SectionName = GradeSection.SectionName
        //    };

        //    return PartialView("_EditGradeAndSectionPartial", model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> EditGradeAndSection(int id, EditGradeAndSectionViewModel model)
        //{
        //    var GradeSection = await context.AcademicClasses.FindAsync(id);

        //    if(GradeSection == null)
        //    {
        //        return Json(new { success = false, error = "Grade and Section does not exist!" });
        //    }

        //    bool sectionExisted = await context.AcademicClasses
        //        .AnyAsync(s => s.GradeLevel == model.GradeLevel 
        //            && s.SectionName == model.SectionName 
        //            && s.Id != id);

        //    if (sectionExisted)
        //    {
        //        ModelState.AddModelError("SectionName", "Section Name is already used!");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState.ToDictionary(
        //                        kvp => kvp.Key,
        //                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
        //                    );
        //        return Json(new { success = false, errors = errors });
        //    }

        //    GradeSection.GradeLevel = model.GradeLevel;
        //    GradeSection.SectionName = model.SectionName;

        //    context.Update(GradeSection);
        //    await context.SaveChangesAsync();

        //    return Json(new { success = true, message = "Grade & Section Successfully Edited!" });
        //}

        //[HttpDelete]
        //public async Task<IActionResult> DeleteGradeAndSection(int id)
        //{
        //    var GradeAndSection = await context.AcademicClasses.FindAsync(id);

        //    if (GradeAndSection == null)
        //    {
        //        return Json(new { success = false, error = "Grade And section does not exist!" });
        //    }

        //    context.AcademicClasses.Remove(GradeAndSection);
        //    await context.SaveChangesAsync();

        //    return Json(new { success = true, message = "Grade and Section Successfully deleted!" });
        //}
        public async Task<IActionResult> TeacherList()//string TeacherRole
        {
            //var teacher = context.Users
            //    .Where(user => context.UserRoles
            //    .Any(ur => ur.UserId == user.Id && context.Roles
            //    .Any(r => r.Id == ur.RoleId && r.Name == "Teacher")))
            //    .ToList();

            var teacher = await userManager.GetUsersInRoleAsync("Teacher");

            return View(teacher);// return view dahil full page ang nirereload
            //return PartialView();// kapag maliit or more on modal ang rereload
        }

        public async Task<IActionResult> SecretaryList()
        {
            //var secretary = context.Users
            //    .Where(user => context.UserRoles
            //    .Any(ur => ur.UserId == user.Id && context.Roles
            //    .Any(r => r.Id == ur.RoleId && r.Name == "Secretary")))
            //    .ToList();

            var secretary = await userManager.GetUsersInRoleAsync("Secretary");

            return View(secretary);
        }

        [HttpGet] //Entity → ViewModel(for display/edit)
        public async Task<IActionResult> EditTeacher(string id)
        {
            // Get Entity From database
            var teacher = await context.Users.FindAsync(id);
            //var teacher = userManager.FindByIdAsync(id);

            if (teacher == null)
            {   // Hindi pwede mag-RedirectToAction sa PartialView. Unless full page load, eh naka modal
                //return RedirectToAction("TeacherList", "Admin");
                return Json(new { success = false, error = "Not Found" });// always gamitin ang json lalo na kapag ajax/modal. Standard para sa ajax ang json
            }
            //Map From entity to view model
            var model = new EditTeacherViewModel()
            {
                Email = teacher.Email, //from entity
                //UserName = teacher.Email,
                SchoolId = teacher.SchoolId,
                EmployeeId = teacher.EmployeeId,
                FirstName = teacher.FirstName,
                MiddleName = teacher.MiddleName,
                LastName = teacher.LastName,
                Sex = teacher.Sex,
                positionTitle = teacher.positionTitle,
            };

            ViewData["imageFileData"] = teacher.imageFileData;
            ViewData["imageFilePath"] = teacher.imageFilePath;
            ViewData["CreatedAt"] = teacher.CreatedAt.ToString("MM/dd/yyyy");

            return PartialView("_EditTeacherPartial", model);
        }

        [HttpGet]
        public async Task<IActionResult> ViewTeacher(string id)
        {
            var teacher = await context.Users.FindAsync(id);

            if (teacher == null)
            {
                return RedirectToAction("TeacherList", "Admin");
            }
            //Manual mapping
            var model = new EditTeacherViewModel()
            {
                Email = teacher.Email,
                //UserName = teacher.Email,
                SchoolId = teacher.SchoolId,
                EmployeeId = teacher.EmployeeId,
                FirstName = teacher.FirstName,
                MiddleName = teacher.MiddleName,
                LastName = teacher.LastName,
                Sex = teacher.Sex,
                positionTitle = teacher.positionTitle,
                imageFilePath = teacher.imageFilePath,
            };

            ViewData["imageFileData"] = teacher.imageFileData;
            //ViewData["imageFilePath"] = teacher.imageFilePath;
            ViewData["CreatedAt"] = teacher.CreatedAt.ToString("MM/dd/yyyy");

            return PartialView("_ViewTeacherPartial", model);
        }
        public IActionResult StudentList()
        {
            return View();
        }

        public IActionResult AddTeacher()
        {
            return PartialView("_AddTeacherPartial");
        }

        public IActionResult AddSecretary()
        {
            return PartialView("_AddSecretaryPartial");
        }

        [HttpPost] //ViewModel → Entity (for saving to database)
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher(TeacherViewModel model)
        {
            bool teacherFirstLastNameExist = await context.Users.AnyAsync(t => t.FirstName == model.FirstName && t.MiddleName == model.MiddleName && t.LastName == model.LastName);

            if (teacherFirstLastNameExist)
            {
                ModelState.AddModelError("FirstName", "A Teacher with this Full name already exists");
                ModelState.AddModelError("MiddleName", "");
                ModelState.AddModelError("LastName", "");
            }

            bool schoolIdExisted = await context.Users.AnyAsync(s => s.SchoolId == model.SchoolId);

            if (schoolIdExisted)
            {
                ModelState.AddModelError("SchoolId", "School Id is already taken!");
            }

            bool employeeIdExisted = await context.Users.AnyAsync(e => e.EmployeeId == model.EmployeeId);

            if (employeeIdExisted)
            {
                ModelState.AddModelError("EmployeeId", "Employee Id is already taken!");
            }

            //Gagamitin to kapag gusto kong gumawa ng sarili kong validation sa Email existed kase may sariling validation si userManager.AnyAsync() about sa email exist
            //bool EmailIsExisted = await context.Users.AnyAsync(e => e.Email == model.Email);

            //if (EmailIsExisted)
            //{
            //    ModelState.AddModelError("Email", "Email is Existed!");
            //}
            if (ModelState.IsValid)
            {
                string? saveImagePath = null;
                byte[]? saveImageData = null;

                if (model.imageFile != null)
                {
                    //In this code, it creates a unique file name for the image using date and time
                    string newFile = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    newFile += Path.GetExtension(model.imageFile.FileName);
                    //create physical path ng image kung saan masesave yung image ex. sa webrooth file named ProfilePic
                    string imageFullPath = environment.WebRootPath + "/ProfilePic/" + newFile;
                    //Sine - save yung actual image file sa wwwroot/ ProfilePic / folder
                    using (var stream = System.IO.File.Create(imageFullPath))
                    {
                        await model.imageFile.CopyToAsync(stream);
                    }
                    saveImagePath = newFile;

                    using (var inputStream = model.imageFile.OpenReadStream())
   
                    //I-convert yung file sa byte array //Using() statement is used for files, database connection etc.
                    using (var memoryStream = new MemoryStream())
                    {
                        await inputStream.CopyToAsync(memoryStream);
                        //await model.imageFile.CopyToAsync(memoryStream);
                        saveImageData = memoryStream.ToArray();
                    }

                }
                //Map from viewmodel to entity
                AppUser teacher = new AppUser()
                {
                    Email = model.Email,
                    UserName = model.Email,
                    SchoolId = model.SchoolId,
                    EmployeeId = model.EmployeeId,
                    FirstName = model.FirstName,
                    MiddleName = model.MiddleName,
                    LastName = model.LastName,
                    Sex = model.Sex,
                    positionTitle = model.positionTitle,
                    imageFileData = saveImageData,
                    imageFilePath = saveImagePath,
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(teacher, model.Password); //.CreateAsync has a build in validation so if email existed it will return an error

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(teacher, "Teacher"); //Assign Teacher role when registered!
                    return Json(new { success = true, message = "Teacher Added Successfully" }); //transfer a message to client side from server side 
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        //eto gagamitin if aasa ako sa built in valdiation ni userManager.CreateAsync(); para ang lalabas is eto ModelState.AddModelError("Email", "Email is already used!");
                        if (error.Code == "DuplicateUserName")
                        {
                            ModelState.AddModelError("Email", "Email is already used!");
                        }else if (error.Description.Contains("Password"))
                        {
                            ModelState.AddModelError("Password", error.Description);
                        }
                        else
                        {
                            ModelState.AddModelError("", error.Description); //general error at isesesnd kay asp-validation-summary
                        }


                        //Eto ang gagamitin ko kapag may sarili akong validation if email is existed!
                        //ModelState.AddModelError("", error.Description); //general error at isesesnd kay asp-validation-summary

                    }
                    var errors = ModelState.ToDictionary(
                                            kvp => kvp.Key,
                                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                                        );

                    return Json(new { success = false, errors = errors });
                }

                //return PartialView("TeacherList", teacher);

            }
            else
            {
                var errors = ModelState.ToDictionary(
                                                       kvp => kvp.Key,
                                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                                                   );

                return Json(new { success = false, errors = errors });
            }
            //return PartialView("_AddTeacherPartial", model);

            //to see actual error in devtools
            //try
            //{
            //}
            //catch (Exception ex)
            //{ 
            //    return Json(new { success = false, message = $"Error: {ex.Message}", stackTrace = ex.StackTrace });
            //}
        }

        //[HttpPut] // ginagamit lang sa mga restful api 
        [HttpPost] //ginagamit parin ang post kagit sa pag update sa mvc kase ang form is only support post and get
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher(string id, EditTeacherViewModel model)//After Edit - Submit(ViewModel → Entity) :

        {
            //var editTeacher = await context.Users.FindAsync(id);
            var editTeacher = await userManager.FindByIdAsync(id.ToString()); //get entity from database

            if(editTeacher == null)
            {
                return Json(new { success = false, message = "Teacher not found" });
            }
            //check for email duplication
            bool sameEmail = await context.Users.AnyAsync(e => e.Email == model.Email && e.Id != id);

            if (sameEmail)
            {
                ModelState.AddModelError("Email", "Email is already used!");
            }

            //duplicate check excluding self
            //Dito gumamit ng s.Id != id para pag nag check ng id is hindi isasama yung current id sa pag hahanap
            //check for Schoold Id Duplication
            bool schoolIdExisted = await context.Users.AnyAsync(s => s.SchoolId == model.SchoolId && s.Id != id);

            if (schoolIdExisted)
            {
                ModelState.AddModelError("SchoolId", "School Id is already taken!");
            }

            //Check for Employee Id duplication
            bool employeeNoExisted = await context.Users.AnyAsync(e => e.EmployeeId == model.EmployeeId && e.Id != id);
            if (employeeNoExisted)
            {
                ModelState.AddModelError("EmployeeId", "Employee Id is already taken!");
            }

            //Check if Full name duplication
            bool FullNameExisted = await context.Users.AnyAsync(f => f.FirstName == model.FirstName && f.MiddleName == model.MiddleName && f.LastName == model.LastName && f.Id != id);
            if (FullNameExisted)
            {
                ModelState.AddModelError("FirstName", "A teacher with this Full name is already existed");
                ModelState.AddModelError("MiddleName", " ");
                ModelState.AddModelError("LastName", " ");
            }

            if (!ModelState.IsValid)
            {
                ViewData["imageFileData"] = editTeacher.imageFileData;
                ViewData["imageFilePath"] = editTeacher.imageFilePath;
                ViewData["CreatedAt"] = editTeacher.CreatedAt.ToString("MM/dd/yyyy");

                var errors = ModelState.ToDictionary(
                                                                       kvp => kvp.Key,
                                                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                                                                   );

                return Json(new { success = false, errors = errors });
            }

            string? saveImagePath = null;
            byte[]? saveImageData = null;

            if(model.imageFile != null)
            {
                string newFile = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                newFile += Path.GetExtension(model.imageFile.FileName);

                string imageFullPath = environment.WebRootPath + "/ProfilePic/" + newFile;

                using(var stream = System.IO.File.Create(imageFullPath))
                {
                    await model.imageFile.CopyToAsync(stream);
                }

                //check muna sa database if may laman ba yung image ng user
                if (!string.IsNullOrEmpty(editTeacher.imageFilePath))
                {
                    //if may laman saka palang bubuuin ang filepath
                    string oldImageFullPath = environment.WebRootPath + "/ProfilePic/" + editTeacher.imageFilePath;
                    //tapos kapag may laman nga, dun palang mag delete
                    if (oldImageFullPath != null)
                    {
                        //then mag execute to!
                        System.IO.File.Delete(oldImageFullPath);
                    }

                }

                saveImagePath = newFile;

                using(var inputStream = model.imageFile.OpenReadStream())
                using(var memoryStream = new MemoryStream())
                {
                    await inputStream.CopyToAsync(memoryStream);
                    saveImageData = memoryStream.ToArray();
                }
                //IMPORTANT: Assign to editTeacher
                editTeacher.imageFilePath = saveImagePath;
                editTeacher.imageFileData = saveImageData;
            }
            //Map ViewModel -> Entity(update existing entity)
            editTeacher.Email = model.Email; //From ViewModel To Entity
            editTeacher.SchoolId = model.SchoolId;
            editTeacher.EmployeeId = model.EmployeeId;
            editTeacher.FirstName = model.FirstName;
            editTeacher.MiddleName = model.MiddleName;
            editTeacher.LastName = model.LastName;
            editTeacher.Sex = model.Sex;
            editTeacher.positionTitle = model.positionTitle;

            var result = await userManager.UpdateAsync(editTeacher);

            if (!result.Succeeded)
            {
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                //return PartialView("_EditTeacherPartial", model);
                var errors = ModelState.ToDictionary(
                                                                       kvp => kvp.Key,
                                                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                                                                   );

                return Json(new { success = false, errors = errors });
            }

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var removePassword = await userManager.RemovePasswordAsync(editTeacher);
                if (removePassword.Succeeded)
                {
                    var addPassword = await userManager.AddPasswordAsync(editTeacher, model.NewPassword);
                    if (!addPassword.Succeeded)
                    {
                        foreach(var error in addPassword.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        //parehas silang babalik sa form , ang pinagkaibahan lang is:
                        //return View(model); // eto babalik sa same page 
                        //return PartialView("_EditTeacherPartial", model); // eto babalik sa same form kase naka modal yung form
                        var errors = ModelState.ToDictionary(
                                                       kvp => kvp.Key,
                                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                                                   );

                        return Json(new { success = false, errors = errors });
                    }
                }
            }

            //No need ng gamitin ang SaveChangesAsync() kase Ang UserManager.UpdateAsync(), RemovePasswordAsync(), at AddPasswordAsync() ay automatically nag-save na sa database.
            //await context.SaveChangesAsync();

            return Json(new { success = true, message = "User Updated Successfully!" });// babalik na sa teacher list table kase walang error and success na sya!

        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            
            var teacher = await context.Users.FindAsync(id);

            if(teacher == null)
            {
                //return RedirectToAction("TeacherList", "Admin");
                return Json(new { success = false, error = "Teacer does not found" });
            }
            //check kung may laman yung image yung user
            if (!string.IsNullOrEmpty(teacher.imageFilePath))
            {
                //string ImagePath = environment.WebRootPath + "/ProfilePic/" + teacher.imageFilePath;
                                   //Path.Combine, static method within System.IO.Path
                string ImagePath = Path.Combine(environment.WebRootPath, "ProfilePic", teacher.imageFilePath);// si Path.Combine is gumagamit ng correct directorty seprator para imbis na "/ProfilePic/ anggamitin is sya na mismo ang bahala kase minsan may mga dobleng slash, kaya pwedeng mag error!
                //check if existing  paba talaga sa ProfilePic yung file
                if (System.IO.File.Exists(ImagePath))
                {
                    System.IO.File.Delete(ImagePath);
                }
            }

            context.Users.Remove(teacher);
            await context.SaveChangesAsync();

            //return RedirectToAction("TeacherList", "Admin");
            return Json(new { success = true, message = "Teacher has been Deleted successfully" }); //JSON store and transport data from server side to client side

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSecretary(SecretaryViewModel model)
        {
            bool schoolIdExisted = await context.Users.AnyAsync(s => s.SchoolId == model.SchoolId);
            if (schoolIdExisted)
            {
                ModelState.AddModelError("SchoolId", "School Id is already taken!");
            }

            bool fullNameExisted = await context.Users.AnyAsync(f => f.FirstName == model.FirstName && f.MiddleName == model.MiddleName && f.LastName == model.LastName);
            if (fullNameExisted)
            {
                ModelState.AddModelError("FirstName", "A secretary with this Full name is already existed");
                ModelState.AddModelError("MiddleName", "");
                ModelState.AddModelError("LastName", "");
            }

            if (!ModelState.IsValid)
            {
                var overallErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = overallErrors });
            }

            string? saveImagePath = null;
            //saveImageData is yung actual data ng image, na mag sesave sa imageFileData row sa database
            byte[]? saveImageData = null; //ginagamit ang byte para magsave ng files tulad ng images, pdf, etc. sa loob ng database

            if(model.imageFile != null)
            {
                //Konektado ito Sa AppUser na object para talagang magsave

                //create filename
                string newFile = DateTime.Now.ToString("yyyyMMddHHmmssff");
                newFile += Path.GetExtension(model.imageFile.FileName);
                //create physical path for the image
                string imageFullPath = environment.WebRootPath + "/ProfilePic/" + newFile;
                //save the actual image to ProfilePic file na naka declare sa variable na imageFullPath
                using(var stream = System.IO.File.Create(imageFullPath))
                {
                    await model.imageFile.CopyToAsync(stream);
                }
                //eto yung part na pag kasave nung actual image na (ex. image.jpg) eh pupunta na sya database sa imageFilePath na row
                saveImagePath = newFile;

                //Itong buong code na ito gang baba, dito kukunin yung mismong data ng image para iconvert sa byte para i-save na sa database
                //Kase diba sa viewmodel ko is IFormFile gamit ko dun, si ang code na ito is iconvert ang iFormFile to byte na iinput ng user
                using(var inputStream = model.imageFile.OpenReadStream())//para basahin yung upload file

                using(var memoryStream = new MemoryStream())// maging temporary kolektor o lalagyan ng data
                {
                    await inputStream.CopyToAsync(memoryStream);// eto na yung may hawak ng raw data
                    saveImageData = memoryStream.ToArray();// ngayon after makollect ng mismong data na naprocess na, dun na icoconvert sa byte
                }
            }

            AppUser secretary = new AppUser()
            {
                Email = model.Email,
                UserName = model.Email,
                SchoolId = model.SchoolId,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                Sex = model.Sex,
                imageFileData = saveImageData,
                imageFilePath = saveImagePath,
                CreatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(secretary, model.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(secretary, "Secretary");
                return Json(new { success = true, message = "Secretary Added Successfully!" });
            }
            else
            {
                foreach(var error in result.Errors)
                {
                    if (error.Code == "DuplicateUserName")
                    {
                        ModelState.AddModelError("Email", "Email is already used!");
                    } else if (error.Description.Contains("Password"))
                    {
                        ModelState.AddModelError("Password", error.Description);
                    }
                    else
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );

                return Json(new { success = false, errors = errors });
            }

        }

        [HttpGet]
        public async Task<IActionResult> ViewSecretary(string id)
        {
            var secretary = await context.Users.FindAsync(id);

            if(secretary == null)
            {
                return Json(new { success = false, error = "Secretary Not Found!" });
            }

            var model = new EditSecretaryViewModel()
            {
                Email = secretary.Email,
                SchoolId = secretary.SchoolId,
                FirstName = secretary.FirstName,
                MiddleName = secretary.MiddleName,
                LastName = secretary.LastName,
                Sex = secretary.Sex,
                imageFilePath = secretary.imageFilePath,
                CreatedAt = secretary.CreatedAt,
            };

            return PartialView("_ViewSecretaryPartial", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditSecretary(string id)
        {
            var secretary = await context.Users.FindAsync(id);

            if(secretary == null)
            {
                return Json(new { success = false, error = "Secretary does not found" });
            }

            var model = new EditSecretaryViewModel()
            {
                Email = secretary.Email,
                SchoolId = secretary.SchoolId,
                FirstName = secretary.FirstName,
                MiddleName = secretary.MiddleName,
                LastName = secretary.LastName,  
                Sex = secretary.Sex,
                imageFilePath = secretary.imageFilePath,
                CreatedAt = secretary.CreatedAt,
            };

            return PartialView("_EditSecretaryPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSecretary(string id, EditSecretaryViewModel model)
        {
            //var editSecretary = await context.Users.FindAsync(id);
            var editSecretary = await userManager.FindByIdAsync(id.ToString());

            if (editSecretary == null)
            {
                return Json(new { success = false, error = "Secretary does not found!" });
            }

            //check for email duplication
            bool sameEmail = await context.Users.AnyAsync(e => e.Email == model.Email && e.Id != id);

            if (sameEmail)
            {
                ModelState.AddModelError("Email", "Email is already used!");
            }

            //duplicate check excluding self
            //Dito gumamit ng s.Id != id para pag nag check ng id is hindi isasama yung current id sa pag hahanap
            //check for Schoold Id Duplication
            bool schoolIdExisted = await context.Users.AnyAsync(s => s.SchoolId == model.SchoolId && s.Id != id);

            if (schoolIdExisted)
            {
                ModelState.AddModelError("SchoolId", "School Id is already taken!");
            }

            //Check if Full name duplication
            bool FullNameExisted = await context.Users.AnyAsync(f => f.FirstName == model.FirstName && f.MiddleName == model.MiddleName && f.LastName == model.LastName && f.Id != id);
            if (FullNameExisted)
            {
                ModelState.AddModelError("FirstName", "A secretary with this Full name is already existed");
                ModelState.AddModelError("MiddleName", " ");
                ModelState.AddModelError("LastName", " ");
            }

            if (!ModelState.IsValid)
            {

                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                );
                return Json(new { sucess = false, errors = errors });
            }

            string? saveImagePath = null;
            byte[]? saveImageData = null;

            if (model.imageFile != null)
            {
                string newFile = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                newFile += Path.GetExtension(model.imageFile.FileName);

                string imageFullPath = environment.WebRootPath + "/ProfilePic/" + newFile;

                using (var stream = System.IO.File.Create(imageFullPath))
                {
                    await model.imageFile.CopyToAsync(stream);
                }

                //check muna sa database if may laman ba yung image ng user
                if (!string.IsNullOrEmpty(editSecretary.imageFilePath))
                {
                    //if may laman saka palang bubuuin ang filepath
                    string oldImageFullPath = environment.WebRootPath + "/ProfilePic/" + editSecretary.imageFilePath;
                    //tapos kapag may laman nga, dun palang mag delete
                    if (oldImageFullPath != null)
                    {
                        //then mag execute to!
                        System.IO.File.Delete(oldImageFullPath);
                    }

                }

                saveImagePath = newFile;

                using (var inputStream = model.imageFile.OpenReadStream())
                using (var memoryStream = new MemoryStream())
                {
                    await inputStream.CopyToAsync(memoryStream);
                    saveImageData = memoryStream.ToArray();
                }
                //IMPORTANT: Assign to editTeacher
                editSecretary.imageFilePath = saveImagePath;
                editSecretary.imageFileData = saveImageData;
            }

            editSecretary.Email = model.Email;
            editSecretary.SchoolId = model.SchoolId;
            editSecretary.FirstName = model.FirstName;
            editSecretary.MiddleName = model.MiddleName;
            editSecretary.LastName = model.LastName;
            editSecretary.Sex = model.Sex;

            var result = await userManager.UpdateAsync(editSecretary);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                //return PartialView("_EditTeacherPartial", model);
                var errors = ModelState.ToDictionary(
                                                                       kvp => kvp.Key,
                                                                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                                                                   );
                return Json(new { success = false, errors = errors });
            }

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var removePassword = await userManager.RemovePasswordAsync(editSecretary);
                if (removePassword.Succeeded)
                {
                    var newPassword = await userManager.AddPasswordAsync(editSecretary, model.NewPassword);
                    if (!newPassword.Succeeded)
                    {
                        foreach(var error in newPassword.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        var errors = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                        );
                        return Json(new { success = false, errors = errors });
                    }
                }
            }

            // No need ng gamitin ang SaveChangesAsync() kase Ang UserManager.UpdateAsync(), RemovePasswordAsync(), at AddPasswordAsync() ay automatically nag - save na sa database.
            //await context.SaveChangesAsync();
            return Json(new { success = true, message = "Secretary Updated Successfully" });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSecretary(string id)
        {
            var secretary = await userManager.FindByIdAsync(id);

            if(secretary == null)
            {
                return Json(new { success = false, error = "Secretary does not Found!" });
            }

            if (!string.IsNullOrEmpty(secretary.imageFilePath))
            {
                string ImagePath = Path.Combine(environment.WebRootPath, "ProfilePic", secretary.imageFilePath);
                if (System.IO.File.Exists(ImagePath))
                {
                    System.IO.File.Delete(ImagePath);
                }
            }

            context.Users.Remove(secretary);
            await context.SaveChangesAsync();

            return Json(new { sucesss = true, message = "Secretary Deleted Successfully!" });
        }
       
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login", "Login");

        }
    }

    
}
