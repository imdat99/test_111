using Acm.Api.Middlewares;
using Acm.Api.Models;

namespace Acm.Api.GraphQL
{
    public partial class AcmQueries
    {
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public async Task<IEnumerable<User>> Users()
        {
            var users = new List<User>();
            for (int i = 0; i < 10; i++)
            {
                users.Add(new User
                {
                    Id = i,
                    Name = $"Test User {i}",
                    Email = $"testuser{i}@example.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-i),
                    UpdatedAt = DateTime.UtcNow.AddDays(-i)
                });
            }
            return users.AsQueryable();
        }

        [UseProjection]
        [UseFiltering]
        [UseSorting]
        [IgnoreFields(["Todos", nameof(Models.User.Name), nameof(Models.User.Email), nameof(Models.User.CreatedAt), nameof(Models.User.UpdatedAt)])]
        [GraphQLDescription("Get user by ID")]
        public async Task<User?> User(
            [ID] int id
        )
        {
            // return await acmAdminService.GetUserByIdAsync(id);
            return new User
            {
                Id = id,
                Name = "Test User",
                Email = "123@gmail.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
