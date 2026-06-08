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

    private async Task<string> GenerateCategoryIdAsync()
    {
        var lastCategory = await _repository
            .Query()
            .OrderByDescending(x => x.CategoryId)
            .FirstOrDefaultAsync();

        int next = 1;

        if (lastCategory != null
            && lastCategory.CategoryId.StartsWith("CAT"))
        {
            int.TryParse(
                lastCategory.CategoryId.Substring(3),
                out int lastNumber);

            next = lastNumber + 1;
        }

        return $"CAT{next:D3}";
    }
}