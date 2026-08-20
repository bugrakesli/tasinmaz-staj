using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tasinmaz_staj
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IPropertyService, PropertyService>();
            services.AddScoped<IPropertyExportService, PropertyExportService>();
            services.AddScoped<IPropertyImportService, PropertyImportService>();
            services.AddScoped<ILogService, LogService>();
            services.AddScoped<ILogExportService, LogExportService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPropertyImageService, PropertyImageService>();
            services.AddScoped<IPropertyGeometryService, PropertyGeometryService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<IEmailService, EmailService>();

            services.AddExceptionHandler<tasinmaz_staj.Middleware.GlobalExceptionHandler>();
            services.AddProblemDetails();


            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "REMS.API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header. Örnek: Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                        new string[] { }
                    }
                });
            });

            services.AddDbContext<RemsDbContext>(options =>
                options.UseNpgsql(
                    Configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.UseNetTopologySuite();
                    }
                )
            );


            services.AddControllers(options =>
            {
                options.Filters.Add<tasinmaz_staj.Filters.AutoLogFilter>();
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular", builder =>
                {
                    builder
                        .WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            services.AddSingleton<TokenService>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Configuration["Jwt:Issuer"],
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                };

                // REQ (4.2-13): "invalidate sessions on logout". JWT
                // stateless oldugu icin imza/sure dogrulamasi basarili
                // olsa bile, token'in jti'si RevokedTokens tablosunda
                // varsa istegi reddediyoruz. Boylece logout sonrasi
                // eldeki token bir daha kullanilamiyor.
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.FindFirst(
                            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

                        if (string.IsNullOrEmpty(jti))
                            return;

                        var dbContext = context.HttpContext.RequestServices
                            .GetRequiredService<RemsDbContext>();

                        bool isRevoked = await dbContext.RevokedTokens
                            .AnyAsync(x => x.Jti == jti);

                        if (isRevoked)
                        {
                            context.Fail("Token has been revoked.");
                        }
                    }
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "REMS.API v1");
                });
            }
            else
            {
                // REQ (Hata Yonetimi): Prod'da tum yakalanmamis hatalar
                // GlobalExceptionHandler uzerinden standart JSON olarak donuyor.
                app.UseExceptionHandler();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors("AllowAngular");
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
