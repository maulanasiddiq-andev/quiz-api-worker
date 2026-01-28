namespace QuizWorker.Queues
{
    public class NotificationQueue
    {
        public List<string> FcmTokens { get; set; } = new List<string>();
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}