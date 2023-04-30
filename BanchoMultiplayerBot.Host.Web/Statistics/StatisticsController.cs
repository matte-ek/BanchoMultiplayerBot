using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace BanchoMultiplayerBot.Host.Web.Statistics
{
    [Route("api/[controller]")]
    [ApiController]
    [FeatureGate("StatisticsController")]
    public class StatisticsController : Controller
    {
        private readonly BotService _bot;
        private readonly StatisticsTrackerService _statisticsTrackerService;

        public StatisticsController(BotService bot, StatisticsTrackerService statisticsTrackerService)
        {
            _bot = bot;
            _statisticsTrackerService = statisticsTrackerService;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            if (_statisticsTrackerService.MinuteSnapshot == null)
            {
                return NoContent();
            }

            return Ok(_statisticsTrackerService.MinuteSnapshot);
        }
    }
}
