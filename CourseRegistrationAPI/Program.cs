using BusinessLayer.Services;
using DataAccessLayer.Data;
using DataAccessLayer.Repositories;
using EntityLayer.Interfaces;
using EntityLayer.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// 2. Register Entity Framework Core with SQL Server
builder.Services.AddDbContext<CourseDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Register Repository Dependency Injection
builder.Services.AddScoped<ICourseRepo, CourseRepo>();

// Register Business Layer Service
builder.Services.AddScoped<RegistrationService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();



// Auto-seed test data for testing
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CourseDbContext>();

    if (!db.Courses.Any(c => c.Id == 1))
    {
        db.Courses.Add(new Course { Id = 1, Title = "Introduction to C#", Capacity = 2, RequiredPrerequisite = "" });
    }

    if (!db.Learners.Any(l => l.Id == "1"))
    {
        db.Learners.Add(new Learner { Id = "1", Name = "Alice", CompletedPrerequisites = new List<string>() });
    }

    db.SaveChanges();
}

app.Run();


app.Run();
