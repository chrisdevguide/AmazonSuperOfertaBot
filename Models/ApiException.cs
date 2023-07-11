namespace ElAhorrador.Models
{
    public class ApiException : Exception
    {
        public string ErrorMessage { get; set; }
        public int StatusCode { get; set; }

        public ApiException(string errorMessage)
        {
            ErrorMessage = errorMessage;
            StatusCode = StatusCodes.Status400BadRequest;
        }

        public ApiException()
        {
        }

    }
}
