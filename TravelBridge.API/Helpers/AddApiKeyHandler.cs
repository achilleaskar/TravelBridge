namespace TravelBridge.API.Helpers
{
    public class AddApiKeyHandler : DelegatingHandler
    {
        //private readonly string _apiKey;

    //    public AddApiKeyHandler(string apiKey)
    //    {
    //        _apiKey = apiKey;
    //    }

    //    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    //    {
    //        // Append the API key as a query parameter
    //        var uriBuilder = new UriBuilder(request.RequestUri!);
    //        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
    //        query["apiKey"] = _apiKey;
    //        uriBuilder.Query = query.ToString();
    //        request.RequestUri = uriBuilder.Uri;

    //        return base.SendAsync(request, cancellationToken);
    //    }
    }
}
