namespace QuotesProject.Responses
{
    /// <summary>
    /// How Sage Intacct returns a reference to a related object
    /// (customer, item, location...) inside a response body.
    /// </summary>
    public class ObjectRefResponse
    {
        public string? Key { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Href { get; set; }
    }
}
