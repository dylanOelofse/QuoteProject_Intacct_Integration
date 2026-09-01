using System.Globalization;
using QuotesProject.Api;
using QuotesProject.Models;

namespace QuotesProject.Services
{
    public class LookupStore
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SemaphoreSlim _loadLock = new(1, 1);

        public LookupStore(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public List<Customer> Customers { get; private set; } = new();

        public List<Item> Items { get; private set; } = new();

        public bool IsLoaded { get; private set; }

        public DateTime? LoadedAt { get; private set; }

        public async Task LoadAsync()
        {
            await _loadLock.WaitAsync();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<QuotesApiEngine>();

                var customerRows = await engine.QueryCustomersAsync();
                var itemRows = await engine.QueryItemsByWarehouseAsync();

                Customers = customerRows
                    .Where(row => !string.IsNullOrWhiteSpace(row.Id))
                    .Select(row => new Customer
                    {
                        Key = row.Key,
                        Id = row.Id!,
                        Name = row.Name,
                        Address = row.ShipToContactId ?? row.DefaultContactId
                    })
                    .OrderBy(customer => customer.Name)
                    .ToList();

                Items = itemRows
                    .Where(row => !string.IsNullOrWhiteSpace(row.ItemId) && row.ItemStatus == "active")
                    .Select(row => new Item
                    {
                        Key = row.ItemKey,
                        Id = row.ItemId!,
                        Name = row.ItemName,
                        QuantityOnHand = row.OnHand ?? 0
                    })
                    .OrderBy(item => item.Name)
                    .ToList();

                IsLoaded = true;
                LoadedAt = DateTime.Now;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsLoaded)
                return;

            await LoadAsync();
        }
    }
}
