using FluentValidation;
using UrlShortener.Application.CustomDomains;
using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Validators;

public sealed class RegisterCustomDomainRequestValidator : AbstractValidator<RegisterCustomDomainRequest>
{
    public RegisterCustomDomainRequestValidator(CustomDomainPolicySettings policy)
    {
        RuleFor(request => request.Host)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(254)
            .Must(host => CustomDomainHostNormalizer.TryNormalize(host, out _, out _))
            .WithMessage("Host must be a valid DNS hostname without a scheme, path, wildcard, or port.");

        RuleFor(request => request.Host)
            .Must(host =>
                !CustomDomainHostNormalizer.TryNormalize(host, out var normalizedHost, out _) ||
                $"{policy.VerificationRecordLabel}.{normalizedHost}".Length <= 253)
            .WithMessage("Host is too long to contain the required DNS verification label.");
    }
}
