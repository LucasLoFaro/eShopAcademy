using SendGrid;
using SendGrid.Helpers.Mail;


namespace NotificationService;

public class SendGridEmailSender : IEmailSender
{
    private readonly SendGridClient _client;
    private readonly string _fromAddress;
    private readonly string _fromName;

    public SendGridEmailSender(IConfiguration config)
    {
        _client = new SendGridClient(config["SendGrid:ApiKey"]);
        _fromAddress = config["SendGrid:FromAddress"] ?? "lucaslofaro@hotmail.com";
        _fromName = config["SendGrid:FromName"] ?? "Lucas";
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var plainText = subject;
        var msg = MailHelper.CreateSingleEmail(
            new EmailAddress(_fromAddress, _fromName),
            new EmailAddress(to),
            subject,
            plainText,
            htmlBody);

        var response = await _client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Body.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"SendGrid send failed: {(int)response.StatusCode} - {content}");
        }
    }
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody);
}
