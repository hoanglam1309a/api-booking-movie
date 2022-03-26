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
using System.Linq;
using static bookingticketAPI.Common;
using ReflectionIT.Mvc.Paging;
using System.IO;
using System.Reflection;
using System.Linq.Expressions;
using bookingticketAPI.StatusConstants;
using bookingticketAPI.Reponsitory;
using bookingticketAPI.Filter;

namespace bookingticketAPI.Controllers
{
    [Route("api/[controller]")]
    // [FilterTokenCyber]
    [ApiController]
    public class QuanLyPhimController : ControllerBase
    {
        dbRapChieuPhimContext db = new dbRapChieuPhimContext();
        ThongBaoLoi tbl = new ThongBaoLoi();
        //string hostName =  "http://movie0706.cybersoft.edu.vn/hinhanh/";
        [HttpGet("LayDanhSachBanner")]
        public async Task<ResponseEntity> LayDanhSachBanner()
        {
            try
            {
                var lstResult = db.Banner.Select(n => new Banner() { MaPhim = n.MaPhim, HinhAnh = DomainImage + n.HinhAnh, MaBanner = n.MaBanner });
                return new ResponseEntity(StatusCodeConstants.OK, lstResult, MessageConstant.MESSAGE_SUCCESS_200);
            }
            catch (Exception ex)
            {
                return new ResponseEntity(StatusCodeConstants.OK, "Không tìm banner !", MessageConstant.MESSAGE_SUCCESS_200);
            }
        }
        [HttpGet("LayDanhSachPhim")]
        public async Task<ResponseEntity> LayDanhSachPhim(string maNhom = "GP01", string tenPhim = "")
        {
            try
            {
                bool ckNhom = db.Nhom.Any(n => n.MaNhom == maNhom);
                if (!ckNhom)
                {
                    var response = await tbl.TBLoi(ThongBaoLoi.Loi500, "Nhóm người dùng không hợp lệ!");
                    return new ResponseEntity(StatusCodeConstants.OK, response, MessageConstant.MESSAGE_SUCCESS_200);
                }
                tenPhim = LoaiBoKyTu.bestLower(tenPhim);
                IEnumerable<PhimVM> lstResult = db.Phim.Where(n => n.BiDanh.Contains(tenPhim) && n.MaNhom == maNhom && n.DaXoa != true).Select(n => new PhimVM { MaPhim = n.MaPhim, BiDanh = n.BiDanh, DanhGia = n.DanhGia, HinhAnh = DomainImage + n.HinhAnh, MaNhom = n.MaNhom, MoTa = n.MoTa, TenPhim = n.TenPhim, Trailer = n.Trailer, NgayKhoiChieu = n.NgayKhoiChieu, DangChieu = n.DangChieu, Hot = n.Hot, SapChieu = n.SapChieu });
                return new ResponseEntity(StatusCodeConstants.OK, lstResult, MessageConstant.MESSAGE_SUCCESS_200);
            }
            catch (Exception ex)
            {
                return new ResponseEntity(StatusCodeConstants.OK, "Không tìm thấy phim !", MessageConstant.MESSAGE_SUCCESS_200);
            }
        }
        [HttpGet("LayDanhSachPhimPhanTrang")]
        public async Task<ResponseEntity> LayDanhSachPhimPhanTrang(string maNhom = "GP01", string tenPhim = "", int soTrang = 1, int soPhanTuTrenTrang = 10)
        {
            bool ckNhom = db.Nhom.Any(n => n.MaNhom == maNhom);
            if (!ckNhom)
            {
                var response = await tbl.TBLoi(ThongBaoLoi.Loi500, "Nhóm người dùng không hợp lệ!");
                return new ResponseEntity(StatusCodeConstants.OK, MessageConstant.MESSAGE_NOTFOUND_GROUP, MessageConstant.MESSAGE_SUCCESS_200);
            }
            tenPhim = LoaiBoKyTu.bestLower(tenPhim);
            IEnumerable<PhimVM> lstResult = db.Phim.Where(n => n.BiDanh.Contains(tenPhim) && n.MaNhom == maNhom && n.DaXoa != true).Select(n => new PhimVM { MaPhim = n.MaPhim, BiDanh = n.BiDanh, DanhGia = n.DanhGia, HinhAnh = DomainImage + n.HinhAnh, MaNhom = n.MaNhom, MoTa = n.MoTa, TenPhim = n.TenPhim, Trailer = n.Trailer, NgayKhoiChieu = n.NgayKhoiChieu, Hot = n.Hot, DangChieu = n.DangChieu, SapChieu = n.SapChieu });
            PaginationSet<PhimVM> result = new PaginationSet<PhimVM>();
            result.CurrentPage = soTrang;
            result.TotalPages = (lstResult.Count() / soPhanTuTrenTrang) + 1;
            result.Items = lstResult.Skip((soTrang - 1) * soPhanTuTrenTrang).Take(soPhanTuTrenTrang);
            result.TotalCount = lstResult.Count();
            return new ResponseEntity(StatusCodeConstants.OK, result, MessageConstant.MESSAGE_SUCCESS_200);
        }
        [HttpGet("LayDanhSachPhimTheoNgay")]
        public async Task<ResponseEntity> LayDanhSachPhimTheoNgay(string maNhom = "GP01", string tenPhim = "", int soTrang = 1, int soPhanTuTrenTrang = 10, string tuNgay = "", string denNgay = "")
        {
            DateTime dtTuNgay = DateTimes.Now();
            DateTime dtDenNgay = DateTimes.Now();
            if (tuNgay != "")
            {
                try
                {
                    dtTuNgay = DateTimes.ConvertDate(tuNgay);
                }
                catch (Exception ex)
                {
                    return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Ngày không hợp lệ", MessageConstant.MESSAGE_ERROR_400);
                }
            }
            if (denNgay != "")
            {
                try
                {
                    dtDenNgay = DateTimes.ConvertDate(denNgay);
                }
                catch (Exception ex)
                {
                    return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Ngày không hợp lệ, Ngày có định dạng dd/MM/yyyy !", MessageConstant.MESSAGE_ERROR_400);
                }
            }
            bool ckNhom = db.Nhom.Any(n => n.MaNhom == maNhom);
            if (!ckNhom)
            {
                var response = await tbl.TBLoi(ThongBaoLoi.Loi500, "Nhóm người dùng không hợp lệ!");
                return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Nhóm người dùng không hợp lệ", MessageConstant.MESSAGE_ERROR_400); ;
            }
            tenPhim = LoaiBoKyTu.bestLower(tenPhim);
            IEnumerable<PhimVM> lstResult = db.Phim.Where(n => n.BiDanh.Contains(tenPhim) && n.MaNhom == maNhom && n.DaXoa != true && n.NgayKhoiChieu.Value >= dtTuNgay.Date && n.NgayKhoiChieu.Value <= dtDenNgay.Date).Select(n => new PhimVM { MaPhim = n.MaPhim, BiDanh = n.BiDanh, DanhGia = n.DanhGia, HinhAnh = DomainImage + n.HinhAnh, MaNhom = n.MaNhom, MoTa = n.MoTa, TenPhim = n.TenPhim, Trailer = n.Trailer, NgayKhoiChieu = n.NgayKhoiChieu, Hot = n.Hot, DangChieu = n.DangChieu, SapChieu = n.SapChieu });
            var model = PagingList.Create(lstResult, soPhanTuTrenTrang, soTrang);
            return new ResponseEntity(StatusCodeConstants.OK, model, MessageConstant.MESSAGE_SUCCESS_200);
        }
        public class test
        {
            public string maPhim = "1";
            public string tenPhim = "";
            public IFormFile hinhAnh;
        }

        //[Authorize(Roles = "QuanTri")]
        private object Convert([FromForm] IFormCollection form, object model)
        {
            Type type = typeof(PhimUpload);
            FieldInfo[] propertyInfos = type.GetFields();
            foreach (FieldInfo propertyInfo in propertyInfos)
            {
                var mykey = propertyInfo.Name;
                if (!string.IsNullOrEmpty(form[mykey]))
                {
                    try
                    {
                        string value = form[mykey];
                        propertyInfo.SetValue(model, value);
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }
            }
            return model;
        }
        [HttpPost("ThemPhimUploadHinh")]
        public async Task<ResponseEntity> ThemPhimUploadHinh([FromForm] IFormCollection frm)
        {
            try
            {
                PhimUpload model = new PhimUpload();
                model = (PhimUpload)Convert(frm, model);
                model.maNhom = model.maNhom.ToUpper();
                if (string.IsNullOrEmpty(model.maNhom))
                {
                    model.maNhom = "GP01";
                }
                if (Request.Form.Files.Count == 0 )
                {
                        return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Chưa chọn hình ảnh !", MessageConstant.MESSAGE_ERROR_500);
                }

                model.hinhAnh = Request.Form.Files[0];
                string request = Request.Form["tenPhim"]; ;
                bool ckb = db.Nhom.Any(n => n.MaNhom == model.maNhom);
                if (!ckb)
                {
                    return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Mã nhóm không hợp lệ!", MessageConstant.MESSAGE_ERROR_500);
                }
                string tenPhim = LoaiBoKyTu.bestLower(model.tenPhim);
                if (string.IsNullOrEmpty(tenPhim))
                {
                    return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Tên phim không hợp lệ!", MessageConstant.MESSAGE_ERROR_500);
                }
                var p = db.Phim.Where(n => n.BiDanh == model.biDanh);
                if (p.Count() > 1)
                {

                    return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Tên phim đã tồn tại!", MessageConstant.MESSAGE_ERROR_500);
                }
                Phim modelInsert = new Phim();
                modelInsert.BiDanh = LoaiBoKyTu.bestLower(model.tenPhim);
                modelInsert.DaXoa = false;
                modelInsert.MaPhim = 0;
                modelInsert.HinhAnh = LoaiBoKyTu.bestLower(model.tenPhim) + "_" + LoaiBoKyTu.bestLower(model.maNhom) + "." + model.hinhAnh.FileName.Split('.')[model.hinhAnh.FileName.Split('.').Length - 1];
                modelInsert.MoTa = model.moTa;
                modelInsert.TenPhim = model.tenPhim;
                modelInsert.Trailer = model.trailer;
                modelInsert.DanhGia = int.Parse(model.danhGia);
                modelInsert.Hot = Boolean.Parse(model.hot);
                modelInsert.SapChieu = Boolean.Parse(model.sapChieu);
                modelInsert.DangChieu = Boolean.Parse(model.dangChieu);
                DateTime temp;
                try
                {
                    try
                    {
                        modelInsert.NgayKhoiChieu = DateTimes.ConvertDate(model.ngayKhoiChieu);
                    }
                    catch (Exception ex)
                    {
                        return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Ngày chiếu không hợp lệ, Ngày chiếu phải có định dạng dd/MM/yyyy!", MessageConstant.MESSAGE_ERROR_500);
                    }
                }
                catch (Exception ex)
                {
                    return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Ngày khởi chiếu không hợp lệ, Ngày chiếu phải có định dạng dd/MM/yyyy !", MessageConstant.MESSAGE_ERROR_500);
                }

                if (!string.IsNullOrEmpty(modelInsert.Trailer))
                {
                    string newString = modelInsert.Trailer.Replace("https://www.youtube.com/embed/", "♥");
                    if (newString.Split('♥').Length == 0)
                    {
                        return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Link trailer không hợp lệ link trailer phải có định dạng: https://www.youtube.com/embed/[thamso]", MessageConstant.MESSAGE_ERROR_500);
                    }
                }
                db.Phim.Add(modelInsert);
                string kq = UploadHinhAnh(Request.Form.Files[0], modelInsert.TenPhim, model.maNhom);
                if (kq != "")
                {
                    return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, kq, MessageConstant.MESSAGE_ERROR_500);
                }
                modelInsert.MaNhom = model.maNhom.ToUpper();
                db.SaveChanges();
                return new ResponseEntity(StatusCodeConstants.OK, new PhimUploadResult { maPhim = modelInsert.MaPhim, tenPhim = modelInsert.TenPhim, dangChieu = (bool)modelInsert.DangChieu, sapChieu = (bool)modelInsert.SapChieu, hot = (bool)modelInsert.Hot, moTa = modelInsert.MoTa, ngayKhoiChieu = modelInsert.NgayKhoiChieu.Value, trailer = modelInsert.Trailer, danhGia = modelInsert.DanhGia.Value, maNhom = modelInsert.MaNhom, hinhAnh = DomainImage + modelInsert.HinhAnh }, MessageConstant.MESSAGE_SUCCESS_200);
            }
            catch (Exception ex)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "thuộc tính hinhAnh không đúng định dạng *.jpg, *.png, *.gif!", MessageConstant.MESSAGE_ERROR_400);
            }
        }

        [Authorize(Roles = "QuanTri")]
        [HttpPost("CapNhatPhimUpload")]
        public async Task<ResponseEntity> CapNhatPhimUpload([FromForm] IFormCollection frm)
        {
            PhimUpload model = new PhimUpload();
            model = (PhimUpload)Convert(frm, model);
            model.maPhim = int.Parse(frm["maPhim"]);
            model.maNhom = model.maNhom.ToUpper();
            if (string.IsNullOrEmpty(model.ngayKhoiChieu))
            {
                return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Ngày chiếu không hợp lệ, Ngày chiếu phải có định dạng dd/MM/yyyy !", MessageConstant.MESSAGE_ERROR_400);
            }
            model.biDanh = LoaiBoKyTu.bestLower(model.tenPhim);
            try
            {
                Phim phimUpdate = db.Phim.SingleOrDefault(n => n.MaPhim == model.maPhim);
                if (phimUpdate == null)
                {
                    return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Mã phim không tồn tại!", MessageConstant.MESSAGE_ERROR_500);
                }
                model.maNhom = model.maNhom.ToUpper();
                bool ckb = db.Nhom.Any(n => n.MaNhom == model.maNhom);
                if (!ckb)
                {
                    return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Mã nhóm không hợp lệ!", MessageConstant.MESSAGE_ERROR_500);
                }
                string tenPhim = LoaiBoKyTu.bestLower(model.tenPhim);
                if (string.IsNullOrEmpty(tenPhim))
                {
                    return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Tên phim không hợp lệ!", MessageConstant.MESSAGE_ERROR_500);
                }
                var p = db.Phim.Any(n => n.BiDanh == model.biDanh && n.MaPhim != model.maPhim);
                if (p)
                {
                    return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Tên phim đã tồn tại!", MessageConstant.MESSAGE_ERROR_500);
                }
                model.tenPhim = Request.Form["tenPhim"];
                phimUpdate.TenPhim = model.tenPhim;
                phimUpdate.BiDanh = LoaiBoKyTu.bestLower(model.tenPhim);
                phimUpdate.MoTa = model.moTa;
                phimUpdate.Trailer = model.trailer;
                phimUpdate.DanhGia = int.Parse(model.danhGia);
                phimUpdate.Hot = Boolean.Parse(model.hot);
                phimUpdate.SapChieu = Boolean.Parse(model.sapChieu);
                phimUpdate.DangChieu = Boolean.Parse(model.dangChieu);
                phimUpdate.MaNhom = model.maNhom;
                    if (Request.Form.Files.Count > 0) { 
                    phimUpdate.HinhAnh = LoaiBoKyTu.bestLower(phimUpdate.TenPhim) + "_" + LoaiBoKyTu.bestLower(phimUpdate.MaNhom) + "." + Request.Form.Files[0].FileName.Split('.')[Request.Form.Files[0].FileName.Split('.').Length - 1];
                        string kq = UploadHinhAnh(Request.Form.Files[0], phimUpdate.TenPhim, phimUpdate.MaNhom);
                        if (kq.Trim() != "")
                        {
                            return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, kq, MessageConstant.MESSAGE_ERROR_500);
                        }
                }
                DateTime temp;
                try
                {
                    try
                    {
                        phimUpdate.NgayKhoiChieu = DateTimes.ConvertDate(model.ngayKhoiChieu);
                    }
                    catch (Exception ex)
                    {
                        phimUpdate.NgayKhoiChieu = DateTime.Now;
                    }
                }
                catch (Exception ex)
                {
                    return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Ngày khởi chiếu không hợp lệ, Ngày chiếu phải có định dạng dd/MM/yyyy !", MessageConstant.MESSAGE_ERROR_400);
                }

                if (!string.IsNullOrEmpty(model.trailer))
                {
                    string newString = phimUpdate.Trailer.Replace("https://www.youtube.com/embed/", "♥");
                    if (newString.Split('♥').Length == 0)
                    {
                        return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Link trailer không hợp lệ link trailer phải có định dạng: https://www.youtube.com/embed/[thamso]", MessageConstant.MESSAGE_ERROR_500);
                    }
                }
                db.SaveChanges();
                return new ResponseEntity(StatusCodeConstants.OK, new PhimUploadResult { maPhim = phimUpdate.MaPhim, tenPhim = phimUpdate.TenPhim, dangChieu = (bool)phimUpdate.DangChieu, sapChieu = (bool)phimUpdate.SapChieu, hot = (bool)phimUpdate.Hot, moTa = phimUpdate.MoTa, ngayKhoiChieu = phimUpdate.NgayKhoiChieu.Value, trailer = phimUpdate.Trailer, danhGia = phimUpdate.DanhGia.Value, maNhom = phimUpdate.MaNhom, hinhAnh = DomainImage + phimUpdate.HinhAnh }, MessageConstant.MESSAGE_SUCCESS_200);
            }
            catch (Exception ex)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Dữ liệu không hợp lệ!", MessageConstant.MESSAGE_ERROR_500);
            }
        }

        private const int TenMegaBytes = 1024 * 1024;
        [HttpPost]
        public string UploadHinhAnh(IFormFile file, string tenPhim, string maNhom)
        {
            maNhom = maNhom.ToUpper();
            tenPhim = LoaiBoKyTu.bestLower(tenPhim);

            if (string.IsNullOrEmpty(tenPhim))
            {
                return "Tên phim không hợp lệ";

            }
            if (string.IsNullOrEmpty(maNhom) || !db.Nhom.Any(n => n.MaNhom == maNhom))
            {
                return "Mã nhóm không hợp lệ";
            }
            if (file.Length > TenMegaBytes)
            {
                return "Dung lượng file vượt quá 1 MB!";
            }
            if (file.ContentType == "image/png" || file.ContentType == "image/jpeg" || file.ContentType == "image/jpg" || file.ContentType == "image/gif")
            {
                try
                {
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hinhanh", tenPhim + "_" + LoaiBoKyTu.bestLower(maNhom) + "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1]);
                    var stream = new FileStream(path, FileMode.Create);
                    file.CopyTo(stream);

                    return "";

                }
                catch(Exception ex)
                { return "Upload file không thành công!";}
            }
            else
            {
                return "Định dạng file không hợp lệ!";
            }
        }
        [Authorize(Roles = "QuanTri")]
        [HttpDelete("XP")]
        public async Task<ResponseEntity> XP(int MaPhim)
        {
            bool ckbPhim = db.Phim.Any(n => n.MaPhim == MaPhim);
            if (!ckbPhim)
            {
                return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Mã phim không hợp lệ!", MessageConstant.BAD_REQUEST);
            }
            bool ckbLichChieu = db.LichChieu.Any(n => n.MaPhim == MaPhim);
            if (ckbLichChieu)
            {
                return new ResponseEntity(StatusCodeConstants.ERROR_SERVER, "Phim đã xếp lịch chiếu không thể xóa!", MessageConstant.MESSAGE_ERROR_500);
            }
            var listLichChieu = db.LichChieu.Where(n => n.MaPhim == MaPhim);
            db.LichChieu.RemoveRange(listLichChieu);
            db.SaveChanges();
            Phim p = db.Phim.SingleOrDefault(n => n.MaPhim == MaPhim);
            string hinhAnh = p.HinhAnh;
            db.Phim.Remove(p);
            db.SaveChanges();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hinhanh", hinhAnh);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            return new ResponseEntity(StatusCodeConstants.OK, "Xóa thành công!", MessageConstant.MESSAGE_SUCCESS_200);
        }
        [Authorize(Roles = "QuanTri")]
        [HttpDelete("XoaPhim")]
        public async Task<ResponseEntity> XoaPhim(int MaPhim)
        {

            bool ckbPhim = db.Phim.Any(n => n.MaPhim == MaPhim);
            if (!ckbPhim)
            {
                return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Mã phim không hợp lệ!", MessageConstant.BAD_REQUEST);
            }
            Phim p = db.Phim.SingleOrDefault(n => n.MaPhim == MaPhim);
            p.DaXoa = true;
            string hinhAnh = p.HinhAnh;
            db.SaveChanges();
            return new ResponseEntity(StatusCodeConstants.OK, "Xóa thành công!", MessageConstant.MESSAGE_SUCCESS_200);
        }

        [HttpGet("LayThongTinPhim")]
        public async Task<ResponseEntity> LayThongTinPhim(int MaPhim = 0)
        {
            if (MaPhim == 0 || !db.Phim.Any(n => n.MaPhim == MaPhim))
            {
                return new ResponseEntity(StatusCodeConstants.BAD_REQUEST, "Mã phim không hợp lệ!", MessageConstant.BAD_REQUEST);
            }
            Phim phim = db.Phim.Single(n => n.MaPhim == MaPhim);
            ChiTietPhimViewModel chiTietPhim = new ChiTietPhimViewModel();
            chiTietPhim.BiDanh = phim.BiDanh;
            chiTietPhim.DanhGia = phim.DanhGia;
            chiTietPhim.HinhAnh = DomainImage + phim.HinhAnh;
            chiTietPhim.MaNhom = phim.MaNhom;
            chiTietPhim.TenPhim = phim.TenPhim;
            chiTietPhim.MaPhim = phim.MaPhim;
            chiTietPhim.MoTa = phim.MoTa;
            chiTietPhim.Trailer = phim.Trailer;
            chiTietPhim.NgayKhoiChieu = phim.NgayKhoiChieu;
            chiTietPhim.Hot = phim.Hot;
            chiTietPhim.DangChieu = phim.DangChieu;
            chiTietPhim.SapChieu = phim.SapChieu;
            return new ResponseEntity(StatusCodeConstants.OK, chiTietPhim, MessageConstant.MESSAGE_SUCCESS_200);
        }
    }
}