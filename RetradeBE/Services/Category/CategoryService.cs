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
                    $"Parent category '{dto.ParentId}' does not exist.");
        }

        var categoryId = await GenerateCategoryIdAsync(dto.Name ?? "");

        var category = new Category
        {
            CategoryId = categoryId,
            Name = dto.Name,
            Description = dto.Description,
            ParentId = dto.ParentId,
            Status = dto.Status ?? "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Attributes = new List<Attributes>()
        };

        if (dto.Attributes != null)
        {
            int index = 1;

            foreach (var attr in dto.Attributes)
            {
                var cleanedName = RetradeBE.Utils.IdGenerator.CleanNameForId(attr.Name);
                category.Attributes.Add(new Attributes
                {
                    AttributeId = $"{categoryId}_attr_{cleanedName}_{index:D3}",
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
                $"Category '{categoryId}' does not exist.");

        if (!string.IsNullOrWhiteSpace(dto.ParentId)
            && dto.ParentId != category.ParentId)
        {
            var parent =
                await _repository.GetByIdAsync(dto.ParentId);

            if (parent == null)
                throw new Exception(
                    $"Parent category '{dto.ParentId}' does not exist.");
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
                    if (!string.Equals(existingAttr.DataType, incomingAttr.DataType, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception($"Changing the data type of the attribute '{existingAttr.Name}' is not allowed. Please delete this attribute and create a new one.");
                    }

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
                    throw new Exception($"Attribute '{incomingAttr.AttributeId}' does not belong to this category.");
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
                    var lastUnderscore = attr.AttributeId.LastIndexOf('_');
                    if (lastUnderscore >= 0)
                    {
                        var suffix = attr.AttributeId.Substring(lastUnderscore + 1);
                        if (suffix.StartsWith("ATTR", StringComparison.OrdinalIgnoreCase))
                        {
                            suffix = suffix.Substring(4);
                        }
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
                    var cleanedName = RetradeBE.Utils.IdGenerator.CleanNameForId(newAttr.Name ?? "");
                    category.Attributes.Add(new Attributes
                    {
                        AttributeId = $"{categoryId}_attr_{cleanedName}_{nextIndex:D3}",
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
                $"Category '{categoryId}' does not exist.");

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
                $"Category '{categoryId}' does not exist.");

        category.Status = "Active";
        category.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(category);
    }

    private Task<string> GenerateCategoryIdAsync(string name)
    {
        string datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        int randomPart = new Random().Next(100000, 1000000);
        string cleanedName = RetradeBE.Utils.IdGenerator.CleanNameForId(name);
        return Task.FromResult($"cat_{cleanedName}_{datePart}_{randomPart}");
    }
}