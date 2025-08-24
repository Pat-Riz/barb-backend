using System.Net;
using Microsoft.AspNetCore.Mvc;
using woodgroveapi.Models;

namespace woodgroveapi.Controllers;

//[Authorize]
[ApiController]
[Route("[controller]")]
public class SignUpStartsTestController : ControllerBase
{
    private readonly ILogger<SignUpStartsTestController> _logger;
    private readonly IConfiguration _configuration;

    public SignUpStartsTestController(ILogger<SignUpStartsTestController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost(Name = "SignUpStartsTest")]
    public AttributeCollectionStartResponse PostAsync([FromBody] AttributeCollectionRequest requestPayload)
    {
        //For Azure App Service with Easy Auth, validate the azp claim value
        // if (!AzureAppServiceClaimsHeader.Authorize(this.Request))
        // {
        //     Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        //     return null;
        // }

        var SimulateDelayInMiliSeconds = 0;
        int.TryParse(_configuration.GetSection("Demos:SimulateDelayMilliseconds").Value, out SimulateDelayInMiliSeconds);

        if (SimulateDelayInMiliSeconds > 0)
            Thread.Sleep(SimulateDelayInMiliSeconds);


        // Message object to return to Microsoft Entra ID
        AttributeCollectionStartResponse r = new AttributeCollectionStartResponse();
        r.data.actions[0].odatatype = AttributeCollectionStartResponse_ActionTypes.SetPrefillValues;
        r.data.actions[0].inputs = new AttributeCollectionStartResponse_Inputs();

        // Return the country and city
        r.data.actions[0].inputs.country = "es";
        // r.data.actions[0].inputs.city = "Madrind";

        // Return a promo code
        Random random = new Random();
        r.data.actions[0].inputs.promoCode = $"Promo code #{random.Next(1236, 9873)}";

        return r;
    }
}
