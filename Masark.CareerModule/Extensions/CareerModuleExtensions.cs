using Microsoft.Extensions.DependencyInjection;
using Masark.CareerModule.Services;

namespace Masark.CareerModule.Extensions
{
    public static class CareerModuleExtensions
    {
        public static IServiceCollection AddCareerModule(this IServiceCollection services)
        {
            services.AddScoped<ICareerModuleService, CareerModuleService>();
            
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CareerModuleService).Assembly));
            
            return services;
        }
    }
}
