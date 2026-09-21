namespace RezervBooking.Application.Common
{
    public class BusinessRuleException : Exception
    {
        public string Code { get; }

        public int StatusCode { get; }

        public BusinessRuleException(
            string code,
            string message,
            int statusCode = 409)
            : base(message)
        {
            Code = code;
            StatusCode = statusCode;
        }
    }
}
