using QuizWorker.Services;
using RabbitMQ.Client;

namespace QuizWorker.Extensions
{
    public static class ServiceExtension
    {
        public static void RegisterRepositories(this IServiceCollection collection)
        {
            collection.AddSingleton(sp =>
            {
                return new ConnectionFactory
               {
                    HostName = "localhost",
                    UserName = "guest",
                    Password = "guest",
                    ConsumerDispatchConcurrency = 1
               }.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            collection.AddHostedService<EmailService>();
        }
    }
}