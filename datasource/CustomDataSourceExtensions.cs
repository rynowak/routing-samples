
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Samples
{
    public static class CustomDataSourceExtensions
    {
        public static IEndpointConventionBuilder MapCustom<TController>(this IEndpointRouteBuilder endpoints, string pattern, Expression<Action<TController>> expression)
        {
            var dataSource = GetOrCreateDataSource(endpoints);

            var method = (MethodCallExpression)expression.Body;
            var mapping = new CustomDataSource.Mapping(RoutePatternFactory.Parse(pattern), typeof(TController), method);
            dataSource.Mappings.Add(mapping);
            return mapping.Builder;
        }

        public static IEndpointConventionBuilder MapCustom<TController>(this IEndpointRouteBuilder endpoints, string pattern, Expression<Func<TController, Task>> expression)
        {
            var dataSource = GetOrCreateDataSource(endpoints);

            var method = (MethodCallExpression)expression.Body;
            var mapping = new CustomDataSource.Mapping(RoutePatternFactory.Parse(pattern), typeof(TController), method);
            dataSource.Mappings.Add(mapping);
            return mapping.Builder;
        }

        private static CustomDataSource GetOrCreateDataSource(IEndpointRouteBuilder endpoints)
        {
            var dataSource = endpoints.DataSources.OfType<CustomDataSource>().SingleOrDefault();
            if (dataSource is object)
            {
                return dataSource;
            }

            endpoints.MapControllers();

            // Get the controller data source and enable "inert" (non-routable) endpoints.
            var typeName = "Microsoft.AspNetCore.Mvc.Routing.ControllerActionEndpointDataSource";
            var assembly = typeof(ControllerBase).Assembly;
            var type = assembly.GetType(typeName, throwOnError: true);
            var controllers = endpoints.DataSources.First(d => type.IsInstanceOfType(d));
            var property = type.GetProperty("CreateInertEndpoints", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(controllers, true);

            dataSource = new CustomDataSource(controllers);
            endpoints.DataSources.Add(dataSource);

            return dataSource;
        }
    }
}