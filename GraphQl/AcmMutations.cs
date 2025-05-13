using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acm.Api.GraphQL
{
    /// <summary>
    /// example mutation:
    /// https://github.com/ChilliCream/graphql-workshop/blob/ccf79744dca2822ba1f98e54e2a116cb0e1eb9e6/code/complete/GraphQL/Speakers/SpeakerMutations.cs
    /// </summary>
    public partial class AcmMutations
    {
        private readonly ILogger<AcmMutations> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AcmMutations(ILogger<AcmMutations> logger,
                            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

    }
}