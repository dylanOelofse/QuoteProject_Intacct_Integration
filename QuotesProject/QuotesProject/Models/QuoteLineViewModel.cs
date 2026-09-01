namespace QuotesProject.Models
{
    public class QuoteLineViewModel
    {
        public Quote quoteOpened { get; set; } = new();
        public List<QuoteLine> quoteLines { get; set; } = new();
    }
}
