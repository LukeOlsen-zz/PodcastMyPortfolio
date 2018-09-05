using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ClientManagement.Utilities;
using ClientManagement.Services;
using ClientManagement.Data;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Serilog.AspNetCore;
using Serilog;
using System;
using Npgsql;
using NodaTime;
using Microsoft.AspNetCore.Http;

namespace ClientManagement
{
    public class Startup
    {
        private readonly ILoggerFactory _loggerFactory;

        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File("myapp.log")
               .CreateLogger();

            _loggerFactory.AddSerilog();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("ClientManagementDatabase");

            services.AddCors();
            services.AddEntityFrameworkNpgsql().AddDbContext<DataContext>(options => options.UseNpgsql(connectionString, o => o.UseNodaTime()).UseLoggerFactory(_loggerFactory));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddAutoMapper();

            // configure strongly typed settings objects
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            // configure jwt authentication
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                        var userId = int.Parse(context.Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
                        var user = userService.Get(userId);
                        if (user == null)
                        {
                            // return unauthorized if user no longer exists
                            context.Fail("Unauthorized");
                        }
                        return Task.CompletedTask;
                    }
                };
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            // configure DI for application services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IFirmService, FirmService>();
            services.AddScoped<IFirmPodcastSettingsService, FirmPodcastSettingsService>();
            services.AddScoped<IVoiceService, VoiceService>();
            services.AddScoped<IFirmPodcastSegmentService, FirmPodcastSegmentService>();
            services.AddScoped<IClientGroupPodcastSegmentService, ClientGroupPodcastSegmentService>();
            services.AddScoped<IClientGroupService, ClientGroupService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IClientMessageService, ClientMessageService>();
            services.AddScoped<IClientMessageTypeService, ClientMessageTypeService>();
            services.AddScoped<IClientAccountService, ClientAccountService>();
            services.AddScoped<IClientAccountPeriodicDataService, ClientAccountPeriodicDataService>();
            services.AddScoped<IClientAccountActivityTypeService, ClientAccountActivityTypeService>();
            services.AddScoped<IClientAccountActivityService, ClientAccountActivityService>();
            services.AddScoped<IInvalidAccessAttemptService, InvalidAccessAttemptService>();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IClock>(c => SystemClock.Instance);

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions()
            {
                ServeUnknownFileTypes = true
            });
            app.UseSpaStaticFiles();

            // global cors policy
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "api/{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }
}
