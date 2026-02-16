using System.Linq;
using Microsoft.EntityFrameworkCore;
using NorthwindModel.Context;
using NorthwindModel.Models;
using Xunit;

namespace NorthwindTestDesign.Tests;

public class NorthwindContextSmokeTests(ITestOutputHelper output) {
    [Fact]
    public async Task Reads_one_record_from_localhost_northwind_context() {
        await using var context = new NorthwindEntities();

        var employee = await context.Employees.AsNoTracking().FirstOrDefaultAsync();
        output.WriteLine($"Connected. Employee record read? {(employee is null ? "no" : $"yes ({employee.LastName}, {employee.FirstName})")}");

        Assert.NotNull(employee);
    }

    [Fact]
    public async Task Updates_or_creates_employee_brief_record() {
        await using var context = new NorthwindEntities();

        var employeeBrief = await context.EmployeeBriefs.FirstOrDefaultAsync();
        if (employeeBrief is null) {
            var newEmployee = await CreateAndSaveEmployeeAsync(context);
            employeeBrief = await CreateEmployeeBriefForEmployeeAsync(context, newEmployee);
        }

        var originalFirstName = employeeBrief.FirstName;
        var firstNameMaxLength = context.Model.FindEntityType(typeof(EmployeeBrief))
            ?.FindProperty(nameof(EmployeeBrief.FirstName))
            ?.GetMaxLength() ?? int.MaxValue;
        employeeBrief.FirstName = MutateWithinMax(originalFirstName, firstNameMaxLength);
        await SaveChangesWithStringTruncationAsync(context);

        var reloadedBrief = await context.EmployeeBriefs
            .SingleAsync(x => x.EmployeeID == employeeBrief.EmployeeID);

        output.WriteLine($"EmployeeBrief updated: {originalFirstName} -> {reloadedBrief.FirstName}");
        Assert.NotEqual(originalFirstName, reloadedBrief.FirstName);
    }

    [Fact]
    public async Task Updates_or_creates_full_employee_record() {
        await using var context = new NorthwindEntities();

        var employee = await context.Employees.FirstOrDefaultAsync();
        if (employee is null) {
            employee = await CreateAndSaveEmployeeAsync(context);
        }

        var originalLastName = employee.LastName;
        var lastNameMaxLength = context.Model.FindEntityType(typeof(Employee))
            ?.FindProperty(nameof(Employee.LastName))
            ?.GetMaxLength() ?? int.MaxValue;
        employee.LastName = MutateWithinMax(originalLastName, lastNameMaxLength);
        await SaveChangesWithStringTruncationAsync(context);

        var reloadedEmployee = await context.Employees
            .SingleAsync(x => x.EmployeeID == employee.EmployeeID);

        output.WriteLine($"Employee updated: {originalLastName} -> {reloadedEmployee.LastName}");
        Assert.NotEqual(originalLastName, reloadedEmployee.LastName);
    }

    private static string RandomSuffix() => Guid.NewGuid().ToString("N")[..8];

    private static string MutateWithinMax(string? current, int maxLength) {
        current ??= string.Empty;

        if (maxLength <= 0) return current;
        if (current.Length == 0) return "X";
        if (current.Length < maxLength) return current + "X";

        var replacement = current[^1] == 'X' ? 'Y' : 'X';
        return current[..^1] + replacement;
    }

    private static async Task<Employee> CreateAndSaveEmployeeAsync(NorthwindEntities context) {
        var suffix = RandomSuffix();
        var employee = new Employee {
            LastName = $"TestLast{suffix}",
            FirstName = $"TestFirst{suffix}",
            Title = "Mr.",
            TitleOfCourtesy = "Mr.",
            Address = "123 Test Street",
            City = "Test City",
            Region = "NA",
            PostalCode = "12345",
            Country = "USA",
            HomePhone = "000-000-0000",
            Extension = "000",
            BirthDate = DateTime.UtcNow.AddYears(-30),
            HireDate = DateTime.UtcNow,
            Photo = [],
            Notes = "Created by tests",
            PhotoPath = "https://example.com/photo.png"
        };

        context.Employees.Add(employee);
        await SaveChangesWithStringTruncationAsync(context);
        return employee;
    }

    private static async Task<EmployeeBrief> CreateEmployeeBriefForEmployeeAsync(NorthwindEntities context, Employee employee) {
        var suffix = RandomSuffix();
        var employeeBrief = new EmployeeBrief {
            FirstName = $"{employee.FirstName}-Brief-{suffix}",
            LastName = $"{employee.LastName}-Brief-{suffix}",
            Title = "Brief Title"
        };
        employeeBrief.Employee = employee;

        context.EmployeeBriefs.Add(employeeBrief);
        await SaveChangesWithStringTruncationAsync(context);
        return employeeBrief;
    }

    private static async Task SaveChangesWithStringTruncationAsync(NorthwindEntities context) {
        TruncateStringsToConfiguredLengths(context);
        await context.SaveChangesAsync();
    }

    private static void TruncateStringsToConfiguredLengths(DbContext context) {
        foreach (var entry in context.ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified)) {
            foreach (var property in entry.Properties
                         .Where(p => p.Metadata.ClrType == typeof(string) && p.CurrentValue is string)) {
                if (property.CurrentValue is not string currentValue) continue;

                var maxLength = property.Metadata.GetMaxLength();
                if (!maxLength.HasValue || maxLength.Value <= 0 || currentValue.Length <= maxLength.Value) continue;

                property.CurrentValue = currentValue[..maxLength.Value];
            }
        }
    }
}
