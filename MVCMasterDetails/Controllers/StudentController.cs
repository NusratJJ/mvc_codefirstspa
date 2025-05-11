using MVCMasterDetails.Data;
using MVCMasterDetails.Models;
using MVCMasterDetails.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVCMasterDetails.Controllers
{
    public class StudentController : Controller
    {
        private AppDbContext db = new AppDbContext();

        public ActionResult Index()
        {
            // Initial data for the list will be rendered server-side
            IEnumerable<Student> students =
                db.Students.Include(c => c.Course).Include(c => c.CourseModules).ToList();
            return View(students);
        }

        // Action to handle AJAX deletion
        [HttpPost]
        public JsonResult DeleteStudent(int id)
        {
            Student student = db.Students.Find(id);
            if (student != null)
            {
                var modules = db.CourseModules.Where(s => s.StudentId == id).ToList();
                db.CourseModules.RemoveRange(modules);
                db.Entry(student).State = EntityState.Deleted;
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Student not found." });
        }
        
        public ActionResult CreatePartial()
        {
            StudentViewModel student = new StudentViewModel();
            student.Courses = db.Courses.ToList();
            student.CourseModules.Add(new CourseModule() { CourseModuleId = 1 });
            return PartialView("_CreateStudentPartial", student);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateStudent(StudentViewModel vobj)
        {
            if (!ModelState.IsValid)
            {
                vobj.Courses = db.Courses.ToList();
                return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            Student student = new Student
            {
                StudentName = vobj.StudentName,
                AdmissionDate = vobj.AdmissionDate,
                MobileNo = vobj.MobileNo,
                CourseId = vobj.CourseId,
                IsEnrolled = vobj.IsEnrolled,
                // ... map other properties ...
                CourseModules = vobj.CourseModules // Assuming you handle module creation correctly
            };

            if (vobj.ProfileFile != null)
            {
                string uniqueFileName = GetFileName(vobj.ProfileFile);
                student.ImageUrl = uniqueFileName;
                // Save the file
            }
            else
            {
                student.ImageUrl = "noimage.png"; // Or your default image
            }

            db.Students.Add(student);
            try
            {
                db.SaveChanges();
                return Json(new { success = true, redirectUrl = Url.Action("Index") });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while creating the student.");
                vobj.Courses = db.Courses.ToList();
                return Json(new { success = false, errors = new[] { "An error occurred while creating the student." } });
            }
        }
        private string GetFileName(HttpPostedFileBase file)
        {
            string fileName = "";
            if (file != null)
            {
                fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string path = Path.Combine(Server.MapPath("~/images/"), fileName);
                file.SaveAs(path);
            }
            return fileName;
        }

        public ActionResult EditPartial(int id)
        {
            var student = db.Students.Include(c => c.Course).Include(c => c.CourseModules).Where(s => s.StudentId == id).FirstOrDefault();
            if (student == null)
            {
                return HttpNotFound("Student Not found");
            }
            var vObj = new StudentViewModel
            {
                StudentName = student.StudentName,
                StudentId = student.StudentId,
                AdmissionDate = student.AdmissionDate,
                MobileNo = student.MobileNo,
                CourseId = student.CourseId,
                IsEnrolled = student.IsEnrolled,
                ImageUrl = student.ImageUrl,
                CourseModules = student.CourseModules.ToList(),
                Courses = db.Courses.ToList()
            };
            return PartialView("_EditStudentPartial", vObj);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EditStudent(StudentViewModel vobj, string OldImageUrl)
        {
            if (!ModelState.IsValid)
            {
                vobj.Courses = db.Courses.ToList();
                return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            Student obj = db.Students
                .Include(a => a.CourseModules)
                .FirstOrDefault(x => x.StudentId == vobj.StudentId);

            if (obj != null)
            {
                obj.StudentName = vobj.StudentName;
                obj.CourseId = vobj.CourseId;
                obj.MobileNo = vobj.MobileNo;
                obj.IsEnrolled = vobj.IsEnrolled;
                obj.AdmissionDate = vobj.AdmissionDate;

                if (vobj.ProfileFile != null)
                {
                    string uniqueFileName = GetFileName(vobj.ProfileFile);
                    obj.ImageUrl = uniqueFileName;
                }
                else
                {
                    obj.ImageUrl = OldImageUrl;
                }

                // Identify modules to remove based on CourseModuleId
                var existingModuleIds = obj.CourseModules.Select(m => m.CourseModuleId).ToList();
                var updatedModuleIds = vobj.CourseModules.Where(m => m.CourseModuleId > 0).Select(m => m.CourseModuleId).ToList();
                var modulesToRemove = obj.CourseModules.Where(m => !updatedModuleIds.Contains(m.CourseModuleId)).ToList();

                foreach (var moduleToRemove in modulesToRemove)
                {
                    db.CourseModules.Remove(moduleToRemove);
                }

                // Add or update modules
                foreach (var item in vobj.CourseModules)
                {
                    if (item.CourseModuleId > 0) // Existing module, update it
                    {
                        var existingModule = obj.CourseModules.FirstOrDefault(m => m.CourseModuleId == item.CourseModuleId);
                        if (existingModule != null)
                        {
                            existingModule.ModuleName = item.ModuleName;
                            existingModule.Duration = item.Duration;
                        }
                    }
                    else // New module, add it
                    {
                        var newModule = new CourseModule
                        {
                            StudentId = obj.StudentId,
                            ModuleName = item.ModuleName,
                            Duration = item.Duration
                        };
                        obj.CourseModules.Add(newModule);
                    }
                }

                try
                {
                    db.SaveChanges();
                    return Json(new { success = true, redirectUrl = Url.Action("Index") });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving the student.");
                    vobj.Courses = db.Courses.ToList();
                    return Json(new { success = false, errors = new[] { "An error occurred while saving the student." } });
                }
            }
            return Json(new { success = false, errors = new[] { "Student not found." } });
        }
    }
}