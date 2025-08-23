using System.Linq.Expressions;
using Idevs.Foundation.EntityFramework.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.Tests.Utilities;

/// <summary>
/// Test implementation of RepositoryBase for testing purposes
/// </summary>
public class TestRepository(DbContext dbContext, ILogger<RepositoryBase<TestProduct, int>> logger)
    : RepositoryBase<TestProduct, int>(dbContext, logger)
{
    public static TestRepository Create(DbContext dbContext, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<RepositoryBase<TestProduct, int>>();
        return new TestRepository(dbContext, logger);
    }

    // Expose protected methods for testing
    public string TestDetectDatabaseProvider() => DetectDatabaseProvider();

    public string TestMapGraphQlFieldToProperty(string fieldName) => MapGraphQlFieldToProperty(fieldName);

    // Make MapGraphQlFieldToProperty accessible for testing
    private static string MapGraphQlFieldToProperty(string fieldName)
    {
        // Convert GraphQL camelCase field names to PascalCase property names
        return fieldName switch
        {
            "name" => "Name",
            "price" => "Price",
            "category" => "Category",
            "id" => "Id",
            "isActive" => "IsActive",
            _ => char.ToUpper(fieldName[0]) + fieldName[1..] // Convert first char to uppercase
        };
    }
}
