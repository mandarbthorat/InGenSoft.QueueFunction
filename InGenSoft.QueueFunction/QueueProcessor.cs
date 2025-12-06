using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InGenSoft.QueueFunction
{
    public class QueueProcessor
    {
        private readonly ILogger<QueueProcessor> _logger;

        public QueueProcessor(ILogger<QueueProcessor> logger)
        {
            _logger = logger;
        }

        [Function(nameof(QueueProcessor))]
        public void Run(
            [QueueTrigger("input-queue", Connection = "AzureWebJobsStorage")] string myQueueItem)
        {
            _logger.LogInformation($"Message RECEIVED at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}");
            _logger.LogInformation($"Message Content: {myQueueItem}");

            try
            {
                _logger.LogInformation("Processing message...");
                System.Threading.Thread.Sleep(1000); // Simulate work
                ProcessMessage(myQueueItem);
                _logger.LogInformation("Message processed SUCCESSFULLY");
                _logger.LogInformation("Message will be ACKNOWLEDGED and DELETED from queue");
                _logger.LogInformation($"Completed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message: {ex.Message}");
                _logger.LogError("Message will remain in queue for RETRY");
                throw; 
            }
            finally
            {
                _logger.LogInformation("========================================");
            }
        }

        private void ProcessMessage(string message)
        {
            _logger.LogInformation($"Business logic executing for: {message}");
        }
    }
}
