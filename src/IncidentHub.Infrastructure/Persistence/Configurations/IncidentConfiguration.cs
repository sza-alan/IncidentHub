using IncidentHub.Domain.Incidents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentHub.Infrastructure.Persistence.Configurations;

public sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("Incidents");

        builder.HasKey(incident => incident.Id);

        builder.Property(incident => incident.Title)
            .IsRequired();

        builder.Property(incident => incident.Description)
            .IsRequired();

        builder.Property(incident => incident.Service)
            .IsRequired();

        builder.Property(incident => incident.Severity)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(incident => incident.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(incident => incident.CreatedAt)
            .IsRequired();

        builder.HasIndex(incident => incident.CreatedAt);

        builder.HasIndex(incident => new { incident.Status, incident.CreatedAt });
    }
}
