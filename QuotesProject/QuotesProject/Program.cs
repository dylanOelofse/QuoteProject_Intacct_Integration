using QuotesProject.Api;
using QuotesProject.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("auth");
builder.Services.AddSingleton<AuthEngine>();

builder.Services.AddHttpClient<QuotesApiEngine>();

// Register services
builder.Services.AddScoped<QuoteApiService>();
builder.Services.AddScoped<QuoteLineApiService>();

builder.Services.AddSingleton<LookupStore>();
builder.Services.AddHostedService<LookupLoaderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Quote/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapGet("/", context =>
{
    context.Response.Redirect("/Quote");
    return Task.CompletedTask;
});

app.MapControllers();

app.Run();
