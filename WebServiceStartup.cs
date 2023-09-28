using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.OpenApi.Models;

namespace IntegrationService
{
    public class WebServiceStartup
    {
        private IWebHostEnvironment _HostingEnvironment;
        private IConfigurationRoot _Configuration;

        public WebServiceStartup(IWebHostEnvironment env)
        {
            _HostingEnvironment = env;

            //env.ConfigureNLog("NLog.config");
            //.AddJsonFile("appSettings.json", optional: false, reloadOnChange: true);

            var Builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath);
            _Configuration = Builder.Build();
        }

        /// <summary>
        /// Configure Services
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            //string client = _Configuration.GetValue<string>("localHMI");
            //string tcpListener = _Configuration.GetValue<string>("localTCPListener");

            //services.AddCors(options =>
            //{
            //    options.AddPolicy("IntegrationServicePolicy");
            //});
            /*
            services.AddMvcCore().AddNewtonsoftJson(o =>
            {
                o.SerializerSettings.Converters.Add(new StringEnumConverter());
                o.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }).AddApiExplorer(); */


           /* services.AddMvcCore().AddJsonFormatters(config =>
            {
                config.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }
            )*/

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "IntegrationService WEB API v1.0",
                    Version = "v1",
                    Description = "MES Integration Service. Used for integration towards External systems.",
                    Contact = new OpenApiContact { Name = "Husqvarna Manufacturing IT" },
                });
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var xmlPath = Path.Combine(basePath, "IntegrationService.xml");

                c.IncludeXmlComments(xmlPath);
            });
            

        }

        /// <summary>
        /// Configuration. Run at startup
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="loggerFactory"></param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //app.UseCors("IntegrationServicePolicy");

            //app.UseStaticFiles();

            //app.UseSwagger();
            //app.UseSwaggerUI(config =>
            //{
            //    config.RoutePrefix = "documentation";
            //    config.SwaggerEndpoint("/swagger/v1/swagger.json", "Integration Service API");
            //}
            //);

            //app.UseMvc(config =>
            //{
            //    config.MapRoute(
            //        name: "Default",
            //        template: "{controller}/{action}/{id}",
            //        defaults: new { controller = "Home", action = "index" }
            //        );
            //});
        }
    }
}

