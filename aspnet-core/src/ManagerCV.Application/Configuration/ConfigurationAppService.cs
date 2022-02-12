using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Runtime.Session;
using ManagerCV.Configuration.Dto;

namespace ManagerCV.Configuration
{
    [AbpAuthorize]
    public class ConfigurationAppService : SolutionsAppServiceBase, IConfigurationAppService
    {
        public async Task ChangeUiTheme(ChangeUiThemeInput input)
        {
            await SettingManager.ChangeSettingForUserAsync(AbpSession.ToUserIdentifier(), AppSettingNames.UiTheme, input.Theme);
        }
    }
}
