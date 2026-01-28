using System.Text;
using System.Text.Json;
using QuizWorker.Constants;
using QuizWorker.Settings;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;
using QuizWorker.Queues;

namespace QuizWorker.Services
{
    public class QuizTakenNotificationService : BackgroundService
    {
        private readonly IConnection connection;
        private readonly PushNotificationSetting pushNotificationSetting;
        public QuizTakenNotificationService(IConnection connection, PushNotificationSetting pushNotificationSetting)
        {
            this.connection = connection;
            this.pushNotificationSetting = pushNotificationSetting;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(QueueConstant.NotificationQueue, true, false, false);
            await channel.BasicQosAsync(0, 1, false);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, args) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(args.Body.ToArray());
                    var message = JsonSerializer.Deserialize<NotificationQueue>(json)!;

                    foreach (var fcmToken in message.FcmTokens)
                    {
                        await SendNotificationAsync(fcmToken, message.Title, message.Body);
                    }

                    await channel.BasicAckAsync(args.DeliveryTag, false);   
                }
                catch (Exception)
                {
                    await channel.BasicNackAsync(args.DeliveryTag, false, true);
                }
            };

            await channel.BasicConsumeAsync(QueueConstant.EmailQueue, false, consumer);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public async Task SendNotificationAsync(
            string fcmToken,
            string title,
            string body
        )
        {
            var message = new
            {
                message = new
                {
                    token = fcmToken,
                    notification = new
                    {
                        title,
                        body
                    }
                }
            };

            var json = JsonSerializer.Serialize(message);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.PostAsync(
                $"https://fcm.googleapis.com/v1/projects/{pushNotificationSetting.ProjectId}/messages:send",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var result = await response.Content.ReadAsStringAsync();
        }

        private async Task<string> GetAccessTokenAsync()
        {
            // redirect the app to firebase service account
            // the file is related to the compiled app
            // var path = Path.Combine(AppContext.BaseDirectory, "firebase-service-account.json");
            
            var credential = await GoogleCredential.GetApplicationDefaultAsync();

            credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");

            var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            return token;
        }
    }
}