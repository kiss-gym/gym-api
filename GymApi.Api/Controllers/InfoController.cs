using GymApi.Infrastructure.Environment;
using Microsoft.AspNetCore.Mvc;

namespace GymApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InfoController(VersionProvider versionProvider) : ControllerBase
{
    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        return Ok(new
        {
            releaseVersion = versionProvider.CodeVersion,
            lastCommitDate = versionProvider.LastCommitDate,
            runtime = versionProvider.Runtime
        });
    }
}
