using AutoMapper;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public CategoryService(ICategoryRepository categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<CategoryResponseDto>>(categories);
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(string categoryId)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null) return null;

            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto)
        {
            // Validate parent category nếu có
            if (!string.IsNullOrEmpty(dto.ParentId))
            {
                var parentExists = await _categoryRepository.ExistsAsync(dto.ParentId);
                if (!parentExists)
                    throw new Exception($"Parent category '{dto.ParentId}' không tồn tại");
            }

            // Lấy CategoryId tiếp theo từ Repository (không gọi DBContext trực tiếp)
            var categoryId = await _categoryRepository.GetNextCategoryIdAsync();

            // Tạo Category (Aggregate Root)
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

            // Tạo Attributes (nếu có)
            if (dto.Attributes != null && dto.Attributes.Any())
            {
                int attrIndex = 1;
                foreach (var attrDto in dto.Attributes)
                {
                    var attributeId = $"{categoryId}_ATTR{attrIndex:D3}";
                    var attribute = new Attributes
                    {
                        AttributeId = attributeId,
                        CategoryId = category.CategoryId,
                        Name = attrDto.Name,
                        DataType = attrDto.DataType,
                        IsRequired = attrDto.IsRequired ?? false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    category.Attributes.Add(attribute);
                    attrIndex++;
                }
            }

            // Lưu Category + Attributes
            await _categoryRepository.AddAsync(category);

            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task<CategoryResponseDto> UpdateAsync(string categoryId, CategoryUpdateDto dto)
        {
            // Lấy Category hiện tại
            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null)
                throw new Exception($"Category '{categoryId}' không tồn tại");

            // Validate parent category nếu thay đổi
            if (!string.IsNullOrEmpty(dto.ParentId) && dto.ParentId != category.ParentId)
            {
                var parentExists = await _categoryRepository.ExistsAsync(dto.ParentId);
                if (!parentExists)
                    throw new Exception($"Parent category '{dto.ParentId}' không tồn tại");
            }

            // Cập nhật thông tin Category
            category.Name = dto.Name ?? category.Name;
            category.Description = dto.Description ?? category.Description;
            category.ParentId = dto.ParentId ?? category.ParentId;
            category.UpdatedAt = DateTime.UtcNow;

            // Cập nhật Attributes
            if (dto.Attributes != null)
            {
                // Xóa attributes cũ không có trong list mới
                var newAttributeIds = dto.Attributes
                    .Where(a => !string.IsNullOrEmpty(a.AttributeId))
                    .Select(a => a.AttributeId)
                    .ToList();

                var attributesToDelete = category.Attributes
                    .Where(a => !newAttributeIds.Contains(a.AttributeId))
                    .ToList();

                foreach (var attr in attributesToDelete)
                {
                    category.Attributes.Remove(attr);
                }

                // Cập nhật hoặc tạo mới attributes
                foreach (var attrDto in dto.Attributes)
                {
                    if (!string.IsNullOrEmpty(attrDto.AttributeId))
                    {
                        // Cập nhật attribute hiện tại
                        var existingAttr = category.Attributes
                            .FirstOrDefault(a => a.AttributeId == attrDto.AttributeId);

                        if (existingAttr != null)
                        {
                            existingAttr.Name = attrDto.Name ?? existingAttr.Name;
                            existingAttr.DataType = attrDto.DataType ?? existingAttr.DataType;
                            existingAttr.IsRequired = attrDto.IsRequired ?? existingAttr.IsRequired;
                            existingAttr.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Tạo attribute mới
                        var nextAttrNumber = category.Attributes.Count + 1;
                        var attributeId = $"{category.CategoryId}_ATTR{nextAttrNumber:D3}";

                        var newAttribute = new Attributes
                        {
                            AttributeId = attributeId,
                            CategoryId = category.CategoryId,
                            Name = attrDto.Name,
                            DataType = attrDto.DataType,
                            IsRequired = attrDto.IsRequired ?? false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        category.Attributes.Add(newAttribute);
                    }
                }
            }

            // Lưu cập nhật
            await _categoryRepository.UpdateAsync(category);

            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task InactiveAsync(string categoryId)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null)
                throw new Exception($"Category '{categoryId}' không tồn tại");

            // Soft delete Category
            await _categoryRepository.InactiveAsync(categoryId);
        }

        public async Task RestoreAsync(string categoryId)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null)
                throw new Exception($"Category '{categoryId}' không tồn tại");

            // Restore Category
            await _categoryRepository.RestoreAsync(categoryId);
        }
    }
}
