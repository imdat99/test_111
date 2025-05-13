using Acm.Api.Models;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Acm.Api.GraphQL
{
    [ExtendObjectType(typeof(User))]
    public class UserExtensions
    {
        public async Task<IEnumerable<Todo>> Todos( [Parent] User item)
        {
            return new List<Todo>
            {
                new Todo
                {
                    Id = 1,
                    Title = "Todo 1 for User " + item.Id,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Todo
                {
                    Id = 2,
                    Title = "Todo 2 for User " + item.Id,
                    IsCompleted = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Todo
                {
                    Id = 3,
                    Title = "Another Todo for User " + item.Id,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                }
            };
        }
    }
}
