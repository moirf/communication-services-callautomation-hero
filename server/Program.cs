using Azure.Communication.Identity;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using CallAutomationHero.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddTransient<IncomingCallHandler>();
builder.Services.AddTransient<RecordingHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCorsPolicy",
        policy =>
        {
            policy.WithOrigins("http://localhost", "http://localhost:5000")
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});

var app = builder.Build();

ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
Logger.SetLoggerInstance(logger);

app.MapGet("/api/status", () =>
{
    Console.WriteLine($"API is running...");
    return Results.Ok("Call Automation API Server is running!!");
}).AddEndpointFilterFactory(EndpointFilter.RequestLogger);

app.MapPost("/api/user/token", async () =>
{
    try
    {
        var communicationIdentityClient = new CommunicationIdentityClient(builder.Configuration["ConnectionString"]);
        List<CommunicationTokenScope> scopes = new List<CommunicationTokenScope> { CommunicationTokenScope.VoIP };
        var response = await communicationIdentityClient.CreateUserAndTokenAsync(scopes);
        return Results.Ok(response.Value);
    }
    catch (Exception ex)
    {
        logger.LogError("Failed to create user and token" + ex.Message);
        return Results.NotFound("Failed to create user and token" + ex.Message);
    }
})
.RequireCors("ClientCorsPolicy")
.Produces(StatusCodes.Status200OK);

app.MapPost("/api/incomingCall", (
    [FromBody] EventGridEvent[] events,
    IncomingCallHandler handler) =>
{
    return handler.HandleIncomingCall(events);
})
.RequireCors("ClientCorsPolicy")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

app.MapPost("/api/calls/{contextId}", (
    [FromBody] CloudEvent[] events,
    [FromRoute] string contextId,
    [FromQuery] string callerId,
    IncomingCallHandler handler
) =>
{
    return handler.HandleCallback(events, callerId);

})
.AddEndpointFilterFactory(EndpointFilter.RequestLogger)
.Produces(StatusCodes.Status200OK);

app.MapPost("api/recording", ([FromBody] EventGridEvent[] eventGridEvents, RecordingHandler handler) =>
{
    return handler.HandleRecording(eventGridEvents);
}).AddEndpointFilterFactory(EndpointFilter.RequestLogger);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
           Path.Combine(builder.Environment.ContentRootPath, "audio")),
    RequestPath = "/audio"
});


app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
