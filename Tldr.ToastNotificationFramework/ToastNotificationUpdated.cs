﻿using System;
using System.Text.Json;
using Microsoft.Xrm.Sdk;

namespace Tldr.ToastNotificationFramework
{
	public class ToastNotificationUpdated : PluginBase
	{
		public override void ExecuteAction (ExecutionContext context)
		{
			try
			{
				var queryService = new QueryService(context.Service);

				var targetEntityName = (string)context.PostImage.Attributes["yyz_sdksteptargetentity"];
				var sdkMessageEntity = queryService.GetSdkMessage(((OptionSetValue)context.PostImage.Attributes["yyz_sdksteptypecode"]).Value);
				var sdkMessageFilter = queryService.GetSdkMessageFilter(targetEntityName, sdkMessageEntity.Id);

				var isUpdateMessage = ((OptionSetValue)context.PostImage.Attributes["yyz_sdksteptypecode"]).Value == (int)SdkStepTypeCode.UPDATE;

				var sdkMessageProcessingStep = new Entity("sdkmessageprocessingstep", ((EntityReference)context.PostImage.Attributes["yyz_sdkstepid"]).Id)
				{
					["name"] = $"{targetEntityName.ToUpper()} ({sdkMessageEntity.Attributes["name"]}): {context.PostImage.Attributes["yyz_name"]}",
					["mode"] = new OptionSetValue((int)PluginStepMode.Async),
					["rank"] = 1,
					["plugintypeid"] = new EntityReference("plugintype", new Guid("384aa67f-aa3d-4ad6-802c-97aa505bcea2")),
					["sdkmessageid"] = new EntityReference("sdkmessage", sdkMessageEntity.Id),
					["stage"] = new OptionSetValue((int)PluginStepStage.PostOperation),
					["supporteddeployment"] = new OptionSetValue((int)PluginStepSupportedDeployment.Server),
					["invocationsource"] = new OptionSetValue((int)PluginStepInvocationSource.Parent),
					["asyncautodelete"] = true,
					["sdkmessagefilterid"] = new EntityReference("sdkmessagefilter", sdkMessageFilter.Id),
					["filteringattributes"] = isUpdateMessage ? context.PostImage["yyz_sdksteptriggerfields"] : null,
					["statecode"] = new OptionSetValue(((OptionSetValue)context.PostImage["statecode"]).Value == (int)ToastNotificationStateCode.ACTIVE ? (int)PluginStepStateCode.ENABLED : (int)PluginStepStateCode.DISABLED)
				};

				context.Service.Update(sdkMessageProcessingStep);

				// Get environment variables & create secure config for MS Teams notifications
				var hasTeamsNotificationAttribute = context.Target.Attributes.TryGetValue("yyz_hasteamsnotification", out object teamsNotificationEnabled);

				if ((bool)teamsNotificationEnabled)
				{
					var environmentVariableCollection = context.GetEnvironmentVariableValues("yyz_TeamsNotificationEndpoint", "yyz_DynamicsHostname");

					var teamsNotificationConfig = new ToastNotificationSecureConfig()
					{
						PowerAutomateEndpoint = (string)environmentVariableCollection["yyz_TeamsNotificationEndpoint"],
						HostUrl = (string)environmentVariableCollection["yyz_DynamicsHostname"],
					};

					var secureConfigEtn = new Entity("sdkmessageprocessingstepsecureconfig")
					{
						["secureconfig"] = JsonSerializer.Serialize(teamsNotificationConfig)
					};

					var secureConfigId = context.Service.Create(secureConfigEtn);

					var updateSdkStepEtn = new Entity("sdkmessageprocessingstep", ((EntityReference)context.PostImage.Attributes["yyz_sdkstepid"]).Id)
					{
						["sdkmessageprocessingstepsecureconfigid"] = new EntityReference("sdkmessageprocessingstepsecureconfig", secureConfigId)
					};

					context.Service.Update(updateSdkStepEtn);
				}
			}
			catch (Exception ex)
			{
				throw new InvalidPluginExecutionException(ex.Message);
			}
		}
	}
}