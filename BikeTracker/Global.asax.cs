namespace BikeTracker
{
    using System;
    using System.Collections.Generic;
    using System.Web.Http;
    using System.Web.Mvc;
    using System.Web.Optimization;
    using System.Web.Routing;

    using Microsoft.ApplicationInsights.Telemetry.Services;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;

    using WebApplication2;

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            InitializeCors();

            ServerAnalytics.Start("9a2d9ad9-45f0-457a-a443-996a75f70e4c");

        }

        private void InitializeCors()
        {
            var tableServiceProperties = new ServiceProperties();
            var tableClient =
                new CloudStorageAccount(
                    new StorageCredentials(
                        CloudConfigurationManager.GetSetting("storageAccountName"),
                        CloudConfigurationManager.GetSetting("storageAccountKey")),
                    true).CreateCloudTableClient();


            tableServiceProperties.HourMetrics = null;
            tableServiceProperties.MinuteMetrics = null;
            tableServiceProperties.Logging = null;

            tableServiceProperties.Cors = new CorsProperties();
            tableServiceProperties.Cors.CorsRules.Add(new CorsRule()
            {
                AllowedHeaders = new List<string>() { "*" },
                AllowedMethods =  CorsHttpMethods.Get | CorsHttpMethods.Head ,
                //AllowedOrigins = new List<string>() { "http://ercenkbike.azurewebsites.net/" },
                AllowedOrigins = new List<string>() { "*" },
                ExposedHeaders = new List<string>() { "*" },
                MaxAgeInSeconds = 1800 // 30 minutes
            });

            tableClient.SetServiceProperties(tableServiceProperties);
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            ServerAnalytics.BeginRequest();
            ServerAnalytics.CurrentRequest.LogEvent(
                Request.Url.AbsolutePath);
        }
    }
}
