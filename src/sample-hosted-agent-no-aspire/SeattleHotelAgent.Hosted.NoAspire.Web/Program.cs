using SeattleHotelAgent.Hosted.NoAspire.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var agentEndpoint = builder.Configuration["AgentEndpoint"] ?? "http://localhost:8088";
builder.Services.AddHttpClient<AgentService>(client =>
{
    client.BaseAddress = new Uri(agentEndpoint);
    client.Timeout = TimeSpan.FromSeconds(120);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
