using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.Http.Results;

namespace WebApplication_JWT
{
    public class CustumAuthenticationFilter : AuthorizeAttribute, IAuthenticationFilter
    {
        public new bool AllowMultiple => false;

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = context.Request;
            AuthenticationHeaderValue authorization = request.Headers.Authorization;

            if (authorization == null)
            {
                context.ErrorResult = new AuthenticationFailureResult("Missing Authorization Header", request);
            }
            else if (authorization.Scheme != "Bearer")
            {
                context.ErrorResult = new AuthenticationFailureResult("Invalid Authorization Schema", request);
            }
            else if (string.IsNullOrEmpty(authorization.Parameter))
            {
                context.ErrorResult = new AuthenticationFailureResult("Missing Token", request);
            }
            else
            {
                context.Principal = TokenManager.GetPrincipal(authorization.Parameter);
                if (context.Principal == null)
                {
                    context.ErrorResult = new AuthenticationFailureResult("Invalid Token", request);
                }
            }
        }

        public async Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await context.Result.ExecuteAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Bearer", "realm=localhost"));
            }
            context.Result = new ResponseMessageResult(response);
        }

        public static void Logout(string token) => TokenManager.AddToBlacklist(token);
    }
}