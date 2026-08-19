using FluentValidation;
using UrlShortener.Application.ApiKeys;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Application.Validators;

public sealed class CreateApiKeyRequestValidator : AbstractValidator<CreateApiKeyRequest>
{
    public CreateApiKeyRequestValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(64)
            .Must(name => name == name.Trim())
            .WithMessage("Name cannot begin or end with whitespace.")
            .Matches("^[A-Za-z0-9][A-Za-z0-9 ._-]*$")
            .WithMessage("Name must begin with a letter or digit and contain only letters, digits, spaces, '.', '_', or '-'.");

        RuleFor(request => request.Scopes)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(scopes => scopes.Count is >= 1 and <= 4)
            .WithMessage("Select between one and four scopes.")
            .Must(scopes => scopes.Distinct(StringComparer.Ordinal).Count() == scopes.Count)
            .WithMessage("Scopes cannot contain duplicates.");

        RuleForEach(request => request.Scopes)
            .Must(scope => ApiKeyScopeNames.TryParse(scope, out _))
            .WithMessage("Scope is not supported.");

        RuleFor(request => request.ExpiresAtUtc)
            .Must(value => !value.HasValue || value.Value.Kind == DateTimeKind.Utc)
            .WithMessage("Expiration must include the UTC 'Z' designator.")
            .Must(value => !value.HasValue || value.Value > dateTimeProvider.UtcNow)
            .WithMessage("Expiration must be in the future.");
    }
}
