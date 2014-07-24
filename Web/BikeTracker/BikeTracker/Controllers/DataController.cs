namespace BikeTracker.Controllers
{
    using System.Threading.Tasks;
    using System.Web.Http;

    using Facebook;

    using WebApplication2.Controllers;

    public class DataController : ApiController
    {

        [HttpGet]
        [Route("api/data/authorize/{token}")]
        public async Task<ApplicationUser> Authorize(string token)
        {
            var fbClient = new FacebookClient(token);
            dynamic me = await fbClient.GetTaskAsync("me");

            return null;
        }
    }
}
