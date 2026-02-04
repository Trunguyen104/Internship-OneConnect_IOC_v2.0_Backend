using AutoMapper;
using System.Reflection;

namespace IOCv2.Application.Extensions.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            ApplyMappingsFromAssembly(Assembly.GetExecutingAssembly());
        }

        private void ApplyMappingsFromAssembly(Assembly assembly)
        {
            // Tìm tất cả các Type triển khai interface IMapFrom<>
            var types = assembly.GetExportedTypes()
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>)))
                .ToList();

            foreach (var type in types)
            {
                var methodInfo = type.GetMethod("Mapping", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                if (methodInfo != null)
                {
                    // If it's overridden, we need an instance to call it
                    var instance = Activator.CreateInstance(type);
                    methodInfo.Invoke(instance, new object[] { this });
                }
                else
                {
                    // If not overridden, use the default mapping logic directly
                    var interfaceType = type.GetInterfaces().First(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>));

                    var sourceType = interfaceType.GetGenericArguments()[0];

                    // This replicates the default behavior: profile.CreateMap(typeof(T), GetType()).ReverseMap();
                    CreateMap(sourceType, type).ReverseMap();
                }
            }
        }
    }
}
