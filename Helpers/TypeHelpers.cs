using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace Acm.Api.Helpers
{
    public class TypeHelpers
    {
        const string AllTypeKeyCache = "KeyCaching@Types";
        public static IEnumerable<Type> GetAllTypes()
        {
            // Create a MemoryCache object.
            MemoryCache cache = MemoryCache.Default;

            var obj = cache.Get(AllTypeKeyCache);
            if (obj != null)
                return (IEnumerable<Type>)obj;

            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(p => !p.FullName.StartsWith(nameof(HotChocolate)));
            cache.Add(AllTypeKeyCache, allTypes, new CacheItemPolicy()
            {
                AbsoluteExpiration = DateTime.Now.AddMinutes(10)
            });
            return allTypes;
        }
    }
}
