using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using Npgsql;

namespace BackendAPI
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
            services.AddCors();
            services.AddControllers().ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var result = new BadRequestObjectResult(context.ModelState);

                    // TODO: add `using System.Net.Mime;` to resolve MediaTypeNames
                    result.ContentTypes.Add(MediaTypeNames.Application.Json);
                    result.ContentTypes.Add(MediaTypeNames.Application.Xml);

                    return result;
                };

            });
            string DbName = "mysql";
            
            string ConnectionString = "Server=localhost;Database=CovidAlerter;Uid=mahan;Pwd=" +
                JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    File.ReadAllText("./passwords.json")
                )[DbName] +
                ";";
            services.AddDbContext<APIDbContext>(optionsbuilder => 
                optionsbuilder.UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString), o => 
                    o.EnableRetryOnFailure().UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
            
            DbName = "pgsql";
            string PgConnectionString = $"Host=localhost;Username=mahan;Password=" +
                JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    File.ReadAllText("./passwords.json")
                )[DbName] +
            ";Database=osm;";
            services.AddSingleton(new NpgsqlConnection(PgConnectionString));
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "BackendAPI", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BackendAPI v1"));
            }
            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
