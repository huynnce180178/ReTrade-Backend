using Microsoft.AspNetCore.Mvc;

namespace RetradeBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "Kết nối thành công từ Frontend đến Backend ReTrade!", status = "Connected" });
        }
    }
}
