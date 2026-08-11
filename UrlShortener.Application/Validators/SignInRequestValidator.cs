using FluentValidation;
using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Validators;

public sealed class SignInRequestValidator : AbstractValidator<SignInRequest>
{
    public SignInRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}
