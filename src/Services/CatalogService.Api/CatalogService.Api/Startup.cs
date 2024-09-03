using CatalogService.Extensions;
using CatalogService.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;

namespace CatalogService;

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
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "CatalogService.Api", Version = "v1" });
        });

        services.Configure<CatalogSettings>(Configuration.GetSection("CatalogSettings"));
        services.ConfigureDbContext(Configuration);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CatalogService.Api v1"));
        }
        app.UseHttpsRedirection();

        app.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = new PhysicalFileProvider(System.IO.Path.Combine(env.ContentRootPath, "Pics")),
            RequestPath = "/pics"
        });

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        //app.RegisterWithConsul(lifetime, Configuration);
    }
}