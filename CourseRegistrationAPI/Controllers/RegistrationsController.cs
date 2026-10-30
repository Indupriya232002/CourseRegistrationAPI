using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using System.Threading.Tasks;

namespace CourseRegistrationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationsController : ControllerBase
    {
        private readonly RegistrationService _registrationService;

        public RegistrationsController(RegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        // DTO for incoming requests
        public class RegistrationRequest
        {
            public required string LearnerId { get; set; }
            public int CourseId { get; set; }
        }

        [HttpPost("enroll")]
        public async Task<IActionResult> Enroll([FromBody] RegistrationRequest request)
        {
            var result = await _registrationService.EnrollAsync(request.LearnerId, request.CourseId);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        [HttpPost("drop")]
        public async Task<IActionResult> Drop([FromBody] RegistrationRequest request)
        {
            var result = await _registrationService.DropAsync(request.LearnerId, request.CourseId);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }
    }
}