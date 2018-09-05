using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.AspNetCore;
using PodcastServer.Utilities;
using PodcastServer.Security;
using PodcastServer.Services;
using ClientManagement.Services;
using ClientManagement.Data;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Microsoft.AspNetCore.Http;
using System.IO;


namespace PodcastServer
{
    public class Startup
    {
        private readonly ILoggerFactory _loggerFactory;
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            string logFile = configuration.GetSection("AppSettings")["LogFile"];
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File(logFile)
               .CreateLogger();

            _loggerFactory.AddSerilog();
            
            Log.Debug("Start");
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("ClientManagementDatabase");

            services.AddCors();
            services.AddEntityFrameworkNpgsql().AddDbContext<DataContext>(options => options.UseNpgsql(connectionString, o => o.UseNodaTime()).UseLoggerFactory(_loggerFactory));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);


            // configure strongly typed settings objects
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

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


            services.AddHttpClient<ICognativeServices, CognativeServices>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();


            //app.UseHttpsRedirection();

            // global cors policy
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            app.UseWhen(x => (x.Request.Path.StartsWithSegments("/podcast", StringComparison.OrdinalIgnoreCase)),
            builder =>
            {
                builder.UseMiddleware<AuthenticationMiddleware>();
            });

            app.UseDefaultFiles();

            var cachePeriod = env.IsDevelopment() ? "600" : "604800";
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    // Requires the following import:
                    ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={cachePeriod}");
                }
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "api/{controller}/{action=Index}/{id?}");
            });

        }
    }
}
