using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace TmsApi.Infrastructure.Persistence;

public static class DataSeeder
{
 private static readonly (string Code, string Title, int MaxCapacity, string Description)[] Courses =
[
    ("CSE-101", "Web Development Fundamentals", 30,
        "Introduces the core building blocks of the web: HTML structure, CSS styling and layout, and JavaScript for interactivity. Students leave with a working understanding of how browsers render pages and how the three languages fit together."),
    ("CSE-102", "TypeScript Essentials", 30,
        "Covers static typing on top of JavaScript, including interfaces, generics, union types, and type narrowing. Focuses on how strong typing catches bugs earlier and makes larger codebases easier to navigate and refactor."),
    ("CSE-103", "Git and Collaborative Workflows", 25,
        "Teaches version control fundamentals — branching, merging, rebasing, and resolving conflicts — alongside team workflows like pull requests and code review. Emphasizes habits that keep a shared codebase clean and traceable."),
    ("CSE-201", "ASP.NET Core Fundamentals", 28,
        "Builds a foundation in the ASP.NET Core framework: routing, controllers, middleware pipelines, and dependency injection. Students build their first working web API from scratch."),
    ("CSE-202", "Entity Framework Core and PostgreSQL", 28,
        "Explores data access with EF Core against a PostgreSQL database, covering migrations, LINQ queries, change tracking, and common pitfalls like the N+1 query problem."),
    ("CSE-203", "Building RESTful Web APIs", 28,
        "Focuses on designing clean, predictable APIs: resource modeling, HTTP verbs and status codes, request validation, and consistent error handling across endpoints."),
    ("CSE-301", "Advanced Web API Patterns", 24,
        "Goes beyond basic CRUD into API versioning, HATEOAS and hypermedia links, pagination strategies, and rate limiting — the patterns that keep large APIs maintainable as they grow."),
    ("CSE-302", "Angular Fundamentals", 26,
        "Introduces the Angular framework: components, templates, data binding, and services. Students build a small standalone-component app and connect it to a live backend API."),
    ("CSE-303", "Angular Advanced", 24,
        "Covers reactive forms, custom directives, RxJS observables and operators, and advanced routing with guards and resolvers, for building richer, more dynamic Angular applications."),
    ("CSE-304", "Full-Stack Integration", 22,
        "Connects the Angular frontend to the ASP.NET Core backend end-to-end: authentication flows, shared DTOs, error handling across layers, and deploying both halves together."),
    ("CSE-305", "Testing and Quality Assurance", 22,
        "Introduces unit, integration, and end-to-end testing across the stack, along with mocking, test coverage, and the mindset of writing testable code from the start."),
    ("CSE-306", "Security and Authentication", 20,
        "Covers authentication and authorization patterns — JWTs, cookies, role-based access control — plus common vulnerabilities like injection and XSS, and how to defend against them."),
    ("DAT-101", "Database Design Foundations", 30,
        "Teaches relational database fundamentals: normalization, primary and foreign keys, entity-relationship modeling, and writing well-structured schemas from a set of requirements."),
    ("DAT-201", "Advanced SQL and Indexing", 26,
        "Digs into query optimization, execution plans, indexing strategies, and window functions, giving students the tools to diagnose and fix slow queries in production databases."),
    ("DAT-202", "Data Modelling for the Web", 26,
        "Focuses on shaping data specifically for web applications: designing schemas that map cleanly to API responses, handling relationships across services, and planning for schema evolution."),
    ("ARC-101", "Software Architecture Patterns", 22,
        "Surveys foundational architecture patterns — layered architecture, clean architecture, CQRS — and the tradeoffs each makes between simplicity and long-term flexibility."),
    ("ARC-201", "Cloud-Native Architecture", 22,
        "Covers designing systems for the cloud: containerization, service boundaries, horizontal scaling, and resilience patterns like caching and circuit breakers."),
    ("DEV-101", "DevOps Foundations", 24,
        "Introduces the DevOps mindset and toolchain: CI basics, containerizing applications with Docker, environment configuration, and automating repetitive build and deploy tasks."),
    ("DEV-201", "Continuous Delivery Pipelines", 22,
        "Builds full CI/CD pipelines from commit to deployment, covering automated testing gates, staged environments, and rollback strategies for safer releases."),
    ("MOB-101", "Mobile App Foundations", 24,
        "Introduces mobile development concepts: platform lifecycles, responsive layouts for varying screen sizes, and the constraints that make mobile apps different from web apps."),
    ("MOB-201", "Cross-Platform Mobile", 22,
        "Covers building a single codebase that runs on iOS and Android, including native module bridging, platform-specific UI adjustments, and performance tuning."),
    ("AI-101", "Applied Machine Learning", 20,
        "Introduces practical machine learning: training and evaluating models on real datasets, feature engineering, and recognizing when a problem actually calls for ML versus simpler logic."),
    ("AI-201", "Generative AI for Developers", 18,
        "Explores building applications on top of large language models: prompt design, retrieval-augmented generation, function calling, and handling model output safely in production code."),
    ("UX-101", "UX Research and Wireframing", 24,
        "Covers user research methods, interviewing, and translating findings into low-fidelity wireframes — building the habit of designing for user needs before writing any code."),
    ("UX-201", "Design Systems and Tokens", 22,
        "Teaches how to build and maintain a design system: reusable components, design tokens for color and spacing, and keeping a product visually consistent as it scales."),
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
    ("TMS-2026-0031", "Betelhem Alemayehu", 3.6m, true),
    ("TMS-2026-0032", "Getachew Mamo", 2.8m, false),
    ("TMS-2026-0033", "Frehiwot Shiferaw", 3.9m, true),
    ("TMS-2026-0034", "Abenezer Tsegaye", 3.0m, true),
    ("TMS-2026-0035", "Alemitu Gebre", 2.6m, true),
    ("TMS-2026-0036", "Tewodros Belay", 3.4m, false),
    ("TMS-2026-0037", "Hanna Fantahun", 3.8m, true),
    ("TMS-2026-0038", "Girum Endale", 2.9m, true),
    ("TMS-2026-0039", "Mahlet Kassahun", 3.5m, true),
    ("TMS-2026-0040", "Solomon Aklilu", 3.1m, false),

];


    public static async Task SeedAsync(TmsDbContext context, CancellationToken ct = default)
    {
        await context.Database.MigrateAsync(ct);

        if (!await context.Courses.AnyAsync(ct))
        {
            foreach (var (code, title, maxCapacity, description) in Courses)
            {
                context.Courses.Add(new Course
                {
                    Code = code,
                    Title = title,
                    MaxCapacity = maxCapacity,
                    Description = description
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