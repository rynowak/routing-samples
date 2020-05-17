using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Samples
{
    public class CustomMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy, IDisposable
    {
        private readonly IServiceProvider services;
        private ConcurrentDictionary<Metadata, Endpoint> cache;
        private EndpointDataSource dataSource;
        private IDisposable subscription;

        // You can't interact with Data Sources in the constructor that causes a cycle.
        public CustomMatcherPolicy(IServiceProvider services)
        {
            this.services = services;
        }

        // Same as dynamic controller policy. We want to go *early* before policies like HTTP methods.
        public override int Order => int.MinValue + 100;

        void IDisposable.Dispose() => subscription.Dispose();

        private void EnsureInitialized()
        {
            if (this.dataSource is object)
            {
                return;
            }

            this.dataSource = services.GetRequiredService<EndpointDataSource>();

            // Cache results, but clear them if the underlying endpoints change.
            this.cache = new ConcurrentDictionary<Metadata, Endpoint>();
            this.subscription = ChangeToken.OnChange(() => this.dataSource.GetChangeToken(), () => cache.Clear());
        }

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            EnsureInitialized();
            return endpoints.Any(e => e.Metadata.GetMetadata<Metadata>() is Metadata);
        }

        public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                if (!candidates.IsValidCandidate(i))
                {
                    continue;
                }

                var metadata = candidates[i].Endpoint.Metadata.GetMetadata<Metadata>();
                if (metadata is null)
                {
                    continue;
                }

                // this one is OURS!
                //
                // since we own it we can update values in place.
                var values = candidates[i].Values ?? new RouteValueDictionary();

                if (!cache.TryGetValue(metadata, out var result))
                {
                    // use whatever criteria you want to match this to an MVC action. We're using an expression/type.
                    result = FindMatchingEndpoint(metadata, dataSource.Endpoints);

                    cache.TryAdd(metadata, result);
                }

                // emplace the MVC standard route values to be convincing
                var action = result.Metadata.GetMetadata<ControllerActionDescriptor>();
                foreach (var kvp in action.RouteValues)
                {
                    if (kvp.Value is string s && s.Length > 0)
                    {
                        values[kvp.Key] = kvp.Value;
                    }
                }

                if (result is null)
                {
                    throw new InvalidOperationException("Derp.");
                }

                candidates.ReplaceEndpoint(i, result, values);
            }

            return Task.CompletedTask;
        }

        private static Endpoint FindMatchingEndpoint(Metadata metadata, IReadOnlyList<Endpoint> endpoints)
        {
            foreach (var endpoint in endpoints)
            {
                if (endpoint is RouteEndpoint)
                {
                    continue;
                }

                var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
                if (action is null)
                {
                    continue;
                }

                if (action.ControllerTypeInfo.AsType() != metadata.ControllerType)
                {
                    continue;
                }

                if (metadata.Method.Method != action.MethodInfo)
                {
                    continue;
                }

                return endpoint;
            }

            return null;
        }

        public class Metadata : IDynamicEndpointMetadata
        {
            public readonly Type ControllerType;
            public readonly MethodCallExpression Method;

            public Metadata(Type controllerType, MethodCallExpression method)
            {
                ControllerType = controllerType;
                Method = method;
            }

            // tell everyone else to EXPECT THE UNEXPECTED
            public bool IsDynamic => true;
        }
    }
}