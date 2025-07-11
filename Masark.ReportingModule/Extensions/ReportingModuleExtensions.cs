using Microsoft.Extensions.DependencyInjection;
using Masark.ReportingModule.Services;

namespace Masark.ReportingModule.Extensions
{
    public static class ReportingModuleExtensions
    {
        public static IServiceCollection AddReportingModule(this IServiceCollection services)
        {
            services.AddScoped<IReportingModuleService, ReportingModuleService>();
            
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ReportingModuleService).Assembly));
            
            return services;
        }
    }
}
