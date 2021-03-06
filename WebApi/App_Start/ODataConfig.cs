﻿namespace forCrowd.Backbone.WebApi
{
    using Facade;
    using Conventions;
    using System.Web.Http;
    using System.Web.Http.OData;
    using System.Web.Http.OData.Batch;
    using System.Web.Http.OData.Extensions;
    using System.Web.Http.OData.Query;
    using System.Web.Http.OData.Routing;
    using System.Web.Http.OData.Routing.Conventions;

    public static class ODataConfig
    {
        public static void RegisterOData(HttpConfiguration config)
        {
            // Query support
            var odataFilter = new EnableQueryAttribute
            {
                AllowedArithmeticOperators = AllowedArithmeticOperators.None,
                AllowedFunctions = AllowedFunctions.SubstringOf,
                AllowedLogicalOperators = AllowedLogicalOperators.And |
                    AllowedLogicalOperators.Equal |
                    AllowedLogicalOperators.Or,
                AllowedQueryOptions = AllowedQueryOptions.Expand |
                    AllowedQueryOptions.Filter |
                    AllowedQueryOptions.InlineCount |
                    AllowedQueryOptions.OrderBy |
                    AllowedQueryOptions.Skip |
                    AllowedQueryOptions.Top,
                MaxExpansionDepth = 4,
                MaxNodeCount = 20,
                PageSize = 100                
            };

            config.AddODataQueryFilter(odataFilter);

            // Add the CompositeKeyRoutingConvention
            var conventions = ODataRoutingConventions.CreateDefault();
            conventions.Insert(0, new CompositeKeyRoutingConvention());

            // Routes
            var edm = DbUtility.GetBackboneEdm();
            config.Routes.MapODataServiceRoute(
                routeName: "ODataRoute",
                routePrefix: "odata/v1",
                model: edm,
                pathHandler: new DefaultODataPathHandler(),
                routingConventions: conventions,
                batchHandler: new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer)); // Without this line, it fails in 'batch save' operations
        }
    }
}
