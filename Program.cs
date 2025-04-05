using Firebase.Auth;
using Firebase.Auth.Providers;
using FutureTech_Academy.Interfaces;
using FutureTech_Academy.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Cosmos DB
builder.Services.AddSingleton<CosmosClient>(sp => new CosmosClient(
    builder.Configuration["CosmosDb:Endpoint"],
    builder.Configuration["CosmosDb:Key"]
));

// Register Student Service
builder.Services.AddScoped<IStudentService, CosmosDbService>();

// Add Azure Blob Storage client
builder.Services.AddSingleton(x => new BlobServiceClient(
    builder.Configuration["AzureBlobStorage:ConnectionString"]
));

builder.Services.AddScoped<BlobStorageService>();

builder.Services.AddSingleton(new FirebaseAuthClient(new FirebaseAuthConfig
{
    ApiKey = builder.Configuration.GetSection("Firebase:ApiKey").Value,
    AuthDomain = builder.Configuration.GetSection("Firebase:AuthDomain").Value,
    Providers =
    [
        // Add authentication type defined on firebase console
        new EmailProvider()
    ]
}));

// Register Authentication Service
builder.Services.AddSingleton<IAuthService, AuthService>();

// Add an authentication schema
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();