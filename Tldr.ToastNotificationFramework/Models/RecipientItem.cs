using System;

namespace Tldr.ToastNotificationFramework
{
	public class RecipientItem
	{
		public Guid Id { get; set; }
		public string Email { get; set; } = string.Empty;

		public RecipientItem (Guid recipientId, string recipientEmail)
		{
			Id = recipientId;
			Email = recipientEmail;
		}
	}
}