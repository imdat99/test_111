using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Acm.Api.GraphQL
{
    public partial class AcmQueries
    {
        private readonly ILogger<AcmQueries> _logger;
        readonly IHttpContextAccessor _httpContextAccessor;
        //private readonly ApiConfiguration _config;
        public AcmQueries(
            ILogger<AcmQueries> logger,
            IHttpContextAccessor httpContextAccessor
        //,IOptions<ApiConfiguration> config
        )
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            //_config = config.Value;
        }
    }
}