using UrlShortener.Api.Extensions;
using UrlShortener.Api.Middlewares;
using UrlShortener.Api.OpenApi;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();
builder.Services.AddApiServices(builder.Configuration, builder.Environment);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token returned by the authentication endpoints."
    });
    options.OperationFilter<AuthorizeOperationFilter>();
    options.OperationFilter<OwnedShortUrlListOperationFilter>();
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("TrustedWebClient");
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
