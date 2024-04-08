using System;

namespace Tldr.ToastNotificationFramework
{
	public enum ToastNotificationStateCode
	{
		ACTIVE = 0,
		INACTIVE = 1
	}

	public enum PluginStepStateCode
	{
		ENABLED = 0,
		DISABLED = 1
	}

	public enum ToastNotificationStatusCode
	{
		ENABLED = 1,
		DISABLED = 2
	}

	public enum PluginStepStatusCode
	{
		ENABLED = 1,
		DISABLED = 2
	}

	public enum ToastNotificationRecipientTypeCode
	{
		EMAIL = 214220000,
		LOOKUP = 214220001
	}

	public enum ToastNotificationBehavior
	{
		TIMED = 200000000,
		HIDDEN = 200000001
	}

	public enum SdkStepTypeCode
	{
		CREATE = 214220000,
		UPDATE = 214220001
	}

	public enum PluginStepMode
	{
		Sync = 0,
		Async = 1
	}

	public enum PluginStepSupportedDeployment
	{
		Server = 0,
		Offline = 1,
		Both = 2
	}

	public enum PluginStepInvocationSource
	{
		Parent = 0,
		Child = 1
	}

	public enum PluginStepStage
	{
		PreValidation = 10,
		PreOperation = 20,
		PostOperation = 40
	}

	public enum PluginStepImageType
	{
		PreImage = 0,
		PostImage = 1,
		Both = 2
	}
}