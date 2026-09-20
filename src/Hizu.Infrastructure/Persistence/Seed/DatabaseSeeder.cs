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
        await SeedCoursesAsync(context, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager, ILogger logger)
    {
        string[] roles = [AppRoles.SysManager, AppRoles.Admin, AppRoles.Customer];
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
        // 1. Admin / System Manager User
        var adminEmail = "admin@hizu.ir";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            logger.LogInformation("Seeding Admin & SysManager user: {Email}", adminEmail);
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
                await userManager.AddToRoleAsync(adminUser, AppRoles.SysManager);
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                await userManager.AddToRoleAsync(adminUser, AppRoles.Customer);
                logger.LogInformation("Admin user created successfully with SysManager & Admin roles.");
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Verify and synchronize admin password
            var isPasswordValid = await userManager.CheckPasswordAsync(adminUser, "Admin123!");
            if (!isPasswordValid)
            {
                logger.LogWarning("Admin user password mismatch detected in database. Resetting password to Admin123!...");
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                var resetResult = await userManager.ResetPasswordAsync(adminUser, resetToken, "Admin123!");
                if (!resetResult.Succeeded)
                {
                    await userManager.RemovePasswordAsync(adminUser);
                    var addResult = await userManager.AddPasswordAsync(adminUser, "Admin123!");
                    if (addResult.Succeeded)
                    {
                        logger.LogInformation("Admin user password reset to Admin123! via AddPasswordAsync.");
                    }
                    else
                    {
                        logger.LogError("Failed to reset admin user password: {Errors}", string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogInformation("Admin user password successfully synchronized to Admin123!.");
                }
            }

            // Ensure SysManager and Admin roles
            if (!await userManager.IsInRoleAsync(adminUser, AppRoles.SysManager))
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.SysManager);
                logger.LogInformation("Assigned SysManager role to {Email}.", adminEmail);
            }
            if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                logger.LogInformation("Assigned Admin role to {Email}.", adminEmail);
            }
            if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Customer))
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Customer);
            }

            // Ensure confirmed & unlocked
            if (!adminUser.EmailConfirmed)
            {
                adminUser.EmailConfirmed = true;
                await userManager.UpdateAsync(adminUser);
            }

            if (await userManager.IsLockedOutAsync(adminUser))
            {
                await userManager.SetLockoutEndDateAsync(adminUser, null);
                await userManager.ResetAccessFailedCountAsync(adminUser);
                logger.LogInformation("Unlocked admin account.");
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
                logger.LogInformation("Demo user created successfully with password Demo123!");
            }
            else
            {
                logger.LogError("Failed to create demo user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            var isPasswordValid = await userManager.CheckPasswordAsync(demoUser, "Demo123!");
            if (!isPasswordValid)
            {
                logger.LogWarning("Demo user password mismatch detected. Resetting password to Demo123!...");
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(demoUser);
                var resetResult = await userManager.ResetPasswordAsync(demoUser, resetToken, "Demo123!");
                if (!resetResult.Succeeded)
                {
                    await userManager.RemovePasswordAsync(demoUser);
                    await userManager.AddPasswordAsync(demoUser, "Demo123!");
                }
            }

            if (!await userManager.IsInRoleAsync(demoUser, AppRoles.Customer))
            {
                await userManager.AddToRoleAsync(demoUser, AppRoles.Customer);
            }

            if (!demoUser.EmailConfirmed)
            {
                demoUser.EmailConfirmed = true;
                await userManager.UpdateAsync(demoUser);
            }

            if (await userManager.IsLockedOutAsync(demoUser))
            {
                await userManager.SetLockoutEndDateAsync(demoUser, null);
                await userManager.ResetAccessFailedCountAsync(demoUser);
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

    private static async Task SeedCoursesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Courses.AnyAsync()) return;

        logger.LogInformation("Seeding sample educational courses...");

        var course1 = new Course
        {
            Id = Guid.NewGuid(),
            Title = "دوره جامع طراحی و مدلسازی صنعتی قطعات با Fusion 360",
            Slug = "fusion360-industrial-cad",
            Subtitle = "آموزش گام‌به‌گام مدلسازی سه‌بعدی پارامتریک، مهندسی معکوس، تلرانس‌گذاری و آماده‌سازی CAM برای ساخت دیجیتال",
            Description = "نرم‌افزار Autodesk Fusion 360 به عنوان یکی از قدرتمندترین ابزارهای طراحی مهندسی (CAD) و ماشین‌کاری کامپیوتری (CAM) در جهان شناخته می‌شود. در این دوره آموزشی تخصصی استودیو هیزو، از مبانی ترسیم اسکچ‌های دوبعدی و تعریف قیدهای پارامتریک شروع کرده و تا طراحی مجموعه‌های مکانیکی پیچیده، تحلیل حرکتی، رندرینگ صنعتی و استخراج کد ماشین‌کاری CNC پیش خواهیم رفت. تمامی پروژه‌ها بر پایه مسائل واقعی کارگاهی و ساخت صنعتی طراحی شده‌اند.",
            Price = 2450000m,
            CompareAtPrice = 3200000m,
            CoverImageUrl = "https://images.unsplash.com/photo-1581092160607-ee22621dd758?auto=format&fit=crop&w=1200&q=80",
            IntroVideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4",
            FileServerBaseUrl = "https://files.hizu.ir",
            StoragePath = "/courses/fusion360-cad/",
            DownloadableFilesUrl = "https://files.hizu.ir/courses/fusion360-cad/exercise-files.zip",
            DurationMinutes = 1320,
            DurationText = "۲۲ ساعت ویدیو تخصصی",
            Level = CourseLevel.AllLevels,
            Language = "فارسی",
            TotalEpisodesCount = 9,
            DownloadableResourcesCount = 14,
            IncludesCertificate = true,
            HasLifetimeAccess = true,
            InstructorName = "مهندس علیرضا سلیمانی",
            InstructorTitle = "سرپرست واحد طراحی و مهندسی معکوس هیزو",
            InstructorBio = "کارشناس ارشد مهندسی مکانیک ساخت و تولید با بیش از ۱۰ سال سابقه مدلسازی سیستم‌های مکاترونیک و نظارت بر ساخت تجهیزات صنعتی دقیق.",
            InstructorAvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=300&q=80",
            WhatYouWillLearn = "تسلط بر ترسیمات دو‌بعدی و قیود پارامتریک در Fusion 360\nمدلسازی قطعات صلب و ارگانیک با ابزارهای Solid و Freeform Sculpting\nطراحی مونتاژی سیستم‌های مکانیکی و تعریف مفصل‌های حرکتی (Joints)\nاستخراج نقشه‌های اجرایی دو‌بعدی با استانداردهای تلرانس GD&T\nآماده‌سازی مدل‌ها برای ماشین‌کاری CNC در محیط CAM فیوژن ۳۶۰\nاصول طراحی ویژه پرینت ۳بعدی و قالب‌سازی (DFM)",
            Requirements = "آشنایی اولیه با کامپیوتر و سیستم‌عامل ویندوز یا مک\nنصب نرم‌افزار Autodesk Fusion 360 (آموزش نصب در جلسه اول ارائه شده است)\nعلاقه‌مندی به طراحی مهندسی، مدلسازی صنعتی و ساخت فیزیکی قطعات",
            TargetAudience = "مهندسان مکانیک، مکاترونیک و طراحی صنعتی\nعلاقه‌مندان به ورود به بازار کار طراحی دیجیتال و کارگاه‌های ساخت\nسازندگان، اپراتورهای پرینتر سه‌بعدی و ماشین‌کاری CNC\nدانشجویان فنی که به دنبال یادگیری پروژه‌محور و ورود مستقیم به کارگاه هستند",
            IsPublished = true,
            IsFeatured = true,
            DisplayOrder = 1,
            AverageRating = 4.95m,
            RatingCount = 38,
            StudentsCount = 142,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var sec1_1 = new CourseSection
        {
            Id = Guid.NewGuid(),
            CourseId = course1.Id,
            Title = "فصل اول: آشنایی با محیط، تفکر پارامتریک و ترسیم اسکچ‌های مهندسی",
            Description = "در این فصل با رابط کاربری نرم‌افزار، تنظیمات واحدهای کاری و استراتژی اسکچ‌زنی دقیق آشنا می‌شوید.",
            DisplayOrder = 1
        };
        var ep1_1_1 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course1.Id,
            SectionId = sec1_1.Id,
            Title = "جلسه ۱: معرفی دوره، نصب نرم‌افزار و فلسفه طراحی پارامتریک",
            Description = "توضیح نقشه راه یادگیری، کانفیگ اولیه محیط کاری و بررسی نمونه پروژه‌های نهایی کارگاهی.",
            DurationMinutes = 24,
            DurationText = "۲۴:۱۵",
            VideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4",
            IsFreePreview = true,
            DisplayOrder = 1
        };
        var ep1_1_2 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course1.Id,
            SectionId = sec1_1.Id,
            Title = "جلسه ۲: ترسیم دوبعدی و قیود هندسی (Geometric Constraints)",
            Description = "قیدهای هم‌مرکزی، تانژانت، تقارن و ابعادگذاری هوشمند برای طرح‌های کاملاً مقید (Fully Defined).",
            DurationMinutes = 38,
            DurationText = "۳۸:۴۰",
            VideoUrl = "https://files.hizu.ir/courses/fusion360-cad/ep02.mp4",
            IsFreePreview = false,
            DisplayOrder = 2
        };
        var ep1_1_3 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course1.Id,
            SectionId = sec1_1.Id,
            Title = "جلسه ۳: پروژه‌ی عملی اول؛ طراحی یک فلنج کوپلینگ صنعتی",
            Description = "پیاده‌سازی تمام قیود آموخته شده در مدلسازی قطعه واقعی صنعتی.",
            DurationMinutes = 45,
            DurationText = "۴۵:۱۰",
            VideoUrl = "https://files.hizu.ir/courses/fusion360-cad/ep03.mp4",
            IsFreePreview = false,
            DisplayOrder = 3
        };
        sec1_1.Episodes = [ep1_1_1, ep1_1_2, ep1_1_3];

        var sec1_2 = new CourseSection
        {
            Id = Guid.NewGuid(),
            CourseId = course1.Id,
            Title = "فصل دوم: تکنیک‌های پیشرفته سالید مدلینگ و طراحی مجموعه مونتاژی",
            Description = "ابزارهای Loft, Sweep, Rib و ساخت قطعات متحرک به همراه Joint ها و جلوگیری از تداخل (Interference Detection).",
            DisplayOrder = 2
        };
        var ep1_2_1 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course1.Id,
            SectionId = sec1_2.Id,
            Title = "جلسه ۴: سطوح پیچیده و حجم‌سازی با Swept & Loft Bodies",
            Description = "طراحی مقاطع متغیر مکانیکی مانند پروانه‌ها و بازوهای منحنی شکل.",
            DurationMinutes = 52,
            DurationText = "۵۲:۰۰",
            VideoUrl = "https://files.hizu.ir/courses/fusion360-cad/ep04.mp4",
            IsFreePreview = false,
            DisplayOrder = 1
        };
        var ep1_2_2 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course1.Id,
            SectionId = sec1_2.Id,
            Title = "جلسه ۵: اسمبلی قطعات، انیمیشن حرکتی و بررسی لقی‌ها",
            Description = "اتصال قطعات با Revolute, Slider و Cylindrical Joints و تست حرکت مکانیکی.",
            DurationMinutes = 60,
            DurationText = "۶۰:۳۰",
            VideoUrl = "https://files.hizu.ir/courses/fusion360-cad/ep05.mp4",
            IsFreePreview = false,
            DisplayOrder = 2
        };
        var ep1_2_3 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course1.Id,
            SectionId = sec1_2.Id,
            Title = "جلسه ۶: پروژه‌ی عملی دوم؛ طراحی گیربکس دو سرعته مینیاتوری",
            Description = "مدلسازی شفت، چرخ‌دنده‌های درگیر و بدنه نگه‌دارنده بلبرینگ‌ها.",
            DurationMinutes = 75,
            DurationText = "۷۵:۲۰",
            VideoUrl = "https://files.hizu.ir/courses/fusion360-cad/ep06.mp4",
            IsFreePreview = false,
            DisplayOrder = 3
        };
        sec1_2.Episodes = [ep1_2_1, ep1_2_2, ep1_2_3];

        var sec1_3 = new CourseSection
        {
            Id = Guid.NewGuid(),
            CourseId = course1.Id,
            Title = "فصل سوم: استخراج نقشه‌های کارگاهی و آماده‌سازی CAM و ساخت",
            Description = "نقشه‌کشی استاندارد، جداول تلرانس، استخراج STEP و خروجی مسیر ابزار G-code برای فرزکاری.",
            DisplayOrder = 3
        };
        var ep1_3_1 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course1.Id,
            SectionId = sec1_3.Id,
            Title = "جلسه ۷: تولید نقشه فنی اجرایی با تلرانس‌های هندسی GD&T",
            Description = "نحوه ثبت علائم پرداخت سطح، تلرانس‌های هم‌راستایی و ابعاد ایزو در شیت استاندارد نقشه.",
            DurationMinutes = 48,
            DurationText = "۴۸:۱۵",
            VideoUrl = "https://files.hizu.ir/courses/fusion360-cad/ep07.mp4",
            IsFreePreview = false,
            DisplayOrder = 1
        };
        var ep1_3_2 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course1.Id,
            SectionId = sec1_3.Id,
            Title = "جلسه ۸: محیط ساخت و تولید CAM؛ استراتژی‌های ۲.۵ و ۳ محوره",
            Description = "تعریف بلوک خام (Stock)، انتخاب فرزها و تنظیم استراتژی Adaptive Clearing.",
            DurationMinutes = 65,
            DurationText = "۶۵:۰۰",
            VideoUrl = "https://files.hizu.ir/courses/fusion360-cad/ep08.mp4",
            IsFreePreview = false,
            DisplayOrder = 2
        };
        var ep1_3_3 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course1.Id,
            SectionId = sec1_3.Id,
            Title = "جلسه ۹: شبیه‌سازی مسیر ابزار و استخراج G-code برای کنترلر دستگاه",
            Description = "پست‌پراسس نهایی برای کنترلرهای Fanuc، Siemens و Mach3 کارگاهی.",
            DurationMinutes = 40,
            DurationText = "۴۰:۲۰",
            VideoUrl = "https://files.hizu.ir/courses/fusion360-cad/ep09.mp4",
            IsFreePreview = false,
            DisplayOrder = 3
        };
        sec1_3.Episodes = [ep1_3_1, ep1_3_2, ep1_3_3];

        course1.Sections = [sec1_1, sec1_2, sec1_3];
        course1.Episodes = [ep1_1_1, ep1_1_2, ep1_1_3, ep1_2_1, ep1_2_2, ep1_2_3, ep1_3_1, ep1_3_2, ep1_3_3];

        // Course 2: 3D Printing Masterclass
        var course2 = new Course
        {
            Id = Guid.NewGuid(),
            Title = "دوره تخصصی پرینت سه‌بعدی صنعتی، اسلایسر و فیلامنت‌های مهندسی",
            Slug = "3d-printing-masterclass",
            Subtitle = "تسلط بر اسلایسرهای Cura و PrusaSlicer، کالیبراسیون سخت‌افزاری و پرینت قطعات صنعتی با نایلون کربن، پلی‌کربنات و TPU",
            Description = "پرینت سه‌بعدی فراتر از صرفاً فشردن دکمه پرینت است! در این دوره کارگاهی هیزو، از کالیبراسیون مکانیکی و الکترونیکی پرینترهای صنعتی آغاز کرده و عمیقاً وارد تنظیمات تخصصی اسلایسرها می‌شویم. یاد می‌گیرید که چگونه زاویه لایه‌نشانی را برای مقاومت مکانیکی بالا تنظیم کنید، دمای انکلوژر را برای متریال‌های سخت مهار کرده و تلرانس‌های میلی‌متری بسیار دقیق در قطعات پیچیده ایجاد نمایید.",
            Price = 1890000m,
            CompareAtPrice = 2500000m,
            CoverImageUrl = "https://images.unsplash.com/photo-1631541909061-71e349d1f203?auto=format&fit=crop&w=1200&q=80",
            IntroVideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerEscapes.mp4",
            FileServerBaseUrl = "https://files.hizu.ir",
            StoragePath = "/courses/3d-printing/",
            DownloadableFilesUrl = "https://files.hizu.ir/courses/3d-printing/slicer-profiles.zip",
            DurationMinutes = 960,
            DurationText = "۱۶ ساعت آموزش کاربردی",
            Level = CourseLevel.Intermediate,
            Language = "فارسی",
            TotalEpisodesCount = 8,
            DownloadableResourcesCount = 18,
            IncludesCertificate = true,
            HasLifetimeAccess = true,
            InstructorName = "مهندس مسعود کریمی",
            InstructorTitle = "مدیر کارگاه نمونه‌سازی سریع و ساخت افزایشی هیزو",
            InstructorBio = "مدرس دوره‌های ساخت افزایشی صنعتی با تخصص در فرآیندهای FDM، SLA و فرمولاسیون رزین‌های مهندسی با تلرانس‌های میکرونی.",
            InstructorAvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=300&q=80",
            WhatYouWillLearn = "کالیبراسیون دقیق اکسترودر، E-Steps و فلو ریت مواد\nتنظیم پروفایل‌های اختصاصی در Cura و PrusaSlicer برای انواع متریال\nپرینت بدون تابیدگی فیلامنت‌های دشوار نظیر Nylon، PC و ASA\nکنترل ساپورت‌گذاری ارگانیک و درختی بدون آسیب به کیفیت سطح\nتکنیک‌های Post-Processing، پرداخت شیمیایی و رزوه‌کشی صنعتی قطعات\nعیب‌یابی پیشرفته: Stringing، Under-extrusion و Z-banding",
            Requirements = "آشنایی عمومی با تکنولوژی پرینت ۳بعدی\nدسترسی به یک دستگاه پرینتر ۳بعدی (جهت انجام پروژه‌های عملی دوره)",
            TargetAudience = "اپراتورها و دارندگان کارگاه‌های خدمات پرینت ۳بعدی\nطراحان محصول و مخترعانی که قصد ساخت پروتوتایپ کاربردی دارند\nعلاقه‌مندان به ارتقای کیفیت قطعات پرینت شده از حالت سرگرمی به استاندارد صنعتی",
            IsPublished = true,
            IsFeatured = true,
            DisplayOrder = 2,
            AverageRating = 4.90m,
            RatingCount = 29,
            StudentsCount = 98,
            CreatedAt = DateTime.UtcNow.AddDays(-20)
        };

        var sec2_1 = new CourseSection
        {
            Id = Guid.NewGuid(),
            CourseId = course2.Id,
            Title = "فصل اول: کالیبراسیون سخت‌افزاری و تنظیمات مکانیکی پرینتر",
            Description = "بررسی استپ موتورها، درایورهای بی‌صدا، تنظیم اکستروژن و تراز خودکار بد.",
            DisplayOrder = 1
        };
        var ep2_1_1 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course2.Id,
            SectionId = sec2_1.Id,
            Title = "جلسه ۱: آناتومی یک پرینتر صنعتی و چک‌لیست قبل از استارت",
            Description = "معرفی اجزای مکانیکی، هات‌اند تمام‌فلزی، نازل‌های ضدسایش و محفظه دمایی.",
            DurationMinutes = 28,
            DurationText = "۲۸:۴۵",
            VideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerEscapes.mp4",
            IsFreePreview = true,
            DisplayOrder = 1
        };
        var ep2_1_2 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course2.Id,
            SectionId = sec2_1.Id,
            Title = "جلسه ۲: کالیبراسیون E-step و PID Tuning حرارتی نازل و هیت‌بد",
            Description = "فرمول دقیق محاسبه استپ‌های اکسترودر و جلوگیری از نوسان دما با الگوریتم PID.",
            DurationMinutes = 35,
            DurationText = "۳۵:۱۰",
            VideoUrl = "https://files.hizu.ir/courses/3d-printing/ep02.mp4",
            IsFreePreview = false,
            DisplayOrder = 2
        };
        sec2_1.Episodes = [ep2_1_1, ep2_1_2];

        var sec2_2 = new CourseSection
        {
            Id = Guid.NewGuid(),
            CourseId = course2.Id,
            Title = "فصل دوم: مسترینگ اسلایسرها (Cura & PrusaSlicer)",
            Description = "تنظیم تراکم پرکردن هوشمند (Adaptive Infill)، الگوهای ژیروید و کنترل دیواره‌ها.",
            DisplayOrder = 2
        };
        var ep2_2_1 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course2.Id,
            SectionId = sec2_2.Id,
            Title = "جلسه ۳: ارتفاع لایه تطبیقی (Adaptive Layers) و جهت‌گیری زاویه قطعه",
            Description = "افزایش سرعت پرینت بدون افت کیفیت سطوح منحنی با تنظیم خودکار لایه‌ها.",
            DurationMinutes = 44,
            DurationText = "۴۴:۰۰",
            VideoUrl = "https://files.hizu.ir/courses/3d-printing/ep03.mp4",
            IsFreePreview = false,
            DisplayOrder = 1
        };
        var ep2_2_2 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course2.Id,
            SectionId = sec2_2.Id,
            Title = "جلسه ۴: ساپورت‌های ارگانیک و درختی در PrusaSlicer",
            Description = "تکنیک‌های ساپورت‌گذاری که با دست و بدون ردپای زبری به راحتی جدا می‌شوند.",
            DurationMinutes = 40,
            DurationText = "۴۰:۵۰",
            VideoUrl = "https://files.hizu.ir/courses/3d-printing/ep04.mp4",
            IsFreePreview = false,
            DisplayOrder = 2
        };
        sec2_2.Episodes = [ep2_2_1, ep2_2_2];

        course2.Sections = [sec2_1, sec2_2];
        course2.Episodes = [ep2_1_1, ep2_1_2, ep2_2_1, ep2_2_2];

        // Course 3: CNC Machining and Operations
        var course3 = new Course
        {
            Id = Guid.NewGuid(),
            Title = "دوره پیشرفته برنامه‌نویسی و اپراتوری ماشین‌کاری CNC",
            Slug = "cnc-machining-and-gcode",
            Subtitle = "اصول ماشین‌کاری قطعات فلزی، شناخت اینسرت‌ها، نگارش G-Code دستی و راه‌اندازی فرز ۳ محوره کارگاهی",
            Description = "ماشین‌کاری CNC شالوده صنایع ساخت دقیق است. در این دوره تخصصی، زبان بین‌المللی کدهای حرکتی دستگاه‌های تراش و فرز (G-Code و M-Code) را به شکل پایه‌ای و عملیاتی یاد می‌گیرید. نحوه انتخاب سرعت برشی (Cutting Speed) و نرخ پیشروی (Feed Rate) برای آلیاژهای آلومینیوم، برنج و فولاد را محاسبه می‌کنید و صفر قطعه‌کار را با ابزارهای دقیق سنسور لبه‌یاب تنظیم خواهید کرد.",
            Price = 3200000m,
            CompareAtPrice = 4100000m,
            CoverImageUrl = "https://images.unsplash.com/photo-1504917599217-d4dc5ebe6122?auto=format&fit=crop&w=1200&q=80",
            IntroVideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerJoyBlazes.mp4",
            FileServerBaseUrl = "https://files.hizu.ir",
            StoragePath = "/courses/cnc-machining/",
            DownloadableFilesUrl = "https://files.hizu.ir/courses/cnc-machining/gcode-examples.zip",
            DurationMinutes = 1680,
            DurationText = "۲۸ ساعت آموزش عملیاتی",
            Level = CourseLevel.Advanced,
            Language = "فارسی",
            TotalEpisodesCount = 12,
            DownloadableResourcesCount = 22,
            IncludesCertificate = true,
            HasLifetimeAccess = true,
            InstructorName = "مهندس کامران رستمی",
            InstructorTitle = "سرپرست واحد ماشین‌کاری و خط تولید CNC هیزو",
            InstructorBio = "بیش از ۱۵ سال تجربه برنامه‌نویسی ماشین‌های فرز و تراش CNC چند محوره در صنایع هوافضا و نفت و گاز.",
            InstructorAvatarUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=300&q=80",
            WhatYouWillLearn = "ساختار سیستم‌های مختصات کارگاهی (G54 تا G59) و صفر دستگاه (Machine Zero)\nنگارش دستی برنامه‌های G-Code برای عملیات شیارزنی، پاکت‌تراشی و سوراخ‌کاری سیکلی\nمحاسبه پارامترهای ماشین‌کاری، سرعت دوران اسپیندل و طول عمر ابزار\nانتخاب الماسه و اینسرت مناسب بر اساس متریال آلیاژی\nفیکسچربندی مطمئن و کنترل تغییرشکل قطعه ناشی از تنش‌های حرارتی براده‌برداری",
            Requirements = "آشنایی با نقشه‌خوانی فنی و استانداردهای صنعتی\nعلاقه‌مندی به کار در محیط کارگاهی و ماشین‌ابزار",
            TargetAudience = "تکنسین‌ها و اپراتورهای ماشین‌های CNC\nمهندسان ساخت و تولید و مدیران کارگاه‌های تولید قطعات\nدانشجویانی که می‌خواهند دانش تئوری خود را به مهارت اجرایی تبدیل کنند",
            IsPublished = true,
            IsFeatured = true,
            DisplayOrder = 3,
            AverageRating = 4.88m,
            RatingCount = 21,
            StudentsCount = 65,
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        };

        var sec3_1 = new CourseSection
        {
            Id = Guid.NewGuid(),
            CourseId = course3.Id,
            Title = "فصل اول: اصول مختصات، ایمنی کارگاهی و کدهای پایه G-Code",
            Description = "دستورات حرکت سریع (G00)، حرکت خطی با پیشروی (G01) و درون‌یابی دایروی (G02/G03).",
            DisplayOrder = 1
        };
        var ep3_1_1 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course3.Id,
            SectionId = sec3_1.Id,
            Title = "جلسه ۱: منطق کاری دستگاه‌های CNC و ساختار برنامه‌نویسی",
            Description = "قوانین نام‌گذاری بلاک‌ها، شماره سطرها و ساختار خطوط فرمان در کنترلرهای صنعتی.",
            DurationMinutes = 32,
            DurationText = "۳۲:۱۰",
            VideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerJoyBlazes.mp4",
            IsFreePreview = true,
            DisplayOrder = 1
        };
        var ep3_1_2 = new CourseEpisode
        {
            Id = Guid.NewGuid(),
            CourseId = course3.Id,
            SectionId = sec3_1.Id,
            Title = "جلسه ۲: سیکل‌های ثابت سوراخ‌کاری (Canned Cycles G81/G83)",
            Description = "سوراخ‌کاری مرحله‌ای (Peck Drilling) برای تخلیه براده در قطعات ضخیم.",
            DurationMinutes = 46,
            DurationText = "۴۶:۲۵",
            VideoUrl = "https://files.hizu.ir/courses/cnc-machining/ep02.mp4",
            IsFreePreview = false,
            DisplayOrder = 2
        };
        sec3_1.Episodes = [ep3_1_1, ep3_1_2];

        course3.Sections = [sec3_1];
        course3.Episodes = [ep3_1_1, ep3_1_2];

        context.Courses.AddRange(course1, course2, course3);
        await context.SaveChangesAsync();
        logger.LogInformation("Sample courses seeded successfully.");
    }
}
