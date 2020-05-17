
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Samples
{
    public static class CustomDynamicExtensions
    {
        public static void MapCustom<TController>(this IEndpointRouteBuilder endpoints, string pattern, Expression<Action<TController>> expression)
        {
            EnsureControllersRegistered(endpoints);

            var method = (MethodCallExpression)expression.Body;
            var metadata = new CustomMatcherPolicy.Metadata(typeof(TController), method);
            var builder = endpoints.Map(pattern, (_) => throw null);
            builder.Add(b => b.Metadata.Add(metadata));

            // DO NOT return the builder here. It's meaningless to customize the builder, because it
            // does not execute.
        }

        public static void MapCustom<TController>(this IEndpointRouteBuilder endpoints, string pattern, Expression<Func<TController, Task>> expression)
        {
            EnsureControllersRegistered(endpoints);

            var method = (MethodCallExpression)expression.Body;
            var metadata = new CustomMatcherPolicy.Metadata(typeof(TController), method);
            var builder = endpoints.Map(pattern, (_) => throw null);
            builder.Add(b => b.Metadata.Add(metadata));

            // DO NOT return the builder here. It's meaningless to customize the builder, because it
            // does not execute.
        }

        private static void EnsureControllersRegistered(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapControllers();

            // Get the controller data source and enable "inert" (non-routable) endpoints.
            var typeName = "Microsoft.AspNetCore.Mvc.Routing.ControllerActionEndpointDataSource";
            var assembly = typeof(ControllerBase).Assembly;
            var type = assembly.GetType(typeName, throwOnError: true);
            var controllers = endpoints.DataSources.First(d => type.IsInstanceOfType(d));
            var property = type.GetProperty("CreateInertEndpoints", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(controllers, true);
        }
    }
}