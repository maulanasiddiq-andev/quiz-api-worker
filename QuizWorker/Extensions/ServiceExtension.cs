using QuizWorker.Services;

namespace QuizWorker.Extensions
{
    public static class ServiceExtension
    {
        public static void RegisterRepositories(this IServiceCollection collection)
        {
            collection.AddHostedService<EmailService>();
        }
    }
}