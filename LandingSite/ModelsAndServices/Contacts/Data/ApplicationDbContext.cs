using LandingPage.ModelsAndServices.Contacts.Models;
//using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LandingPage.ModelsAndServices.Contacts.Data;

public class ApplicationDbContext //: DbContext
{
    public ApplicationDbContext()
    {
        ContactMessages = new List<ContactMessage>();
        UserRegistrations = new List<UserRegistration>();
    }

    //public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    //: base(options)
    //{
    //}

    //public DbSet<ContactMessage> ContactMessages { get; set; }
    //public DbSet<UserRegistration> UserRegistrations { get; set; }

    public List<ContactMessage> ContactMessages { get; set; }
    public List<UserRegistration> UserRegistrations { get; set; }

    //protected override void OnModelCreating(ModelBuilder modelBuilder)
    //{
    //    modelBuilder.Entity<ContactMessage>()
    //        .HasIndex(m => m.Email);

    //    modelBuilder.Entity<UserRegistration>()
    //            .HasIndex(u => u.Email)
    //            .IsUnique();
    //}
}
