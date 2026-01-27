using Microsoft.AspNetCore.Mvc;
using SmartCache.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace SmartCache.Api.Controllers
{
    [ApiController]
    [Route("api/pwned")]
    public class PwnedController : ControllerBase
    {
        private readonly IGrainFactory _grains;

        public PwnedController(IGrainFactory grains)
        {
            _grains = grains;
        }

        [HttpGet("{email}")]
        public async Task<IActionResult> Get(string email)
        {
            var hash = HashEmail(email);
            var grain = _grains.GetGrain<IEmailCheckGrain>(hash);
            var result = await grain.CheckAsync(email);

            return result.IsPwned ? Ok(result) : NotFound(result);
        }

        [HttpPost("{email}")]
        public async Task<IActionResult> Post(string email)
        {
            var hash = HashEmail(email);
            var grain = _grains.GetGrain<IEmailCheckGrain>(hash);
            var added = await grain.AddEmailAsync(email);

            return added ? Created("", email) : Conflict();
        }

        private static string HashEmail(string email)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(email.ToLowerInvariant()));
            return Convert.ToHexString(bytes);
        }
    }
}
