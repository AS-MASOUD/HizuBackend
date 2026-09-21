using Hizu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Hizu.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<SpecialOrder> SpecialOrders { get; }
    DbSet<SpecialOrderImage> SpecialOrderImages { get; }
    DbSet<Article> Articles { get; }
    DbSet<ProductComment> ProductComments { get; }
    DbSet<Course> Courses { get; }
    DbSet<CourseSection> CourseSections { get; }
    DbSet<CourseEpisode> CourseEpisodes { get; }
    DbSet<CourseEnrollment> CourseEnrollments { get; }
    DbSet<ProcessStep> ProcessSteps { get; }
    DbSet<FabricationService> FabricationServices { get; }
    DbSet<FaqItem> FaqItems { get; }
    DbSet<HeroSlide> HeroSlides { get; }
    DbSet<TeamMember> TeamMembers { get; }
    DbSet<CompanyGoal> CompanyGoals { get; }
    DbSet<CompanyMilestone> CompanyMilestones { get; }
    DbSet<ApplicationUser> Users { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
