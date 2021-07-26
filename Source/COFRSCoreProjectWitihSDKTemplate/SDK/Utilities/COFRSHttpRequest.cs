using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace $safeprojectname$.Utilities
{
    public class COFRSHttpRequest : HttpRequest
    {
        private Pipe _pipe;

        public COFRSHttpRequest(HttpResponseMessage response)
        {
            Method = response.RequestMessage.Method.ToString();
            Scheme = response.RequestMessage.RequestUri.Scheme;
            IsHttps = response.RequestMessage.RequestUri.Scheme.ToLower() == "https";
            Host = HostString.FromUriComponent(response.RequestMessage.RequestUri);
            PathBase = PathString.FromUriComponent(response.RequestMessage.RequestUri);
            Path = PathString.FromUriComponent(response.RequestMessage.RequestUri);
            QueryString = QueryString.FromUriComponent(response.RequestMessage.RequestUri);
            Protocol = $"HTTP{response.RequestMessage.Version.Major}.{response.RequestMessage.Version.Minor}";

            var headers = new HeaderDictionary();

            foreach (var h in response.RequestMessage.Headers)
            {
                var kvp = new KeyValuePair<string, StringValues>(h.Key, new StringValues(h.Value.ToArray()));
                headers.Add(kvp);
            }

            Headers = headers;

            ContentType = response.Content.Headers.ContentType.ToString();
            ContentLength = response.Content.Headers.ContentLength;
            Body = response.Content.ReadAsStreamAsync().Result;

            Body.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[Convert.ToInt32(ContentLength)];
            Body.Read(buffer, 0, Convert.ToInt32(ContentLength));

            _pipe = new Pipe();
            _ = _pipe.Writer.WriteAsync(buffer).Result;
            _pipe.Writer.Complete();
            BodyReader = _pipe.Reader;
        }

        public override HttpContext HttpContext => throw new NotImplementedException();

        public override string Method { get; set; }
        public override string Scheme { get; set; }
        public override bool IsHttps { get; set; }
        public override HostString Host { get; set; }
        public override PathString PathBase { get; set; }
        public override PathString Path { get; set; }
        public override QueryString QueryString { get; set; }
        public override IQueryCollection Query { get; set; }
        public override string Protocol { get; set; }

        public override IHeaderDictionary Headers { get; }

        public override IRequestCookieCollection Cookies { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override long? ContentLength { get; set; }
        public override string ContentType { get; set; }
        public override Stream Body { get; set; }

        public override PipeReader BodyReader { get; }

        public override bool HasFormContentType => throw new NotImplementedException();

        public override IFormCollection Form { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
