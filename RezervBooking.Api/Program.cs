using Microsoft.EntityFrameworkCore;
using RezervBooking.Api.Middleware;
using RezervBooking.Application.Abstractions;
using RezervBooking.Application.Services;
using RezervBooking.Infrastructure.Persistence;
using RezervBooking.Infrastructure.Redis;
using RezervBooking.Infrastructure.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var mysqlConnection = builder.Configuration.GetConnectionString("MySql")!;

builder.Services.AddDbContext<BookingDbContext>(options => options.UseMySql(mysqlConnection, ServerVersion.AutoDetect(mysqlConnection)));

builder.Services.AddScoped<IBookingDbContext>(provider => provider.GetRequiredService<BookingDbContext>());

var redisConnection = builder.Configuration["Redis:ConnectionString"]!;

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "Rezerv:";
    });

builder.Services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();

builder.Services.AddScoped<ICacheService, RedisCacheService>();

builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddScoped<PackageService>();

builder.Services.AddScoped<TimetableService>();

builder.Services.AddScoped<BookingService>();

builder.Services.AddScoped<WaitlistService>();

var app = builder.Build();

//using (var scope = app.Services.CreateScope()) (for internal testing)
//{
//    var redis = scope.ServiceProvider
//        .GetRequiredService<IConnectionMultiplexer>();

//    var db = redis.GetDatabase();

//    await db.StringSetAsync(
//        "rezerv:test",
//        "working",
//        TimeSpan.FromMinutes(1));

//    var result =
//        await db.StringGetAsync("rezerv:test");

//    Console.WriteLine($"Redis Test: {result}");
//}

app.UseMiddleware<ApiExceptionMiddleware>();

app.UseSwagger();

app.UseSwaggerUI();

app.MapControllers();

app.Run();