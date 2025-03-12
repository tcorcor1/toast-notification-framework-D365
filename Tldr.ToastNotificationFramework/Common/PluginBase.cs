using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Tldr.ToastNotificationFramework
{
	public abstract class PluginBase : IPlugin
	{
		public string _unsecureConfig { get; set; }
		public string _secureConfig { get; set; }

		public PluginBase ()
		{
		}

		public PluginBase (string unsecureConfig, string secureConfig)
		{
			_unsecureConfig = unsecureConfig;
			_secureConfig = secureConfig;
		}

		public void Execute (IServiceProvider serviceProvider)
		{
			var ctx = new ExecutionContext(serviceProvider, _unsecureConfig, _secureConfig);

			ExecuteAction(new ExecutionContext(serviceProvider, _unsecureConfig, _secureConfig));
		}

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
			SecureConfig = new PluginConfig(secureConfig);
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

		public Dictionary<string, object> GetEnvironmentVariableValues (params object[] envVariableNameCollection)
		{
			var query = new QueryExpression("environmentvariabledefinition")
			{
				ColumnSet = new ColumnSet("defaultvalue", "schemaname", "environmentvariabledefinitionid", "type"),
				LinkEntities =
				{
					new LinkEntity
					{
						JoinOperator = JoinOperator.LeftOuter,
						LinkFromEntityName = "environmentvariabledefinition",
						LinkFromAttributeName = "environmentvariabledefinitionid",
						LinkToEntityName = "environmentvariablevalue",
						LinkToAttributeName = "environmentvariabledefinitionid",
						Columns = new ColumnSet("statecode", "value", "environmentvariablevalueid"),
						EntityAlias = "aliasvalue"
					}
				}
			};

			var conditionExpressions = envVariableNameCollection.Select(var => new ConditionExpression("schemaname", ConditionOperator.Equal, var));

			var filter = new FilterExpression(LogicalOperator.Or);
			filter.Conditions.AddRange(conditionExpressions);

			query.Criteria.AddFilter(filter);

			var envVarResponse = Service.RetrieveMultiple(query);

			var envVarDictionary = new Dictionary<string, object>();

			foreach (var item in envVarResponse.Entities)
			{
				var containsAliasedValue = item.Attributes.Contains("aliasvalue.value");

				var value = (containsAliasedValue) ? ((AliasedValue)item.Attributes["aliasvalue.value"]).Value : (string)item.Attributes["defaultvalue"];
				envVarDictionary.Add((string)item.Attributes["schemaname"], value);
			}

			return envVarDictionary;
		}
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