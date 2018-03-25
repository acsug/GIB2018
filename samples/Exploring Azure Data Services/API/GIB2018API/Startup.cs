using System;
using System.Buffers;
using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Swashbuckle.AspNetCore.Swagger;

using GIB2018API.Model;
using GIB2018API.Model.Config;
using GIB2018API.DataAccess;
using GIB2018API.Serialization;

namespace GIB2018API
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
            services.Configure<CosmosDbConfig>(Configuration.GetSection("CosmosDB"));

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info {
                    Title = "GIB 2018 API",
                    Version = "v1",
                    Description = "A sample ASP.NET Core Web API for GIB 2018",
                    TermsOfService = "None",
                    Contact = new Contact { Name = "Grant Harmer", Email = "", Url = "https://www.linkedin.com/in/grantharmer/" }
                 });
            });

            services.AddSingleton<IDataAccess<Customer>, CosmosDBDataAccess<Customer>>();
            services.AddSingleton<IDataAccess<Product>, CosmosDBDataAccess<Product>>();
            services.AddSingleton<IDataAccess<Order>, CosmosDBDataAccess<Order>>();

            services.AddTransient<IDbScaffolding, DbScaffolding>();

            services.AddMvc(config =>
            {
                // These lines prevent serialization errors when not returning a JSON object in the response.
                //   e.g. when just returning a Guid
                config.OutputFormatters.RemoveType<JsonOutputFormatter>();
                config.OutputFormatters.Insert(0, GetJsonFormatter());
            }).AddJsonOptions(options =>
            {
                options.SerializerSettings.Converters = GetJsonSerializerConverters();

                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                options.SerializerSettings.DateParseHandling = DateParseHandling.None;

            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "GIB 2018 API");
            });

            app.UseMvc();

            var dbScaffoling = app.ApplicationServices.GetService<IDbScaffolding>();
            dbScaffoling.RunAsync().Wait();
        }

        private List<JsonConverter> GetJsonSerializerConverters()
        {
            return new List<JsonConverter>()
                {
                    new StringEnumConverter(),
                    new ApiJsonConverter()
                };
        }

        private JsonOutputFormatter GetJsonFormatter()
        {
            var jsonSerialiserSettings = new JsonSerializerSettings
            {
                Converters = GetJsonSerializerConverters(),
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateParseHandling = DateParseHandling.None
            };

            return new JsonOutputFormatter(jsonSerialiserSettings, ArrayPool<Char>.Shared);
        }
    }
}
