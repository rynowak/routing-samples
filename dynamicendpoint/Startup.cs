using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Samples
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, CustomMatcherPolicy>());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapCustom<MyController>("/controller", c => c.MyAction());

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Go to /controller to see custom routing in action.");
                });
            });
        }
    }
}
