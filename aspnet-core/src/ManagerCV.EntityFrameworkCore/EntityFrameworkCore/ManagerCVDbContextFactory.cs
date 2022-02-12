using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using ManagerCV.Configuration;
using ManagerCV.Web;

namespace ManagerCV.EntityFrameworkCore
{
    /* This class is needed to run "dotnet ef ..." commands from command line on development. Not used anywhere else */
    public class ManagerCVDbContextFactory : IDesignTimeDbContextFactory<ManagerCVDbContext>
    {
        public ManagerCVDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ManagerCVDbContext>();
            var configuration = AppConfigurations.Get(WebContentDirectoryFinder.CalculateContentRootFolder());

            ManagerCVDbContextConfigurer.Configure(builder, configuration.GetConnectionString(SolutionsConsts.ConnectionStringName));

            return new ManagerCVDbContext(builder.Options);
        }
    }
}
