using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using ManagerCV.ConfigToSendMail.Exporting;
using ManagerCV.ConfigToSendMail.EmailSender;
using ManagerCV.ConfigToSendMail.Dto;
using ManagerCV.Models;

namespace ManagerCV.ConfigToSendMail
{
    public class ConfigToSendMailAppService : SolutionsAppServiceBase, IConfigToSendMailAppService
    {
        private readonly IRepository<SysConfigToSendMail> _sysConfigToSendMailRepository;
        private readonly ConfigToSendMailListExcelExporter _configToSendMailListExcelExporter;
        private readonly ConfigEmailSender _configEmailSender;
        public ConfigToSendMailAppService(
            IRepository<Models.SysConfigToSendMail> sysConfigToSendMailRepository,
            ConfigToSendMailListExcelExporter configToSendMailListExcelExporter,
            ConfigEmailSender configEmailSender
            )
        {
            _sysConfigToSendMailRepository = sysConfigToSendMailRepository;
            _configToSendMailListExcelExporter = configToSendMailListExcelExporter;
            _configEmailSender = configEmailSender;
        }
        public async Task<long> Create(CreateConfigToSendMailDto input)
        {
            input.TenantId = AbpSession.TenantId;
            var configToSendMail = ObjectMapper.Map<Models.SysConfigToSendMail>(input);
            await _sysConfigToSendMailRepository.InsertAsync(configToSendMail);
            await CurrentUnitOfWork.SaveChangesAsync();
            return configToSendMail.Id;
        }

        public async Task Delete(int id)
        {
            await _sysConfigToSendMailRepository.DeleteAsync(id);
        }

        public async Task<PagedResultDto<GetConfigToSendMailListDto>> GetAll(GetConfigToSendMailInput input)
        {
            if (!input.Filter.IsNullOrEmpty())
            {
                input.Filter = Regex.Replace(input.Filter.Trim(), @"\s+", " ");
            }
            var query = _sysConfigToSendMailRepository.GetAll()
                      .WhereIf(!input.Filter.IsNullOrEmpty(), x => x.Title.Contains(input.Filter));
            var provinces = await query
              .OrderBy(input.Sorting)
              .PageBy(input)
              .ToListAsync();
            var tatolCount = await query.CountAsync();
            return new PagedResultDto<GetConfigToSendMailListDto>(tatolCount, ObjectMapper.Map<List<GetConfigToSendMailListDto>>(provinces));
        }

        public async Task<FileDto> GetConfigToSendMailToExcel(GetConfigToSendMailInput input)
        {
            var query = await GetAll(input);
            var dto = query.Items.ToList();
            return _configToSendMailListExcelExporter.ExportToExcel(dto);
        }

        public async Task<CreateConfigToSendMailDto> GetId(int id)
        {
            var Config = await _sysConfigToSendMailRepository.FirstOrDefaultAsync(id);
            var dto = ObjectMapper.Map<CreateConfigToSendMailDto>(Config);
            return dto;
        }

        public async Task Update(CreateConfigToSendMailDto input)
        {
            var dto = await _sysConfigToSendMailRepository.FirstOrDefaultAsync(input.Id);
            ObjectMapper.Map(input, dto);
        }
        public async Task SendMail(CreateConfigToSendMailDto input)
        {
            await _configEmailSender.Contact(input);
        }

        public async Task StateTransitions(CreateConfigToSendMailDto input)
        {
         
             var result = await _sysConfigToSendMailRepository.FirstOrDefaultAsync(x => x.IsActive == true);
            if(result.Id != input.Id)
            {
                result.IsActive = false;
            }
        }

        public async Task<CreateConfigToSendMailDto> GetEmailActive()
        {
            var Config = await _sysConfigToSendMailRepository.FirstOrDefaultAsync(x=>x.IsActive == true);
            var dto = ObjectMapper.Map<CreateConfigToSendMailDto>(Config);
            return dto;
        }

    }
 }
