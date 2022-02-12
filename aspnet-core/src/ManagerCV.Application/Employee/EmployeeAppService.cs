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
using ManagerCV.Employee.Exporting;
using ManagerCV.Models;
using Microsoft.AspNetCore.Mvc;

namespace ManagerCV.Employee
{
    public class EmployeeAppService : SolutionsAppServiceBase, IEmployeeAppService
    {
        private readonly IRepository<Models.Employee> _employeeRepository;
        private readonly IRepository<Models.EmployeeLanguage> _employeeLanguageRepository;
        private readonly IRepository<Models.CtgLanguage> _ctLganguageRepository;
        private readonly IAppFolders _appFolders;
        private readonly EmployeeListExcelExporter _employeeListExcelExporter;
        public EmployeeAppService(IRepository<Models.Employee> employeeRepository,
            IAppFolders appFolders, 
            EmployeeListExcelExporter employeeListExcelExporter,
            IRepository<Models.CtgLanguage> ctgLanguageRepository,
            IRepository<Models.EmployeeLanguage> employeeLanguageRepository)
        {
            _employeeRepository = employeeRepository;
            _appFolders = appFolders;
            _employeeListExcelExporter = employeeListExcelExporter;
            _employeeLanguageRepository = employeeLanguageRepository;
            _ctLganguageRepository = ctgLanguageRepository;
        }
        [HttpPost]
        public async Task<long> Create(CreateEmployeeDto input)
        {
            input.TenantId = AbpSession.TenantId;
            var emp = ObjectMapper.Map<Models.Employee>(input);
            // thao tac voi file 
            // xoa file cu di
            if (emp.CVName != null)
            {
                AppFileHelper.DeleteFilesInFolderIfExists(_appFolders.AttachmentsFolder, input.CVName);
                var sourceFile = Path.Combine(_appFolders.TempFileUploadFolder, input.CVName);
                var destFile = Path.Combine(_appFolders.AttachmentsFolder, input.CVName);
                System.IO.File.Move(sourceFile, destFile);
                var filePath = Path.Combine(_appFolders.AttachmentsFolder, input.CVName);
                emp.CVUrl = filePath;
            }
            var employeeId = await _employeeRepository.InsertAndGetIdAsync(emp);
            // thêm mới dữ liệu trong bảng employeelanguage
            if (input.Languages.Count > 0)
            {
                foreach (var item in input.Languages)
                {
                    var el = new EmployeeLanguage();
                    el.TenantId = AbpSession.TenantId ?? 1;
                    el.CtgLanguage_Id = item;
                    el.Employee_Id = employeeId;
                    await _employeeLanguageRepository.InsertAsync(el);
                }
            }
            await CurrentUnitOfWork.SaveChangesAsync();
            return employeeId;
        }

        public async Task CVGuiDi(CVGuiDi input)
        {
            var dto = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == input.EmployeeId);
            dto.CtyNhan = input.CtyNhan;
            dto.NgayHoTro = input.NgayHoTro;
        }

        [HttpDelete]
        public async Task Delete(int id)
        {
            var cv = _employeeRepository.Get(id);
            //Delete old document file
            AppFileHelper.DeleteFilesInFolderIfExists(_appFolders.AttachmentsFolder, cv.CVName);

            await _employeeRepository.DeleteAsync(id);
        }

        public async Task<PagedResultDto<EmployeeListDto>> GetAll(EmployeeInputDto input)
        {
            if (!input.Filter.IsNullOrEmpty())
            {
                input.Filter = Regex.Replace(input.Filter.Trim(), @"\s+", " ");
            }
            var query = _employeeRepository.GetAll()
                       .Include(x => x.EmployeeLanguages)
                       .ThenInclude(l => l.CtgLanguage_)
                       .Where(x => x.TrangThai == false)
                       .WhereIf(input.BangCap.Count > 0, x=> input.BangCap.Any(a=>a == x.BangCap))
                       .WhereIf(input.NgonNgu.Count > 0 , x=> x.EmployeeLanguages.Any(x=> input.NgonNgu.Any(a=>a == x.CtgLanguage_Id)))
                       .WhereIf(!input.Filter.IsNullOrEmpty(),
                       x => x.HoTen.ToUpper().Contains(input.Filter.ToUpper())
                      || x.ChoOHienTai.ToUpper().Contains(input.Filter.ToUpper())
                      || x.Email.ToUpper().Contains(input.Filter.ToUpper())
                      || x.SDT.ToUpper().Contains(input.Filter.ToUpper())
                      || x.KinhNghiem.ToUpper().Contains(input.Filter.ToUpper())
                      ).WhereIf(input.StartDate.HasValue, x => x.NgayNhanCV >= input.StartDate)
                       .WhereIf(input.EndDate.HasValue, x => x.NgayNhanCV <= input.EndDate);

            var tatolCount = await query.CountAsync();
            var result = await query.OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();
            var output = ObjectMapper.Map<List<EmployeeListDto>>(result);
            foreach(var item in output)
            {
                item.NhungNgonNgu =await GetLangugeName(item.Id);
            }
            return new PagedResultDto<EmployeeListDto>(tatolCount, output);
        }
        public async Task<string> GetLangugeName(int input)
        {
            var ls = "";
            var els = await _employeeLanguageRepository.GetAll().Include(x=>x.CtgLanguage_).Where(x => x.Employee_Id == input).ToListAsync();
            foreach(var item in els)
            {
                ls = ls + item.CtgLanguage_.NgonNgu + ',';
            }
            var result = ls.Substring(0, ls.Length - 1);
            return result;
        }

        public async Task<PagedResultDto<EmployeeListDto>> GetAll_Gui(EmployeeGuiInputDto input)
        {
            if (!input.Filter.IsNullOrEmpty())
            {
                input.Filter = Regex.Replace(input.Filter.Trim(), @"\s+", " ");
            }
            var query = _employeeRepository.GetAll()
                      .Where(x => x.TrangThai == true)
                      .WhereIf(!input.Filter.IsNullOrEmpty(),
                       x => x.HoTen.ToUpper().Contains(input.Filter.ToUpper())
                      || x.NgonNgu.ToUpper().Contains(input.Filter.ToUpper())
                      || x.CtyNhan.ToUpper().Contains(input.Filter.ToUpper())
                      || x.NguyenVong.ToUpper().Contains(input.Filter.ToUpper())
                      || x.SDT.ToUpper().Contains(input.Filter.ToUpper())
                      || x.Email.ToUpper().Contains(input.Filter.ToUpper())
                      ).WhereIf(input.StartDate.HasValue, x=>x.NgayHoTro >= input.StartDate)
                       .WhereIf(input.EndDate.HasValue, x => x.NgayHoTro <= input.EndDate)
                       .WhereIf(input.StartNgayPV.HasValue, x=>x.NgayPhongVan >= input.StartNgayPV)
                       .WhereIf(input.EndNgayPV.HasValue, x=>x.NgayPhongVan <= input.EndNgayPV.Value)
                       .WhereIf(input.KetQua.HasValue, x => x.KetQua == input.KetQua);
            var tatolCount = await query.CountAsync();
            var result = await query.OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();
            return new PagedResultDto<EmployeeListDto>(tatolCount, ObjectMapper.Map<List<EmployeeListDto>>(result));
        }

        public async Task<CreateEmployeeDto> GetId(int id)
        {
            var dto = await _employeeRepository.FirstOrDefaultAsync(id);
            var result = ObjectMapper.Map<CreateEmployeeDto>(dto);
            var languages = await _employeeLanguageRepository.GetAll().Where(x => x.Employee_Id == id).Select(x => x.CtgLanguage_Id).ToListAsync();
            result.Languages = languages;
            return result;
 
        }

        [HttpPost]
        public async Task CapNhat(CreateEmployeeDto input)
        {
            var emp = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
            if (input.CVName != null && input.IsSeletedFile)
            {
                //Delete old document file
                AppFileHelper.DeleteFilesInFolderIfExists(_appFolders.AttachmentsFolder, input.CVName);
                var sourceFile = Path.Combine(_appFolders.TempFileUploadFolder, input.CVName);
                var destFile = Path.Combine(_appFolders.AttachmentsFolder, input.CVName);
                System.IO.File.Move(sourceFile, destFile);
                var filePath = Path.Combine(_appFolders.AttachmentsFolder, input.CVName);
                emp.CVName = input.CVName;
                emp.CVUrl = input.CVUrl;
            }
            emp.BangCap = input.BangCap;
            emp.ChoOHienTai = input.ChoOHienTai;
            emp.ContentType = input.ContentType;
            emp.CtyNhan = input.CtyNhan;
            emp.DanhGiaNgonNgu = input.DanhGiaNgonNgu;
            emp.Email = input.Email;
            emp.FaceBook = input.FaceBook;
            emp.GioiTinh = input.GioiTinh;
            emp.HoTen = input.HoTen;
            emp.Id = input.Id;
            emp.KetQua = input.KetQua;
            emp.KinhNghiem = input.KinhNghiem;
            emp.LuongMongMuon = input.LuongMongMuon;
            emp.NamSinh = input.NamSinh;
            emp.NgayHoTro = input.NgayHoTro;
            emp.NgayNhanCV = input.NgayNhanCV;
            emp.NgonNgu = input.NgonNgu;
            emp.NguyenVong = input.NguyenVong;
            emp.NoiDung = input.NoiDung;
            emp.QueQuan = input.QueQuan;
            emp.SDT = input.SDT;
            emp.TrangThai = input.TrangThai;
            emp.TenantId = (int)input.TenantId;


            var listEmployeeLanguege = _employeeLanguageRepository.GetAll().Where(x => x.Employee_Id == input.Id);

            // tìm cái đã xóa trong lst
            foreach (var item in listEmployeeLanguege)
            {
                var index = input.Languages.Any(x => x == item.CtgLanguage_Id);
                if (index != true)
                {
                    await _employeeLanguageRepository.DeleteAsync(item.Id);
                }
            }
            // tìm những cái chưa có để thêm mới trong danh sách
            foreach (var item in input.Languages)
            {
                var index = listEmployeeLanguege.Where(x => x.CtgLanguage_Id == item);
                if (index.Count() < 1)
                {
                    var el = new EmployeeLanguage
                    {
                        Employee_Id = input.Id,
                        TenantId = AbpSession.TenantId ?? 1,
                        CtgLanguage_Id = item,
                    };
                    await _employeeLanguageRepository.InsertAsync(el);
                }
            }

            await _employeeRepository.UpdateAsync(emp);
        }

        public FileDto DownloadTempAttachment(int id)
        {
            var file = _employeeRepository.Get(id);

            if (file != null && !string.IsNullOrEmpty(file.CVUrl) && File.Exists(file.CVUrl))
            {
                var zipFileDto = new FileDto(file.CVName, file.ContentType);

                var outputZipFilePath = Path.Combine(_appFolders.TempFileDownloadFolder, zipFileDto.FileToken);

                File.Copy(file.CVUrl, outputZipFilePath, true);

                return zipFileDto;
            }
            return null;
        }

        public void DeleteDocumentTempFile(string DocumentName)
        {
            AppFileHelper.DeleteFilesInFolderIfExists(_appFolders.TempFileUploadFolder, DocumentName);
        }

        public async Task DaNhan(int id)
        {
            var emp = await _employeeRepository.FirstOrDefaultAsync(id);
            emp.KetQua = true;
        }

        public async Task HuyNhan(int id)
        {
            var emp = await _employeeRepository.FirstOrDefaultAsync(id);
            emp.KetQua = false;
        }

        public async Task ChuyenVeQLCV(int id)
        {
            var emp = await _employeeRepository.FirstOrDefaultAsync(id);
            emp.KetQua = false;
            emp.TrangThai = false;
            emp.NgayHoTro = null;
            emp.CtyNhan = null;
            emp.NgayPhongVan = null;
        }

        public async Task GuiCV(int id, string tencty,DateTime? NgayPV)
        {
            var emp = await _employeeRepository.FirstOrDefaultAsync(id);
            emp.TrangThai = true;
            emp.CtyNhan = tencty;
            emp.NgayHoTro = DateTime.Now;
            emp.NgayPhongVan = NgayPV;
        }

        public async Task<FileDto> GetCVToExcel(EmployeeInputDto input)
        {
            var list = await GetAll(input);
            var dto = list.Items.ToList();
            return _employeeListExcelExporter.Export(dto);
        }

        public async Task<FileDto> GetGuiCVToExcel(EmployeeGuiInputDto inputDto)
        {
            var list = await GetAll_Gui(inputDto);
            var dto = list.Items.ToList();
            return _employeeListExcelExporter.Export_CVNhan(dto);
        }

        public async Task<string> GetListEmail(List<int> inputs)
        {
            var result = "";
            var emps = await _employeeRepository.GetAll().Where(x => inputs.Any(a => a == x.Id)).ToListAsync();
            foreach(var item in emps)
            {
                if (!item.Email.IsNullOrEmpty())
                {
                    result = result + item.Email + ";";
                }
            }
            return result;
        }
        [HttpDelete]
        public void DeleteFileJD(string file)
        {
            var filePath = Path.Combine(_appFolders.TempFileUploadJDFolder, file);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}
