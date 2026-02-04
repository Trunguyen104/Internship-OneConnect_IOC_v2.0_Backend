using FluentValidation;
using MediatR;

namespace IOCv2.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Nếu có validators cho request này
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                // Chạy tất cả validators
                var validationResults = await Task.WhenAll(
                    _validators.Select(v =>
                        v.ValidateAsync(context, cancellationToken)));
                // Lấy tất cả lỗi
                var failures = validationResults
                    .SelectMany(r => r.Errors)
                    .Where(f => f != null)
                    .ToList();
                // Nếu có lỗi, throw exception
                if (failures.Count != 0)
                    throw new ValidationException(failures);
            }
            // Nếu validation pass, tiếp tục đến Handler
            return await next();
        }
    }
}
