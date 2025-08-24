using Microsoft.Extensions.Caching.Memory;
using woodgroveapi.Models;
using System.Text.Json;
using System.Net;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Helper method to validate JWT token from Authorization header
static IResult? ValidateJwtAuth(HttpRequest request, IConfiguration configuration, ILogger logger)
{
    // Check if JWT authentication is enabled via environment variable (default: true)
    bool enableJwtAuth = configuration.GetValue<bool>("EnableJwtAuth", true);
    
    // Get expected values from configuration
    var expectedAud = configuration.GetValue<string>("ExpectedAudience");
    var expectedAzp = configuration.GetValue<string>("ExpectedAzp");
    
    // Log configuration values
    logger.LogInformation("JWT Auth Configuration - EnableJwtAuth: {EnableJwtAuth}, ExpectedAudience: {ExpectedAudience}, ExpectedAzp: {ExpectedAzp}", 
        enableJwtAuth, expectedAud, expectedAzp);
    
    if (!enableJwtAuth)
    {
        logger.LogInformation("JWT authentication is disabled, skipping validation");
        return null; // Skip validation if disabled
    }
    
    // Get Authorization header
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        logger.LogWarning("JWT authentication failed: Missing Authorization header");
        return Results.Unauthorized();
    }
    
    var token = authHeader.FirstOrDefault()?.Replace("Bearer ", "");
    if (string.IsNullOrEmpty(token))
    {
        logger.LogWarning("JWT authentication failed: Missing or invalid Bearer token");
        return Results.Unauthorized();
    }
    
    try
    {
        // Decode JWT without validation (we only need to read claims)
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        
        logger.LogInformation("JWT token decoded successfully");
        
        // Validate 'aud' claim
        if (!string.IsNullOrEmpty(expectedAud))
        {
            var audClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "aud")?.Value;
            logger.LogInformation("JWT aud claim validation - Expected: {ExpectedAud}, Actual: {ActualAud}", expectedAud, audClaim);
            
            if (audClaim != expectedAud)
            {
                logger.LogWarning("JWT authentication failed: aud claim mismatch - Expected: {ExpectedAud}, Actual: {ActualAud}", expectedAud, audClaim);
                return Results.Unauthorized();
            }
        }
        else
        {
            logger.LogInformation("JWT aud claim validation skipped (no expected value configured)");
        }
        
        // Validate 'azp' claim  
        if (!string.IsNullOrEmpty(expectedAzp))
        {
            var azpClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;
            logger.LogInformation("JWT azp claim validation - Expected: {ExpectedAzp}, Actual: {ActualAzp}", expectedAzp, azpClaim);
            
            if (azpClaim != expectedAzp)
            {
                logger.LogWarning("JWT authentication failed: azp claim mismatch - Expected: {ExpectedAzp}, Actual: {ActualAzp}", expectedAzp, azpClaim);
                return Results.Unauthorized();
            }
        }
        else
        {
            logger.LogInformation("JWT azp claim validation skipped (no expected value configured)");
        }
        
        logger.LogInformation("JWT authentication successful");
        return null; // Authorized
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "JWT authentication failed: Error decoding token");
        return Results.Unauthorized();
    }
}

// Helper method to validate Azure App Service authentication (legacy)
static IResult? ValidateAzureAuth(HttpRequest request, IConfiguration configuration)
{
    // Check if Azure authentication is enabled via environment variable (default: false)
    bool enableAzureAuth = configuration.GetValue<bool>("EnableAzureAuth", false);
    
    if (!enableAzureAuth)
    {
        return null; // Skip validation if disabled
    }
    
    if (!AzureAppServiceClaimsHeader.Authorize(request))
    {
        return Results.Unauthorized();
    }
    
    return null; // Authorized
}

// Minimal API endpoint for attribute collection start
// app.MapPost("/api/attributecollectionstart", (AttributeCollectionRequest requestPayload, HttpRequest request, ILogger<Program> logger, IConfiguration configuration) =>
// {
//     // Validate Azure App Service authentication if enabled
//     var authResult = ValidateAzureAuth(request, configuration);
//     if (authResult != null) return authResult;
//     
//     // Log the incoming request
//     logger.LogInformation("AttributeCollectionStart API called with request: {RequestJson}", JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { WriteIndented = true }));
//     // Message object to return to Microsoft Entra ID
//     AttributeCollectionStartResponse r = new AttributeCollectionStartResponse();
//     r.data.actions[0].odatatype = AttributeCollectionStartResponse_ActionTypes.SetPrefillValues;
//     r.data.actions[0].inputs = new AttributeCollectionStartResponse_Inputs();
//
//     // Return the country and city
//     r.data.actions[0].inputs.country = "es";
//     // r.data.actions[0].inputs.city = "Madrind";
//
//     // Return a promo code
//     Random random = new Random();
//     r.data.actions[0].inputs.promoCode = $"#{random.Next(1236, 9873)}";
//
//     await context.Response.WriteAsJsonAsync(r);
// })
// .WithName("AttributeCollectionStart");

// Minimal API endpoint for attribute collection submit
app.MapPost("/api/attributecollectionsubmit", async (AttributeCollectionRequest requestPayload, HttpRequest request, ILogger<Program> logger, IConfiguration configuration) =>
{
    // Validate JWT authentication
    var authResult = ValidateJwtAuth(request, configuration, logger);
    if (authResult != null) return authResult;
    
    // Log the incoming request
    logger.LogInformation("AttributeCollectionSubmit API called with request: {RequestJson}", JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { WriteIndented = true }));
        
    var res = new AttributeCollectionSubmitResponse();
    res.data.actions[0].odatatype = AttributeCollectionSubmitResponse_ActionTypes.ContinueWithDefaultBehavior;
    
    return Results.Ok(res);
})
.WithName("AttributeCollectionSubmit");

// Minimal API endpoint for OTP send
app.MapPost("/api/otpsend", async (OnOtpSendRequest requestPayload, HttpRequest request, ILogger<Program> logger, IConfiguration configuration) =>
{
    try
    {
        // Validate JWT authentication
        var authResult = ValidateJwtAuth(request, configuration, logger);
        if (authResult != null) return authResult;
        
        // Log the incoming request
        logger.LogInformation("OtpSend API called with request: {RequestJson}", JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { WriteIndented = true }));
        // Email sending logic would go here
        // For now, just simulate email sending
        Console.WriteLine($"OTP {requestPayload.data.otpContext.onetimecode} would be sent to {requestPayload.data.otpContext.identifier}");
    }
    catch (System.Exception ex)
    {
        // Log exception if needed
    }

    return Results.Ok(new OnOtpSendResponse());
})
.WithName("OtpSend");

// Minimal API endpoint for token issuance start
app.MapPost("/api/tokenissuancestart", (TokenIssuanceStartRequest requestPayload, HttpRequest request, ILogger<Program> logger, IConfiguration configuration) =>
{
    // Validate JWT authentication
    var authResult = ValidateJwtAuth(request, configuration, logger);
    if (authResult != null) return authResult;
    
    // Log the incoming request
    logger.LogInformation("TokenIssuanceStart API called with request: {RequestJson}", JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { WriteIndented = true }));
    
    // Read the correlation ID from the Microsoft Entra ID request    
    string correlationId = requestPayload.data.authenticationContext.correlationId;

    // Claims to return to Microsoft Entra ID
    TokenIssuanceStartResponse r = new TokenIssuanceStartResponse();
    // r.data.actions[0].claims.CorrelationId = correlationId;
    // r.data.actions[0].claims.ApiVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!.ToString();
    //
    // // Loyalty program data
    // Random random = new Random();
    // string[] tiers = { "Silver", "Gold", "Platinum", "Diamond" };
    // r.data.actions[0].claims.LoyaltyNumber = random.Next(123467, 999989).ToString();
    // r.data.actions[0].claims.LoyaltySince = DateTime.Now.AddDays((-1) * random.Next(30, 365)).ToString("dd MMMM yyyy");
    // r.data.actions[0].claims.LoyaltyTier = tiers[random.Next(0, tiers.Length)];
    //
    // // Custom roles
    // r.data.actions[0].claims.CustomRoles.Add("Writer");
    // r.data.actions[0].claims.CustomRoles.Add("Editor");

    return Results.Ok(r);
})
.WithName("TokenIssuanceStart");

app.Run();