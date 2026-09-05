using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.DeleteCategory;

public sealed class DeleteCategoryHandler(
    ICategoryRepository categoryRepo,
    IApplicationDbContext db,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.Id), storeId, ct);
        if (category is null)
            return Result<Guid>.Failure("Danh mục không tồn tại.");

        var productCount = await db.Products
            .CountAsync(p => p.CategoryId == new CategoryId(request.Id) && p.StoreId == storeId, ct);

        if (productCount > 0)
            return Result<Guid>.Failure($"Không thể xóa — còn {productCount} sản phẩm thuộc danh mục này.");

        categoryRepo.Remove(category);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(category.Id.Value);
    }
}
