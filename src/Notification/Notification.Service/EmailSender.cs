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
        _fromAddress = config["SendGrid:FromAddress"] ?? "lucaslofaro@hotmail.com"; //?? "no-reply@eshopacademy.com";
        _fromName = config["SendGrid:FromName"] ?? "Lucas";//?? "eShopAcademy";
    }

    public async Task SendStatusUpdateAsync(string to, string orderNumber, string status)
    {
        var subject = $"Order {orderNumber} - {status}";
        var plainTextContent = $"Your order n° {orderNumber} is now: {status}";
        var htmlContent = "<h2><strong>eShopAcademy</strong></h2>";
        var msg = MailHelper.CreateSingleEmail(new EmailAddress(_fromAddress, _fromName), new EmailAddress(to), subject, plainTextContent, htmlContent);

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
    Task SendStatusUpdateAsync(string to, string orderNumber, string status);
}
