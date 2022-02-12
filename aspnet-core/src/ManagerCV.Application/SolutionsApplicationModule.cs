using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using ManagerCV.Authorization;

namespace ManagerCV
{
    [DependsOn(
        typeof(SolutionsCoreModule), 
        typeof(AbpAutoMapperModule))]
    public class SolutionsApplicationModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Authorization.Providers.Add<SolutionsAuthorizationProvider>();

            //Adding custom AutoMapper configuration
            Configuration.Modules.AbpAutoMapper().Configurators.Add(CustomDtoMapper.CreateMappings);
        }

        public override void Initialize()
        {
            var thisAssembly = typeof(SolutionsApplicationModule).GetAssembly();

            IocManager.RegisterAssemblyByConvention(thisAssembly);

            Configuration.Modules.AbpAutoMapper().Configurators.Add(
                // Scan the assembly for classes which inherit from AutoMapper.Profile
                cfg => cfg.AddMaps(thisAssembly)
            );
        }
    }
}
