using Microsoft.AspNetCore.Mvc;
using QuotesProject.Models;
using QuotesProject.Services;
using System.Diagnostics;
using System.Net;

namespace QuotesProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuoteController : Controller
    {
        private readonly QuoteApiService _quoteApiService;

        public QuoteController(QuoteApiService quoteApiService)
        {
            _quoteApiService = quoteApiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var quotes = await _quoteApiService.GetQuotesAsync();
                var viewModel = new QuoteViewModel { Quotes = quotes };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error loading quotes: " + ex.Message);
            }
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuote(Quote quote)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdQuote = await _quoteApiService.CreateQuoteAsync(quote);
                return Ok(createdQuote);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error creating quote: " + ex.Message);
            }
        }



        [HttpPut]
        public async Task<IActionResult> UpdateQuote(Quote quote)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _quoteApiService.UpdateQuoteAsync(quote);
                return Ok(quote);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("Quote not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error updating quote: " + ex.Message);
            }
        }

        [HttpDelete("{key}")]
        public async Task<IActionResult> DeleteQuote(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return BadRequest("A valid quote key is required");

            try
            {
                await _quoteApiService.DeleteQuoteAsync(key);
                return Ok("Quote deleted successfully");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("Quote not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error deleting quote: " + ex.Message);
            }
        }

        [HttpGet("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
