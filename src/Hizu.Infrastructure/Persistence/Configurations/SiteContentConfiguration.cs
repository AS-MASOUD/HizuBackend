using Hizu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hizu.Infrastructure.Persistence.Configurations;

public class ProcessStepConfiguration : IEntityTypeConfiguration<ProcessStep>
{
    public void Configure(EntityTypeBuilder<ProcessStep> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StepNumber).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
    }
}

public class FabricationServiceConfiguration : IEntityTypeConfiguration<FabricationService>
{
    public void Configure(EntityTypeBuilder<FabricationService> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Slug);
        builder.Property(x => x.Code).HasMaxLength(50);
        builder.Property(x => x.Badge).HasMaxLength(100);
        builder.Property(x => x.Coord).HasMaxLength(50);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ShortTitle).HasMaxLength(100);
        builder.Property(x => x.Subtitle).HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(3000);
        builder.Property(x => x.ShortDescription).HasMaxLength(1000);
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.Property(x => x.IconName).HasMaxLength(50);
        builder.Property(x => x.Equipment).HasMaxLength(1000);
    }
}

public class FaqItemConfiguration : IEntityTypeConfiguration<FaqItem>
{
    public void Configure(EntityTypeBuilder<FaqItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Question).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Answer).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.Badge).HasMaxLength(100);
    }
}

public class HeroSlideConfiguration : IEntityTypeConfiguration<HeroSlide>
{
    public void Configure(EntityTypeBuilder<HeroSlide> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Subtitle).HasMaxLength(200);
        builder.Property(x => x.Badge).HasMaxLength(100);
        builder.Property(x => x.ImageUrl).IsRequired().HasMaxLength(1000);
    }
}

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Role).IsRequired().HasMaxLength(200);
        builder.Property(x => x.EnglishRole).HasMaxLength(200);
        builder.Property(x => x.Education).HasMaxLength(200);
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.Property(x => x.Bio).HasMaxLength(2000);
        builder.Property(x => x.Email).HasMaxLength(150);
    }
}

public class CompanyGoalConfiguration : IEntityTypeConfiguration<CompanyGoal>
{
    public void Configure(EntityTypeBuilder<CompanyGoal> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GoalNumber).HasMaxLength(20);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Subtitle).HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.IconName).HasMaxLength(50);
    }
}

public class CompanyMilestoneConfiguration : IEntityTypeConfiguration<CompanyMilestone>
{
    public void Configure(EntityTypeBuilder<CompanyMilestone> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Year).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
    }
}
