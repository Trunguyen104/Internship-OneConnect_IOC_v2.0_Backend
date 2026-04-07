using FluentValidation;
using FluentValidation.AspNetCore;
using IOCv2.Application.Common.Behaviors;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace IOCv2.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Tự động quét và đăng ký tất cả các Profile của AutoMapper trong Assembly này
            services.AddAutoMapper(config =>
            {
                config.AddProfile<MappingProfile>();
            });



            // Đăng ký MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // Đăng ký FluentValidation
            // KHÔNG DÙNG AutoValidation vì nó sẽ validate ModelBinding trước khi Controller kịp gán Route Parameter.
            // Hệ thống ĐÃ CÓ ValidationBehavior của MediatR để lo việc này.
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);

            // Đăng ký Application Services
            services.AddScoped<IUserServices, UserServices>();
            services.AddScoped<IMessageService, MessageService>();

            return services;
        }
    }
}
