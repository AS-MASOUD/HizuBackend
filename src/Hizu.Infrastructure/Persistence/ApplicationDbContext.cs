using System.Reflection;
using Hizu.Application.Common.Interfaces;
using Hizu.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Hizu.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<SpecialOrder> SpecialOrders => Set<SpecialOrder>();
    public DbSet<SpecialOrderImage> SpecialOrderImages => Set<SpecialOrderImage>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ProductComment> ProductComments => Set<ProductComment>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseSection> CourseSections => Set<CourseSection>();
    public DbSet<CourseEpisode> CourseEpisodes => Set<CourseEpisode>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<ProcessStep> ProcessSteps => Set<ProcessStep>();
    public DbSet<FabricationService> FabricationServices => Set<FabricationService>();
    public DbSet<FaqItem> FaqItems => Set<FaqItem>();
    public DbSet<HeroSlide> HeroSlides => Set<HeroSlide>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<CompanyGoal> CompanyGoals => Set<CompanyGoal>();
    public DbSet<CompanyMilestone> CompanyMilestones => Set<CompanyMilestone>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
