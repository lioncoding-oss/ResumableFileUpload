using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumableFileUpload.Server.Services;
using ResumableFileUpload.Server.Startup;
using tusdotnet;
using static ResumableFileUpload.Server.Startup.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();

builder.ConfigureKestrelFileUpload();
builder.Services.AddTusServices(builder.Configuration);

builder.Services.AddDatabase();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.ConfigureTusCors();

var app = builder.Build();

app.UseCors(TusCorsPolicyName);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add tus endpoint
app.MapTus("/upload", UploadService.TusConfigurationFactory)
    .WithOpenApi()
    .RequireAuthorization();

app.MapGroup("/users").MapIdentityApi<IdentityUser>()
    .WithTags("users");

app.UseAuthorization();
app.Run();