using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Samples
{
    internal class CustomDataSource : EndpointDataSource
    {
        private readonly EndpointDataSource controllers;

        public CustomDataSource(EndpointDataSource controllers)
        {
            this.controllers = controllers;
        }

        public List<Mapping> Mappings { get; } = new List<Mapping>();

        // top level endpoint collection is cached, it would complicate the code to do caching here.
        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                var endpoints = new List<Endpoint>();
                foreach (var mapping in Mappings)
                {
                    var match = FindMatchingEndpoint(mapping, controllers.Endpoints);

                    var builder = new RouteEndpointBuilder(match.RequestDelegate, mapping.Pattern, order: 0);
                    foreach (var metadata in match.Metadata)
                    {
                        builder.Metadata.Add(metadata);
                    }

                    foreach (var action in mapping.Builder.Actions)
                    {
                        action(builder);
                    }

                    endpoints.Add(builder.Build());
                }

                return endpoints;
            }
        }

        public override IChangeToken GetChangeToken()
        {
            return controllers.GetChangeToken(); // Change when controllers change (usually never).
        }

        private static Endpoint FindMatchingEndpoint(Mapping mapping, IReadOnlyList<Endpoint> endpoints)
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

                if (action.ControllerTypeInfo.AsType() != mapping.ControllerType)
                {
                    continue;
                }

                if (mapping.Method.Method != action.MethodInfo)
                {
                    continue;
                }

                return endpoint;
            }

            return null;
        }

        public readonly struct Mapping
        {
            public readonly Type ControllerType;
            public readonly MethodCallExpression Method;
            public readonly RoutePattern Pattern;

            public readonly ConventionBuilder Builder;

            public Mapping(RoutePattern pattern, Type controllerType, MethodCallExpression method)
            {
                Pattern = pattern;
                ControllerType = controllerType;
                Method = method;

                Builder = new ConventionBuilder();
            }
        }

        public class ConventionBuilder : IEndpointConventionBuilder
        {
            public List<Action<EndpointBuilder>> Actions { get; } = new List<Action<EndpointBuilder>>();

            public void Add(Action<EndpointBuilder> convention)
            {
                Actions.Add(convention);
            }
        }
    }
}