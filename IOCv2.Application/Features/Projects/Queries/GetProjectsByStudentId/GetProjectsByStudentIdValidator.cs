using FluentValidation;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectsByStudentId
{
    public class GetProjectsByStudentIdValidator : AbstractValidator<GetProjectsByStudentIdQuery>
    {
        private readonly IMessageService _messageService;
        public GetProjectsByStudentIdValidator(IMessageService messageService)
        {
            _messageService = messageService;
            // PageNumber must be greater than 0
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage(_ => _messageService.GetMessage("PageNumber.MinValue"));

            // PageSize must be between 1 and 100
            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage(_ => _messageService.GetMessage("PageSize.MinValue"))
                .LessThanOrEqualTo(100).WithMessage(_ => _messageService.GetMessage("PageSize.MaxValue"));

            // SearchTerm maximum length
            RuleFor(x => x.SearchTerm)
                .MaximumLength(200).WithMessage(_ => _messageService.GetMessage("SearchTerm.MaxLength"))
                .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));

            // SortColumn must be valid if provided
            RuleFor(x => x.SortColumn)
                .Must(BeAValidSortColumn).WithMessage(_ => _messageService.GetMessage("SortColumn.AllowedValues"))
                .When(x => !string.IsNullOrWhiteSpace(x.SortColumn));

            // SortOrder must be 'asc' or 'desc' if provided
            RuleFor(x => x.SortOrder)
                .Must(order => order?.ToLower() == "asc" || order?.ToLower() == "desc")
                .WithMessage(_ => _messageService.GetMessage("SortOrder.AllowedValues"))
                .When(x => !string.IsNullOrWhiteSpace(x.SortOrder));
        }

        private bool BeAValidSortColumn(string? sortColumn)
        {
            if (string.IsNullOrWhiteSpace(sortColumn))
                return true;

            var validColumns = new[] { "projectname", "startdate", "enddate", "status", "createdat" };
            return validColumns.Contains(sortColumn.ToLower());
        }

    }
}
