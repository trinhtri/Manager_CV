using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using ManagerCV.Employee.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;
using ManagerCV.IO;
using System.IO;
using ManagerCV.Language.Dto;
using Microsoft.AspNetCore.Mvc;

namespace ManagerCV.Language
{
	public class LanguageAppService : SolutionsAppServiceBase, ILanguageAppService
	{
		private readonly IRepository<Models.CtgLanguage> _ctgLanguageRepository;
		public LanguageAppService(IRepository<Models.CtgLanguage> ctgLanguageRepository)
		{
			_ctgLanguageRepository = ctgLanguageRepository;
		}
		public async Task<long> Create(LanguageDto input)
		{
			input.TenantId = AbpSession.TenantId ?? 1;
			var languge = ObjectMapper.Map<Models.CtgLanguage>(input);
			await _ctgLanguageRepository.InsertAsync(languge);
			await CurrentUnitOfWork.SaveChangesAsync();
			return languge.Id;
		}

		[HttpDelete]
		public async Task Delete(int id)
		{
			await _ctgLanguageRepository.DeleteAsync(id);
		}

		public async Task<PagedResultDto<LanguageDto>> GetAll(GetLangugeInputDto input)
		{
			if (!input.Filter.IsNullOrEmpty())
			{
				input.Filter = Regex.Replace(input.Filter.Trim(), @"\s+", " ");
			}
			var query = _ctgLanguageRepository.GetAll()
					    .WhereIf(!input.Filter.IsNullOrEmpty(),
					     x=> x.NgonNgu.ToUpper().Contains(input.Filter.ToUpper())
					    );
			var tatolCount = await query.CountAsync();
			var result = await query.OrderBy(input.Sorting)
				.PageBy(input)
				.ToListAsync();
			return new PagedResultDto<LanguageDto>(tatolCount, ObjectMapper.Map<List<LanguageDto>>(result));
		}

		public async Task<LanguageDto> GetId(int Id)
		{
			var input = await _ctgLanguageRepository.FirstOrDefaultAsync(Id);
			var result = ObjectMapper.Map<LanguageDto>(input);
			return result;
		}

		[HttpPost]
		public async Task Update(LanguageDto input)
		{
			var languge = await _ctgLanguageRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
			ObjectMapper.Map(input, languge);
		}
	}
}
