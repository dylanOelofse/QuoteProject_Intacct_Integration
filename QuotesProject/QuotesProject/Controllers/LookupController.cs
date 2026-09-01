using Microsoft.AspNetCore.Mvc;
using QuotesProject.Services;

namespace QuotesProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LookupController : Controller
    {
        private readonly LookupStore _store;

        public LookupController(LookupStore store)
        {
            _store = store;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                await _store.EnsureLoadedAsync();

                return Ok(new
                {
                    customers = _store.Customers,
                    items = _store.Items,
                    loadedAt = _store.LoadedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error loading customers and items: " + ex.Message);
            }
        }

        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                await _store.LoadAsync();

                return Ok(new
                {
                    customerCount = _store.Customers.Count,
                    itemCount = _store.Items.Count,
                    loadedAt = _store.LoadedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error refreshing customers and items: " + ex.Message);
            }
        }
    }
}
