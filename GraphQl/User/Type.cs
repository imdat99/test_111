using Acm.Api.Models;
using System.Data;

namespace Acm.Api.GraphQL
{
    internal static class TodoDataLoader
    {
        [DataLoader(name: nameof(TodoDataLoader.GetTodosByUserId))]
        public static Task<Dictionary<int, Todo[]>> GetTodosByUserId(
            IReadOnlyList<int> ids,
            CancellationToken cancellationToken)
            => Task.FromResult(ids.ToDictionary(
                    id => id,
                    id => new Todo[]
                {
                new Todo
                {
                    Id = 1,
                    Title = "Todo 1 for User " + id,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Todo
                {
                    Id = 2,
                    Title = "Todo 2 for User " + id,
                    IsCompleted = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Todo
                {
                    Id = 3,
                    Title = "Another Todo for User " + id,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                }
                }));
    }

    /// <summary>
    /// DataLoader for Todo items by UserId. (Clas base)
    /// 1 - n relationship
    /// </summary>
    /// <param name="services"></param>
    /// <param name="batchScheduler"></param>
    /// <param name="options"></param>
    public class TodoByUserIdDataLoader_legacy(
        IServiceProvider services,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options) : BatchDataLoader<int, Todo[]>(batchScheduler, options)
    {
        private readonly IServiceProvider _services = services;

        protected override async Task<IReadOnlyDictionary<int, Todo[]>> LoadBatchAsync(
            IReadOnlyList<int> ids,
            CancellationToken cancellationToken)
        {
            // await using var scope = _services.CreateAsyncScope();
            // await using var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();

            // return await context.Products
            //     .Where(t => keys.Contains(t.BrandId))
            //     .GroupBy(t => t.BrandId)
            //     .Select(t => new { t.Key, Items = t.OrderBy(p => p.Name).ToArray() })
            //     .ToDictionaryAsync(t => t.Key, t => t.Items, cancellationToken);
            return ids.ToDictionary(
                id => id,
                id => new Todo[]
            {
                new Todo
                {
                    Id = 1,
                    Title = "Todo 1 for User " + id,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Todo
                {
                    Id = 2,
                    Title = "Todo 2 for User " + id,
                    IsCompleted = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Todo
                {
                    Id = 3,
                    Title = "Another Todo for User " + id,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                }
            });
        }
    }
    [ExtendObjectType(typeof(User))]
    public class UserExtensions
    {
        public async Task<IEnumerable<Todo>> Todos(
        TodoByUserIdDataLoader_legacy todoById,
        // TodoByUserIdDataLoader todoById,
        // Todo todoById,
        getTodosByUserId todoById,
        [Parent] User item, CancellationToken cancellationToken)
        {
            return await todoById.LoadAsync(item.Id, cancellationToken) ?? Array.Empty<Todo>();
        }
    }
}
