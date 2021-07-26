using System;
using System.Net.Http;
using System.Threading.Tasks;
using $safeprojectname$.Contracts;
using $safeprojectname$.Utilities;
using $saferootprojectname$.Models.ResourceModels;
using COFRS;

namespace $safeprojectname$.Facades
{
    public class $saferootprojectname$HealthCheck : I$saferootprojectname$HealthCheck
    {
        private readonly HttpClient _client;
        private readonly string[] supportedMediaTypes;
        private readonly JsonFormatterOptions _formatterOptions;

        public $saferootprojectname$HealthCheck(HttpClient client, SDKOptions sdkOptions)
        {
            _client = client;

            supportedMediaTypes = sdkOptions.SupportedMediaTypes;

            _formatterOptions = new JsonFormatterOptions
            {
                RootUrl = _client.BaseAddress,
                HrefType = HrefType.RELATIVE
            };
        }

        public async Task<HealthCheck> GetHealthCheckAsync()
        {
            var url = new Uri("/health_check", UriKind.Relative);
            var response = await _client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var healthCheck = JsonConversion.Deserialize<HealthCheck>(response, supportedMediaTypes, _formatterOptions);
                return healthCheck;
            }
            else
            {
                var reason = await response.Content.ReadAsStringAsync();
                throw new Exception(reason);
            }
        }
    }
}
