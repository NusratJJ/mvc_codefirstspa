using MVCMasterDetails.Data;
using MVCMasterDetails.Models;
using MVCMasterDetails.ViewModels;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVCMasterDetails.Controllers{
    public class StudentsController : Controller
    {
        AppDbContext db = new AppDbContext();
        [HttpGet]
        public ActionResult Index(string sortOrder,string currentFilter, string searchString,int? page)
        {
            ViewBag.NameSortParam = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParam = sortOrder == "Date" ? "date_desc" : "Date";
            IEnumerable<Student> students =
            db.Students.Include(c => c.Course).Include(c => c.CourseModules).ToList();
            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            ViewBag.currentFilter=searchString;
            if (!String.IsNullOrEmpty(searchString))
            {

                students = students.Where(s => s.StudentName.ToUpper().Contains(searchString.ToUpper()));
            }
            switch (sortOrder)
            {
                case "name_desc":
                    students = students.OrderByDescending(s => s.StudentName);
                    break;
                case "Date":
                    students = students.OrderBy(s => s.AdmissionDate);
                    break;
                case "date_desc":
                    students = students.OrderByDescending(s => s.AdmissionDate);
                    break;
                default:
                    students = students.OrderBy(s => s.StudentName);
                    break;
            }
            int pageSize = 1;
            int pageNumber = (page ?? 1);
            return View(students.ToPagedList(pageNumber,pageSize));
        }
        public JsonResult IsMobileNoAvailAble(string MobileNo)
        {
            return Json(db.Students.Any(s => s.MobileNo == MobileNo), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            Student student = db.Students.Find(id);
            if (student != null)
            {
                var modules = db.CourseModules.Where(s => s.StudentId == id).ToList();
                db.CourseModules.RemoveRange(modules);
            }
            db.Entry(student).State = EntityState.Deleted;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //[HttpPost]
        //[ActionName("Delete")]
        //public ActionResult DeleteConfirmed(int id)
        //{
        //    Student student = db.Students.Find(id);
        //    if (student != null)
        //    {
        //        var modules = db.CourseModules.Where(s => s.StudentId == id).ToList();
        //        db.CourseModules.RemoveRange(modules);
        //    }
        //    db.Entry(student).State = EntityState.Deleted;
        //    db.SaveChanges();
        //    return RedirectToAction("Index");
        //}
        [HttpGet]
        public ActionResult Create()
        {
            StudentViewModel student = new StudentViewModel();
            student.Courses = db.Courses.ToList();
            student.CourseModules.Add(new CourseModule() { CourseModuleId = 1 });
            return View(student);
        }

        [HttpPost]
        public ActionResult Create(StudentViewModel vObj)
        {
            if (!ModelState.IsValid)
            {
                vObj.Courses = db.Courses.ToList();
                return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }
            Student obj = new Student();
            if (vObj.ProfileFile != null)
            {
                HttpPostedFileBase file = vObj.ProfileFile;
                string fileName = GetFileName(file);
                obj.ImageUrl = fileName;
            }
            else
            {
                obj.ImageUrl = "noimage.png";
            }
            obj.StudentName = vObj.StudentName;
            obj.AdmissionDate = vObj.AdmissionDate;
            obj.MobileNo = vObj.MobileNo;
            obj.IsEnrolled = vObj.IsEnrolled;
            obj.CourseId = vObj.CourseId;
            obj.CourseModules = new List<CourseModule>();
            if (vObj.CourseModules != null)
            {
                foreach (var moduleViewModel in vObj.CourseModules)
                {
                    if (!string.IsNullOrEmpty(moduleViewModel.ModuleName) && moduleViewModel.Duration > 0)
                    {
                        var module = new CourseModule
                        {
                            ModuleName = moduleViewModel.ModuleName,
                            Duration = moduleViewModel.Duration,
                            StudentId = obj.StudentId
                        };
                        obj.CourseModules.Add(module);
                    }
                }
            }
            db.Students.Add(obj);
            try
            {
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving the student.");
                vObj.Courses = db.Courses.ToList();
                return Json(new { success = false, errors = new[] { "An error occurred while saving the student." } });
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
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var student = db.Students.Include(c => c.Course).Include(c => c.CourseModules).Where(s => s.StudentId == id).FirstOrDefault();
            if (student == null)
                return HttpNotFound("Student Not found");
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
            return View(vObj);
        }
        [HttpPost]
        public ActionResult Edit(StudentViewModel vobj, string OldImageUrl)
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
                var modulesToRemove = obj.CourseModules
                    .Where(existingModule => !vobj.CourseModules.Any(updatedModule =>
                        updatedModule.ModuleName == existingModule.ModuleName &&
                        updatedModule.Duration == existingModule.Duration))
                    .ToList();
                foreach (var moduleToRemove in modulesToRemove)
                {
                    db.CourseModules.Remove(moduleToRemove);
                }
                foreach (var item in vobj.CourseModules)
                {
                    if (!obj.CourseModules.Any(existingModule =>
                        existingModule.ModuleName == item.ModuleName &&
                        existingModule.Duration == item.Duration))
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
                    return Json(new { success = true });
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