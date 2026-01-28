using Microsoft.AspNetCore.Mvc;
using SmartCache.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace SmartCache.Api.Controllers
{
    [ApiController]
    [Route("emails")]
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
            email = email.ToLowerInvariant();

            if (!TryGetDomain(email, out var domain))
            {
                return BadRequest(new ApiResponse<object>
                {
                    StatusCode = 400,
                    ErrorMessage = "Invalid email format"
                });
            }

            var domainHash = HashString(domain);
            var grain = _grains.GetGrain<IEmailCheckGrain>(domainHash);
            var result = await grain.CheckAsync(email);

            var response = new ApiResponse<PwnedResultDto>
            {
                StatusCode = result.IsPwned ? 200 : 404,
                Data = result,
                ErrorMessage = result.IsPwned ? null : "Email not found"
            };

            return result.IsPwned ? Ok(response) : NotFound(response);
        }


        [HttpPost("{email}")]
        public async Task<IActionResult> Post(string email)
        {
            email = email.ToLowerInvariant();

            if (!TryGetDomain(email, out var domain))
            {
                return BadRequest(new ApiResponse<object>
                {
                    StatusCode = 400,
                    ErrorMessage = "Invalid email format"
                });
            }

            var domainHash = HashString(domain);

            var grain = _grains.GetGrain<IEmailCheckGrain>(domainHash);
            var added = await grain.AddEmailAsync(email);

            var response = new ApiResponse<string>
            {
                StatusCode = added ? 201 : 409,
                Data = email,
                ErrorMessage = added ? null : "Email already exists"
            };

            return added ? Created(string.Empty, response) : Conflict(response);
        }

        private static bool TryGetDomain(string email, out string domain)
        {
            domain = string.Empty;

            if (string.IsNullOrWhiteSpace(email))
                return false;

            var atIndex = email.IndexOf('@');

            if (atIndex <= 0 || atIndex != email.LastIndexOf('@') || atIndex == email.Length - 1)
                return false;

            var candidateDomain = email.Substring(atIndex + 1);

            if (!candidateDomain.Contains('.'))
                return false;

            if (candidateDomain.StartsWith('.') || candidateDomain.EndsWith('.'))
                return false;

            if (candidateDomain.Contains(".."))
                return false;

            domain = candidateDomain;
            return true;
        }

        private static string HashString(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input.ToLowerInvariant()));
            return Convert.ToHexString(bytes);
        }
    }
}
