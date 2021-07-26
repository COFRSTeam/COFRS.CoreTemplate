using $saferootprojectname$.Models.ResourceModels;
using System.Threading.Tasks;

namespace $safeprojectname$.Contracts
{
    interface I$saferootprojectname$HealthCheck
    {
        Task<HealthCheck> GetHealthCheckAsync();
    }
}
