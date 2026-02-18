using FluentValidation;
using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Validators;

public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.IsActive)
            .Must(x => x == true || x == false);
    }
}
