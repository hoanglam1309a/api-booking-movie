using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using bookingticketAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using bookingticketAPI.Models.ViewModel;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using static bookingticketAPI.Common;
using Microsoft.AspNetCore.Mvc;
using ReflectionIT.Mvc.Paging;
using bookingticketAPI.Reponsitory;
using bookingticketAPI.StatusConstants;
using Microsoft.Net.Http.Headers;
using bookingticketAPI.Filter;
//using ReflectionIT.Mvc.Paging

namespace bookingticketAPI.Controllers
{
    [Route("api/[controller]")]
    // [FilterTokenCyber]
    [ApiController]
    public class QuanLyNguoiDungController : ControllerBase
    {
        dbRapChieuPhimContext db = new dbRapChieuPhimContext();
        Common commonService = new Common();

        ThongBaoLoi tbl = new ThongBaoLoi();
        private const string SECRET_KEY = "0123456789123456";//Khóa bí mật
        public static readonly SymmetricSecurityKey SIGNING_KEY = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SECRET_KEY));


        [HttpGet("LayDanhSachLoaiNguoiDung")]
        
        public async Task<ResponseEntity> LayDanhSachLoaiNguoiDung()
        {
            var lstModel = db.LoaiNguoiDung.Select(n => new { n.MaLoaiNguoiDung, n.TenLoai });
            return new ResponseEntity(StatusCodeConstants.OK, lstModel, MessageConstant.MESSAGE_SUCCESS_200);

        }


        [HttpPost("DangNhap")]
        public async Task<ResponseEntity> DangNhap(ThongTinDangNhapVM ndDN)
        {
            NguoiDung nguoiDungCapNhat = db.NguoiDung.SingleOrDefault(n => n.TaiKhoan == ndDN.TaiKhoan && n.MatKhau == ndDN.MatKhau);
            if (nguoiDungCapNhat != null)
            {
                NguoiDungDangNhap nd = new NguoiDungDangNhap { TaiKhoan = nguoiDungCapNhat.TaiKhoan, HoTen = nguoiDungCapNhat.HoTen, Email = nguoiDungCapNhat.Email, SoDT = nguoiDungCapNhat.SoDt, MaNhom = nguoiDungCapNhat.MaNhom, MaLoaiNguoiDung = nguoiDungCapNhat.MaLoaiNguoiDung };
                string accessToken = GenerateToken(nd);
                nd.accessToken = accessToken;
                return new ResponseEntity(StatusCodeConstants.OK, nd, MessageConstant.MESSAGE_SUCCESS_200);

            }
            return new ResponseEntity(StatusCodeConstants.NOT_FOUND, "Tài khoản hoặc mật khẩu không đúng!", MessageConstant.MESSAGE_ERROR_404);

            //var response = await tbl.TBLoi(ThongBaoLoi.Loi500, "Tài khoản hoặc mật khẩu không đúng!");
            //return response;
        }
        [HttpPost("DangKy")]
        public async Task<ResponseEntity> DangKy(NguoiDung_VM nd)
        {
            nd.MaNhom = "GP00";
            bool ckEmail = db.NguoiDung.Any(n => n.Email == nd.Email);
            if (ckEmail)
            {
                return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Email đã tồn tại!", MessageConstant.BAD_REQUEST);
            }
            var nguoiDung = db.NguoiDung.SingleOrDefault(n => n.TaiKhoan == nd.TaiKhoan);
            if (nguoiDung != null)
            {
                return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Tài khoản đã tồn tại!", MessageConstant.BAD_REQUEST);
            }
            //try
            //{
            NguoiDung ndInsert = new NguoiDung();
            ndInsert.TaiKhoan = nd.TaiKhoan;
            ndInsert.HoTen = nd.HoTen;
            ndInsert.MatKhau = nd.MatKhau;
            ndInsert.Email = nd.Email;
            ndInsert.MaNhom = nd.MaNhom;
            ndInsert.MaLoaiNguoiDung = LoaiND.KhachHang;
            ndInsert.BiDanh = LoaiBoKyTu.bestLower(ndInsert.HoTen);
            db.NguoiDung.Add(ndInsert);
                db.SaveChanges();
                return new ResponseEntity(StatusCodeConstants.OK, nd, MessageConstant.MESSAGE_SUCCESS_200);
        }
        private string GenerateToken(NguoiDungDangNhap ndDN)
        {
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                    claims: new Claim[] {
                        new Claim(ClaimTypes.Name,ndDN.TaiKhoan),
                        new Claim(ClaimTypes.Email,ndDN.Email),
                        new Claim(ClaimTypes.Role,ndDN.MaLoaiNguoiDung),
                        new Claim(ClaimTypes.Role,ndDN.Email),
                        new Claim(ClaimTypes.Role,ndDN.MaNhom),
                    },
                    notBefore: new DateTimeOffset(DateTime.Now).DateTime,
                    expires: new DateTimeOffset(DateTime.Now.AddMinutes(60)).DateTime,
                    signingCredentials: new SigningCredentials(SIGNING_KEY, SecurityAlgorithms.HmacSha256)
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("LayDanhSachNguoiDung")]
        public async Task<ResponseEntity> LayDanhSachNguoiDung(string MaNhom = "GP01", string tuKhoa = "")
        {
            var ktNhom = db.Nhom.Any(n => n.MaNhom == MaNhom);
            if (!ktNhom)
            {
                return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Mã nhóm không hợp lệ!", MessageConstant.BAD_REQUEST);
            }
            var lstResult = db.NguoiDung.Where(n => n.MaNhom == MaNhom && n.BiDanh.Contains(tuKhoa)).Select(n => new { n.TaiKhoan, n.HoTen, n.Email, n.SoDt, n.MatKhau,n.MaLoaiNguoiDung });
            if (lstResult.Count() == 0)
            {
                lstResult = db.NguoiDung.Where(n => n.MaNhom == MaNhom && n.TaiKhoan == tuKhoa).Select(n => new { n.TaiKhoan, n.HoTen, n.Email, n.SoDt, n.MatKhau, n.MaLoaiNguoiDung });
            }
            if (lstResult.Count() == 0)
            {
                lstResult = db.NguoiDung.Where(n => n.MaNhom == MaNhom && n.SoDt.Contains(tuKhoa)).Select(n => new { n.TaiKhoan, n.HoTen, n.Email, n.SoDt, n.MatKhau ,n.MaLoaiNguoiDung});
            }
            return new ResponseEntity(StatusCodeConstants.OK, lstResult, MessageConstant.MESSAGE_SUCCESS_200);
        }

        [HttpGet("LayDanhSachNguoiDungPhanTrang")]
        public async Task<ResponseEntity> LayDanhSachNguoiDungPhanTrang(string MaNhom = "GP01", string tuKhoa = "",int soTrang = 1,int soPhanTuTrenTrang = 20)
        {
            var ktNhom = db.Nhom.Any(n => n.MaNhom == MaNhom);
            if (!ktNhom)
            {
                return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Mã nhóm không hợp lệ!", MessageConstant.BAD_REQUEST);
            }
            IEnumerable<NguoiDungVM> lstResult = db.NguoiDung.Where(n => n.MaNhom == MaNhom && n.BiDanh.Contains(tuKhoa)).Select(n => new NguoiDungVM {TaiKhoan= n.TaiKhoan,HoTen= n.HoTen, Email = n.Email, SoDt= n.SoDt,MatKhau= n.MatKhau,MaLoaiNguoiDung= n.MaLoaiNguoiDung });
            if (lstResult.Count() == 0)
            {
                lstResult = db.NguoiDung.Where(n => n.MaNhom == MaNhom && n.TaiKhoan == tuKhoa).Select(n => new NguoiDungVM { TaiKhoan = n.TaiKhoan, HoTen = n.HoTen, Email = n.Email, SoDt = n.SoDt, MatKhau = n.MatKhau, MaLoaiNguoiDung = n.MaLoaiNguoiDung });
            }
            if (lstResult.Count() == 0)
            {
                lstResult = db.NguoiDung.Where(n => n.MaNhom == MaNhom && n.SoDt.Contains(tuKhoa)).Select(n => new NguoiDungVM { TaiKhoan = n.TaiKhoan, HoTen = n.HoTen, Email = n.Email, SoDt = n.SoDt, MatKhau = n.MatKhau, MaLoaiNguoiDung = n.MaLoaiNguoiDung });
            }
            PaginationSet<NguoiDungVM> result = new PaginationSet<NguoiDungVM>();
            result.CurrentPage = soTrang;
            result.TotalPages = (lstResult.Count() / soPhanTuTrenTrang) + 1;
            result.Items = lstResult.Skip((soTrang - 1) * soPhanTuTrenTrang).Take(soPhanTuTrenTrang);
            result.TotalCount = lstResult.Count();
            return new ResponseEntity(StatusCodeConstants.OK, result, MessageConstant.MESSAGE_SUCCESS_200);
        }

        [HttpGet("TimKiemNguoiDung")]
        public async Task<ResponseEntity> TimKiemNguoiDung(string MaNhom = "GP01", string tuKhoa = "")
        {
            var ktNhom = db.Nhom.Any(n => n.MaNhom == MaNhom);
            if (!ktNhom)
            {
                // I wish to return an error response how can i do that?
                return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Mã nhóm không hợp lệ!", MessageConstant.BAD_REQUEST);
            }
            var lstResult = db.NguoiDung.Where(n => n.MaNhom == MaNhom && n.BiDanh.Contains(tuKhoa)).Select(n => new { n.TaiKhoan, n.HoTen, n.Email, n.SoDt, n.MatKhau ,n.MaLoaiNguoiDung});
            if (lstResult.Count() == 0)
            {
                lstResult = db.NguoiDung.Where(n => n.MaNhom == MaNhom && n.TaiKhoan == tuKhoa).Select(n => new { n.TaiKhoan, n.HoTen, n.Email, n.SoDt, n.MatKhau, n.MaLoaiNguoiDung });
            }
            if (lstResult.Count() == 0)
            {
                lstResult = db.NguoiDung.Where(n => n.MaNhom == MaNhom && n.SoDt.Contains(tuKhoa)).Select(n => new { n.TaiKhoan, n.HoTen, n.Email, n.SoDt, n.MatKhau, n.MaLoaiNguoiDung });
            }
            return new ResponseEntity(StatusCodeConstants.OK, lstResult, MessageConstant.MESSAGE_SUCCESS_200);
        }

        [HttpGet("TimKiemNguoiDungPhanTrang")]
        public async Task<ResponseEntity> TimKiemNguoiDungPhanTrang(string MaNhom = "GP01", string tuKhoa = "",int soTrang = 1, int soPhanTuTrenTrang = 1)
        {
            var ktNhom = db.Nhom.Any(n => n.MaNhom == MaNhom);
            if (!ktNhom)
            {
                return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Mã nhóm không hợp lệ!", MessageConstant.BAD_REQUEST);
            }
            IEnumerable<NguoiDungVM> lstResult = db.NguoiDung.Where(n => n.MaNhom == MaNhom && n.BiDanh.Contains(tuKhoa)).Select(n => new NguoiDungVM { TaiKhoan= n.TaiKhoan,HoTen= n.HoTen,Email= n.Email,SoDt= n.SoDt,MatKhau= n.MatKhau,MaLoaiNguoiDung= n.MaLoaiNguoiDung });
            if (lstResult.Count() == 0)
            {
                lstResult = db.NguoiDung.Where(n => n.MaNhom == MaNhom && n.TaiKhoan == tuKhoa).Select(n => new NguoiDungVM { TaiKhoan = n.TaiKhoan, HoTen = n.HoTen, Email = n.Email, SoDt = n.SoDt, MatKhau = n.MatKhau, MaLoaiNguoiDung = n.MaLoaiNguoiDung });
            }
            if (lstResult.Count() == 0)
            {
                lstResult = db.NguoiDung.Where(n => n.MaNhom == MaNhom && n.SoDt.Contains(tuKhoa)).Select(n => new NguoiDungVM { TaiKhoan = n.TaiKhoan, HoTen = n.HoTen, Email = n.Email, SoDt = n.SoDt, MatKhau = n.MatKhau, MaLoaiNguoiDung = n.MaLoaiNguoiDung });
            }
            PaginationSet<NguoiDungVM> result = new PaginationSet<NguoiDungVM>();
            result.CurrentPage = soTrang;
            result.TotalPages = (lstResult.Count() / soPhanTuTrenTrang) + 1;
            result.Items = lstResult.Skip((soTrang - 1) * soPhanTuTrenTrang).Take(soPhanTuTrenTrang);
            result.TotalCount = lstResult.Count();
            return new ResponseEntity(StatusCodeConstants.OK, result, MessageConstant.MESSAGE_SUCCESS_200);
        }
        [Authorize]
        [HttpPost("ThongTinTaiKhoan")]
        public async Task<ResponseEntity> ThongTinTaiKhoan()
        {
            var accessToken = Request.Headers[HeaderNames.Authorization];
            userToken tttk = commonService.getUserByToken(accessToken).Result;
            if(tttk == null)
            {
                return new ResponseEntity(StatusCodeConstants.AUTHORIZATION, "Token đã hết hạn bạn hãy đăng nhập lại!", MessageConstant.MESSAGE_ERROR_401);
            }

            NguoiDung tt = db.NguoiDung.SingleOrDefault(n => n.TaiKhoan == tttk.taiKhoan);
            if (tt == null)
            {
                return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Tài khoản không hợp lệ!", MessageConstant.BAD_REQUEST);

            }
            IEnumerable<DatVe> lstDatVe = db.DatVe.Where(n => n.TaiKhoanNguoiDung == tttk.taiKhoan).ToList();
            List<ThongTinDatVe> lstThongTinDatVe = new List<ThongTinDatVe>();
            if (lstDatVe.Count() !=0)
            {
                foreach (var item in lstDatVe.GroupBy(n=>n.MaLichChieu))
                {
                    ThongTinDatVe ttdv = new ThongTinDatVe();
                    ttdv.MaVe = item.First().MaVe;
                    foreach (var ghe in item)
                    {
                        Ghe gheNavigation = ghe.MaGheNavigation;
                        Rap rap = ghe.MaGheNavigation.MaRapNavigation;
                        CumRap cumRap = rap.MaCumRapNavigation;
                        HeThongRap heThongRap = cumRap.MaHeThongRapNavigation;
                        ThongTinGhe ttg = new ThongTinGhe() { MaCumRap = rap.TenRap, TenCumRap = rap.TenRap, MaGhe = ghe.MaGhe, TenGhe = gheNavigation.TenGhe, MaRap = gheNavigation.MaRap, TenRap = rap.TenRap, MaHeThongRap = heThongRap.MaHeThongRap, TenHeThongRap = cumRap.TenCumRap };
                        ttdv.DanhSachGhe.Add(ttg);
                    }
                    ttdv.NgayDat = item.First().NgayDat.Value;
                    ttdv.HinhAnh = DomainImage +  item.First().MaLichChieuNavigation.MaPhimNavigation.HinhAnh;
                    ttdv.TenPhim = item.First().MaLichChieuNavigation.MaPhimNavigation.TenPhim;
                    ttdv.GiaVe = item.First().GiaVe.Value;
                    ttdv.ThoiLuongPhim = item.First().MaLichChieuNavigation.ThoiLuong.Value;
                    lstThongTinDatVe.Add(ttdv);

                }

            }

            ThongTinTaiKhoanVM ttTK = Mapper.Map<NguoiDung, ThongTinTaiKhoanVM>(tt);
            ttTK.MaLoaiNguoiDung = tttk.maLoaiNguoiDung;
            LoaiNguoiDungVM loai = new LoaiNguoiDungVM();
            LoaiNguoiDung loaiNguoiDung = db.LoaiNguoiDung.SingleOrDefault(n => n.MaLoaiNguoiDung == ttTK.MaLoaiNguoiDung);
            if (loaiNguoiDung != null)
            {
                loai.MaLoaiNguoiDung = loaiNguoiDung.MaLoaiNguoiDung;
                loai.TenLoai = loaiNguoiDung.TenLoai;
            }
            ttTK.LoaiNguoiDung = loai;
            ttTK.ThongTinDatVe = lstThongTinDatVe;
            return new ResponseEntity(StatusCodeConstants.OK, ttTK, MessageConstant.MESSAGE_SUCCESS_200);
        }
        [Authorize(Roles = "QuanTri")]
        [HttpPost("LayThongTinNguoiDung")]
        public async Task<ResponseEntity> LayThongTinNguoiDung(string taiKhoan)
        {
            NguoiDung tt = db.NguoiDung.SingleOrDefault(n => n.TaiKhoan == taiKhoan);
            if (tt == null)
            {
                return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Tài khoản không hợp lệ!", MessageConstant.BAD_REQUEST);
            }
            IEnumerable<DatVe> lstDatVe = db.DatVe.Where(n => n.TaiKhoanNguoiDung == taiKhoan).ToList();
            List<ThongTinDatVe> lstThongTinDatVe = new List<ThongTinDatVe>();
            if (lstDatVe.Count() != 0)
            {
                foreach (var item in lstDatVe.GroupBy(n => n.MaLichChieu))
                {
                    ThongTinDatVe ttdv = new ThongTinDatVe();
                    ttdv.MaVe = item.First().MaVe;
                    foreach (var ghe in item)
                    {
                        Ghe gheNavigation = ghe.MaGheNavigation;
                        Rap rap = ghe.MaGheNavigation.MaRapNavigation;
                        CumRap cumRap = rap.MaCumRapNavigation;
                        HeThongRap heThongRap = cumRap.MaHeThongRapNavigation;
                        ThongTinGhe ttg = new ThongTinGhe() { MaCumRap = rap.TenRap, TenCumRap = rap.TenRap, MaGhe = ghe.MaGhe, TenGhe = gheNavigation.TenGhe, MaRap = gheNavigation.MaRap, TenRap = rap.TenRap, MaHeThongRap = heThongRap.MaHeThongRap, TenHeThongRap = cumRap.TenCumRap };
                        ttdv.DanhSachGhe.Add(ttg);
                    }
                    ttdv.NgayDat = item.First().NgayDat.Value;
                    ttdv.HinhAnh = DomainImage + item.First().MaLichChieuNavigation.MaPhimNavigation.HinhAnh;
                    ttdv.TenPhim = item.First().MaLichChieuNavigation.MaPhimNavigation.TenPhim;
                    ttdv.GiaVe = item.First().GiaVe.Value;
                    ttdv.ThoiLuongPhim = item.First().MaLichChieuNavigation.ThoiLuong.Value;
                    lstThongTinDatVe.Add(ttdv);
                }
            }
            ThongTinTaiKhoanVM ttTK = Mapper.Map<NguoiDung, ThongTinTaiKhoanVM>(tt);
            ttTK.MaLoaiNguoiDung = tt.MaLoaiNguoiDung;
            LoaiNguoiDungVM loai = new LoaiNguoiDungVM();
            LoaiNguoiDung loaiNguoiDung = db.LoaiNguoiDung.SingleOrDefault(n => n.MaLoaiNguoiDung == ttTK.MaLoaiNguoiDung);
            if (loaiNguoiDung != null)
            {
                loai.MaLoaiNguoiDung = loaiNguoiDung.MaLoaiNguoiDung;
                loai.TenLoai = loaiNguoiDung.TenLoai;
            }
            ttTK.LoaiNguoiDung = loai;
            ttTK.ThongTinDatVe = lstThongTinDatVe;
            return new ResponseEntity(StatusCodeConstants.OK, ttTK, MessageConstant.MESSAGE_SUCCESS_200);
        }
        [Authorize(Roles = "QuanTri")]
        [HttpPost("ThemNguoiDung")]
        public async Task<ResponseEntity> ThemNguoiDung(NguoiDungVM nd)
        {
            nd.MaNhom = nd.MaNhom.ToUpper();
            bool ckbLoaiND = db.LoaiNguoiDung.Any(n => n.MaLoaiNguoiDung == nd.MaLoaiNguoiDung);
            if (!ckbLoaiND)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Loại người dùng không hợp lệ!", MessageConstant.ERROR);
            }
            bool ckNhom = db.Nhom.Any(n => n.MaNhom == nd.MaNhom);
            if (!ckNhom)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Nhóm người dùng không hợp lệ!", MessageConstant.ERROR);
            }
            bool ckEmail = db.NguoiDung.Any(n => n.Email == nd.Email);
            if (ckEmail)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Email đã tồn tại!", MessageConstant.ERROR);
            }
            var nguoiDung = db.NguoiDung.SingleOrDefault(n => n.TaiKhoan == nd.TaiKhoan);
            if (nguoiDung != null)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Tài khoản đã tồn tại!", MessageConstant.ERROR);
            }
            try
            {
                NguoiDung ndInsert = Mapper.Map<NguoiDungVM, NguoiDung>(nd);
                ndInsert.BiDanh = LoaiBoKyTu.bestLower(ndInsert.HoTen);
                db.NguoiDung.Add(ndInsert);
                db.SaveChanges();
                return new ResponseEntity(StatusCodeConstants.OK,nd, MessageConstant.MESSAGE_SUCCESS_200);
            }
            catch (Exception ex)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Dữ liệu không hợp lệ!", MessageConstant.ERROR);
            }
        }
        [Authorize(Roles = "QuanTri")]
        [HttpPost("CapNhatThongTinNguoiDung")]
        public async Task<ResponseEntity> CapNhat(NguoiDungVM nd)
        {
            nd.MaNhom = nd.MaNhom.ToUpper();
            bool ckbLoaiND = db.LoaiNguoiDung.Any(n => n.MaLoaiNguoiDung == nd.MaLoaiNguoiDung);
            if (!ckbLoaiND)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Loại người dùng không hợp lệ!", MessageConstant.ERROR);
            }
            bool ckNhom = db.Nhom.Any(n => n.MaNhom == nd.MaNhom);
            if (!ckNhom)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Nhóm người dùng không hợp lệ!", MessageConstant.ERROR);
            }
            bool ckEmail = db.NguoiDung.Any(n => n.Email == nd.Email && n.TaiKhoan != nd.TaiKhoan);
            if (ckEmail)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Email đã tồn tại!", MessageConstant.ERROR);
            }
            NguoiDung nguoiDungCapNhat = db.NguoiDung.SingleOrDefault(n => n.TaiKhoan == nd.TaiKhoan);
            if (nguoiDungCapNhat == null)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Tài khoản không tồn tại!", MessageConstant.ERROR);
            }
            try
            {
                if (nd.MatKhau == "")
                {
                    nd.MatKhau = nguoiDungCapNhat.MatKhau;
                }
                nguoiDungCapNhat.HoTen = nd.HoTen;
                nguoiDungCapNhat.MatKhau = nd.MatKhau;
                nguoiDungCapNhat.BiDanh = LoaiBoKyTu.bestLower(nd.HoTen);
                nguoiDungCapNhat.SoDt = nd.SoDt;
                nguoiDungCapNhat.MaLoaiNguoiDung = nd.MaLoaiNguoiDung;
                nguoiDungCapNhat.Email = nd.Email;
                db.SaveChanges();
                var result = new ThongTinTaiKhoanVM { TaiKhoan = nguoiDungCapNhat.TaiKhoan, MatKhau = nguoiDungCapNhat.MatKhau, HoTen = nguoiDungCapNhat.HoTen, Email = nguoiDungCapNhat.Email, SoDT = nguoiDungCapNhat.SoDt, MaNhom = nguoiDungCapNhat.MaNhom, MaLoaiNguoiDung = nguoiDungCapNhat.MaLoaiNguoiDungNavigation.TenLoai };
                return new ResponseEntity(StatusCodeConstants.OK, result, MessageConstant.MESSAGE_SUCCESS_200);
            }
            catch (Exception ex)
            {
                return new ResponseEntity(StatusCodeConstants.OK, "Dữ liệu không hợp lệ!", MessageConstant.MESSAGE_SUCCESS_200);
            }
        }
        [Authorize]
        [HttpPut("CapNhatThongTinNguoiDung")]
        public async Task<ResponseEntity> CapNhatThongTinNguoiDung(NguoiDungVM nd)
        {
            var accessToken = Request.Headers[HeaderNames.Authorization];
            userToken usToken = commonService.getUserByToken(accessToken).Result;
            if (usToken == null)
            {
                return new ResponseEntity(StatusCodeConstants.AUTHORIZATION, "Token đã hết hạn bạn hãy đăng nhập lại!", MessageConstant.MESSAGE_ERROR_401);
            }
            if (usToken.taiKhoan !=  nd.TaiKhoan)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Bạn không có quyền thay đổi tài khoản người khác !", MessageConstant.MESSAGE_ERROR_500);
            }
                nd.MaNhom = nd.MaNhom.ToUpper();
            bool ckbLoaiND = db.LoaiNguoiDung.Any(n => n.MaLoaiNguoiDung == nd.MaLoaiNguoiDung);
            if (!ckbLoaiND)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Loại người dùng không hợp lệ!", MessageConstant.ERROR);
            }
            bool ckNhom = db.Nhom.Any(n => n.MaNhom == nd.MaNhom);
            if (!ckNhom)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Nhóm người dùng không hợp lệ!", MessageConstant.ERROR);
            }
            bool ckEmail = db.NguoiDung.Any(n => n.Email == nd.Email && n.TaiKhoan != nd.TaiKhoan);
            if (ckEmail)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Email đã tồn tại!", MessageConstant.ERROR);
            }
            NguoiDung nguoiDungCapNhat = db.NguoiDung.SingleOrDefault(n => n.TaiKhoan == nd.TaiKhoan);
            if (nguoiDungCapNhat == null)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Tài khoản không tồn tại!", MessageConstant.ERROR);
            }
            try
            {
                if (nd.MatKhau == "")
                {
                    nd.MatKhau = nguoiDungCapNhat.MatKhau;
                }
                nguoiDungCapNhat.HoTen = nd.HoTen;
                nguoiDungCapNhat.MatKhau = nd.MatKhau;
                nguoiDungCapNhat.BiDanh = LoaiBoKyTu.bestLower(nd.HoTen);
                nguoiDungCapNhat.SoDt = nd.SoDt;
                nguoiDungCapNhat.MaLoaiNguoiDung = nd.MaLoaiNguoiDung;
                nguoiDungCapNhat.Email = nd.Email;
                db.SaveChanges();
                var result = new ThongTinTaiKhoanVM { TaiKhoan = nguoiDungCapNhat.TaiKhoan, MatKhau = nguoiDungCapNhat.MatKhau, HoTen = nguoiDungCapNhat.HoTen, Email = nguoiDungCapNhat.Email, SoDT = nguoiDungCapNhat.SoDt, MaNhom = nguoiDungCapNhat.MaNhom, MaLoaiNguoiDung = nguoiDungCapNhat.MaLoaiNguoiDungNavigation.TenLoai };
                return new ResponseEntity(StatusCodeConstants.OK, result, MessageConstant.MESSAGE_SUCCESS_200);
            }
            catch (Exception ex)
            {
                return new ResponseEntity(StatusCodeConstants.OK, "Dữ liệu không hợp lệ!", MessageConstant.MESSAGE_SUCCESS_200);
            }
        }
        [Authorize(Roles = "QuanTri")]
        [HttpDelete("XoaNguoiDung")]
        public async Task<ResponseEntity> XoaNguoiDung(string TaiKhoan)
        {
            var ckND = db.NguoiDung.SingleOrDefault(n => n.TaiKhoan == TaiKhoan);
            if (ckND == null)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Dữ liệu không hợp lệ!", MessageConstant.ERROR);
            }
            var ckTaoKhoaHoc = db.DatVe.Where(n => n.TaiKhoanNguoiDung == TaiKhoan);
            if (ckTaoKhoaHoc.Count() > 0)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Người dùng này đã đặt vé xem phim không thể xóa!", MessageConstant.ERROR);
            }
          
            db.NguoiDung.Remove(ckND);
            db.SaveChanges();
            return new ResponseEntity(StatusCodeConstants.OK, "Xóa thành công!", MessageConstant.MESSAGE_SUCCESS_200);
        }
    }
}
