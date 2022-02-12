using Abp.AspNetCore.Mvc.Controllers;
using Abp.IdentityFramework;
using Microsoft.AspNetCore.Identity;

namespace ManagerCV.Controllers
{
    public abstract class ManagerCVControllerBase: AbpController
    {
        protected ManagerCVControllerBase()
        {
            LocalizationSourceName = SolutionsConsts.LocalizationSourceName;
        }

        protected void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
    }
}
