using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Luôn đảm bảo tài khoản demo tồn tại và mật khẩu hợp lệ
        var seedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "demo@minimalapi.local");
        Store? seedStore = null;

        if (seedUser is null)
        {
            var passwordHash = HashPassword("Demo@123456");
            seedUser = User.Create("demo@minimalapi.local", "Demo Store", passwordHash);
            seedStore = Store.Create(seedUser.Id, "Cửa hàng mẫu", "cua-hang-mau");
            var seedMember = StoreMember.Create(seedStore.Id, seedUser.Id, StoreRole.Owner);

            context.Users.Add(seedUser);
            context.Stores.Add(seedStore);
            context.StoreMembers.Add(seedMember);
            await context.SaveChangesAsync();
        }
        else
        {
            seedUser.UpdatePassword(HashPassword("Demo@123456"));
            seedStore = await context.Stores.FirstOrDefaultAsync(s => s.OwnerId == seedUser.Id);
            if (seedStore is not null && !await context.StoreMembers.AnyAsync(m => m.StoreId == seedStore.Id && m.UserId == seedUser.Id))
            {
                var seedMember = StoreMember.Create(seedStore.Id, seedUser.Id, StoreRole.Owner);
                context.StoreMembers.Add(seedMember);
            }
            await context.SaveChangesAsync();
        }

        // 1.1 Đảm bảo tài khoản superadmin (Quản trị hệ thống) tồn tại với mật khẩu 12345678
        var superAdmin = await context.Users.FirstOrDefaultAsync(u => u.Email == "superadmin@minimalapi.local" || u.Email == "superadmin");
        if (superAdmin is null)
        {
            var superAdminHash = HashPassword("12345678");
            superAdmin = User.Create("superadmin@minimalapi.local", "Quản Trị Viên Hệ Thống", superAdminHash);
            context.Users.Add(superAdmin);
            await context.SaveChangesAsync();

            if (seedStore is not null && !await context.StoreMembers.AnyAsync(m => m.StoreId == seedStore.Id && m.UserId == superAdmin.Id))
            {
                var adminMember = StoreMember.Create(seedStore.Id, superAdmin.Id, StoreRole.Owner);
                context.StoreMembers.Add(adminMember);
                await context.SaveChangesAsync();
            }
        }
        else
        {
            superAdmin.UpdatePassword(HashPassword("12345678"));
            if (seedStore is not null && !await context.StoreMembers.AnyAsync(m => m.StoreId == seedStore.Id && m.UserId == superAdmin.Id))
            {
                var adminMember = StoreMember.Create(seedStore.Id, superAdmin.Id, StoreRole.Owner);
                context.StoreMembers.Add(adminMember);
            }
            await context.SaveChangesAsync();
        }

        // 2. Tạo categories & products mẫu cho cửa hàng demo nếu chưa có
        if (seedStore is not null && !context.Categories.Any(c => c.StoreId == seedStore.Id))
        {
            var electronics = Category.Create(seedStore.Id, "Điện tử", "Các sản phẩm điện tử");
            var clothing = Category.Create(seedStore.Id, "Thời trang", "Quần áo, giày dép");
            var books = Category.Create(seedStore.Id, "Sách", "Sách và tài liệu");

            context.Categories.AddRange(electronics, clothing, books);
            await context.SaveChangesAsync();

            // Tạo Products
            var products = new[]
            {
                Product.Create(
                    seedStore.Id,
                    "LAP-DELL-XPS13", 10,
                    ProductName.Create("Laptop Dell XPS 13"),
                    Money.VND(25000000),
                    electronics.Id,
                    "Laptop cao cấp, màn hình 13 inch"
                ),
                Product.Create(
                    seedStore.Id,
                    "PHONE-IP15PRO", 15,
                    ProductName.Create("iPhone 15 Pro"),
                    Money.VND(30000000),
                    electronics.Id,
                    "Điện thoại thông minh Apple mới nhất"
                ),
                Product.Create(
                    seedStore.Id,
                    "SHIRT-MEN-001", 50,
                    ProductName.Create("Áo thun nam"),
                    Money.VND(150000),
                    clothing.Id,
                    "Áo thun cotton 100%"
                ),
                Product.Create(
                    seedStore.Id,
                    "SHOE-NIKE-001", 20,
                    ProductName.Create("Giày thể thao Nike"),
                    Money.VND(2500000),
                    clothing.Id,
                    "Giày chạy bộ chuyên nghiệp"
                ),
                Product.Create(
                    seedStore.Id,
                    "BOOK-CLEANCODE", 25,
                    ProductName.Create("Clean Code"),
                    Money.VND(350000),
                    books.Id,
                    "Sách lập trình - Robert C. Martin"
                )
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }

        // 2.1 Đảm bảo sản phẩm Shopee demo (Nệm Sora) luôn tồn tại cho cửa hàng demo
        if (seedStore is not null && !await context.Products.AnyAsync(p => p.StoreId == seedStore.Id && p.Sku == "SORA10-6"))
        {
            var homeCat = await context.Categories.FirstOrDefaultAsync(c => c.StoreId == seedStore.Id && c.Name == "Đồ gia dụng");
            if (homeCat is null)
            {
                homeCat = Category.Create(seedStore.Id, "Đồ gia dụng", "Nệm, chăn ga gối đệm");
                context.Categories.Add(homeCat);
                await context.SaveChangesAsync();
            }

            var soraProduct = Product.Create(
                seedStore.Id,
                "SORA10-6", 25,
                ProductName.Create("Nệm Foam Việt Nhật Sora - Hỗ Trợ Nâng Đỡ, Êm Ái Mềm Mại"),
                Money.VND(2270000),
                homeCat.Id,
                "Nệm Foam cao cấp Việt Nhật Sora chính hãng, hỗ trợ nâng đỡ cột sống"
            );
            context.Products.Add(soraProduct);
            await context.SaveChangesAsync();
        }

        // 2.2 Cấu hình quà tặng mẫu cho sản phẩm demo nếu chưa có
        if (seedStore is not null)
        {
            var dell = await context.Products.FirstOrDefaultAsync(p => p.StoreId == seedStore.Id && p.Sku == "LAP-DELL-XPS13");
            var book = await context.Products.FirstOrDefaultAsync(p => p.StoreId == seedStore.Id && p.Sku == "BOOK-CLEANCODE");
            if (dell is not null && book is not null && dell.GiftProductId is null)
            {
                dell.SetGiftProduct(book.Id, book.Name.Value);
            }

            var sora = await context.Products.FirstOrDefaultAsync(p => p.StoreId == seedStore.Id && p.Sku == "SORA10-6");
            var shirt = await context.Products.FirstOrDefaultAsync(p => p.StoreId == seedStore.Id && p.Sku == "SHIRT-MEN-001");
            if (sora is not null && shirt is not null && sora.GiftProductId is null)
            {
                sora.SetGiftProduct(shirt.Id, shirt.Name.Value);
            }

            await context.SaveChangesAsync();
        }

        // 3. Tạo khách hàng mẫu nếu chưa có
        if (seedStore is not null && !context.Customers.Any(c => c.StoreId == seedStore.Id))
        {
            var customer1 = Customer.Create(seedStore.Id, "Nguyễn Văn An", "0901234567", "an.nguyen@example.com", "123 Lê Lợi, Quận 1, TP.HCM");
            var customer2 = Customer.Create(seedStore.Id, "Trần Thị Bình", "0912345678", "binh.tran@example.com", "456 Nguyễn Huệ, Quận 1, TP.HCM");

            context.Customers.AddRange(customer1, customer2);
            await context.SaveChangesAsync();

            // 4. Tạo đơn hàng mẫu nếu chưa có
            if (!context.Orders.Any(o => o.StoreId == seedStore.Id))
            {
                var phone = await context.Products.FirstOrDefaultAsync(p => p.StoreId == seedStore.Id && p.Sku == "PHONE-IP15PRO");
                var shirt = await context.Products.FirstOrDefaultAsync(p => p.StoreId == seedStore.Id && p.Sku == "SHIRT-MEN-001");
                var book = await context.Products.FirstOrDefaultAsync(p => p.StoreId == seedStore.Id && p.Sku == "BOOK-CLEANCODE");

                if (phone is not null)
                {
                    var order1Items = new List<OrderItem>
                    {
                        OrderItem.Create(OrderId.New(), phone.Id, phone.Name.Value, phone.Price, 1)
                    };
                    var order1 = Order.Create(seedStore.Id, customer1.Id, "ORD-20260905-001", order1Items);
                    order1.Confirm();
                    phone.AdjustStock(-1);
                    context.Orders.Add(order1);
                }

                if (shirt is not null && book is not null)
                {
                    var order2Items = new List<OrderItem>
                    {
                        OrderItem.Create(OrderId.New(), shirt.Id, shirt.Name.Value, shirt.Price, 2),
                        OrderItem.Create(OrderId.New(), book.Id, book.Name.Value, book.Price, 1)
                    };
                    var order2 = Order.Create(seedStore.Id, customer2.Id, "ORD-20260905-002", order2Items);
                    context.Orders.Add(order2);
                }

                await context.SaveChangesAsync();
            }
        }

        // 7. Seed promotions mẫu nếu chưa có
        if (seedStore is not null && !await context.Promotions.AnyAsync(p => p.StoreId == seedStore.Id))
        {
            var promo1 = Promotion.Create(seedStore.Id, "KM5", "Giảm 5% đơn hàng", DiscountType.Percentage, 5, "Áp dụng giảm 5% trên tổng giá trị sản phẩm");
            var promo2 = Promotion.Create(seedStore.Id, "KM10", "Giảm 10% đơn hàng", DiscountType.Percentage, 10, "Áp dụng giảm 10% cho khách hàng thân thiết");
            var promo3 = Promotion.Create(seedStore.Id, "KM100K", "Giảm 100.000đ", DiscountType.FixedAmount, 100000, "Giảm trực tiếp 100.000đ cho đơn hàng");
            var promo4 = Promotion.Create(seedStore.Id, "KM200K", "Giảm 200.000đ", DiscountType.FixedAmount, 200000, "Giảm trực tiếp 200.000đ cho đơn hàng giá trị cao");

            context.Promotions.AddRange(promo1, promo2, promo3, promo4);
            await context.SaveChangesAsync();
        }
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210_000, HashAlgorithmName.SHA512, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
}
