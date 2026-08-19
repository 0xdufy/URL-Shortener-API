using Microsoft.AspNetCore.Identity;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Identity;

namespace UrlShortener.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public UserAccountStatus Status { get; set; } = UserAccountStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<RefreshSession> RefreshSessions { get; } = [];
    public ICollection<ShortUrl> ShortUrls { get; } = [];
    public ICollection<ApiKey> ApiKeys { get; } = [];
}
