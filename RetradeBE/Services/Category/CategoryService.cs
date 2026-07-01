using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using RetradeBE.Services;
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly IMapper _mapper;

    public CategoryService(
        ICategoryRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public IQueryable<CategoryResponseDto> Query()
    {
        return _repository
            .Query()
            .ProjectTo<CategoryResponseDto>(
                _mapper.ConfigurationProvider);
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(string categoryId)
    {
        var category = await _repository.GetByIdAsync(categoryId);

        if (category == null)
            return null;

        return _mapper.Map<CategoryResponseDto>(category);
    }

    public async Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.ParentId))
        {
            var parent = await _repository.GetByIdAsync(dto.ParentId);

            if (parent == null)
                throw new Exception(
                    $"Parent category '{dto.ParentId}' không tồn tại");
        }

        var categoryId = await GenerateCategoryIdAsync();

        var category = new Category
        {
            CategoryId = categoryId,
            Name = dto.Name,
            Description = dto.Description,
            ParentId = dto.ParentId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Attributes = new List<Attributes>()
        };

        if (dto.Attributes != null)
        {
            int index = 1;

            foreach (var attr in dto.Attributes)
            {
                category.Attributes.Add(new Attributes
                {
                    AttributeId = $"{categoryId}_ATTR{index:D3}",
                    CategoryId = categoryId,
                    Name = attr.Name,
                    DataType = attr.DataType,
                    IsRequired = attr.IsRequired ?? false,
                    MinValue = attr.MinValue,
                    MaxValue = attr.MaxValue,
                    Unit = attr.Unit,
                    DisplayOrder = attr.DisplayOrder ?? index,
                    IsFilterable = attr.IsFilterable ?? false,
                    IsSearchable = attr.IsSearchable ?? false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });

                index++;
            }
        }

        await _repository.AddAsync(category);

        return _mapper.Map<CategoryResponseDto>(category);
    }

    public async Task<CategoryResponseDto> UpdateAsync(
        string categoryId,
        CategoryUpdateDto dto)
    {
        var category =
            await _repository.GetByIdAsync(categoryId);

        if (category == null)
            throw new Exception(
                $"Category '{categoryId}' không tồn tại");

        if (!string.IsNullOrWhiteSpace(dto.ParentId)
            && dto.ParentId != category.ParentId)
        {
            var parent =
                await _repository.GetByIdAsync(dto.ParentId);

            if (parent == null)
                throw new Exception(
                    $"Parent category '{dto.ParentId}' không tồn tại");
        }

        category.Name = dto.Name ?? category.Name;
        category.Description =
            dto.Description ?? category.Description;
        category.ParentId =
            dto.ParentId ?? category.ParentId;
        category.UpdatedAt = DateTime.UtcNow;

        if (dto.Attributes != null)
        {
            foreach (var a in dto.Attributes)
            {
            }

            var existingAttributes = category.Attributes.ToList();

            var incomingAttrsWithId = dto.Attributes
                .Where(a => !string.IsNullOrWhiteSpace(a.AttributeId))
                .ToList();

            var incomingIds = incomingAttrsWithId.Select(a => a.AttributeId).ToHashSet();
            foreach (var existingAttr in existingAttributes)
            {
                if (!incomingIds.Contains(existingAttr.AttributeId))
                {
                    existingAttr.IsDeleted = true;
                    existingAttr.UpdatedAt = DateTime.UtcNow;
                }
            }

            foreach (var incomingAttr in incomingAttrsWithId)
            {
                var existingAttr = existingAttributes.FirstOrDefault(a => a.AttributeId == incomingAttr.AttributeId);
                if (existingAttr != null)
                {
                    existingAttr.Name = incomingAttr.Name ?? existingAttr.Name;
                    existingAttr.DataType = incomingAttr.DataType ?? existingAttr.DataType;
                    existingAttr.IsRequired = incomingAttr.IsRequired ?? existingAttr.IsRequired ?? false;
                    existingAttr.MinValue = incomingAttr.MinValue;
                    existingAttr.MaxValue = incomingAttr.MaxValue;
                    existingAttr.Unit = incomingAttr.Unit;
                    existingAttr.DisplayOrder = incomingAttr.DisplayOrder ?? existingAttr.DisplayOrder;
                    existingAttr.IsFilterable = incomingAttr.IsFilterable ?? existingAttr.IsFilterable;
                    existingAttr.IsSearchable = incomingAttr.IsSearchable ?? existingAttr.IsSearchable;
                    existingAttr.IsDeleted = false;
                    existingAttr.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    throw new Exception($"Attribute '{incomingAttr.AttributeId}' không thuộc Category này.");
                }
            }

            var newAttrs = dto.Attributes
                .Where(a => string.IsNullOrWhiteSpace(a.AttributeId))
                .ToList();

            if (newAttrs.Any())
            {
                int nextIndex = 1;
                foreach (var attr in existingAttributes)
                {
                    if (attr.AttributeId.StartsWith(categoryId + "_ATTR"))
                    {
                        var suffix = attr.AttributeId.Substring((categoryId + "_ATTR").Length);
                        if (int.TryParse(suffix, out int index))
                        {
                            if (index >= nextIndex)
                            {
                                nextIndex = index + 1;
                            }
                        }
                    }
                }

                foreach (var newAttr in newAttrs)
                {
                    category.Attributes.Add(new Attributes
                    {
                        AttributeId = $"{categoryId}_ATTR{nextIndex:D3}",
                        CategoryId = categoryId,
                        Name = newAttr.Name,
                        DataType = newAttr.DataType,
                        IsRequired = newAttr.IsRequired ?? false,
                        MinValue = newAttr.MinValue,
                        MaxValue = newAttr.MaxValue,
                        Unit = newAttr.Unit,
                        DisplayOrder = newAttr.DisplayOrder ?? nextIndex,
                        IsFilterable = newAttr.IsFilterable ?? false,
                        IsSearchable = newAttr.IsSearchable ?? false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    });
                    nextIndex++;
                }
            }
        }

        await _repository.UpdateAsync(category);

        return _mapper.Map<CategoryResponseDto>(category);
    }

    public async Task InactiveAsync(string categoryId)
    {
        var category =
            await _repository.GetByIdAsync(categoryId);

        if (category == null)
            throw new Exception(
                $"Category '{categoryId}' không tồn tại");

        category.Status = "Inactive";
        category.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(category);
    }

    public async Task RestoreAsync(string categoryId)
    {
        var category =
            await _repository.GetByIdAsync(categoryId);

        if (category == null)
            throw new Exception(
                $"Category '{categoryId}' không tồn tại");

        category.Status = "Active";
        category.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(category);
    }

    private Task<string> GenerateCategoryIdAsync()
    {
        return Task.FromResult(RetradeBE.Utils.IdGenerator.GenerateId("cat"));
    }
}