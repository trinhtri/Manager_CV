using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.UI;
using ManagerCV.ConfigToSendMail.Dto;
using ManagerCV.Models;
using ManagerCV.MultiTenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using ManagerCV.Configuration;
namespace ManagerCV.ConfigToSendMail.EmailSender
{
	public class ConfigEmailSender : SolutionsAppServiceBase, IConfigEmailSender
	{
		private readonly IWebHostEnvironment _env;
		private IConfigurationRoot _configRoot;
		private readonly IAppFolders _appFolders;
		public ConfigEmailSender(
		IConfiguration configRoot,
		IAppFolders appFolders
		 )
		{
			_configRoot = (IConfigurationRoot)configRoot;
			_appFolders = appFolders;
		}

		public async Task Contact(CreateConfigToSendMailDto input)
		{
			try
			{
				var ssl = input.UseSSL == 1 ? true : false;

				var messageToSend = new MimeMessage
				{
					Sender = new MailboxAddress("Sender Name", input.UserName),
					Subject = input.Title,
				};
				messageToSend.Body = new TextPart(TextFormat.Html) { Text = input.Content };
				messageToSend.To.Add(new MailboxAddress(input.ToMail));

				if (input.CCMail != null)
				{
					// cắt email từ list email
					foreach (var address in input.CCMail.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
					{
						messageToSend.To.Add(new MailboxAddress(address));
					}
				}

				using (MailKit.Net.Smtp.SmtpClient emailClient = new MailKit.Net.Smtp.SmtpClient())
				{
					emailClient.CheckCertificateRevocation = false;
					emailClient.Connect(input.ServerURL, input.Port, ssl);
					emailClient.Authenticate(input.UserName, input.PassWord);
					emailClient.Send(messageToSend);
					emailClient.Disconnect(true);
				}
			}
			catch (Exception e)
			{
				Logger.Error(e.Message);
				Logger.Error(e.InnerException?.Message);
				Logger.Error(e.StackTrace);
				throw new UserFriendlyException(L("ConfigToSendMailFail.PleaseChecked."));
			}
		}

		public async Task SendJDForCustomer(SenJDForCustomerDto input)
		{
			string UserName = _configRoot["ConfigEmail:UserName"];
			string Password = _configRoot["ConfigEmail:Password"];
			string Port = _configRoot["ConfigEmail:Port"];
			string Host = _configRoot["ConfigEmail:Host"];
			string ssl = _configRoot["ConfigEmail:EnableSsl"];
			var messageToSend = new MimeMessage
			{
				Sender = new MailboxAddress("Sender Name", UserName),
				Subject = input.Title,
			};

			messageToSend.Body = new TextPart(TextFormat.Html) { Text = input.Content };
			// cắt email từ list email
			if (!input.ToMail.IsNullOrEmpty())
			{
				foreach (var address in input.ToMail.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
				{
					messageToSend.Bcc.Add(new MailboxAddress(address));
				}
			}

			if (input.CCMail != null)
			{
				foreach (var ccMailAddress in input.CCMail.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
				{
					messageToSend.Cc.Add(new MailboxAddress(ccMailAddress));
				}
			}
			// gửi email đính kèm PDF
			if (input.IsAttackJD)
			{
				var filePath = Path.Combine(_appFolders.TempFileUploadJDFolder, input.JDName);
				BodyBuilder bodyBuilder = new BodyBuilder();
				bodyBuilder.HtmlBody = input.Content;
				bodyBuilder.Attachments.Add(filePath);
				messageToSend.Body = bodyBuilder.ToMessageBody();
			}
			try
			{
				using (MailKit.Net.Smtp.SmtpClient emailClient = new MailKit.Net.Smtp.SmtpClient())
				{
					emailClient.CheckCertificateRevocation = false;
					await emailClient.ConnectAsync(Host, Int32.Parse(Port), bool.Parse(ssl));
					await emailClient.AuthenticateAsync(UserName, Password);
					await emailClient.SendAsync(messageToSend);
					await emailClient.DisconnectAsync(true);
				}
			}
			catch (Exception e)
			{
				throw e;
			}
		}
	}
}