using Hizu.Domain.Constants;
using Hizu.Domain.Entities;
using Hizu.Domain.Enums;
using Hizu.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hizu.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedDatabaseAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger logger)
    {
        await SeedRolesAsync(roleManager, logger);
        await SeedUsersAsync(userManager, logger);
        await SeedCategoriesAsync(context, logger);
        await SeedProductsAsync(context, logger);
        await SeedSpecialOrdersAsync(context, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager, ILogger logger)
    {
        string[] roles = [AppRoles.Admin, AppRoles.Customer];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                logger.LogInformation("Seeding role: {Role}", role);
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        // 1. Admin User
        var adminEmail = "admin@hizu.ir";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            logger.LogInformation("Seeding Admin user: {Email}", adminEmail);
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "مدیر سیستم",
                LastName = "هیزو",
                PhoneNumber = "09120000000",
                Address = new Address("خیابان آزادی، ناحیه نوآوری شریف، کارگاه هیزو", "تهران", "تهران", "1458888888", "ایران"),
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                await userManager.AddToRoleAsync(adminUser, AppRoles.Customer);
            }
        }

        // 2. Demo Customer User
        var demoEmail = "demo@hizu.ir";
        var demoUser = await userManager.FindByEmailAsync(demoEmail);
        if (demoUser == null)
        {
            logger.LogInformation("Seeding Demo customer user: {Email}", demoEmail);
            demoUser = new ApplicationUser
            {
                UserName = demoEmail,
                Email = demoEmail,
                EmailConfirmed = true,
                FirstName = "آرش",
                LastName = "مهندسی",
                PhoneNumber = "09121111111",
                Address = new Address("میدان ونک، خیابان ملاصدرا، پلاک ۲۴", "تهران", "تهران", "1993645891", "ایران"),
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(demoUser, "Demo123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(demoUser, AppRoles.Customer);
            }
        }
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Categories.AnyAsync()) return;

        logger.LogInformation("Seeding Hizu fabrication categories...");

        var categories = new List<Category>
        {
            new()
            {
                Name = "پرینت ۳بعدی صنعتی",
                Slug = "3d-printing",
                Code = "3DP",
                Description = "تولید نمونه اولیه دقیق و قطعات نهایی با پلیمرهای مهندسی FDM، SLA و نایلون کربن SLS.",
                ImageUrl = "https://images.unsplash.com/photo-1581092160607-ee22621dd758?auto=format&fit=crop&w=800&q=80",
                IsActive = true,
                DisplayOrder = 1
            },
            new()
            {
                Name = "ماشین‌کاری CNC",
                Slug = "cnc-machining",
                Code = "CNC",
                Description = "فرزکاری ۵ محوره فلزات سبک، برنج شمش، چوب‌های سخت و قالب‌های دقیق صنعتی با تلرانس ±0.01mm.",
                ImageUrl = "https://images.unsplash.com/photo-1565043589221-1a6fd9ae45c7?auto=format&fit=crop&w=800&q=80",
                IsActive = true,
                DisplayOrder = 2
            },
            new()
            {
                Name = "برش و حکاکی لیزر",
                Slug = "laser-cutting",
                Code = "LSR",
                Description = "برش فوق‌سریع و حکاکی فیبر و CO2 روی ورق‌های آلومینیوم، فولاد، پلکسی و چوب طبیعی.",
                ImageUrl = "https://images.unsplash.com/photo-1504917599217-d4dc5ebe6122?auto=format&fit=crop&w=800&q=80",
                IsActive = true,
                DisplayOrder = 3
            },
            new()
            {
                Name = "اکسسوری و ابزار کارگاهی",
                Slug = "tools-accessories",
                Code = "TLS",
                Description = "اکسسوری‌های رومیزی مهندسی، کیس‌های ماشین‌کاری شده، ابزارهای دقیق اندازه‌گیری و تجهیزات کارگاه.",
                ImageUrl = "https://images.unsplash.com/photo-1581092335397-9583fe92d232?auto=format&fit=crop&w=800&q=80",
                IsActive = true,
                DisplayOrder = 4
            },
            new()
            {
                Name = "قطعات سفارشی مکاترونیک",
                Slug = "custom-parts",
                Code = "CST",
                Description = "طراحی و ساخت سفارشی بازوهای رباتیک، چرخ‌دنده‌ها، شاسی‌های مهندسی معکوس و مکانیزم‌های صنعتی.",
                ImageUrl = "https://images.unsplash.com/photo-1581092580497-e0d23cbdf1dc?auto=format&fit=crop&w=800&q=80",
                IsActive = true,
                DisplayOrder = 5
            }
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Products.AnyAsync()) return;

        logger.LogInformation("Seeding Hizu engineered products...");

        var cat3dp = await context.Categories.FirstAsync(c => c.Code == "3DP");
        var catCnc = await context.Categories.FirstAsync(c => c.Code == "CNC");
        var catLsr = await context.Categories.FirstAsync(c => c.Code == "LSR");
        var catTls = await context.Categories.FirstAsync(c => c.Code == "TLS");
        var catCst = await context.Categories.FirstAsync(c => c.Code == "CST");

        var products = new List<Product>
        {
            new()
            {
                Name = "کیس کیبورد مکانیکال برنجی شمش",
                Slug = "solid-brass-keyboard-case",
                Sku = "HZ-KB-BRS-65",
                Description = "کیس کیبورد پریمیوم ۶۵ درصد ساخته شده از یک تکه شمش برنجی C360 با ماشین‌کاری ۵ محوره CNC و پرداخت دستی مات. طراحی آکوستیک بهینه‌سازی شده با فوم داخلی PORON و واشر مانتینگ ایزوله.",
                ShortDescription = "ماشین‌کاری ۵ محوره CNC از شمش برنجی خالص با فینیشینگ مات دستی",
                Price = 8900000,
                CompareAtPrice = 9800000,
                StockQuantity = 8,
                IsActive = true,
                IsFeatured = true,
                Material = "Solid Brass C360",
                Dimensions = "320 × 115 × 32 mm",
                Weight = "1850 g",
                Color = "برنجی مات طبیعی (Matte Brass)",
                Technique = "5-Axis CNC Milling",
                Tolerance = "±0.01 mm",
                CategoryId = catCnc.Id,
                ImageUrl = "https://images.unsplash.com/photo-1595225476474-87563907a212?auto=format&fit=crop&w=800&q=80",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "گریپر سبک‌سازی بازوی رباتیک AL-7075",
                Slug = "robotic-gripper-al7075",
                Sku = "HZ-GRP-7075",
                Description = "گریپر دو فکی فوق سبک با بهینه‌سازی توپولوژی برای کاهش ممان اینرسی و تحمل بار تا ۱۵ کیلوگرم. ساخته شده از آلومینیوم فوق‌مقاوم آلیاژ هوایی 7075-T6 با آنودایزینگ سخت.",
                ShortDescription = "سبک‌سازی مکانیکی توپولوژیک با آلیاژ هوایی و تلورانس ابعادی ±0.02mm",
                Price = 4600000,
                CompareAtPrice = 5200000,
                StockQuantity = 12,
                IsActive = true,
                IsFeatured = true,
                Material = "Aerospace Aluminum 7075-T6",
                Dimensions = "140 × 85 × 45 mm",
                Weight = "142 g",
                Color = "آبی متالیک متال (Matte Cyan Anodized)",
                Technique = "High-Speed CNC Milling",
                Tolerance = "±0.02 mm",
                CategoryId = catCst.Id,
                ImageUrl = "https://images.unsplash.com/photo-1616401784845-180882ba9ba8?auto=format&fit=crop&w=800&q=80",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "پاویون دکوراتیو پارامتریک بلوط طبیعی",
                Slug = "parametric-oak-pavilion-sculpture",
                Sku = "HZ-PVN-OAK-01",
                Description = "المان معماری پارامتریک طراحی شده بر اساس الگوریتم ورونوی با استفاده از چوب بلوط گرجستان. دارای اتصالات چوبی کاملاً پنهان ژاپنی و برش ترکیبی با فرز CNC و لیزر صنعتی.",
                ShortDescription = "اتصالات چوبی پنهان و برش ترکیبی الگوریتمیک CNC و لیزر",
                Price = 12500000,
                CompareAtPrice = 14000000,
                StockQuantity = 4,
                IsActive = true,
                IsFeatured = true,
                Material = "Solid Oak Wood & Eco Wax",
                Dimensions = "480 × 260 × 220 mm",
                Weight = "3400 g",
                Color = "بلوط طبیعی عسلی",
                Technique = "Parametric CNC & Laser Cutting",
                Tolerance = "±0.1 mm",
                CategoryId = catLsr.Id,
                ImageUrl = "https://images.unsplash.com/photo-1513519245088-0e12902e5a38?auto=format&fit=crop&w=800&q=80",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "پایه نگهدارنده ارگونومیک مانیتور هیزو",
                Slug = "hizu-ergonomic-monitor-stand",
                Sku = "HZ-DSK-MNT-02",
                Description = "پایه رومیزی مینیمال با شاسی آلومینیومی برش‌خورده با واترجت و صفحه رویی از چوب گردوی رگه‌دار روغنی. دارای محفظه پنهان مدیریت کابل‌ها و شیار ابزارهای مهندسی.",
                ShortDescription = "شاسی یکپارچه آلومینیوم آنودایز شده با چوب گردوی طبیعی",
                Price = 3850000,
                CompareAtPrice = 4200000,
                StockQuantity = 15,
                IsActive = true,
                IsFeatured = true,
                Material = "Anodized Aluminum 6061 & Solid Walnut",
                Dimensions = "550 × 220 × 90 mm",
                Weight = "2100 g",
                Color = "مشکی مات و قهوه‌ای گردویی",
                Technique = "Laser Sheet Bending & CNC Mill",
                Tolerance = "±0.05 mm",
                CategoryId = catTls.Id,
                ImageUrl = "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?auto=format&fit=crop&w=800&q=80",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "مجموعه چرخ‌دنده‌های دقیق SLS فیبرکربن",
                Slug = "precision-sls-carbon-gears",
                Sku = "HZ-SLS-GEAR-SET",
                Description = "ست چرخ‌دنده‌های خورشیدی و هلیکال صنعتی پرینت شده با فناوری لیزری SLS و متریال PA12-CF (پلی‌آمید تقویت شده با فیبر کربن). خود‌روان‌کننده با مقاومت حرارتی تا ۱۶۰ درجه سانتی‌گراد.",
                ShortDescription = "پرینت ۳بعدی صنعتی SLS با پلیمر تقویت‌شده با فیبرکربن",
                Price = 1750000,
                CompareAtPrice = 1950000,
                StockQuantity = 25,
                IsActive = true,
                IsFeatured = false,
                Material = "Polyamide 12 Carbon-Filled (PA12-CF)",
                Dimensions = "قطرهای ۲۰ تا ۸۰ میلیمتر",
                Weight = "180 g (ست کامل)",
                Color = "خاکستری تیره مات فیبرکربن",
                Technique = "Selective Laser Sintering (SLS)",
                Tolerance = "±0.05 mm",
                CategoryId = cat3dp.Id,
                ImageUrl = "https://images.unsplash.com/photo-1581092335878-2d9ff86ca2bf?auto=format&fit=crop&w=800&q=80",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "خط‌کش کولیس و خط‌کشی مهندسی تیتانیومی",
                Sku = "HZ-TLS-RULER-TI",
                Slug = "titanium-precision-engineering-scale",
                Description = "خط‌کش مهندسی دقیق با حکاکی لیزری نانو متری در دو سیستم متریک و امپریال. ساخته شده از ورق گرید ۵ تیتانیوم ضدخش و ضداکسایش با سختی بالا.",
                ShortDescription = "ورق تیتانیوم گرید ۵ با حکاکی لیزری فایبر نانو",
                Price = 1200000,
                CompareAtPrice = 1450000,
                StockQuantity = 30,
                IsActive = true,
                IsFeatured = false,
                Material = "Titanium Grade 5 (Ti-6Al-4V)",
                Dimensions = "200 × 25 × 2 mm",
                Weight = "45 g",
                Color = "تیتانیوم سندبلاست شده",
                Technique = "Fiber Laser Engraving & Wire EDM",
                Tolerance = "±0.005 mm",
                CategoryId = catTls.Id,
                ImageUrl = "https://images.unsplash.com/photo-1581092162384-8987c1d64718?auto=format&fit=crop&w=800&q=80",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSpecialOrdersAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.SpecialOrders.AnyAsync()) return;

        logger.LogInformation("Seeding sample special orders...");

        var sampleOrder = new SpecialOrder
        {
            OrderNumber = "FAB-20250916-4102",
            CustomerName = "مهندس سهراب پارسا",
            CustomerPhone = "09123456789",
            CustomerEmail = "sohrab.parsa@gmail.com",
            Title = "ماشین‌کاری ۵ محوره ایمپلر توربین میکرو",
            Category = "ماشین‌کاری CNC",
            Description = "نیاز به تراشکاری و فرزکاری یک قطعه ایمپلر ۱۲ پره از جنس آلومینیوم 7075 با تلورانس متقارن ±0.01mm و فینیشینگ سوپرفاین بدون پلیسه. فایل STEP ضمیمه خواهد شد.",
            MaterialPreference = "AL-7075-T6",
            EstimatedBudget = 15000000,
            QuotedPrice = 14200000,
            Status = SpecialOrderStatus.Reviewing,
            AdminReply = "فایل CAD بررسی شد و پارامترهای DFM تایید شد. زمان تحویل ۴ روز کاری پس از تایید نهایی خواهد بود.",
            AdminRepliedAt = DateTime.UtcNow.AddHours(-12),
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        context.SpecialOrders.Add(sampleOrder);
        await context.SaveChangesAsync();
    }
}
