using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Template.Application.Abstractions;
using Template.Infrastructure.Persistence;

namespace Template.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // The connection string is resolved lazily, so tests and the EF migration bundle
        // can supply it through any configuration source.
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' is missing. Set ConnectionStrings__{ConnectionStringName}.");

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

        return services;
    }
}
