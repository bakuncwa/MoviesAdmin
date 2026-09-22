namespace MoviesAdmin.Models
{
    public class ErrorViewModel
    {
        // Diagnostic trace identifier for the failed request, shown on the error page for support/debugging.
        public string? RequestId { get; set; }

        // Whether the error view should render the RequestId line at all.
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
