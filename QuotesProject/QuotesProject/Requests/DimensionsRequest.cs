namespace QuotesProject.Requests
{
    public class DimensionsRequest
    {
        public ObjectRefRequest? Item { get; set; }
        public ObjectRefRequest? Warehouse { get; set; }
        public ObjectRefRequest? Location { get; set; }
    }
}
