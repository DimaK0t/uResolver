using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace uPackageResolver.Login
{
    public class AuthManager
    {
        public async Task<HttpResponseMessage> LoginAsync(string host, string userName, string password, HttpClient client)
        {
            var authParams = new Dictionary<string, string>
                {
                    {"password", password},
                    {"username", userName}
                };

            var authUrl = string.Format("{0}/umbraco/backoffice/UmbracoApi/Authentication/PostLogin", host);
            var response = await client.PostAsync(authUrl, new FormUrlEncodedContent(authParams));
            if (response.StatusCode.Equals(HttpStatusCode.BadRequest))
            {
                throw new HttpRequestException("Cannot login to Umbaco. Recheck credentials.");
            }
            return response;
        }
    }
}