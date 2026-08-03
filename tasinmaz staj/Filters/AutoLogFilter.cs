using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using System;
using System.Security.Claims;

namespace tasinmaz_staj.Filters
{
    public class AutoLogFilter : IAsyncActionFilter
    {
        private readonly RemsDbContext _context;

        public AutoLogFilter(RemsDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Controller'daki metot çalýþýp response dönene kadar bekle
            var executedContext = await next();

            var user = context.HttpContext.User;

            // Kullanýcý yetki doðrulamasý yapmýþsa log at
            if (user.Identity.IsAuthenticated)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int userId = int.TryParse(userIdClaim, out int parsedId) ? parsedId : 0;

                var method = context.HttpContext.Request.Method;

                // Sadece veri deðiþtiren (CRUD'un C, U, D) isteklerini logluyoruz
                if (method == "POST" || method == "PUT" || method == "DELETE")
                {
                    string operation = method switch
                    {
                        "POST" => "Create",
                        "PUT" => "Update",
                        "DELETE" => "Delete",
                        _ => "Unknown"
                    };

                    string controllerName = context.RouteData.Values["controller"]?.ToString();

                    var log = new Log
                    {
                        UserId = userId,
                        OperationType = operation,
                        Description = $"{controllerName} üzerinde {operation} iþlemi gerçekleþtirildi.",
                        UserIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmiyor",
                        Timestamp = DateTime.UtcNow,
                        Status = executedContext.Exception == null ? "Success" : "Failed"
                    };

                    await _context.Logs.AddAsync(log);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}