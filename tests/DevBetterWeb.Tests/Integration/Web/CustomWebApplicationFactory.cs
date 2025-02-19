﻿using System;
using System.Threading.Tasks;
using DevBetterWeb.Core.Entities;
using DevBetterWeb.Core.Interfaces;
using DevBetterWeb.Infrastructure.Data;
using DevBetterWeb.Infrastructure.Identity.Data;
using DevBetterWeb.Web;
using DevBetterWeb.Web.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace DevBetterWeb.Tests.Integration.Web;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureServices(services =>
    {
        // Create a new service provider.
        var serviceProvider = new ServiceCollection()
          .AddEntityFrameworkInMemoryDatabase()
          .BuildServiceProvider();

        // Add a database context (AppDbContext) using an in-memory
        // database for testing.
        services.AddDbContext<AppDbContext>(options =>
      {
        options.UseInMemoryDatabase("InMemoryDbForTesting");
        options.UseInternalServiceProvider(serviceProvider);
      });

      services.AddScoped<IDomainEventDispatcher, NoOpDomainEventDispatcher>();

        // Build the service provider.
        var sp = services.BuildServiceProvider();

        // Create a scope to obtain a reference to the database
        // context (AppDbContext).
        using (var scope = sp.CreateScope())
      {
        var scopedServices = scope.ServiceProvider;
        var db = scopedServices.GetRequiredService<AppDbContext>();

        var logger = scopedServices
                  .GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

          // Ensure the database is created.
          db.Database.EnsureCreated();

        try
        {
            // Seed the database with test data.
            SeedData.PopulateTestData(db, scopedServices.GetRequiredService<UserManager<ApplicationUser>>());
        }
        catch (Exception ex)
        {
          logger.LogError(ex, $"An error occurred seeding the database with test messages. Error: {ex.Message}");
        }
      }
    });
  }
}
