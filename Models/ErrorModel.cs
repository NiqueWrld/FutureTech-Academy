namespace FutureTech_Academy.Models
{
    public class ErrorModel
    {
        public int code { get; set; }

        public string message { get; set; }

        public List<ErrorModel> errors { get; set; }

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
