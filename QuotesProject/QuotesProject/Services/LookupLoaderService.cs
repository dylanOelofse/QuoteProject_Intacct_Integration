namespace QuotesProject.Services
{
    public class LookupLoaderService : BackgroundService
    {
        private readonly LookupStore _store;
        private readonly ILogger<LookupLoaderService> _logger;

        public LookupLoaderService(LookupStore store, ILogger<LookupLoaderService> logger)
        {
            _store = store;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _store.LoadAsync();

                _logger.LogInformation(
                    "Loaded {CustomerCount} customers and {ItemCount} items from Sage Intacct.",
                    _store.Customers.Count,
                    _store.Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not load customers and items at startup.");
            }
        }
    }
}
