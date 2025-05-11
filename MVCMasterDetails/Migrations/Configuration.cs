namespace MVCMasterDetails.Migrations
{
    using MVCMasterDetails.Models;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<MVCMasterDetails.Data.AppDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(MVCMasterDetails.Data.AppDbContext context)
        {
           // var courses = new List<Course>
           //{
           //    new Course {CourseName="C#" },
           //    new Course {CourseName="J2EE" },
           //    new Course {CourseName="NT" }
           //};
           // courses.ForEach(s => context.Courses.AddOrUpdate(p => p.CourseName,s));
           // context.SaveChanges();
        }
    }
}
