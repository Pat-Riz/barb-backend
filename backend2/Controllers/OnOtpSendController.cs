using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using woodgroveapi.Models;

namespace woodgroveapi.Controllers;


[Authorize(AuthenticationSchemes = "EntraExternalIdCustomAuthToken")]
[ApiController]
[Route("[controller]")]
public class OnOtpSendController : ControllerBase
{
    private readonly ILogger<OnOtpSendController> _logger;
    private readonly IConfiguration _configuration;
    public OnOtpSendController(ILogger<OnOtpSendController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost(Name = "OnOtpSend")]
    public async Task<OnOtpSendResponse> PostAsync([FromBody] OnOtpSendRequest requestPayload)
    {
        try
        {

            //For Azure App Service with Easy Auth, validate the azp claim value
            // if (!AzureAppServiceClaimsHeader.Authorize(this.Request))
            // {
            //     Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            //     return null;
            // }

            // Email sending logic would go here
            // For now, just log the OTP that would be sent
            _logger.LogInformation($"OTP {requestPayload.data.otpContext.onetimecode} would be sent to {requestPayload.data.otpContext.identifier}");

        }
        catch (System.Exception ex)
        {
        }

        return new OnOtpSendResponse();
    }


}