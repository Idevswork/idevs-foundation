namespace Idevs.Foundation.Abstractions.Mapper;

public interface IBaseMapper<TEntity, TDto> where TEntity : class where TDto : class
{
    IQueryable<TDto> ProjectToDto(IQueryable<TEntity> query);
    IQueryable<TEntity> ProjectToEntity(IQueryable<TDto> query);
    TDto MapToDto(TEntity entity);
    TEntity MapToEntity(TDto dto);
}
