using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Template.Application.Orders.CreateOrder;
using Template.Application.Orders.DeleteOrder;
using Template.Application.Orders.GetOrder;
using Template.Application.Orders.ListOrders;
using Template.Application.Orders.UpdateOrder;

namespace Template.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        // Handlers are plain classes. Register each new one here (or switch to assembly scanning
        // once there are enough of them to justify it).
        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<GetOrderHandler>();
        services.AddScoped<ListOrdersHandler>();
        services.AddScoped<UpdateOrderHandler>();
        services.AddScoped<DeleteOrderHandler>();

        return services;
    }
}
