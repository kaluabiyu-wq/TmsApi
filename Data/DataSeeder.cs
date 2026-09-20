using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

namespace Tms.Api.Persistence;

public static class DataSeeder
{
    private static readonly (string Code, string Title, int MaxCapacity)[] Courses =
    [
        ("CSE-101", "Web Development Fundamentals", 30),
        ("CSE-102", "TypeScript Essentials", 30),
        ("CSE-103", "Git and Collaborative Workflows", 25),
        ("CSE-201", "ASP.NET Core Fundamentals", 28),
        ("CSE-202", "Entity Framework Core and PostgreSQL", 28),
        ("CSE-203", "Building RESTful Web APIs", 28),
        ("CSE-301", "Advanced Web API Patterns", 24),
        ("CSE-302", "Angular Fundamentals", 26),
        ("CSE-303", "Angular Advanced", 24),
        ("CSE-304", "Full-Stack Integration", 22),
        ("CSE-305", "Testing and Quality Assurance", 22),
        ("CSE-306", "Security and Authentication", 20),
        ("DAT-101", "Database Design Foundations", 30),
        ("DAT-201", "Advanced SQL and Indexing", 26),
        ("DAT-202", "Data Modelling for the Web", 26),
        ("ARC-101", "Software Architecture Patterns", 22),
        ("ARC-201", "Cloud-Native Architecture", 22),
        ("DEV-101", "DevOps Foundations", 24),
        ("DEV-201", "Continuous Delivery Pipelines", 22),
        ("MOB-101", "Mobile App Foundations", 24),
        ("MOB-201", "Cross-Platform Mobile", 22),
        ("AI-101", "Applied Machine Learning", 20),
        ("AI-201", "Generative AI for Developers", 18),
        ("UX-101", "UX Research and Wireframing", 24),
        ("UX-201", "Design Systems and Tokens", 22),
    ];


       private static readonly (string RegistrationNumber, string Name, decimal Gpa, bool IsActive)[] Students =
[
    ("TMS-2026-0001", "Alice Smith", 3.8m, true),
    ("TMS-2026-0002", "Bob Jones", 2.9m, true),
    ("TMS-2026-0003", "Charlie Brown", 3.4m, false),
    ("TMS-2026-0004", "Diana Prince", 3.9m, true),
    ("TMS-2026-0005", "Evan Wright", 2.5m, true),
    ("TMS-2026-0006", "Abebe Kebede", 3.6m, true),
    ("TMS-2026-0007", "Tigist Alemu", 3.9m, true),
    ("TMS-2026-0008", "Dawit Mekonnen", 2.7m, true),
    ("TMS-2026-0009", "Hiwot Girma", 3.2m, false),
    ("TMS-2026-0010", "Yohannes Tesfaye", 3.5m, true),
    ("TMS-2026-0011", "Selamawit Bekele", 3.8m, true),
    ("TMS-2026-0012", "Bereket Haile", 2.6m, true),
    ("TMS-2026-0013", "Meron Assefa", 3.4m, true),
    ("TMS-2026-0014", "Nahom Wolde", 2.9m, false),
    ("TMS-2026-0015", "Rahel Tadesse", 3.7m, true),
    ("TMS-2026-0016", "Kaleb Fikru", 3.1m, true),
    ("TMS-2026-0017", "Eden Solomon", 3.9m, true),
    ("TMS-2026-0018", "Mikiyas Getachew", 2.8m, true),
    ("TMS-2026-0019", "Bethlehem Yilma", 3.3m, false),
    ("TMS-2026-0020", "Elias Demeke", 3.0m, true),
    ("TMS-2026-0021", "Sara Abera", 3.6m, true),
    ("TMS-2026-0022", "Yared Negash", 2.5m, true),
    ("TMS-2026-0023", "Feven Teshome", 3.8m, true),
    ("TMS-2026-0024", "Samuel Desta", 3.2m, false),
    ("TMS-2026-0025", "Lidya Worku", 3.5m, true),
    ("TMS-2026-0026", "Henok Ayele", 2.9m, true),
    ("TMS-2026-0027", "Martha Zewdu", 3.7m, true),
    ("TMS-2026-0028", "Yonas Berhanu", 3.1m, false),
    ("TMS-2026-0029", "Ruth Melaku", 3.9m, true),
    ("TMS-2026-0030", "Amanuel Sisay", 2.7m, true),

];
    

    public static async Task SeedAsync(TmSDbContext context, CancellationToken ct = default)
    {
        await context.Database.MigrateAsync(ct);

        if (!await context.Courses.AnyAsync(ct))
        {
            foreach (var (code, title, maxCapacity) in Courses)
            {
                context.Courses.Add(new Course
                {
                    Code = code,
                    Title = title,
                    MaxCapacity = maxCapacity
                });
            }
        }

        if (!await context.Students.AnyAsync(ct))
        {
            foreach (var (registrationNumber, name, gpa, isActive) in Students)
            {
                context.Students.Add(new Student
                {
                    RegistrationNumber = registrationNumber,
                    Name = name,
                    GPA = gpa,
                    IsActive = isActive
                });
            }
        }

        await context.SaveChangesAsync(ct);
    }
}