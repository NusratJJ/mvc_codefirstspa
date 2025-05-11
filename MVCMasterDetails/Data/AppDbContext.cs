using MVCMasterDetails.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace MVCMasterDetails.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext():base("AppDbContext"){}
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseModule> CourseModules { get; set; }
    }
}