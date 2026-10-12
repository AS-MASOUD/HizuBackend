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
        ILogger logger,
        string? bootstrapAdminEmail,
        string? bootstrapAdminPassword)
    {
        await SeedRolesAsync(roleManager, logger);
        await SeedUsersAsync(userManager, logger, bootstrapAdminEmail, bootstrapAdminPassword);
        await SeedCategoriesAsync(context, logger);
        await SeedProductsAsync(context, logger);
        await SeedSpecialOrdersAsync(context, logger);
        await SeedCoursesAsync(context, logger);
        await SeedArticlesAsync(context, logger);
        await SeedProcessStepsAsync(context, logger);
        await SeedServicesAsync(context, logger);
        await SeedFaqsAsync(context, logger);
        await SeedSlidesAsync(context, logger);
        await SeedAboutContentAsync(context, logger);
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

    private static async Task SeedUsersAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string? bootstrapAdminEmail,
        string? bootstrapAdminPassword)
    {
        if (string.IsNullOrWhiteSpace(bootstrapAdminEmail) &&
            string.IsNullOrWhiteSpace(bootstrapAdminPassword))
        {
            logger.LogInformation(
                "Bootstrap administrator credentials are not configured; skipping administrator seeding.");
            return;
        }

        if (string.IsNullOrWhiteSpace(bootstrapAdminEmail) ||
            string.IsNullOrWhiteSpace(bootstrapAdminPassword))
        {
            throw new InvalidOperationException(
                "Both BootstrapAdmin:Email and BootstrapAdmin:Password must be configured together.");
        }

        var adminEmail = bootstrapAdminEmail.Trim();
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "مدیر سیستم",
                LastName = "هیزو",
                PhoneNumber = "09120000000",
                Address = new Address(
                    "خیابان آزادی، ناحیه نوآوری شریف، کارگاه هیزو",
                    "تهران",
                    "تهران",
                    "1458888888",
                    "ایران"),
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, bootstrapAdminPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to create bootstrap administrator: " +
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }

            logger.LogInformation("Created bootstrap administrator: {Email}", adminEmail);
        }
        else
        {
            // Never reset an existing administrator's password during application startup.
            logger.LogInformation("Bootstrap administrator already exists: {Email}", adminEmail);
        }

        var requiredRoles = new[] { AppRoles.SysManager, AppRoles.Admin, AppRoles.Customer };

        foreach (var role in requiredRoles)
        {
            if (await userManager.IsInRoleAsync(adminUser, role))
                continue;

            var roleResult = await userManager.AddToRoleAsync(adminUser, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign role '{role}' to bootstrap administrator: " +
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
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

    private static async Task SeedArticlesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Articles.AnyAsync()) return;

        logger.LogInformation("Seeding initial engineering articles...");

        var articles = new List<Article>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "راهنمای جامع فیلامنت‌های پرینت سه‌بعدی: مقایسه PLA، PETG، ABS، TPU و نایلون کربن",
                Slug = "3d-printing-filament-comparison-guide",
                Summary = "بررسی تخصصی خواص مکانیکی، دمای اکستروژن، مقاومت حرارتی و رفتار جمع‌شدگی رایج‌ترین متریال‌های پرینت 3بعدی صنعتی و انتخاب بهترین گزینه برای ساخت قطعات در کارگاه هیزو.",
                Category = "متریال و فیلامنت",
                Tags = "پرینت 3بعدی,فیلامنت,PLA,PETG,ABS,PA12-CF,متریال شناسی",
                Author = "تیم مهندسی مواد هیزو",
                AuthorRole = "متخصص متریال و ساخت دیجیتال",
                ReadTimeMinutes = 7,
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-3),
                ViewsCount = 342,
                CoverImage = "https://images.unsplash.com/photo-1581092160607-ee22621dd758?auto=format&fit=crop&w=1200&q=80",
                Content = @"## مقدمه: انتخاب هوشمندانه متریال در ساخت دیجیتال

انتخاب متریال مناسب در فرآیند پرینت سه‌بعدی (FDM / FFF) نقشی تعیین‌کننده در عملکرد مکانیکی، دقت ابعادی و دوام بلندمدت محصول دارد. هر فیلامنت دارای مشخصات حرارتی، انقباضی و پیوند بین لایه‌ای متفاوتی است که پیش از شروع ساخت باید تحلیل شود.

در این مقاله، تجربیات عملی کارگاه هیزو در تست و تولید هزاران قطعه صنعتی را بررسی می‌کنیم تا تفاوت‌های کلیدی بین متریال‌های پرکاربرد را به دقت بشناسید.

---

### 1. مقایسه جامع پارامترهای فنی فیلامنت‌ها

در جدول زیر، مشخصات کلیدی پنج فیلامنت اصلی مقایسه شده است:

| نوع فیلامنت | دمای نازل (°C) | دمای بستر (°C) | مقاومت ضربه‌ای | انقباض و جمع‌شدگی (Warping) | نیاز به انکلوژر محفظه |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **PLA+** | 195 - 215 | 50 - 60 | متوسط | بسیار کم | خیر |
| **PETG** | 230 - 250 | 70 - 85 | خوب | کم | اختیاری |
| **ABS / ASA** | 240 - 260 | 90 - 110 | بسیار بالا | بالا | **بله (ضروری)** |
| **TPU (انعطاف‌پذیر)** | 210 - 230 | 30 - 50 | عالی (الاستیک) | بسیار کم | خیر |
| **PA12-CF (نایلون کربن)** | 260 - 290 | 90 - 110 | فوق‌العاده مقاوم | متوسط | بله |

---

### 2. بررسی ویژگی‌های هر فیلامنت در کاربردهای واقعی

#### الف) پلی‌لاکتیک اسید (PLA / PLA+)
- **مزایا:** پرینت بسیار آسان بدون اعوجاج، جزئیات ظاهری فوق‌العاده، عدم تصاعد گازهای سمی، سازگار با محیط زیست.
- **معایب:** مقاومت دمایی پایین (تغییر فرم در بالای 55 درجه سانتی‌گراد)، شکنندگی در برابر بارهای دینامیکی ضربه‌ای.
- **بهترین کاربرد:** نمونه‌های اولیه ارگونومی، ماکت‌های معماری، قالب‌های سیلیکونی غیرحرارتی و کیس‌های دکوراتیو.

#### ب) پلی‌اتیلن ترفتالات گلیکول (PETG)
- **مزایا:** مقاومت شیمیایی و رطوبتی بالا، انعطاف‌پذیری بیشتر نسبت به PLA، چسبندگی فوق‌العاده بین لایه‌ها، مقاومت حرارتی تا 75 درجه سانتی‌گراد.
- **معایب:** تمایل به رشته‌زنی (Stringing)، نیاز به کنترل دقیق رطوبت فیلامنت قبل از پرینت.
- **بهترین کاربرد:** قطعات مکانیکی سبک، قطعات در تماس با آب و محیط بیرون، پایه‌ها و نگه‌دارنده‌های ابزار کارگاه.

#### ج) آکریلونیتریل بوتادین استایرن (ABS / ASA)
- **مزایا:** مقاومت ضربه‌ای و سایشی بسیار عالی، قابلیت سنباده‌زنی و پولیش با بخار استون، مقاومت دمایی تا 100 درجه سانتی‌گراد.
- **معایب:** انقباض حرارتی شدید (نیاز به هیت‌بد داغ و محفظه ایزوله کاملاً بسته)، بوی متصاعده هنگام ذوب.
- **بهترین کاربرد:** قطعات خودرو، محفظه تجهیزات الکترونیکی صنعتی، چرخ‌دنده‌ها و اتصالات تحت تنش.

#### د) نایلون تقویت‌شده با الیاف کربن (PA12-CF)
- **مزایا:** استحکام مکانیکی نزدیک به آلومینیوم با وزنی بسیار کمتر، مقاومت حرارتی تا 150 درجه، ضریب اصطکاک پایین، ثبات ابعادی بی‌نظیر.
- **معایب:** قیمت بالا، سایندگی شدید نازل‌های برنجی (نیاز به نازل فولاد سخت یا یاقوت)، جذب رطوبت سریع.
- **بهترین کاربرد:** قطعات پهپاد و رباتیک پیشرفته، فیکسچرهای صنعتی ماشین‌کاری و براکت‌های جایگزین فلز.

---

> **توصیه فنی کارگاه هیزو:** برای قطعاتی که در زیر نور آفتاب یا محیط باز نصب می‌شوند، متریال **ASA** به مراتب از ABS عملکرد بهتری دارد، زیرا زنجیره پلیمری آن در برابر اشعه فرابنفش (UV) تخریب نمی‌شود.

### نتیجه‌گیری و انتخاب نهایی
اگر اولویت شما سهولت ساخت و دقت بصری است، PLA انتخاب اول است. برای قطعات کاربردی رومیزی PETG مطمئن‌ترین انتخاب به شمار می‌رود، و برای قطعات نهایی با کارکرد مکانیکی سنگین، نایلون کربن PA12-CF در آزمایشگاه هیزو استاندارد طلایی است."
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "فرآیند طراحی محصول صنعتی: از ایده اولیه و CAD تا نمونه فیزیکی در هیزو",
                Slug = "industrial-product-design-and-prototyping-workflow",
                Summary = "گام‌به‌گام نحوه مهندسی محصول، بهینه‌سازی ساخت‌پذیری (DFM)، تحلیل تلورانس‌ها و ساخت پروتوتایپ‌های صنعتی ترکیبی با CNC و پرینت 3بعدی.",
                Category = "طراحی محصول",
                Tags = "طراحی صنعتی,DFM,CAD,نمونه‌سازی سریع,مهندسی معکوس",
                Author = "سهراب پارسا",
                AuthorRole = "مدیر ارشد طراحی صنعتی هیزو",
                ReadTimeMinutes = 6,
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-6),
                ViewsCount = 289,
                CoverImage = "https://images.unsplash.com/photo-1581092580497-e0d23cbdf1dc?auto=format&fit=crop&w=1200&q=80",
                Content = @"## از طراحی مفهومی تا کارگاه ساخت فیزیکی

در توسعه محصولات سخت‌افزاری و مکاترونیک، فاصله میان یک ایده روی کاغذ و محصولی که به درستی مونتاژ و آزمایش شود، نیازمند یک فرآیند مهندسی نظام‌مند است. در استودیو طراحی و ساخت هیزو، این مسیر به هفت گام استاندارد تفکیک شده است.

---

### مراحل هفت‌گانه توسعه محصول در هیزو

#### 1. تعریف نیازمندی‌های محصول (PRD)
پیش از باز کردن نرم‌افزارهای سه‌بعدی، مشخصات عملکردی، شرایط محیطی کارکرد (دما، رطوبت، ارتعاش)، حجم تولید هدف و بازه قیمتی تدوین می‌شود.

#### 2. اسکچینگ و طراحی فرم ارگونومیک
بررسی تعامل کاربر با قطعه، محل قرارگیری پورت‌ها، کلیدها یا شاسی‌های اتصال از طریق مدل‌سازی خمیری و اسکچ‌های سریع دیجیتال.

#### 3. مدل‌سازی سه‌بعدی پارامتریک در CAD
ایجاد هندسه دقیق با نرم‌افزارهای SolidWorks و Rhino با تعریف متغیرهای پارامتریک برای اصلاح سریع در فازهای بعدی.

#### 4. تحلیل ساخت‌پذیری (Design for Manufacturing - DFM)
آیا قطعه بدون ایجاد تیغه‌برخورد در دستگاه 5 محوره CNC قابل ماشین‌کاری است؟ آیا ضخامت دیواره‌ها در پرینت سه‌بعدی تحمل خمش دارند؟ در این مرحله زوایای درفت (Draft Angle) و شعاع فیلت‌ها بهینه‌سازی می‌شوند.

#### 5. پروتوتایپ سریع اولیه (Rapid Prototyping)
پرینت سریع 24 ساعته با پرینترهای ابعاد بزرگ هیزو جهت ارزیابی ابعاد واقعی در دست (Fit & Form Test).

#### 6. ساخت قطعه عملکردی نهایی با متریال مهندسی
فرزکاری آلومینیوم 7075 با CNC یا پخت لیزری نایلون کربن برای آزمون‌های بارگذاری مکانیکی و کارکرد تحت فشار واقعی.

#### 7. تولید محدود و تدوین مستندات تولید انبوه
تهیه نقشه‌های دوبعدی انفجاری مطابق استاندارد ISO، تحلیل تلورانس زنجیره‌ای (Tolerance Stack-up) و فیکسچربندی برای خط تولید.

---

> **نکته کلیدی مهندسی:** هزینه اصلاح یک خطای طراحی در فاز CAD یک دلار است، در فاز پروتوتایپ ده دلار، در فاز قالب‌سازی هزار دلار، و پس از تحویل به مشتری ده‌هزار دلار! آزمون زودهنگام نمونه اولیه، مهم‌ترین ابزار کاهش ریسک پروژه است."
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "فناوری پرینت سه‌بعدی پخت لیزری SLS در برابر FDM صنعتی: کدام متد مناسب پروژه شماست؟",
                Slug = "industrial-sls-vs-fdm-comparison",
                Summary = "تحلیل مقایسه‌ای دو روش برتر پرینت صنعتی از نظر خواص ایزوتروپیک، مقاومت در برابر فشار، عدم نیاز به ساپورت و صرفه اقتصادی در تیراژهای نیمه‌صنعتی.",
                Category = "پرینت 3بعدی",
                Tags = "SLS,FDM,فناوری ساخت,پرینت صنعتی,تکنولوژی",
                Author = "کارگروه فناوری هیزو",
                AuthorRole = "بخش پژوهش و توسعه",
                ReadTimeMinutes = 5,
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-9),
                ViewsCount = 195,
                CoverImage = "https://images.unsplash.com/photo-1565043589221-1a6fd9ae45c7?auto=format&fit=crop&w=1200&q=80",
                Content = @"## رقابت دو غول ساخت افزایشی: FDM و SLS

فناوری‌های **FDM (Fused Deposition Modeling)** و **SLS (Selective Laser Sintering)** هر دو ستون‌های اصلی ساخت قطعات پلیمری هستند، اما رویکرد آن‌ها در لایه‌نشانی متریال کاملاً متفاوت است.

### تفاوت اساسی مکانیسم ساخت
- در **FDM** یک رشته ترموپلاستیک پیوسته ذوب شده و روی میز کار لایه‌به‌لایه‌ اکسترود می‌شود. پیوند بین لایه‌ها فیزیکی-حرارتی است و مقاومت در جهت محور Z کمتر از محورهای X و Y است (خاصیت آن‌ایزوتروپیک).
- در **SLS** یک پرتو لیزر پرتوان CO2 ذرات میکرونی پودر پلیمر را به یکدیگر جوش می‌دهد. پودر فشرده نشده پیرامون قطعه به عنوان ساپورت عمل کرده و نیاز به هرگونه داربست ساپورتی را از بین می‌برد. در نتیجه قطعه در تمام جهات خواص مقاومتی یکنواخت (ایزوتروپیک) دارد.

---

### جدول مقایسه سر‌به‌سر

| مشخصه فنی | تکنولوژی FDM صنعتی | تکنولوژی SLS پخت لیزری |
| :--- | :--- | :--- |
| **آزادی هندسی و شیارهای داخلی** | متوسط (محدود به زاویه ساپورت) | **نامحدود (ساپورت طبیعی با پودر)** |
| **رفتار مکانیکی محور Z** | حدود 60% تا 75% محور XY | **بالای 90% (تقریباً ایزوتروپیک کامل)** |
| **زبری سطح و کیفیت ظاهری** | خطوط لایه مشهود (نیاز به سنباده) | سطح مات شنی یکنواخت و حرفه‌ای |
| **هزینه نمونه تکی** | بسیار اقتصادی | مناسب برای قطعات حساس و تیراژ متوسط |
| **هوابندی و مقاومت در برابر فشار مایعات** | نیاز به سیلر و پرداخت شیمیایی | مناسب برای محفظه‌های فیلتر و هیدرولیک سبک |

---

### چه زمانی کدام روش را در هیزو سفارش دهیم؟
1. **FDM را انتخاب کنید اگر:** به دنبال پروتوتایپ سریع ارزان‌قیمت، بررسی ابعاد قطعه یا قطعات حجیم بزرگ‌تر از 500 میلیمتر با هزینه مناسب هستید.
2. **SLS را انتخاب کنید اگر:** قطعه نهایی با کانال‌های خنک‌کاری پیچیده، قطعات زنده یا لولایی متحرک یکپارچه، یا قطعات نهایی با ظاهر کارخانه‌ای و مقاومت برابر در تمام محورها می‌خواهید."
            }
        };

        context.Articles.AddRange(articles);
        await context.SaveChangesAsync();
        logger.LogInformation("Engineering articles seeded successfully.");
    }

    private static async Task SeedProcessStepsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.ProcessSteps.AnyAsync()) return;

        logger.LogInformation("Seeding workflow process steps...");

        var steps = new List<ProcessStep>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StepNumber = "01",
                Title = "کشف و تحلیل نیاز",
                Description = "دریافت فایل CAD، اسکچ یا نمونه فیزیکی و بررسی اهداف پروژه.",
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                StepNumber = "02",
                Title = "بررسی مهندسی DFM",
                Description = "بهینه‌سازی هندسه برای ساخت و تعیین دقیق تلرانس‌های مجاز.",
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                StepNumber = "03",
                Title = "انتخاب متریال",
                Description = "تطبیق آلیاژ آلومینیوم، چوب، فیبرکربن یا رزین متناسب با کارکرد.",
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                StepNumber = "04",
                Title = "تولید دقیق کارگاهی",
                Description = "اجرای ماشین‌کاری CNC، پرینت صنعتی و مونتاژ قطعات در آتلیه.",
                DisplayOrder = 4,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                StepNumber = "05",
                Title = "تست کیفیت و تحویل",
                Description = "اندازه‌گیری میکرومتری، کنترل کیفیت نهایی و بسته‌بندی ایمن.",
                DisplayOrder = 5,
                IsActive = true
            }
        };

        context.ProcessSteps.AddRange(steps);
        await context.SaveChangesAsync();
        logger.LogInformation("Workflow process steps seeded successfully.");
    }

    private static async Task SeedServicesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.FabricationServices.AnyAsync()) return;

        logger.LogInformation("Seeding fabrication services...");

        var services = new List<FabricationService>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Slug = "3d-print",
                Code = "PROTOCOL 01",
                Badge = "FDM / SLS / SLA | PA12-CF",
                Coord = "15mm",
                Title = "پرینت 3بعدی صنعتی و نمونه‌سازی سریع",
                ShortTitle = "پرینت 3بعدی صنعتی",
                Subtitle = "LARGE-FORMAT INDUSTRIAL 3D PRINTING & RAPID TOOLING",
                Description = "تولید نمونه‌های دقیق مهندسی، جیگ و فیکسچرهای سبک، پوسته‌های محافظ و قطعات تیراژ با مجهزترین پرینترهای ابعاد صنعتی. هیزو با تکیه بر تکنولوژی‌های FDM با محفظه حرارتی فعال و SLS نوری، قابلیت تزریق و پرینت پلیمرهای مقاوم در برابر ضربه، سایش و حرارت‌های بالا را فراهم کرده است.",
                ShortDescription = "تولید نمونه اولیه دقیق و قطعات نهایی با پلیمرهای مهندسی FDM/SLA/SLS و نایلون کربن.",
                ImageUrl = "https://images.unsplash.com/photo-1581092160607-ee22621dd758?auto=format&fit=crop&w=1200&q=80",
                IconName = "Layers",
                SpecsJson = "[{\"label\":\"تلورانس ابعادی\",\"value\":\"±0.05 میلی‌متر مطابق استاندارد\"},{\"label\":\"حداکثر ابعاد ساخت یکپارچه\",\"value\":\"500×500×600 میلی‌متر\"},{\"label\":\"رزولوشن لایه‌گذاری\",\"value\":\"20 تا 200 میکرون قابل انتخاب\"},{\"label\":\"مقاومت حرارتی پلیمرها\",\"value\":\"تا 180 درجه سانتی‌گراد نسوز\"}]",
                MaterialsJson = "[\"نایلون فیبر کربن PA12-CF\",\"پلی‌کربنات نسوز (PC)\",\"رزین مهندسی Tough\",\"ABS-ESD آنتی‌استاتیک\",\"پلیمرهای انعطاف‌پذیر TPU\"]",
                ApplicationsJson = "[\"قطعات سبک پهپاد و هوافضا\",\"پوسته و فریم تجهیزات رباتیک\",\"قطعات تست کارگاهی و جایگزین آلومینیوم\",\"هوزینگ‌های ضدآب الکترونیک\"]",
                Equipment = "پارک پرینترهای صنعتی Bambu Lab با محفظه محصور، سیستم لیزری SLS نوری و پرینترهای فرمت بزرگ FDM",
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Slug = "cnc",
                Code = "PROTOCOL 02",
                Badge = "ISO 2768-FINE | ±0.01mm",
                Coord = "±0.01",
                Title = "ماشین‌کاری 5 محوره همزمان CNC",
                ShortTitle = "ماشین‌کاری CNC",
                Subtitle = "SIMULTANEOUS 5-AXIS PRECISION CNC MILLING",
                Description = "فرزکاری فوق‌دقیق 5 محوره همزمان برای قطعات با هندسه‌های پیچیده سه‌بعدی و سطوح آزاد (Freeform) بدون نیاز به بستن‌های چندباره و حذف خطای زوایای متقاطع. کارگاه هیزو با تجهیزات تخصصی و ابزارگیرهای صلب، قطعات حساس هوافضا، هیدرولیک و قالب‌های دقیق را از شمش‌های یکپارچه فلزی فرزکاری می‌کند.",
                ShortDescription = "فرزکاری 5 محوره فلزات سبک، برنج، چوب و ساخت قالب‌های مستحکم با تلورانس ابعادی میکرونی.",
                ImageUrl = "https://images.unsplash.com/photo-1565043589221-1a6fd9ae45c7?auto=format&fit=crop&w=1200&q=80",
                IconName = "Wrench",
                SpecsJson = "[{\"label\":\"دقت موقعیت‌یابی و تکرارپذیری\",\"value\":\"±0.008 میلی‌متر میکرونی\"},{\"label\":\"محدوده کارگیر 5 محوره\",\"value\":\"800×600×500 میلی‌متر\"},{\"label\":\"زبری سطح پرداختی (Ra)\",\"value\":\"تا 0.4 میکرومتر آینه‌ای\"},{\"label\":\"سرعت چرخش اسپیندل\",\"value\":\"24,000 دور در دقیقه با کولانت\"}]",
                MaterialsJson = "[\"آلومینیوم 7075-T6 و 6061\",\"برنج آلیاژی شمش\",\"استیل نگیر 316 و 304\",\"فولاد ابزار SPK سخت‌کاری شده\",\"چوب‌های سخت متراکم صنعتی\"]",
                ApplicationsJson = "[\"پره‌های توربین و ایمپلرهای دینامیک\",\"پوسته‌های هیدرولیک و پنوماتیک\",\"براکت‌های حساس رباتیک و اتوماسیون\",\"قالب‌های فرم‌دهی و کشش دقیق\"]",
                Equipment = "مرکز فرزکاری 5 محوره همزمان، سیستم پروب لیزری Renishaw و خنک‌کاری پیشرفته مایع تحت فشار",
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Slug = "laser",
                Code = "PROTOCOL 03",
                Badge = "1064nm FIBER & CO2 HIGH-POWER",
                Coord = "1064nm",
                Title = "برش فایبر نوری و حکاکی پرسرعت لیزر",
                ShortTitle = "برش و لیزر",
                Subtitle = "ULTRA-FAST FIBER & CO2 LASER PROCESSING",
                Description = "برش نوری بی‌نقص انواع ورق‌های فلزی سخت و متریال‌های غیرفلزی با حداقل منطقه متاثر از حرارت (HAZ). استفاده از لیزر فایبر با طول موج 1064 نانومتر و گازهای خلوص بالای نیتروژن و اکسیژن، لبه‌هایی کاملاً صیقلی، عمود و بدون اکسیداسیون یا پلیسه را تضمین می‌کند.",
                ShortDescription = "برش فوق سریع و حکاکی فیبر و CO2 روی چوب طبیعی، پلکسی و ورق‌های سخت صنعتی.",
                ImageUrl = "https://images.unsplash.com/photo-1504917599217-d4dc5ebe6122?auto=format&fit=crop&w=1200&q=80",
                IconName = "Sparkles",
                SpecsJson = "[{\"label\":\"حداکثر ضخامت برش فولاد\",\"value\":\"تا 20 میلی‌متر با نیتروژن خلوص 99.99%\"},{\"label\":\"دقت پرتو و نقطه فوکوس\",\"value\":\"±0.03 میلی‌متر روی کانون\"},{\"label\":\"توان سورس فایبر نوری\",\"value\":\"6000 وات پیوسته صنعتی\"},{\"label\":\"حکاکی نوری عمیق\",\"value\":\"بارکد، شماره فنی، لوگو و درجه‌بندی\"}]",
                MaterialsJson = "[\"ورق آهن St37 و St52\",\"ورق استنلس استیل 304 و 316\",\"ورق مس خالص و برنج فنری\",\"آلومینیوم سری 5000 و 6000\",\"چوب طبیعی، MDF و پلکسی گلاس\"]",
                ApplicationsJson = "[\"شاسی‌های مدولار الکترونیک\",\"پنل‌های رک و جعبه‌های مخابراتی\",\"فلنج‌ها و دیسک‌های ترمز صنعتی\",\"المان‌های مدرن معماری و مشبک\"]",
                Equipment = "میز برش فایبر 3000×1500 میلی‌متر با هد فوکوس اتوماتیک Precitec و سیستم تزریق گاز نیتروژن و اکسیژن",
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Slug = "dfm",
                Code = "PROTOCOL 04",
                Badge = "METROLOGY GRADE | 5-MICRON SCAN",
                Coord = "ISO CAD",
                Title = "اسکن نوری سه‌بعدی و بهینه‌سازی ساخت (DFM)",
                ShortTitle = "مهندسی معکوس",
                Subtitle = "METROLOGY 3D SCANNING & DESIGN FOR MANUFACTURING",
                Description = "اسکن اپتیکال بدون تماس قطعات مستهلک، نایاب یا دست‌ساز با رزولوشن میکرونی، استخراج ابر نقاط متراکم و بازسازی مدل‌های پارامتریک CAD در نرم‌افزارهای مهندسی. در مرحله بعد، تحلیل جامع قابلیت ساخت (DFM) جهت سبک‌سازی سازه (Topology Optimization) و کاهش هزینه‌های تولید انجام می‌گیرد.",
                ShortDescription = "اسکن سه‌بعدی نوری، تلورانس‌گذاری هندسی و بهینه‌سازی فایل‌های CAD برای ساخت تجاری.",
                ImageUrl = "https://images.unsplash.com/photo-1581092335397-9583fe92d232?auto=format&fit=crop&w=1200&q=80",
                IconName = "Scan",
                SpecsJson = "[{\"label\":\"دقت اندازه‌گیری اسکنر\",\"value\":\"5 میکرومتر (گواهی کالیبراسیون Metrology)\"},{\"label\":\"فرمت فایل‌های خروجی\",\"value\":\"STEP, IGES, Parasolid, Native SolidWorks/NX\"},{\"label\":\"تحلیل المان محدود (FEA)\",\"value\":\"استاتیک، خستگی و توزیع تنش فون مایسز\"},{\"label\":\"گزارش بازرسی ابعادی (Inspection)\",\"value\":\"نقشه انحراف رنگی با تلورانس GD&T\"}]",
                MaterialsJson = "[\"اسکن کلیه قطعات فلزی، پلیمری و کامپوزیتی\",\"اسکن قطعات ریخته‌گری با سطوح نامنظم\",\"قالب‌های قدیمی و قطعات از رده خارج\"]",
                ApplicationsJson = "[\"بازسازی قطعات نایاب خطوط تولید خارجی\",\"تحلیل عیوب و انحرافات ابعادی قطعه ساخته شده\",\"بهینه‌سازی توپولوژی و کاهش وزن تا 40%\",\"تبدیل نقشه‌های دوبعدی به مدل‌های سه‌بعدی\"]",
                Equipment = "اسکنر نوری سه‌بعدی نور آبی (Blue Light Optical Scanner) و ایستگاه‌های پردازش محاسباتی Geomagic Control X",
                DisplayOrder = 4,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Slug = "automation",
                Code = "PROTOCOL 05",
                Badge = "EMBEDDED CONTROL & SERVO AUTOMATION",
                Coord = "PLC/ARM",
                Title = "توسعه مکاترونیک، رباتیک و ماشین‌آلات سفارشی",
                ShortTitle = "ساخت ماشین‌آلات",
                Subtitle = "CUSTOM MECHATRONIC SYSTEMS & WORKSHOP AUTOMATION",
                Description = "مهندسی یکپارچه مکانیک، برق صنعتی و نرم‌افزار برای توسعه سیستم‌های خودکار خط تولید، ربات‌های کارتزین، استیج‌های تست لرزش و خستگی، و فیکسچرهای هوشمند مونتاژ. هیزو کلیه مراحل را از تحلیل فیزیکی و نقشه‌کشی تا مونتاژ قطعات، سیم‌کشی و برنامه‌نویسی میکروکنترلر به صورت درون‌سازمانی به اجرا درمی‌آورد.",
                ShortDescription = "طراحی مکاترونیک، اسمبل فیزیکی و برنامه‌نویسی ماشین‌های خط تولید و سیستم‌های اتوماسیون.",
                ImageUrl = "https://images.unsplash.com/photo-1581092580497-e0d23cbdf1dc?auto=format&fit=crop&w=1200&q=80",
                IconName = "Cog",
                SpecsJson = "[{\"label\":\"سیستم‌های هدایت حرکتی\",\"value\":\"بال‌اسکروهای پیش‌بارگذاری C3 و ریل‌های خطی\"},{\"label\":\"محرک‌ها و موتورها\",\"value\":\"سروموتورهای سنکرون AC و استپرهای حلقه بسته\"},{\"label\":\"معماری کنترلر صنعتی\",\"value\":\"PLC صنعتی زیمنس، دلتا و بردهای امبدد ARM\"},{\"label\":\"رابط کاربری و مانیتورینگ\",\"value\":\"HMI لمسی صنعتی و اتصال شبکه Modbus/CAN\"}]",
                MaterialsJson = "[\"شاسی‌های پروفیل آلومینیوم مهندسی سخت\",\"صفحات صلب فولادی تنش‌زدایی شده\",\"پوشش‌های محافظ شفاف پلی‌کربنات صنعتی\"]",
                ApplicationsJson = "[\"دستگاه‌های تست دوام و لرزش قطعات\",\"ربات‌های پیک اند پلیس و چیدمان کارگاهی\",\"فیکسچرهای تست عایق و ابعاد آنلاین\",\"تجهیزات مکانیزه بسته‌بندی و مونتاژ\"]",
                Equipment = "آزمایشگاه مونتاژ مکاترونیک، دستگاه‌های جوشکاری دقیق TIG، اسیلوسکوپ‌های دیجیتال و تست‌بنچ کالیبراسیون سنسور",
                DisplayOrder = 5,
                IsActive = true
            }
        };

        context.FabricationServices.AddRange(services);
        await context.SaveChangesAsync();
        logger.LogInformation("Fabrication services seeded successfully.");
    }

    private static async Task SeedFaqsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.FaqItems.AnyAsync()) return;

        logger.LogInformation("Seeding FAQ items...");

        var faqs = new List<FaqItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Question = "چه فرمت‌های فایلی برای بررسی DFM و استعلام ساخت در هیزو پذیرفته می‌شود؟",
                Badge = "CAD / CAM / VECTOR",
                Answer = "ما تمامی فایل‌های استاندارد طراحی سه‌بعدی و مهندسی را در سامانه هیزو پردازش و بررسی می‌کنیم: مدل‌های سه‌بعدی و قالب: .STEP, .STP, .IGES, .SLDPRT, .X_T | پرینت 3بعدی صنعتی: .STL, .OBJ, .3MF (Mesh High-Poly) | برش لیزر و CNC تختی: .DXF, .DWG, .AI, .SVG | نقشه‌های مهندسی ساخت: فایل‌های نقشه‌کشی PDF با مشخصات تلورانسی GD&T.",
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Question = "تلورانس ابعادی و دقت ماشین‌کاری CNC و پرینت در هیزو چگونه تضمین می‌شود؟",
                Badge = "ISO 2768-m",
                Answer = "در فرآیند فرزکاری 5 محوره CNC، تلورانس هندسی و ابعادی قطعات تا محدوده ±0.01 میلی‌متر (مطابق استاندارد بین‌المللی ISO 2768-Fine) تضمین می‌گردد. در پرینت سه‌بعدی صنعتی با روش‌های SLS و رزین مهندسی SLA، دقت ابعادی در بازه ±0.05 میلی‌متر قرار دارد. کلیه قطعات حساس صنعتی همراه با گزارش اندازه‌گیری ابعادی CMM و برگه کنترل کیفیت نهایی تحویل می‌گردند.",
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Question = "زمان‌بندی تحویل سفارش‌های ساخت فوری و نمونه‌های اولیه (Prototype) چقدر است؟",
                Badge = "FAST TURNAROUND",
                Answer = "برای قطعات پرینت سه‌بعدی و برش لیزر فوری، تحویل نمونه اولیه بین 24 الی 48 ساعت کاری انجام می‌شود. سفارش‌های ماشین‌کاری دقیق CNC بسته به پیچیدگی سطوح 5 محوره، نوع آلیاژ و عملیات حرارتی بین 3 الی 6 روز کاری زمان‌بندی خواهند شد.",
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Question = "آیا امکان سفارش تیراژ محدود (Low-Volume Manufacturing) یا نمونه تک وجود دارد؟",
                Badge = "PROTOTYPE TO BATCH",
                Answer = "بله؛ ساختار کارگاه و خطوط ساخت هیزو بر پایه تولید انعطاف‌پذیر (Agile Fabrication) طراحی شده‌اند. شما می‌توانید از 1 قطعه نمونه اولیه تکی تا تیراژهای 500 الی 10,000 عددی را با هزینه بهینه ابزارسازی و بدون نیاز به هزینه‌های سنگین قالب‌سازی سفارش دهید.",
                DisplayOrder = 4,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Question = "چه متریال‌ها و پرداخت‌های سطحی (Surface Finishing) در کارگاه اجرا می‌شود؟",
                Badge = "FINISHING LAB",
                Answer = "متریال‌های قابل پردازش در هیزو شامل آلیاژهای آلومینیوم سری 6000 و 7075، برنج صنعتی، فولادهای زنگ‌نزن 304 و 316، تیتانیوم، تفلون صنعتی POM، پلیمرهای کربنی و چوب‌های سخت عمل‌آوری شده است. خدمات تکمیلی شامل آنودایز مات و رنگی، سندبلاست شیشه‌ای، پوشش‌های ضدسایش، پولیش مکانیکی و حکاکی نوری مشخصات فنی با لیزر می‌باشد.",
                DisplayOrder = 5,
                IsActive = true
            }
        };

        context.FaqItems.AddRange(faqs);
        await context.SaveChangesAsync();
        logger.LogInformation("FAQ items seeded successfully.");
    }

    private static async Task SeedSlidesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.HeroSlides.AnyAsync()) return;

        logger.LogInformation("Seeding hero slides...");

        var slides = new List<HeroSlide>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ImageUrl = "/slideshow/slide-1.jpg",
                Title = "کارگاه ماشین‌کاری پیشرفته 5 محوره CNC",
                Subtitle = "CAM 01 // CNC MILLING & PROTOTYPING",
                Badge = "TOLERANCE ±0.01mm",
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                ImageUrl = "/slideshow/slide-2.jpg",
                Title = "پرینت 3بعدی صنعتی ابعاد بزرگ",
                Subtitle = "ADDITIVE 02 // INDUSTRIAL SLS & FDM",
                Badge = "PA12 CARBON / RESIN",
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                ImageUrl = "/slideshow/slide-3.jpg",
                Title = "برش و حکاکی فوق دقیق لیزر فایبر",
                Subtitle = "OPTICS 03 // FIBER LASER 1064nm",
                Badge = "METALS & ACRYLIC",
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                ImageUrl = "/slideshow/slide-4.jpg",
                Title = "توسعه مکاترونیک و ساخت خط تولید",
                Subtitle = "SYSTEMS 04 // INDUSTRIAL AUTOMATION",
                Badge = "DFM & ASSEMBLY",
                DisplayOrder = 4,
                IsActive = true
            }
        };

        context.HeroSlides.AddRange(slides);
        await context.SaveChangesAsync();
        logger.LogInformation("Hero slides seeded successfully.");
    }

    private static async Task SeedAboutContentAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.TeamMembers.AnyAsync() || await context.CompanyGoals.AnyAsync() || await context.CompanyMilestones.AnyAsync()) return;

        logger.LogInformation("Seeding about page content (team, goals, milestones)...");

        var team = new List<TeamMember>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "مهندس علیرضا رادمنش",
                Role = "بنیان‌گذار و مدیر ارشد مهندسی ساخت",
                EnglishRole = "CHIEF FABRICATION ENGINEER & CO-FOUNDER",
                Education = "کارشناسی ارشد مهندسی مکانیک - ساخت و تولید",
                ImageUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=600&q=80",
                Bio = "با بیش از 10 سال سابقه تخصصی در برنامه‌نویسی پیشرفته CAM، راه‌اندازی مراکز فرزکاری 5 محوره همزمان و طراحی سازه‌های دقیق هوافضا و رباتیک. علیرضا با هدف پل زدن میان ایده‌های بلندپروازانه مهندسی و واقعیت ماشین‌کاری صلب، استودیو هیزو را پایه‌گذاری کرد.",
                SpecialtiesJson = "[\"فرزکاری همزمان 5 محوره\",\"برنامه‌نویسی Mastercam & Hypermill\",\"تلورانس‌گذاری هندسی (GD&T)\",\"متالورژی آلومینیوم 7075\"]",
                Email = "a.radmanesh@hizustudio.ir",
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "مهندس سارا نیک‌پیام",
                Role = "مدیر طراحی محاسباتی و تحلیل DFM",
                EnglishRole = "COMPUTATIONAL DESIGN LEAD & DFM SPECIALIST",
                Education = "کارشناسی ارشد معماری دیجیتال و الگوریتم‌های ساخت",
                ImageUrl = "https://images.unsplash.com/photo-1580489944761-15a19d654956?auto=format&fit=crop&w=600&q=80",
                Bio = "متخصص فرم‌یابی پارامتریک، شبیه‌سازی تنش المان محدود (FEA) و تبدیل طرح‌های آزاد مفهومی به مدل‌های بهینه‌سازی‌شده برای تولید صنعتی (DFM). سارا مسئول بازطراحی ساختاری قطعات با متد بهینه‌سازی توپولوژی جهت کاهش وزن و هزینه در کارگاه هیزو است.",
                SpecialtiesJson = "[\"طراحی الگوریتمیک Rhino/Grasshopper\",\"بهینه‌سازی توپولوژی (Topology Opt)\",\"اسکن نوری سه‌بعدی و مهندسی معکوس\",\"پلیمرهای پیشرفته و SLS\"]",
                Email = "s.nikpayam@hizustudio.ir",
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "مهندس پویا داوودی",
                Role = "سرپرست تحقیق و توسعه مکاترونیک و اتوماسیون",
                EnglishRole = "MECHATRONICS & EMBEDDED SYSTEMS LEAD",
                Education = "کارشناسی ارشد مهندسی مکاترونیک و کنترل صنعتی",
                ImageUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=600&q=80",
                Bio = "طراح و معمار سیستم‌های حرکتی خودکار، استیج‌های تست لرزش و کنترلرهای فیدبک‌دار کارگاهی. پویا رهبری پروژه‌های ساخت ماشین‌آلات خاص، درایورهای سروموتور دقیق و سیستم‌های کنترل تعبیه‌شده (Embedded ARM) را در هیزو بر عهده دارد.",
                SpecialtiesJson = "[\"کنترلرهای حرکت صنعتی (Motion Control)\",\"برنامه‌نویسی PLC زیمنس و دلتا\",\"طراحی بردهای صنعتی STM32\",\"طراحی مکانیزم‌های رباتیک\"]",
                Email = "p.davoodi@hizustudio.ir",
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "مهندس کیان مهرداد",
                Role = "مدیر کنترل کیفیت (QC) و متریال صنعتی",
                EnglishRole = "QUALITY ASSURANCE & MATERIALS SPECIALIST",
                Education = "کارشناسی ارشد مهندسی مواد و متالورژی صنعتی",
                ImageUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=600&q=80",
                Bio = "مسئول اعتبارسنجی ابعادی قطعات با ابزارهای دقیق متولوژی، آزمون‌های سایش، مقاومت کششی و نظارت مستقیم بر کنترل کیفیت تلورانسی ISO 2768-Fine. کیان با وسواس مهندسی تضمین می‌کند که هیچ قطعه‌ای بدون تاییدیه تست متالورژی از هیزو خارج نشود.",
                SpecialtiesJson = "[\"کنترل ابعادی اپتیکال و CMM\",\"استاندارد تلورانس ISO 2768\",\"آنالیز ساختار پلیمرهای کربنی\",\"صدور گواهینامه‌های کیفی ساخت\"]",
                Email = "k.mehrdad@hizustudio.ir",
                DisplayOrder = 4,
                IsActive = true
            }
        };

        var goals = new List<CompanyGoal>
        {
            new()
            {
                Id = Guid.NewGuid(),
                GoalNumber = "01",
                Title = "بومی‌سازی قطعات های‌تک و دقیق",
                Subtitle = "PRECISION LOCALIZATION",
                Description = "تامین و ساخت قطعات فوق‌حساس مکانیکی با تلورانس‌های میکرونی در داخل کشور، بی‌نیاز کردن شرکت‌های دانش‌بنیان، تجهیزات پزشکی و رباتیک از واردات گران‌قیمت.",
                IconName = "Cpu",
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                GoalNumber = "02",
                Title = "نهادینه‌سازی فرهنگ DFM",
                Subtitle = "DESIGN FOR MANUFACTURING",
                Description = "اصلاح و بهینه‌سازی مدل‌های سه‌بعدی پیش از برش یا فرزکاری، با هدف به صفر رساندن خطاهای تولیدی، کاهش وزن سازه و پایین آوردن هزینه‌های تمام‌شده.",
                IconName = "Target",
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                GoalNumber = "03",
                Title = "تولید با متریال ناب و اصیل",
                Subtitle = "MATERIAL INTEGRITY",
                Description = "پرهیز قطعی از آلیاژهای ضایعاتی یا پلیمرهای بازیافتی؛ استفاده انحصاری از آلومینیوم 7075 گواهی‌دار، برنج شمش، فولادهای استاندارد و نایلون کربن مقاوم.",
                IconName = "ShieldCheck",
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                GoalNumber = "04",
                Title = "ترویج ساخت دیجیتال و آزمایشگاه باز",
                Subtitle = "OPEN FABRICATION LAB",
                Description = "برگزاری دوره‌های تخصصی، انتقال تجربه ساخت به طراحان و مهندسان جوان و انتشار مقالات فنی برای ارتقای دانش ساخت در اکوسیستم سخت‌افزاری کشور.",
                IconName = "Sparkles",
                DisplayOrder = 4,
                IsActive = true
            }
        };

        var milestones = new List<CompanyMilestone>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Year = "1400",
                Title = "تاسیس استودیو ساخت هیزو",
                Description = "تجهیز کارگاه اولیه در تهران با اولین ماشین‌های 3 محوره فرزکاری و پرینترهای ابعاد متوسط FDM.",
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Year = "1401",
                Title = "ورود تکنولوژی SLS و فیبر کربن",
                Description = "راه‌اندازی خط پرینت پیشرفته با نایلون کربن PA12-CF و ثبت اولین قراردادهای تامین قطعات رباتیک صنعتی.",
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Year = "1402",
                Title = "ارتقا به ماشین‌کاری 5 محوره همزمان",
                Description = "نصب سنتر 5 محوره همزمان CNC، اسکنر نوری 3بعدی متولوژی و دریافت گواهی انطباق استاندارد ISO 2768-Fine.",
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Year = "1403",
                Title = "توسعه دپارتمان مکاترونیک و پلتفرم آنلاین",
                Description = "راه‌اندازی آزمایشگاه تست مکاترونیک، سامانه آنلاین استعلام فایل CAD و راه‌اندازی فروشگاه قطعات پیش‌ساخته هیزو.",
                DisplayOrder = 4,
                IsActive = true
            }
        };

        context.TeamMembers.AddRange(team);
        context.CompanyGoals.AddRange(goals);
        context.CompanyMilestones.AddRange(milestones);
        await context.SaveChangesAsync();
        logger.LogInformation("About page content seeded successfully.");
    }
}
