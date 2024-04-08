using System;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace Tldr.ToastNotificationFramework
{
	public abstract class PluginBase : IPlugin
	{
		public PluginBase ()
		{
		}

		public PluginBase (string UnsecureConfig, string SecureConfig)
		{
			this._UnsecureConfig = UnsecureConfig;
			this._SecureConfig = SecureConfig;
		}

		public void Execute (IServiceProvider serviceProvider)
		{
			var ctx = new ExecutionContext(serviceProvider, _UnsecureConfig, _SecureConfig);

			ExecuteAction(new ExecutionContext(serviceProvider, _UnsecureConfig, _SecureConfig));
		}

		private string _UnsecureConfig;
		private string _SecureConfig;

		public abstract void ExecuteAction (ExecutionContext context);
	}

	public class ExecutionContext
	{
		public ExecutionContext (IServiceProvider serviceProvider, string unsecureConfig, string secureConfig)
		{
			PluginContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
			TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
			var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
			Service = serviceFactory.CreateOrganizationService(null);
			UnsecureConfig = new PluginConfig(unsecureConfig);
		}

		public IPluginExecutionContext PluginContext;
		public IOrganizationService Service;
		public ITracingService TracingService;
		public PluginConfig UnsecureConfig;
		public PluginConfig SecureConfig;
		public Entity Target => (Entity)PluginContext.InputParameters["Target"];
		public Entity PreImage => PluginContext.PreEntityImages.Values.FirstOrDefault();
		public Entity PostImage => PluginContext.PostEntityImages.Values.FirstOrDefault();
		public Guid InitiatingUserId => PluginContext.InitiatingUserId;
	}

	public class PluginConfig
	{
		public PluginConfig (string config)
		{
			if (!string.IsNullOrEmpty(config))
				Setting = config;
		}

		public string Setting { get; set; }
	}
}