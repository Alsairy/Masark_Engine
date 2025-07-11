using Microsoft.Extensions.DependencyInjection;
using Masark.AssessmentModule.Services;

namespace Masark.AssessmentModule.Extensions
{
    public static class AssessmentModuleExtensions
    {
        public static IServiceCollection AddAssessmentModule(this IServiceCollection services)
        {
            services.AddScoped<IAssessmentModuleService, AssessmentModuleService>();
            
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssessmentModuleService).Assembly));
            
            return services;
        }
    }
}
