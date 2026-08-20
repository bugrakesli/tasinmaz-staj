using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace tasinmaz_staj.Middleware
{
    // REQ (Hata Yonetimi): Controller'larda tek tek try/catch yazmak yerine,
    // yakalanmamis (unhandled) tum hatalari burada merkezi olarak loglayip
    // Angular tarafina standart bir JSON hata modeli donuyoruz.
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception,
                "Yakalanmamış hata: {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            await httpContext.Response.WriteAsJsonAsync(
                new { message = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin." },
                cancellationToken);

            return true;
        }
    }
}
