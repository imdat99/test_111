using HotChocolate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Acm.Api.Helpers
{
    public static partial class Utilities
    {

        /// <summary>
        /// Get HttpStatusCode with absolute Exception type: AuthenException, ApiException, HttpRequestException.
        /// Default is default~~null
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static HttpStatusCode? GetStatusCode(this Exception ex)
        {
            // if (ex is AuthenException authenException)
            // {
            //     return (HttpStatusCode)authenException.StatusCode;
            // }
            // else if (ex is ApiException apiException)
            // {
            //     return (HttpStatusCode)apiException.StatusCode;
            // }
            // else 
            if (ex is HttpRequestException httpException)
            {
                return httpException.StatusCode;
            }
            return default;
        }

        /// <summary>
        /// Get IError of GrahpQL.
        /// Spec errors: https://spec.graphql.org/June2018/#example-fce18
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        /// <param name="code"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IError BuildError(string message, string? code = null, Exception? ex = null, HotChocolate.Path? path = null, IReadOnlyDictionary<string, object?>? extensions = null, IReadOnlyList<HotChocolate.Location>? locations = null)
        {
            var ier = ErrorBuilder.New()
                .SetMessage(message)
                .SetCode(code)
                .SetPath(path)
                .SetException(ex);

            if (locations != null)
            {
                foreach (var location in locations)
                    ier.AddLocation(location);
            }

            if (ex?.StackTrace != null)
                ier.SetExtension("stackTrace", ex?.StackTrace);

            if (extensions != null)
            {
                foreach (var extension in extensions)
                {
                    ier.SetExtension(extension.Key, extension.Value);
                }
            }

            return ier.Build();
        }

        /// <summary>
        /// input like: .mediaPlanApprovals.{customerId}.id.
        /// then return "customerId"
        /// </summary>
        public static string? GetNameInBrace(string input)
        {
            var regex = RegexGetNameInBraces();
            var matchs = regex.Matches(input);
            return matchs.Count > 0 ? matchs.First().Groups[1].Value : null;
        }

        public static IEnumerable<string> GetNameInBraces(string input)
        {
            var regex = RegexGetNameInBraces();
            var matchs = regex.Matches(input);
            return matchs.Select(x => x.Groups[1].Value);
        }

        [GeneratedRegex("{(.*?)}")]
        private static partial Regex RegexGetNameInBraces();
    }
}
