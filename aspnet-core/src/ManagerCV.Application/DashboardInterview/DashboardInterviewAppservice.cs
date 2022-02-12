using Abp.Domain.Repositories;
using ManagerCV.DashboardInterview.Dto;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagerCV.DashboardInterview
{
    public class DashboardInterviewAppservice : SolutionsAppServiceBase, IDashboardInterviewAppservice
    {
        private readonly IRepository<Models.Employee> _employeeRepository;
        public DashboardInterviewAppservice(IRepository<Models.Employee> employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }
        public async Task<List<GetEmployeeForChartDto>> GetDataForDashboard()
        {
            var emps = await _employeeRepository.GetAll().Where(a => a.NgayPhongVan.HasValue)
                .Select(a => new GetEmployeeForChartDto
                {
                    Id = a.Id,
                    Title = a.CtyNhan,
                    Start = a.NgayPhongVan ?? DateTime.Now
                })
                .ToListAsync();
            return emps;
        }
    }
}
