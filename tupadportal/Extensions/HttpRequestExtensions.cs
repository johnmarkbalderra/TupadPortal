using Microsoft.AspNetCore.Http;

namespace tupadportal.Extensions
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Determines whether the current request is an AJAX request.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>True if the request is an AJAX request; otherwise, false.</returns>
        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
