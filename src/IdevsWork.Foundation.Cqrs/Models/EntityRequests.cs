using IdevsWork.Foundation.Abstractions.Common;
using System.Linq.Expressions;

namespace IdevsWork.Foundation.Cqrs.Models;

/// <summary>
/// Filter operators for query conditions
/// </summary>
public enum FilterOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith,
    IsNull,
    IsNotNull,
    In,
    NotIn
}

/// <summary>
/// Sort direction for ordering results
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Logical operators for combining filters
/// </summary>
public enum LogicalOperator
{
    And,
    Or
}

/// <summary>
/// Filter descriptor for individual field conditions
/// </summary>
public class FilterDescriptor
{
    public string FieldName { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; }
    public object? Value { get; set; }

    public FilterDescriptor() { }

    public FilterDescriptor(string fieldName, FilterOperator filterOperator, object? value)
    {
        FieldName = fieldName;
        Operator = filterOperator;
        Value = value;
    }
}

/// <summary>
/// Sort descriptor for ordering results
/// </summary>
public class SortDescriptor
{
    public string FieldName { get; set; } = string.Empty;
    public SortDirection Direction { get; set; }

    public SortDescriptor() { }

    public SortDescriptor(string fieldName, SortDirection direction)
    {
        FieldName = fieldName;
        Direction = direction;
    }
}

/// <summary>
/// Composite filter descriptor for complex filtering conditions
/// </summary>
public class CompositeFilterDescriptor
{
    public LogicalOperator Operator { get; set; } = LogicalOperator.And;
    public List<object> Filters { get; set; } = new();
}

/// <summary>
/// Command request for entity operations
/// </summary>
/// <typeparam name="TDto">The DTO type</typeparam>
/// <typeparam name="TId">The ID type</typeparam>
public class EntityCommandRequest<TDto, TId>
    where TDto : class, IHasId<TId>
{
    public TDto? Entity { get; set; }
    public List<TDto>? Entities { get; set; }
    public TId? EntityId { get; set; }
    public List<TId>? EntityIds { get; set; }

    public bool IsValid =>
        (Entity != null && EntityId == null && Entities == null && EntityIds == null) ||
        (Entities is { Count: > 0 } && EntityId == null && EntityIds == null && Entity == null) ||
        (EntityId != null && Entity == null && Entities == null && EntityIds == null) ||
        (EntityIds is { Count: > 0 } && Entity == null && Entities == null && EntityId == null);

    public bool IsSingleEntity =>
        (Entity != null && EntityId == null && Entities == null && EntityIds == null) ||
        (EntityId != null && Entity == null && Entities == null && EntityIds == null);

    public bool IsMultipleEntities =>
        (Entities is { Count: > 0 } && EntityId == null && EntityIds == null && Entity == null) ||
        (EntityIds is { Count: > 0 } && Entity == null && Entities == null && EntityId == null);
}

/// <summary>
/// Query request for entity operations
/// </summary>
/// <typeparam name="TDto">The DTO type</typeparam>
/// <typeparam name="TId">The ID type</typeparam>
public class EntityQueryRequest<TDto, TId>
    where TDto : class, IHasId<TId>
{
    public List<string> SearchableFields { get; set; } = new();
    public string? SearchText { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public CompositeFilterDescriptor? Filters { get; set; }
    public List<SortDescriptor>? Sorts { get; set; }
    public TId? EntityId { get; set; }
    public List<TId>? EntityIds { get; set; }
    public Expression<Func<TDto, bool>>? Predicate { get; set; }

    public bool IsValid =>
        (EntityId != null && EntityIds == null && Filters == null && Sorts == null && PageNumber == null && PageSize == null && Predicate == null) ||
        (EntityIds != null && EntityId == null && Filters == null && Sorts == null && PageNumber == null && PageSize == null && Predicate == null) ||
        (Predicate != null && EntityId == null && EntityIds == null && Filters == null && Sorts == null && PageNumber == null && PageSize == null) ||
        (Filters != null && EntityId == null && EntityIds == null && Predicate == null) ||
        (Sorts != null && EntityId == null && EntityIds == null && Predicate == null) ||
        (PageNumber != null && PageSize != null && EntityId == null && EntityIds == null);

    public bool IsSingleEntity =>
        EntityId != null && EntityIds == null && Filters == null && Sorts == null && PageNumber == null && PageSize == null && Predicate == null;

    public bool IsMultipleEntities =>
        (EntityIds != null && EntityId == null) ||
        (Filters != null || Sorts != null || Predicate != null || (PageNumber != null && PageSize != null));

    public bool IsPaged => PageNumber != null && PageSize != null;
}

/// <summary>
/// Command response for entity operations
/// </summary>
/// <typeparam name="TDto">The DTO type</typeparam>
public class EntityCommandResponse<TDto>
{
    public TDto? Entity { get; set; }
    public List<TDto>? Entities { get; set; }
    public int RowsAffected { get; set; }
    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    public static EntityCommandResponse<TDto> Success(TDto? entity)
    {
        return new EntityCommandResponse<TDto>
        {
            Entity = entity,
            RowsAffected = entity is null ? 0 : 1
        };
    }

    public static EntityCommandResponse<TDto> Success(IEnumerable<TDto> entities, int rowsAffected = -1)
    {
        var collection = entities as ICollection<TDto> ?? entities.ToList();
        var count = rowsAffected >= 0 ? rowsAffected : collection.Count;
        return new EntityCommandResponse<TDto>
        {
            Entities = collection.ToList(),
            RowsAffected = count
        };
    }

    public static EntityCommandResponse<TDto> Success(int rowsAffected)
    {
        return new EntityCommandResponse<TDto>
        {
            RowsAffected = rowsAffected
        };
    }

    public static EntityCommandResponse<TDto> Failure(string errorMessage)
    {
        return new EntityCommandResponse<TDto>
        {
            ErrorMessage = errorMessage,
            RowsAffected = 0
        };
    }

    public static EntityCommandResponse<TDto> ValidationFailure(Dictionary<string, string[]> validationErrors)
    {
        return new EntityCommandResponse<TDto>
        {
            ErrorMessage = "Validation failed",
            ValidationErrors = validationErrors,
            RowsAffected = 0
        };
    }
}

/// <summary>
/// Query response for entity operations
/// </summary>
/// <typeparam name="TDto">The DTO type</typeparam>
public class EntityQueryResponse<TDto>
    where TDto : class
{
    public TDto? Entity { get; set; }
    public List<TDto>? Entities { get; set; }
    public int TotalCount { get; set; }
    public int? PageSize { get; set; }
    public int? PageNumber { get; set; }
    public int TotalPages => PageSize.HasValue && PageSize > 0 
        ? (int)Math.Ceiling((double)TotalCount / PageSize.Value) 
        : 1;
    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    public string? ErrorMessage { get; set; }

    public static EntityQueryResponse<TDto> Success(TDto? entity)
    {
        return new EntityQueryResponse<TDto>
        {
            Entity = entity,
            TotalCount = entity is null ? 0 : 1
        };
    }

    public static EntityQueryResponse<TDto> Success(IEnumerable<TDto> entities, int totalCount = -1)
    {
        var collection = entities as ICollection<TDto> ?? entities.ToList();
        var count = totalCount >= 0 ? totalCount : collection.Count;
        return new EntityQueryResponse<TDto>
        {
            Entities = collection.ToList(),
            TotalCount = count
        };
    }

    public static EntityQueryResponse<TDto> Success(IEnumerable<TDto> entities, int totalCount, int pageNumber, int pageSize)
    {
        var collection = entities as ICollection<TDto> ?? entities.ToList();
        return new EntityQueryResponse<TDto>
        {
            Entities = collection.ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public static EntityQueryResponse<TDto> Failure(string errorMessage)
    {
        return new EntityQueryResponse<TDto>
        {
            ErrorMessage = errorMessage,
            TotalCount = 0
        };
    }
}
