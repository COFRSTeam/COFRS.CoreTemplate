using COFRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net.Http;

namespace $safeprojectname$.Utilities
{
    public static class JsonConversion
    {
        public static T Deserialize<T>(HttpResponseMessage response, string[] supportedMediaTypes, JsonFormatterOptions options)
        {
            var formatter = new COFRSJsonFormatter(supportedMediaTypes, options);
            var context = ConstructContext<T>(response);

            if (formatter.CanRead(context))
            {
                var result = formatter.ReadAsync(context).Result;

                return (T)result.Model;
            }

            return default;
        }

        public static InputFormatterContext ConstructContext<T>(HttpResponseMessage response)
        {
            var httpContext = ConstructHttpContext(response);

            var modelState = new ModelStateDictionary();

            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(T));

            var context = new InputFormatterContext(httpContext, "", modelState, metadata,
                new COFRSStreamReaderFactory().CreateReader);

            return context;
        }

        public static HttpContext ConstructHttpContext(HttpResponseMessage response)
        {
            return new COFRSHttpContext(response);
        }
    }
}
