using Acm.Api.Extensions;
using Acm.Api.GraphQL;
using Acm.Api.Middlewares;
using Acm.Api.Models;
using Confluent.Kafka;

var sourcePoint = ".10.";
var sourceRootPath = ".0.9";
var pathTest = ".0.111.101.10.100.";
var pathIndex = pathTest.IndexOf(sourcePoint);
if (pathIndex > 0)
{
    var storagePath = string.Concat(sourceRootPath, pathTest.AsSpan(pathIndex, pathTest.Length - pathIndex));
    var test = storagePath;
}

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
});

builder.Services.AddControllers();

builder.Services.AddCustomServices(builder.Configuration)
                .AddApplicationServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
Console.WriteLine(app.Environment);
app.UseStaticFiles();

app.UseCors("AllowMyOrigins");
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.Zero // Tắt Keep-Alive
});
app.MapGraphQL();

app.MapControllers();

app.Run();

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddCors(options =>
        {
            options.AddPolicy("AllowMyOrigins",
                builder =>
                {
                    builder
                        .WithOrigins(configuration.GetSection("Acm:Api:AllowOrigins").Value.Split(';'))
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithExposedHeaders("Content-Disposition");
                });
        });

        var kafkaBootstrapServers = configuration.GetSection("Acm:Kafka:Server").Value;
        services.AddSingleton(sp =>
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = kafkaBootstrapServers
            };
            return new ProducerBuilder<Null, string>(producerConfig).Build();
        });
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        //services.AddSignalR();

        var pagingOptions = new HotChocolate.Types.Pagination.PagingOptions
        {
            IncludeTotalCount = true,
            DefaultPageSize = 1000,
            MaxPageSize = 1000
        };
        services.AddSingleton(pagingOptions);
        services.AddGraphQLServer()
                .UseField<FieldMiddleware>()    //validation fields
                                                //.UseRequest<RequestMiddleware>()//check authen then make something after
                // .AddDiagnosticEventListener<DiagnosticMiddleware>()
                .AddQueryType<AcmQueries>()
                .AddMutationType<AcmMutations>()
                .AddCustomTypes()
                .ModifyCostOptions(o => o.EnforceCostLimits = false)    //bỏ qua giới hạn cost
                .ModifyOptions((o) =>
                {
                    //khai báo 2 lệnh sau để có thể sử dụng được các service trong mutation/field request, và ko cần dùng [Service]
                    o.DefaultQueryDependencyInjectionScope = DependencyInjectionScope.Resolver;
                    o.DefaultMutationDependencyInjectionScope = DependencyInjectionScope.Request;
                    o.EnableDefer = true;
                    o.EnableStream = true;
                })
                .ModifyPagingOptions((op) =>
                {
                    op = pagingOptions;
                })
                // .AddFiltering<CustomFilteringConvention>()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddInMemorySubscriptions() //using InMemory type of subscription
                .TryAddTypeInterceptor<RemoveFieldsInterceptor>()
                .AddErrorFilter<CustomErrorFilterExtensions>()    //handle graphql error!    
                .UseField<IgnoreFieldsMiddleware>()//ignore fields in query
                .AddType<User>()    //add custom type
                ;
        return services;
    }
}
