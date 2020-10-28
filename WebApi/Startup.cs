using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation.TypeMappers;
using NSwag;

namespace WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            var configureTypeMappers = new List<ITypeMapper>
            {
                new PrimitiveTypeMapper(typeof(Guid), schema =>
                {
                    schema.Type = JsonObjectType.String;
                    schema.Format = JsonFormatStrings.Guid;
                })
            };

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
            var converters = new List<JsonConverter>
            {
                new StringEnumConverter()
            };
            var swaggerLicense = new OpenApiLicense
            {
                Name = "NSwag Api licensee",
                Url = "github.com"
            };
            services.AddSwaggerDocument(
                    (configure, serviceProvider) =>
                    {
                        configure.Title = "NSwag WebApi";
                        configure.DocumentName = "swagger";

                        configure.SerializerSettings = new JsonSerializerSettings
                        {
                            Converters = converters,
                            NullValueHandling = NullValueHandling.Ignore,
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        };

                        configure.TypeMappers = configureTypeMappers;


                        configure.PostProcess = document =>
                        {
                            document.Info.Description = "ASP.NET Core Web API ";
                            // `Unable to render this definition` Bug
                            // comment next line and all works fine
                            document.Info.License = swaggerLicense;
                        };
                    })
                .AddOpenApiDocument((document, serviceProvider) =>
                {
                    document.Title = "NSwag Test API (openapi)";
                    document.TypeMappers = configureTypeMappers;
                    document.DocumentName = "v1";
                    document.ApiGroupNames = new[] {"1"};
                    document.PostProcess = d =>
                    {
                        d.Info.Title = "NSwag Test (openapi)";
                        d.Info.Description = "ASP.NET Core Web API v1.0 OpenAPI " + d.BaseUrl;
                    };
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            app.UseOpenApi(options =>
            {
                options.DocumentName = "swagger";
                options.Path = "/swagger/v1/swagger.json";
            });

            app.UseOpenApi(config =>
            {
                config.DocumentName = "v1";
                config.Path = "/openapi/v1/openapi.json";
            });

            // Add web UIs to interact with the document
            app.UseSwaggerUi3(options =>
            {
                // Define web UI route
                options.Path = "/swagger";
                options.DocumentPath = "/swagger/v1/swagger.json";
            });

            // Add web UIs to interact with the document
            app.UseSwaggerUi3(options =>
            {
                options.Path = "/openapi";
                options.DocumentPath = "/openapi/v1/openapi.json";
            });
        }
    }
}