using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Host.Web.Extra.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace BanchoMultiplayerBot.Host.Web.Extra;

[Route("[controller]")]
[ApiController]
[FeatureGate("DataEndpoint")]
public class DataController : Controller
{
    [HttpGet("maps/popular")]
    public async Task<ActionResult<IEnumerable<MapDTO>>> GetPopularMaps(int count = 50)
    {
        if (count > 100)
        {
            return BadRequest("Maximum map count is 100");
        }

        using var mapRepository = new MapRepository();

        return Ok(await mapRepository.GetPopularMaps(count));
    }
    
    [HttpGet("maps/hard")]
    public async Task<ActionResult<IEnumerable<MapDTO>>> GetHardestMaps(int count = 50)
    {
        if (count > 100)
        {
            return BadRequest("Maximum map count is 100");
        }

        using var mapRepository = new MapRepository();

        return Ok(await mapRepository.GetHardestMaps(count));
    }
    
    [HttpGet("maps/boring")]
    public async Task<ActionResult<IEnumerable<MapDTO>>> GetBoringMaps(int count = 50)
    {
        if (count > 100)
        {
            return BadRequest("Maximum map count is 100");
        }

        using var mapRepository = new MapRepository();

        return Ok(await mapRepository.GetBoringMap(count));
    }
}