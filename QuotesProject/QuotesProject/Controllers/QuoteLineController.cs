using Microsoft.AspNetCore.Mvc;
using QuotesProject.Models;
using QuotesProject.Services;
using System.Net;

namespace QuotesProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuoteLineController : Controller
    {
        private readonly QuoteLineApiService _quoteLineApiService;

        public QuoteLineController(QuoteLineApiService quoteLineApiService)
        {
            _quoteLineApiService = quoteLineApiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string quoteKey)
        {
            try
            {
                var quote = await _quoteLineApiService.GetQuoteWithLinesAsync(quoteKey);

                var viewModel = new QuoteLineViewModel
                {
                    quoteOpened = quote,
                    quoteLines = quote.Lines
                };

                return View(viewModel);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("Quote not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error loading quote lines: " + ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateQuoteLine(QuoteLine line)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _quoteLineApiService.UpdateQuoteLineAsync(line);
                return Ok(line);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("Quote line not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error updating quote line: " + ex.Message);
            }
        }

        [HttpDelete("{quoteKey}/{lineKey}")]
        public async Task<IActionResult> DeleteQuoteLine(string quoteKey, string lineKey)
        {
            if (string.IsNullOrWhiteSpace(quoteKey) || string.IsNullOrWhiteSpace(lineKey))
                return BadRequest("A valid quote key and line key are required");

            try
            {
                await _quoteLineApiService.DeleteQuoteLineAsync(quoteKey, lineKey);
                return Ok("Quote line deleted successfully");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("Quote line not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error deleting quote line: " + ex.Message);
            }
        }
    }
}
