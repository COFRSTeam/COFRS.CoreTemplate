using COFRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;

namespace $safeprojectname$.Utilities
{
    public class COFRSHttpContext : HttpContext
    {
        private CancellationTokenSource tokenSource;

        public COFRSHttpContext(HttpResponseMessage response)
        {
            Request = new COFRSHttpRequest(response);
            tokenSource = new CancellationTokenSource();
            RequestAborted = tokenSource.Token;
            RequestServices = ServiceContainer.RequestServices;
        }

        public override IFeatureCollection Features => throw new NotImplementedException();

        public override HttpRequest Request { get; }

        public override HttpResponse Response => throw new NotImplementedException();

        public override ConnectionInfo Connection => throw new NotImplementedException();

        public override WebSocketManager WebSockets => throw new NotImplementedException();

        public override ClaimsPrincipal User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IServiceProvider RequestServices { get; set; }
        public override CancellationToken RequestAborted { get; set; }
        public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Abort()
        {
            tokenSource.Cancel();
        }
    }
}
