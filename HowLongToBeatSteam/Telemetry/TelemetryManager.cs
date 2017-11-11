using System;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.Web;
using Microsoft.ApplicationInsights.WindowsServer;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;

namespace HowLongToBeatSteam.Telemetry
{
    public static class TelemetryManager
    {
        private const string AdaptiveSamplingType = "Event";
        private const int AdaptiveSamplingMaxItemsPerSecond = 5;

        public static void SetInstrumentationKey(string instrumentationKey)
        {
            if (string.IsNullOrWhiteSpace(instrumentationKey)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(instrumentationKey));

            TelemetryConfiguration.Active.InstrumentationKey = instrumentationKey;
        }

        public static void SetDeveloperMode()
        {
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;
        }

        public static void Setup(string instrumentationKey)
        {
            SetInstrumentationKey(instrumentationKey);

#if DEBUG
            SetDeveloperMode();
#endif

            //AddAndInitializeModule<DeveloperModeWithDebuggerAttachedTelemetryModule>(); TODO enable when made public in the SDK

            AddAndInitializeModule<UnhandledExceptionTelemetryModule>();
            AddAndInitializeModule<UnobservedExceptionTelemetryModule>();
            AddAndInitializeModule<ExceptionTrackingTelemetryModule>();
            AddAndInitializeModule<AspNetDiagnosticTelemetryModule>();

            AddAndInitializeModule(() => new RequestTrackingTelemetryModule
            {
                Handlers =
                {
                   "System.Web.Handlers.TransferRequestHandler",
                   "Microsoft.VisualStudio.Web.PageInspector.Runtime.Tracing.RequestDataHttpHandler",
                   "System.Web.StaticFileHandler",
                   "System.Web.Handlers.AssemblyResourceLoader",
                   "System.Web.Optimization.BundleHandler",
                   "System.Web.Script.Services.ScriptHandlerFactory",
                   "System.Web.Handlers.TraceHandler",
                   "System.Web.Services.Discovery.DiscoveryRequestHandler",
                   "System.Web.HttpDebugHandler"
                }
            });

            AddAndInitializeModule(() => new DependencyTrackingTelemetryModule
            {
                ExcludeComponentCorrelationHttpHeadersOnDomains =
                {
                    "core.windows.net",
                    "core.chinacloudapi.cn",
                    "core.cloudapi.de",
                    "core.usgovcloudapi.net",
                    "localhost",
                    "127.0.0.1"
                }
            });

            // for performance counter collection see: http://apmtips.com/blog/2015/10/07/performance-counters-in-non-web-applications/
            AddAndInitializeModule<PerformanceCollectorModule>();

            // for more information on QuickPulse see: http://apmtips.com/blog/2017/02/13/enable-application-insights-live-metrics-from-code/ 
            var quickPulseModule = AddAndInitializeModule<QuickPulseTelemetryModule>();

            TelemetryConfiguration.Active.TelemetryProcessorChainBuilder
                .Use(next => new AutocollectedMetricsExtractor(next))
                .Use(next =>
                {
                    var processor = new QuickPulseTelemetryProcessor(next);
                    quickPulseModule.RegisterTelemetryProcessor(processor);
                    return processor;
                })
                .UseAdaptiveSampling(AdaptiveSamplingMaxItemsPerSecond, excludedTypes: AdaptiveSamplingType)
                .UseAdaptiveSampling(AdaptiveSamplingMaxItemsPerSecond, excludedTypes: null, includedTypes: AdaptiveSamplingType)
                .Build();

            TelemetryConfiguration.Active.TelemetryInitializers.Add(new BuildInfoConfigComponentVersionTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new DeviceTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new AzureRoleEnvironmentTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new AzureWebAppRoleEnvironmentTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new WebTestTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new SyntheticUserAgentTelemetryInitializer {Filters = "search|spider|crawl|Bot|Monitor|AlwaysOn" });

            TelemetryConfiguration.Active.TelemetryInitializers.Add(new ClientIpHeaderTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new OperationNameTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new Microsoft.ApplicationInsights.Web.OperationCorrelationTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new UserTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new AuthenticatedUserIdTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new AccountIdTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new SessionTelemetryInitializer());

            // ReSharper disable once UseObjectOrCollectionInitializer
            var channel = new ServerTelemetryChannel();
            channel.Initialize(TelemetryConfiguration.Active);
            TelemetryConfiguration.Active.TelemetryChannel = channel;
        }

        private static T AddAndInitializeModule<T>(Func<T> factory = null)
            where T : ITelemetryModule, new()
        {
            var module = (factory != null) ? factory() : new T();
            module.Initialize(TelemetryConfiguration.Active);
            TelemetryModules.Instance.Modules.Add(module);
            return module;
        }
    }
}