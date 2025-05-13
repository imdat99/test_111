using System.Threading.Tasks;
using Acm.Api.Models;

namespace Acm.Api.GraphQL
{
    public partial class AcmMutations
    {
        public async Task<bool> UpdateUser(
            [ID] int id,
            User input
        )
        {
            return true;
        }
    }
}
