# Entity CQRS Usage Examples

This document provides comprehensive examples of how to use the Entity CQRS components from your real-estate-platform integrated into the IdevsWork.Foundation framework.

## Overview

The Entity CQRS pattern provides a unified approach to handling entity operations through commands and queries. This pattern is similar to what you have in your real-estate-platform but enhanced with Foundation's features like structured logging, validation, and caching.

## Key Components

- **EntityCommand<TDto, TId>**: Generic command for Create, Update, Delete operations
- **EntityQuery<TDto, TId>**: Generic query for Retrieve and List operations  
- **EntityCommandHandler<TEntity, TDto, TId>**: Handles command processing
- **EntityQueryHandler<TEntity, TDto, TId>**: Handles query processing
- **ServiceBase**: Enhanced with Entity CQRS convenience methods

## Basic Setup

First, set up your application with Foundation and Entity CQRS support:

```csharp
using Autofac;
using Autofac.Extensions.DependencyInjection;
using IdevsWork.Foundation.Autofac.Extensions;
using IdevsWork.Foundation.Serilog.Extensions;
using Microsoft.EntityFrameworkCore;

var host = Host.CreateDefaultBuilder(args)
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .UseSerilogLogging()
    .ConfigureContainer<ContainerBuilder>((context, builder) =>
    {
        // Register Foundation with Serilog and Entity CQRS handlers
        builder.RegisterFoundationWithSerilog(
            context.Configuration,
            Assembly.GetExecutingAssembly());
        
        // Register your DbContext
        builder.Register(c => 
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection"));
            return new ApplicationDbContext(optionsBuilder.Options);
        }).AsSelf().InstancePerLifetimeScope();
        
        // Register repositories
        builder.RegisterGeneric(typeof(RepositoryBase<,>))
            .As(typeof(IRepositoryBase<,>))
            .InstancePerLifetimeScope();
        
        // Register mappers
        builder.RegisterType<ProductMapper>()
            .As<IMapper<Product, ProductDto>>()
            .InstancePerLifetimeScope();
        
        // Register Entity CQRS handlers
        builder.RegisterGeneric(typeof(EntityCommandHandler<,,>))
            .As(typeof(ICommandHandler<,>))
            .InstancePerLifetimeScope();
            
        builder.RegisterGeneric(typeof(EntityQueryHandler<,,>))
            .As(typeof(IQueryHandler<,>))
            .InstancePerLifetimeScope();
    })
    .Build();
```

## Entity and DTO Setup

Define your entity and DTO:

```csharp
// Entity
public class Product : SoftDeletableEntity<int>
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string Description { get; set; } = null!;
    public bool IsActive { get; set; }
    public int CategoryId { get; set; }
}

// DTO
public class ProductDto : IHasId<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string Description { get; set; } = null!;
    public bool IsActive { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Mapper
public class ProductMapper : BaseMapper<Product, ProductDto>
{
    protected override void MapToEntity(ProductDto dto, Product entity)
    {
        entity.Id = dto.Id;
        entity.Name = dto.Name;
        entity.Price = dto.Price;
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;
        entity.CategoryId = dto.CategoryId;
    }

    protected override void MapToDto(Product entity, ProductDto dto)
    {
        dto.Id = entity.Id;
        dto.Name = entity.Name;
        dto.Price = entity.Price;
        dto.Description = entity.Description;
        dto.IsActive = entity.IsActive;
        dto.CategoryId = entity.CategoryId;
        dto.CreatedAt = entity.CreatedAt;
        dto.UpdatedAt = entity.UpdatedAt;
    }
}
```

## Using Entity Commands

### Create Operations

```csharp
public class ProductService : ServiceBase
{
    public ProductService(IMediator mediator, ILogger<ProductService> logger)
        : base(mediator, logger) { }

    public async Task<EntityCommandResponse<ProductDto>> CreateProductAsync(
        string name, decimal price, string description, int categoryId)
    {
        var productDto = new ProductDto
        {
            Name = name,
            Price = price,
            Description = description,
            CategoryId = categoryId,
            IsActive = true
        };

        // Using ServiceBase convenience method
        return await CreateEntityAsync<ProductDto, int>(productDto);
    }

    public async Task<EntityCommandResponse<ProductDto>> CreateProductsInBulkAsync(
        List<CreateProductRequest> requests)
    {
        var productDtos = requests.Select(r => new ProductDto
        {
            Name = r.Name,
            Price = r.Price,
            Description = r.Description,
            CategoryId = r.CategoryId,
            IsActive = true
        }).ToList();

        return await CreateEntitiesAsync<ProductDto, int>(productDtos);
    }
}
```

### Update Operations

```csharp
public async Task<EntityCommandResponse<ProductDto>> UpdateProductAsync(ProductDto product)
{
    // Using ServiceBase convenience method
    return await UpdateEntityAsync<ProductDto, int>(product);
}

public async Task<EntityCommandResponse<ProductDto>> UpdateProductPricesAsync(
    List<ProductPriceUpdate> updates)
{
    // First retrieve the products to update
    var productIds = updates.Select(u => u.ProductId).ToList();
    var productsResponse = await ListEntitiesByIdsAsync<ProductDto, int>(productIds);
    
    if (!productsResponse.IsSuccess || productsResponse.Entities == null)
    {
        return EntityCommandResponse<ProductDto>.Failure("Failed to retrieve products for update");
    }

    // Update the prices
    foreach (var product in productsResponse.Entities)
    {
        var priceUpdate = updates.FirstOrDefault(u => u.ProductId == product.Id);
        if (priceUpdate != null)
        {
            product.Price = priceUpdate.NewPrice;
        }
    }

    return await UpdateEntitiesAsync<ProductDto, int>(productsResponse.Entities);
}
```

### Delete Operations

```csharp
public async Task<EntityCommandResponse<ProductDto>> DeleteProductAsync(int productId)
{
    return await DeleteEntityAsync<ProductDto, int>(productId);
}

public async Task<EntityCommandResponse<ProductDto>> DeleteProductsAsync(List<int> productIds)
{
    return await DeleteEntitiesAsync<ProductDto, int>(productIds);
}
```

## Using Entity Queries

### Retrieve Operations

```csharp
public async Task<ProductDto?> GetProductByIdAsync(int productId)
{
    var response = await RetrieveEntityAsync<ProductDto, int>(
        productId, 
        cacheKey: $"product_{productId}",
        cacheDuration: TimeSpan.FromMinutes(15));
    
    return response.IsSuccess ? response.Entity : null;
}
```

### List Operations

```csharp
public async Task<List<ProductDto>> GetActiveProductsAsync()
{
    var response = await ListEntitiesByPredicateAsync<ProductDto, int>(
        p => p.IsActive && !p.IsDeleted,
        sorts: new List<SortDescriptor>
        {
            new("Name", SortDirection.Ascending)
        },
        cacheKey: "active_products",
        cacheDuration: TimeSpan.FromMinutes(10));
    
    return response.IsSuccess ? response.Entities ?? new List<ProductDto>() : new List<ProductDto>();
}

public async Task<PagedResult<ProductDto>> GetProductsPagedAsync(
    int pageNumber, int pageSize, string? searchText = null)
{
    var filters = new CompositeFilterDescriptor
    {
        Operator = LogicalOperator.And,
        Filters = new List<object>
        {
            new FilterDescriptor("IsActive", FilterOperator.Equals, true)
        }
    };

    var sorts = new List<SortDescriptor>
    {
        new("CreatedAt", SortDirection.Descending)
    };

    var response = await ListEntitiesPagedAsync<ProductDto, int>(
        pageNumber, pageSize, filters, sorts);
    
    if (response.IsSuccess)
    {
        return new PagedResult<ProductDto>
        {
            Items = response.Entities ?? new List<ProductDto>(),
            TotalCount = response.TotalCount,
            PageNumber = response.PageNumber ?? pageNumber,
            PageSize = response.PageSize ?? pageSize,
            TotalPages = response.TotalPages
        };
    }
    
    return new PagedResult<ProductDto>();
}
```

## Direct Command/Query Usage

You can also use commands and queries directly:

```csharp
public class ProductController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public ProductController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var productDto = new ProductDto
        {
            Name = request.Name,
            Price = request.Price,
            Description = request.Description,
            CategoryId = request.CategoryId,
            IsActive = true
        };

        var command = EntityCommand.Create<ProductDto, int>(productDto);
        var result = await _mediator.SendAsync<EntityCommand<ProductDto, int>, EntityCommandResponse<ProductDto>>(
            command);

        if (result.IsSuccess)
        {
            return Ok(result.Entity);
        }
        
        return BadRequest(result.ErrorMessage);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var query = EntityQuery.Retrieve<ProductDto, int>(
            id, 
            cacheKey: $"product_{id}",
            cacheDuration: TimeSpan.FromMinutes(15));
            
        var result = await _mediator.QueryAsync<EntityQuery<ProductDto, int>, EntityQueryResponse<ProductDto>>(
            query);

        if (result.IsSuccess && result.Entity != null)
        {
            return Ok(result.Entity);
        }
        
        return NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var query = EntityQuery.PagedList<ProductDto, int>(
            pageNumber, 
            pageSize,
            cacheKey: $"products_page_{pageNumber}_{pageSize}",
            cacheDuration: TimeSpan.FromMinutes(5));
            
        var result = await _mediator.QueryAsync<EntityQuery<ProductDto, int>, EntityQueryResponse<ProductDto>>(
            query);

        if (result.IsSuccess)
        {
            return Ok(new
            {
                Items = result.Entities,
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            });
        }
        
        return BadRequest(result.ErrorMessage);
    }
}
```

## Advanced Filtering and Sorting

```csharp
public async Task<List<ProductDto>> GetProductsWithAdvancedFilteringAsync()
{
    var filters = new CompositeFilterDescriptor
    {
        Operator = LogicalOperator.And,
        Filters = new List<object>
        {
            new FilterDescriptor("IsActive", FilterOperator.Equals, true),
            new CompositeFilterDescriptor
            {
                Operator = LogicalOperator.Or,
                Filters = new List<object>
                {
                    new FilterDescriptor("Price", FilterOperator.LessThan, 100.00m),
                    new FilterDescriptor("CategoryId", FilterOperator.In, new[] { 1, 2, 3 })
                }
            }
        }
    };

    var sorts = new List<SortDescriptor>
    {
        new("CategoryId", SortDirection.Ascending),
        new("Price", SortDirection.Descending),
        new("Name", SortDirection.Ascending)
    };

    var response = await ListEntitiesAsync<ProductDto, int>(
        filters: filters,
        sorts: sorts,
        cacheKey: "filtered_products",
        cacheDuration: TimeSpan.FromMinutes(5));
    
    return response.IsSuccess ? response.Entities ?? new List<ProductDto>() : new List<ProductDto>();
}
```

## Custom Service with Entity CQRS

```csharp
public class ProductManagementService : ServiceBase
{
    public ProductManagementService(IMediator mediator, ILogger<ProductManagementService> logger)
        : base(mediator, logger) { }

    public async Task<Result<ProductDto>> CreateProductWithValidationAsync(CreateProductRequest request)
    {
        return await ExecuteWithLoggingAsync(async () =>
        {
            // Custom validation
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Product name is required");
                
            if (request.Price <= 0)
                throw new ArgumentException("Product price must be greater than zero");

            // Check for duplicate name
            var existingProducts = await ListEntitiesByPredicateAsync<ProductDto, int>(
                p => p.Name.ToLower() == request.Name.ToLower());
                
            if (existingProducts.IsSuccess && existingProducts.Entities?.Any() == true)
                throw new InvalidOperationException($"Product with name '{request.Name}' already exists");

            // Create the product
            var productDto = new ProductDto
            {
                Name = request.Name,
                Price = request.Price,
                Description = request.Description,
                CategoryId = request.CategoryId,
                IsActive = true
            };

            var createResponse = await CreateEntityAsync<ProductDto, int>(productDto);
            
            if (!createResponse.IsSuccess)
                throw new InvalidOperationException($"Failed to create product: {createResponse.ErrorMessage}");

            LogInformation("Successfully created product {ProductName} with ID {ProductId}", 
                request.Name, createResponse.Entity?.Id);
                
            return createResponse.Entity!;
        }, 
        "CreateProductWithValidation",
        request);
    }

    public async Task<Result<List<ProductDto>>> GetProductsByCategoryAsync(int categoryId, bool activeOnly = true)
    {
        return await ExecuteWithLoggingAsync(async () =>
        {
            var predicate = activeOnly 
                ? (Expression<Func<ProductDto, bool>>)(p => p.CategoryId == categoryId && p.IsActive)
                : p => p.CategoryId == categoryId;

            var response = await ListEntitiesByPredicateAsync<ProductDto, int>(
                predicate,
                sorts: new List<SortDescriptor>
                {
                    new("Name", SortDirection.Ascending)
                },
                cacheKey: $"products_category_{categoryId}_active_{activeOnly}",
                cacheDuration: TimeSpan.FromMinutes(10));

            if (!response.IsSuccess)
                throw new InvalidOperationException($"Failed to retrieve products: {response.ErrorMessage}");

            return response.Entities ?? new List<ProductDto>();
        },
        "GetProductsByCategory",
        new { categoryId, activeOnly });
    }
}
```

## Error Handling and Validation

```csharp
public class ProductServiceWithValidation : ServiceBase
{
    public ProductServiceWithValidation(IMediator mediator, ILogger<ProductServiceWithValidation> logger)
        : base(mediator, logger) { }

    public async Task<EntityCommandResponse<ProductDto>> CreateProductWithValidationAsync(
        CreateProductRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await ValidateCreateProductRequestAsync(request);
            if (!validationResult.IsValid)
            {
                return EntityCommandResponse<ProductDto>.ValidationFailure(validationResult.Errors);
            }

            // Create product DTO
            var productDto = new ProductDto
            {
                Name = request.Name,
                Price = request.Price,
                Description = request.Description,
                CategoryId = request.CategoryId,
                IsActive = true
            };

            return await CreateEntityAsync<ProductDto, int>(productDto);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error creating product with validation");
            return EntityCommandResponse<ProductDto>.Failure($"Failed to create product: {ex.Message}");
        }
    }

    private async Task<ValidationResult> ValidateCreateProductRequestAsync(CreateProductRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(nameof(request.Name), new[] { "Name is required" });
            
        if (request.Name?.Length > 100)
            errors.Add(nameof(request.Name), new[] { "Name must be 100 characters or less" });

        if (request.Price <= 0)
            errors.Add(nameof(request.Price), new[] { "Price must be greater than zero" });

        if (request.CategoryId <= 0)
            errors.Add(nameof(request.CategoryId), new[] { "Valid category is required" });

        // Check for duplicate names
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var existingResponse = await ListEntitiesByPredicateAsync<ProductDto, int>(
                p => p.Name.ToLower() == request.Name.ToLower());
                
            if (existingResponse.IsSuccess && existingResponse.Entities?.Any() == true)
            {
                errors.Add(nameof(request.Name), new[] { "Product with this name already exists" });
            }
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public Dictionary<string, string[]> Errors { get; set; } = new();
}
```

## Benefits of Entity CQRS Pattern

1. **Consistency**: Unified approach to all entity operations
2. **Logging**: Automatic structured logging for all operations  
3. **Caching**: Built-in query result caching with configurable durations
4. **Validation**: Centralized validation with detailed error responses
5. **Mapping**: Automatic entity ↔ DTO conversion with custom mappers
6. **Performance**: Optimized queries with filtering and paging support
7. **Maintainability**: Separation of commands and queries with clear responsibilities
8. **Scalability**: Easy to extend with new operations and behaviors

## Best Practices

1. **Use ServiceBase**: Inherit from ServiceBase for automatic logging and error handling
2. **Implement Mappers**: Create custom mappers for complex entity ↔ DTO conversions
3. **Cache Strategically**: Use caching for read operations with appropriate durations
4. **Validate Early**: Implement validation in service methods before sending commands
5. **Handle Errors**: Always check command/query responses for errors
6. **Log Appropriately**: Use structured logging with meaningful context
7. **Filter at Database**: In production, implement proper database-level filtering instead of in-memory filtering

This Entity CQRS pattern provides a powerful, consistent way to handle all your entity operations while maintaining the flexibility and features you're used to from your real-estate-platform.
