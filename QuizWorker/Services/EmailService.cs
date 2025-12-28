using MailKit.Net.Smtp;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MimeKit;
using QuizWorker.Constants;
using QuizWorker.Queues;
using QuizWorker.Settings;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace QuizWorker.Services
{
    public class EmailService : BackgroundService
    {
        private readonly IConnection connection;
        private readonly EmailSetting emailSetting;
        public EmailService(
            IConnection connection,
            IOptions<EmailSetting> emailOptions
        )
        {
            this.connection = connection;
            emailSetting = emailOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(QueueConstant.EmailQueue, true, false, false);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, args) =>
            {
                var json = Encoding.UTF8.GetString(args.Body.ToArray());
                var message = JsonSerializer.Deserialize<EmailQueue>(json)!;

                await SendEmailAsync(message);

                await channel.BasicAckAsync(args.DeliveryTag, false);  
            };

            await channel.BasicConsumeAsync(QueueConstant.EmailQueue, false, consumer);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private Task SendEmailAsync(EmailQueue email)
        {
            // plug your SMTP provider here
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Everyday Quiz", "maulanasiddiqdeveloper@gmail.com"));
            message.To.Add(new MailboxAddress(email.Name, email.Email));
            message.Subject = "Kode OTP";

            message.Body = new TextPart("html")
            {
                Text = $@"
                <!DOCTYPE html>
                <html>
                    <body style='font-family:Arial, sans-serif; background:#f4f4f4; padding:20px;'>

                    <div style='max-width:600px; margin:0 auto; background:#ffffff; padding:20px; border-radius:10px;'>
                        <h2 style='color:#333;'>Hai {email.Name},</h2>

                        <p style='color:#555; line-height:1.6;'>
                            {email.Text}
                        </p>

                        <p style='font-size:12px; color:#888; margin-top:25px;'>
                            — Everyday Quiz
                        </p>
                    </div>

                    </body>
                </html>"
            };

            using (var client = new SmtpClient())
            {
                client.Connect(emailSetting.SmtpServer, emailSetting.Port, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate(emailSetting.Username, emailSetting.Password);

                client.Send(message);
                client.Disconnect(true);
            }
            
            return Task.CompletedTask;
        }
    }
}