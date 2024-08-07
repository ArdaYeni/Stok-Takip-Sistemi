﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Web.Mvc;
using System.Web.Security;
using StokTakipStajyer2.Models;
using System.Data.Entity;



namespace StokTakipStajyer2.Controllers
{
    public class HomeController : Controller
    {

        StokTakipDBEntities2 stokdata = new StokTakipDBEntities2();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult KullaniciBilgileri()
        {
            if (Session["ID"] != null)
            {
                int kullaniciId = Convert.ToInt32(Session["ID"]);
                
                var kullanici = stokdata.KULLANICI.Find(kullaniciId); 
                if (kullanici != null)
                {
                    return View(kullanici);
                }
            }
            return RedirectToAction("Giris", "Home");
        }


        [HttpGet]
        public ActionResult Giris()
        {
            return View();
        }


        [HttpPost]
        public ActionResult Giris(KULLANICI kul)
        {
            var kullanici = stokdata.KULLANICI.FirstOrDefault(k => k.KUL_USERNAME == kul.KUL_USERNAME && k.KUL_SIFRE == kul.KUL_SIFRE);
            if (kullanici != null)
            {
                Session["ID"] = kullanici.KUL_ID;
                Session["KullaniciAdi"] = kullanici.KUL_AD;
                Session["KullaniciTipi"] = kullanici.KUL_TIP;

                return RedirectToAction("KullaniciBilgileri", "Home");
            }
            else
            {
                ViewBag.Mesaj = "Geçersiz kullanıcı adı veya şifre";
                return View();
            }
        }


        [HttpGet]
        public ActionResult KullaniciEkle()
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }
            var model = new KULLANICI();

            return View(model);
        }

        [HttpPost]
        public ActionResult KullaniciEkle(KULLANICI kullanici)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            if (ModelState.IsValid)
            {
                var model = new KULLANICI()
                {
                    KUL_USERNAME = kullanici.KUL_USERNAME,
                    KUL_AD = kullanici.KUL_AD,
                    KUL_SOYAD = kullanici.KUL_SOYAD,
                    KUL_SIFRE = kullanici.KUL_SIFRE,
                    KUL_TIP = kullanici.KUL_TIP,
                    STATU = kullanici.STATU,
                    OLUSTURAN_KULLANICI = Convert.ToInt32(Session["ID"]),
                    OLUSTURMA_TARIHI = DateTime.Now,
                    GUNCELLEME_TARIHI = DateTime.Now,
                    GUNCELLEYEN_KULLANICI = Convert.ToInt32(Session["ID"]),
                };

                stokdata.KULLANICI.Add(model);
                stokdata.SaveChanges();
                TempData["SuccessMessage"] = "Kullanıcı başarıyla eklendi.";

                return RedirectToAction("KullaniciListele");
            }

            return View(kullanici);
        }

        [HttpGet]
        public ActionResult KullaniciListele(string searchString = null)
        {
            if (Session["ID"] == null)
            {
                return RedirectToAction("Giris");
            }

            var kullanicilar = from k in stokdata.KULLANICI select k;

            if (!String.IsNullOrEmpty(searchString))
            {
                kullanicilar = kullanicilar.Where(s => s.KUL_USERNAME.Contains(searchString) ||
                                                       s.KUL_AD.Contains(searchString) ||
                                                       s.KUL_SOYAD.Contains(searchString) ||
                                                       s.KUL_ID.ToString().Contains(searchString) ||
                                                       s.KUL_TIP.ToString().Contains(searchString) ||
                                                       (s.STATU == true ? "true" : "false").Contains(searchString));
            }

            return View(kullanicilar.ToList());
        }

        public ActionResult ExportKullaniciToExcel()
        {
            var kullanicilar = stokdata.KULLANICI.ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Kullanıcı Listesi");


                worksheet.Cells[1, 1].Value = "Kullanıcı ID";
                worksheet.Cells[1, 2].Value = "Kullanıcı Adı";
                worksheet.Cells[1, 3].Value = "Soyadı";
                worksheet.Cells[1, 4].Value = "Kullanıcı Tipi";
                worksheet.Cells[1, 5].Value = "Statü";
                worksheet.Cells[1, 6].Value = "Oluşturan Kullanıcı";
                worksheet.Cells[1, 7].Value = "Oluşturma Tarihi";
                worksheet.Cells[1, 8].Value = "Güncelleyen Kullanıcı";
                worksheet.Cells[1, 9].Value = "Güncelleme Tarihi";


                for (int i = 0; i < kullanicilar.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cells[row, 1].Value = kullanicilar[i].KUL_ID;
                    worksheet.Cells[row, 2].Value = kullanicilar[i].KUL_AD;
                    worksheet.Cells[row, 3].Value = kullanicilar[i].KUL_SOYAD;
                    worksheet.Cells[row, 4].Value = kullanicilar[i].KUL_TIP;
                    worksheet.Cells[row, 5].Value = kullanicilar[i].STATU == true ? "True" : "False";
                    worksheet.Cells[row, 6].Value = kullanicilar[i].OLUSTURAN_KULLANICI;
                    worksheet.Cells[row, 7].Value = kullanicilar[i].OLUSTURMA_TARIHI.HasValue ? kullanicilar[i].OLUSTURMA_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                    worksheet.Cells[row, 8].Value = kullanicilar[i].GUNCELLEYEN_KULLANICI;
                    worksheet.Cells[row, 9].Value = kullanicilar[i].GUNCELLEME_TARIHI.HasValue ? kullanicilar[i].GUNCELLEME_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                }


                using (var range = worksheet.Cells[1, 1, 1, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    range.Style.Font.Color.SetColor(Color.White);
                }


                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                var content = stream.ToArray();

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "KullaniciListesi.xlsx");
            }
        }


        [HttpGet]
        public ActionResult KullaniciSil(int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            var kullanici = stokdata.KULLANICI.Find(id);
            if (kullanici == null)
            {
                return HttpNotFound();
            }

            stokdata.KULLANICI.Remove(kullanici);
            stokdata.SaveChanges();
            TempData["SuccessMessage"] = "Kullanıcı başarıyla silindi.";
            return RedirectToAction("KullaniciListele");
        }

        [HttpGet]
        public ActionResult KullaniciGuncelle(int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            var kullanici = stokdata.KULLANICI.Find(id);
            if (kullanici == null)
            {
                return HttpNotFound();
            }

            return View(kullanici);
        }

        [HttpPost]
        public ActionResult KullaniciGuncelle(KULLANICI kullanici)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }
            if (ModelState.IsValid)
            {
                var updateKullanici = stokdata.KULLANICI.Find(kullanici.KUL_ID);

                if (updateKullanici != null)
                {
                    updateKullanici.KUL_USERNAME = kullanici.KUL_USERNAME;
                    updateKullanici.KUL_AD = kullanici.KUL_AD;
                    updateKullanici.KUL_SOYAD = kullanici.KUL_SOYAD;
                    updateKullanici.KUL_SIFRE = kullanici.KUL_SIFRE;
                    updateKullanici.KUL_TIP = kullanici.KUL_TIP;
                    updateKullanici.STATU = kullanici.STATU;

                    stokdata.SaveChanges();
                    TempData["SuccessMessage"] = "Kullanıcı başarıyla güncellendi.";
                    return RedirectToAction("KullaniciListele");
                }
            }

            return View(kullanici);
        }


        [HttpGet]
        public ActionResult DepoEkle()
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            var depoModel = new DEPO();
            return View(depoModel);
        }

        [HttpPost]
        public ActionResult DepoEkle(DEPO depo)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            if (ModelState.IsValid)
            {
                var depoModel = new DEPO()
                {
                    DEPO_ADI = depo.DEPO_ADI,
                    STATU = depo.STATU,
                    OLUSTURAN_KULLANICI = Convert.ToInt32(Session["ID"]),
                    OLUSTURMA_TARIHI = DateTime.Now,
                    GUNCELLEME_TARIHI = DateTime.Now,
                    GUNCELLEYEN_KULLANICI = Convert.ToInt32(Session["ID"]),
                };

                stokdata.DEPO.Add(depoModel);
                stokdata.SaveChanges();
                TempData["SuccessMessage"] = "Depo başarıyla eklendi.";
                return RedirectToAction("DepoListele");
            }

            return View(depo);
        }



        [HttpGet]
        public ActionResult DepoListele(string searchString = null)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            var depolar = from d in stokdata.DEPO select d;

            if (!String.IsNullOrEmpty(searchString))
            {
                depolar = depolar.Where(s => s.DEPO_ADI.Contains(searchString) ||
                                             s.STATU.ToString().Contains(searchString));
            }

            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View(depolar.ToList());
        }

        public ActionResult ExportDepoToExcel()
        {
            var depolar = stokdata.DEPO.ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Depo Listesi");

                worksheet.Cells[1, 1].Value = "Depo ID";
                worksheet.Cells[1, 2].Value = "Depo Adı";
                worksheet.Cells[1, 3].Value = "Statü";
                worksheet.Cells[1, 4].Value = "Oluşturan Kullanıcı";
                worksheet.Cells[1, 5].Value = "Oluşturma Tarihi";
                worksheet.Cells[1, 6].Value = "Güncelleyen Kullanıcı";
                worksheet.Cells[1, 7].Value = "Güncelleme Tarihi";


                for (int i = 0; i < depolar.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cells[row, 1].Value = depolar[i].DEPO_ID;
                    worksheet.Cells[row, 2].Value = depolar[i].DEPO_ADI;
                    worksheet.Cells[row, 3].Value = depolar[i].STATU == "true" ? "True" : "False"; //problematic
                    worksheet.Cells[row, 4].Value = depolar[i].OLUSTURAN_KULLANICI;
                    worksheet.Cells[row, 5].Value = depolar[i].OLUSTURMA_TARIHI.HasValue ? depolar[i].OLUSTURMA_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                    worksheet.Cells[row, 6].Value = depolar[i].GUNCELLEYEN_KULLANICI;
                    worksheet.Cells[row, 7].Value = depolar[i].GUNCELLEME_TARIHI.HasValue ? depolar[i].GUNCELLEME_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                }


                using (var range = worksheet.Cells[1, 1, 1, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    range.Style.Font.Color.SetColor(Color.White);
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                var content = stream.ToArray();

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DepoListesi.xlsx");
            }
        }


        [HttpGet]
        public ActionResult DepoSil(int id)
        {
            if (Session["ID"] == null)
            {
                return RedirectToAction("Giris");
            }

            var depo = stokdata.DEPO.Find(id);
            if (depo == null)
            {
                return HttpNotFound();
            }

            return View(depo);
        }

        [HttpPost, ActionName("DepoSil")]
        public ActionResult DepoSilConfirmed(int id)
        {
            if (Session["ID"] == null)
            {
                return RedirectToAction("Giris");
            }

            var depo = stokdata.DEPO.Find(id);
            if (depo != null)
            {
                var hasReferences = stokdata.DEPO_ESLESTIRME.Any(e => e.DEPO_ID == id);

                if (hasReferences)
                {
                    depo.STATU = "false"; 
                    stokdata.Entry(depo).State = System.Data.Entity.EntityState.Modified;
                    TempData["SuccessMessage"] = "Depo başarıyla pasif duruma çekildi.";
                }
                else
                {
                    stokdata.DEPO.Remove(depo);
                    TempData["SuccessMessage"] = "Depo başarıyla silindi.";
                }

                stokdata.SaveChanges();
            }

            return RedirectToAction("DepoListele");
        }


        [HttpGet]
        public ActionResult DepoGuncelle(int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            var depo = stokdata.DEPO.Find(id);
            if (depo == null)
            {
                return HttpNotFound();
            }

            return View(depo);
        }

        [HttpPost]
        public ActionResult DepoGuncelle(DEPO depo, int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }
            if (ModelState.IsValid)
            {
                var updateDepo = stokdata.DEPO.Find(id);

                if (updateDepo != null)
                {
                    updateDepo.DEPO_ADI = depo.DEPO_ADI;
                    updateDepo.STATU = depo.STATU;
                    updateDepo.GUNCELLEME_TARIHI = DateTime.Now; 
                    updateDepo.GUNCELLEYEN_KULLANICI = Convert.ToInt32(Session["ID"]); 


                    stokdata.SaveChanges();
                    TempData["SuccessMessage"] = "Depo başarıyla güncellendi.";
                    return RedirectToAction("DepoListele");
                }
            }

            return View(depo);
        }



        [HttpGet]
        public ActionResult AltDepoEkle()
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }
            var altdepoModel = new ALT_DEPO();
            ViewBag.Depolar = new SelectList(stokdata.DEPO.ToList(), "DEPO_ID", "DEPO_ADI");

            return View(altdepoModel);
        }

        [HttpPost]
        public ActionResult AltDepoEkle(ALT_DEPO depo, int selectedDepoId)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            if (ModelState.IsValid)
            {
                var depoModel = new ALT_DEPO()
                {
                    ALT_DEPO_ADI = depo.ALT_DEPO_ADI,
                    STATU = depo.STATU,
                    OLUSTURAN_KULLANICI = Convert.ToInt32(Session["ID"]),
                    OLUSTURMA_TARIHI = DateTime.Now,
                    GUNCELLEME_TARIHI = DateTime.Now,
                    GUNCELLEYEN_KULLANICI = Convert.ToInt32(Session["ID"]),
                };

                stokdata.ALT_DEPO.Add(depoModel);
                stokdata.SaveChanges();

                var depoEslestirme = new DEPO_ESLESTIRME()
                {
                    DEPO_ID = selectedDepoId,
                    ALT_DEPO_ID = depoModel.ALT_DEPO_ID,
                    STATU = true,
                    OLUSTURAN_KULLANICI = Convert.ToInt32(Session["ID"]),
                    OLUSTURMA_TARIHI = DateTime.Now,
                    GUNCELLEME_TARIHI = DateTime.Now,
                    GUNCELLEYEN_KULLANICI = Convert.ToInt32(Session["ID"]),
                };

                stokdata.DEPO_ESLESTIRME.Add(depoEslestirme);
                stokdata.SaveChanges();

                TempData["SuccessMessage"] = "Alt depo başarıyla eklendi.";
                return RedirectToAction("AltDepoListele");
            }

            ViewBag.Depolar = new SelectList(stokdata.DEPO.ToList(), "DEPO_ID", "DEPO_ADI");

            return View(depo);
        }



        [HttpGet]
        public ActionResult AltDepoListele(string searchString = null)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            var altDepolar = from ad in stokdata.ALT_DEPO select ad;

            if (!String.IsNullOrEmpty(searchString))
            {
                altDepolar = altDepolar.Where(s => s.ALT_DEPO_ADI.Contains(searchString) ||
                                                   s.STATU.ToString().Contains(searchString));
            }

            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View(altDepolar.ToList());
        }

        public ActionResult ExportAltDepoToExcel()
        {
            var altDepolar = stokdata.ALT_DEPO.ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Alt Depo Listesi");


                worksheet.Cells[1, 1].Value = "Alt Depo ID";
                worksheet.Cells[1, 2].Value = "Alt Depo Adı";
                worksheet.Cells[1, 3].Value = "Statü";
                worksheet.Cells[1, 4].Value = "Oluşturan Kullanıcı";
                worksheet.Cells[1, 5].Value = "Oluşturma Tarihi";
                worksheet.Cells[1, 6].Value = "Güncelleyen Kullanıcı";
                worksheet.Cells[1, 7].Value = "Güncelleme Tarihi";


                for (int i = 0; i < altDepolar.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cells[row, 1].Value = altDepolar[i].ALT_DEPO_ID;
                    worksheet.Cells[row, 2].Value = altDepolar[i].ALT_DEPO_ADI;
                    worksheet.Cells[row, 3].Value = altDepolar[i].STATU == true ? "True" : "False";
                    worksheet.Cells[row, 4].Value = altDepolar[i].OLUSTURAN_KULLANICI;
                    worksheet.Cells[row, 5].Value = altDepolar[i].OLUSTURMA_TARIHI.HasValue ? altDepolar[i].OLUSTURMA_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                    worksheet.Cells[row, 6].Value = altDepolar[i].GUNCELLEYEN_KULLANICI;
                    worksheet.Cells[row, 7].Value = altDepolar[i].GUNCELLEME_TARIHI.HasValue ? altDepolar[i].GUNCELLEME_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                }

                using (var range = worksheet.Cells[1, 1, 1, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    range.Style.Font.Color.SetColor(Color.White);
                }


                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                var content = stream.ToArray();

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AltDepoListesi.xlsx");
            }
        }


        [HttpGet]
        public ActionResult AltDepoSil(int id)
        {
            if (Session["ID"] == null)
            {
                return RedirectToAction("Giris");
            }

            var altdepo = stokdata.ALT_DEPO.Find(id);
            if (altdepo == null)
            {
                return HttpNotFound();
            }

            return View(altdepo);
        }

        [HttpPost, ActionName("AltDepoSil")]
        public ActionResult AltDepoSilConfirmed(int id)
        {
            if (Session["ID"] == null)
            {
                return RedirectToAction("Giris");
            }

            var altdepo = stokdata.ALT_DEPO.Find(id);
            if (altdepo != null)
            {
                var hasReferences = stokdata.DEPO_ESLESTIRME.Any(e => e.ALT_DEPO_ID == id);

                if (hasReferences)
                {
                    altdepo.STATU = false;
                    stokdata.Entry(altdepo).State = System.Data.Entity.EntityState.Modified;
                    TempData["SuccessMessage"] = "Alt Depo başarıyla pasif duruma çekildi.";
                }
                else
                {
                    stokdata.ALT_DEPO.Remove(altdepo);
                    TempData["SuccessMessage"] = "Alt Depo başarıyla silindi.";
                }

                stokdata.SaveChanges();
            }

            return RedirectToAction("AltDepoListele");
        }


        [HttpGet]
        public ActionResult AltDepoGuncelle(int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            var depo = stokdata.ALT_DEPO.Find(id);
            if (depo == null)
            {
                return HttpNotFound();
            }

            return View(depo);
        }

        [HttpPost]
        public ActionResult AltDepoGuncelle(ALT_DEPO depo)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }
            if (ModelState.IsValid)
            {
                var updateDepo = stokdata.ALT_DEPO.Find(depo.ALT_DEPO_ID);

                if (updateDepo != null)
                {
                    updateDepo.ALT_DEPO_ADI = depo.ALT_DEPO_ADI;
                    updateDepo.STATU = depo.STATU;

                    stokdata.SaveChanges();
                    TempData["SuccessMessage"] = "Alt depo başarıyla güncellendi.";
                    return RedirectToAction("AltDepoListele");
                }
            }

            return View(depo);
        }


        [HttpGet]
        public ActionResult DepoAltdepoGuncelle(int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            var depoEsleme = stokdata.DEPO_ESLESTIRME.Find(id);
            if (depoEsleme == null)
            {
                return HttpNotFound();
            }

            ViewBag.AltDepolar = new SelectList(stokdata.ALT_DEPO.ToList(), "ALT_DEPO_ID", "ALT_DEPO_ADI", depoEsleme.ALT_DEPO_ID);
            ViewBag.Depolar = new SelectList(stokdata.DEPO.ToList(), "DEPO_ID", "DEPO_ADI", depoEsleme.DEPO_ID);

            return View(depoEsleme);
        }

        [HttpPost]
        public ActionResult DepoAltdepoGuncelle(DEPO_ESLESTIRME model)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            if (ModelState.IsValid)
            {
                var depoEsleme = stokdata.DEPO_ESLESTIRME.Find(model.DEPO_ESLESTIRME_ID);
                if (depoEsleme != null)
                {
                    depoEsleme.DEPO_ID = model.DEPO_ID;
                    depoEsleme.ALT_DEPO_ID = model.ALT_DEPO_ID;
                    depoEsleme.STATU = model.STATU;
                    depoEsleme.GUNCELLEYEN_KULLANICI = Convert.ToInt32(Session["ID"]);
                    depoEsleme.GUNCELLEME_TARIHI = DateTime.Now;

                    stokdata.SaveChanges();
                    TempData["SuccessMessage"] = "Depo ve Alt Depo eşleştirmesi başarıyla güncellendi.";

                    return RedirectToAction("DepoAltdepoListele");
                }
            }

            ViewBag.AltDepolar = new SelectList(stokdata.ALT_DEPO.ToList(), "ALT_DEPO_ID", "ALT_DEPO_ADI", model.ALT_DEPO_ID);
            ViewBag.Depolar = new SelectList(stokdata.DEPO.ToList(), "DEPO_ID", "DEPO_ADI", model.DEPO_ID);

            return View(model);
        }


        [HttpGet]
        public ActionResult DepoAltdepoListele(string searchString = null)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            var eslestirmeler = from e in stokdata.DEPO_ESLESTIRME select e;

            if (!String.IsNullOrEmpty(searchString))
            {
                eslestirmeler = eslestirmeler.Where(e => e.DEPO.DEPO_ADI.Contains(searchString) ||
                                                         e.ALT_DEPO.ALT_DEPO_ADI.Contains(searchString));
            }

            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View(eslestirmeler.ToList());
        }

        public ActionResult DepoAltdepoSil(int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            var altdepo = stokdata.DEPO_ESLESTIRME.Find(id);
            if (altdepo == null)
            {
                return HttpNotFound();
            }

            try
            {
                stokdata.DEPO_ESLESTIRME.Remove(altdepo);
                stokdata.SaveChanges();
                TempData["SuccessMessage"] = "Alt depo başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Alt depo silinirken bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction("DepoAltdepoListele");
        }


        [HttpGet]
        public ActionResult StokEkle()
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            ViewBag.OlcuBirimleri = new SelectList(stokdata.OLCU_BIRIM.ToList(), "OLCUBIRIM_ID", "OLCUBIRIM_ADI");

            return View(new STOK());
        }

        [HttpPost]
        public ActionResult StokEkle(STOK stok)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            if (ModelState.IsValid)
            {
                stok.OLUSTURMA_TARIHI = DateTime.Now;
                stok.OLUSTURAN_KULLANICI = Convert.ToInt32(Session["ID"]);
                stok.KAYIT_TARIHI = true;
                stok.GUNCELLEYEN_KULLANICI = Convert.ToInt32(Session["ID"]);
                stok.GUNCELLEME_TARIHI = DateTime.Now;

                stokdata.STOK.Add(stok);
                stokdata.SaveChanges();
                TempData["SuccessMessage"] = "Stok başarıyla eklendi.";

                return RedirectToAction("StokListele");
            }

            ViewBag.OlcuBirimleri = new SelectList(stokdata.OLCU_BIRIM.ToList(), "OLCUBIRIM_ID", "OLCUBIRIM_ADI");
            return View(stok);
        }


        [HttpGet]
        public ActionResult StokGuncelle(int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            var stok = stokdata.STOK.Include(s => s.OLCU_BIRIM).FirstOrDefault(s => s.STOK_ID == id);
            if (stok == null)
            {
                return HttpNotFound();
            }

            ViewBag.OlcuBirimleri = new SelectList(stokdata.OLCU_BIRIM.ToList(), "OLCUBIRIM_ID", "OLCUBIRIM_ADI", stok.STOK_OLCUBIRIM);

            return View(stok);
        }

        [HttpPost]
        public ActionResult StokGuncelle(STOK stok)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            if (ModelState.IsValid)
            {
                var updateStok = stokdata.STOK.Find(stok.STOK_ID);
                if (updateStok != null)
                {
                    updateStok.STOK_AD = stok.STOK_AD;
                    updateStok.STOK_DETAY = stok.STOK_DETAY;
                    updateStok.STOK_MARKA = stok.STOK_MARKA;
                    updateStok.STOK_OLCUBIRIM = stok.STOK_OLCUBIRIM;
                    updateStok.MIN_MIKTAR = stok.MIN_MIKTAR;
                    updateStok.STATU = stok.STATU;
                    updateStok.GUNCELLEYEN_KULLANICI = Convert.ToInt32(Session["ID"]);
                    updateStok.GUNCELLEME_TARIHI = DateTime.Now;

                    stokdata.SaveChanges();
                    TempData["SuccessMessage"] = "Stok başarıyla güncellendi.";

                    return RedirectToAction("StokListele");
                }
            }

            ViewBag.OlcuBirimleri = new SelectList(stokdata.OLCU_BIRIM.ToList(), "OLCUBIRIM_ID", "OLCUBIRIM_ADI", stok.STOK_OLCUBIRIM);
            return View(stok);
        }

        [HttpGet]
        public ActionResult StokListele(string searchString = null)
        {
            if (Session["ID"] == null)
            {
                return RedirectToAction("Giris");
            }

            var stoklar = from k in stokdata.STOK select k;

            if (!String.IsNullOrEmpty(searchString))
            {
                stoklar = stoklar.Where(s => s.STOK_AD.Contains(searchString) ||
                                             s.STOK_DETAY.Contains(searchString) ||
                                             (s.STOK_DURUM != null && s.STOK_DURUM.ToString().Contains(searchString)) ||
                                             s.STOK_MARKA.Contains(searchString) ||
                                             s.MIN_MIKTAR.ToString().Contains(searchString) ||
                                             (s.STATU == true ? "true" : "false").Contains(searchString));
            }

            return View(stoklar.ToList());
        }


        public ActionResult ExportStokToExcel()
        {
            var stoklar = stokdata.STOK.ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Stok Listesi");


                worksheet.Cells[1, 1].Value = "Stok ID";
                worksheet.Cells[1, 2].Value = "Ad";
                worksheet.Cells[1, 3].Value = "Ölçü Birimi";
                worksheet.Cells[1, 4].Value = "Marka";
                worksheet.Cells[1, 5].Value = "Detay";
                worksheet.Cells[1, 6].Value = "Minimum Miktar";
                worksheet.Cells[1, 7].Value = "Statü";
                worksheet.Cells[1, 8].Value = "Oluşturan Kullanıcı";
                worksheet.Cells[1, 9].Value = "Oluşturma Tarihi";
                worksheet.Cells[1, 10].Value = "Güncelleyen Kullanıcı";
                worksheet.Cells[1, 11].Value = "Güncelleme Tarihi";


                for (int i = 0; i < stoklar.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cells[row, 1].Value = stoklar[i].STOK_ID;
                    worksheet.Cells[row, 2].Value = stoklar[i].STOK_AD;
                    worksheet.Cells[row, 3].Value = stoklar[i].OLCU_BIRIM?.OLCUBIRIM_ADI;
                    worksheet.Cells[row, 4].Value = stoklar[i].STOK_MARKA;
                    worksheet.Cells[row, 5].Value = stoklar[i].STOK_DETAY;
                    worksheet.Cells[row, 6].Value = stoklar[i].MIN_MIKTAR;
                    worksheet.Cells[row, 7].Value = stoklar[i].STATU == true ? "True" : "False";
                    worksheet.Cells[row, 8].Value = stoklar[i].OLUSTURAN_KULLANICI;
                    worksheet.Cells[row, 9].Value = stoklar[i].OLUSTURMA_TARIHI.HasValue ? stoklar[i].OLUSTURMA_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                    worksheet.Cells[row, 10].Value = stoklar[i].GUNCELLEYEN_KULLANICI;
                    worksheet.Cells[row, 11].Value = stoklar[i].GUNCELLEME_TARIHI.HasValue ? stoklar[i].GUNCELLEME_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                }


                using (var range = worksheet.Cells[1, 1, 1, 11])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    range.Style.Font.Color.SetColor(Color.White);
                }


                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                var content = stream.ToArray();

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StokListesi.xlsx");
            }
        }


        public ActionResult StokSil(int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 1)
            {
                return RedirectToAction("Giris");
            }

            var stok = stokdata.STOK.Find(id);
            if (stok == null)
            {
                return HttpNotFound();
            }

            var ilgiliStokHareketler = stokdata.STOK_HAREKET.Where(sh => sh.STOK_ID == id).ToList();
            foreach (var stokHareket in ilgiliStokHareketler)
            {
                stokdata.STOK_HAREKET.Remove(stokHareket);
            }

            stokdata.STOK.Remove(stok);
            stokdata.SaveChanges();
            TempData["SuccessMessage"] = "Stok başarıyla silindi.";

            return RedirectToAction("StokListele");
        }



        [HttpGet]
        public ActionResult StokHareketEkle()
        {
            var stoklar = stokdata.STOK.ToList();
            ViewBag.stoklar = new SelectList(stoklar, "STOK_ID", "STOK_AD");

            var eslesmeler = stokdata.DEPO_ESLESTIRME.ToList();
            ViewBag.eslesmeler = new SelectList(eslesmeler, "DEPO_ESLESTIRME_ID", "DEPO_ESLESTIRME_ID");

            var sorumlular = stokdata.SORUMLU.ToList();
            ViewBag.sorumlular = new SelectList(sorumlular, "SORUMLU_ID", "SORUMLU_ADI");

            var harekettip = stokdata.HAREKET_TIP.ToList();
            ViewBag.harekettip = new SelectList(harekettip, "HAREKET_TIP_ID", "HAREKET_TIP_ADI");

            return View();


        }

        [HttpPost]
        public ActionResult StokHareketEkle(STOK_HAREKET stokHareket)
        {

            if (Session["ID"] == null)
            {
                return RedirectToAction("Giris");
            }

            if (ModelState.IsValid)
            {
                var newHareket = new STOK_HAREKET
                {
                    GUNCELLEYEN_KULLANICI = Convert.ToInt32(Session["KulID"]),
                    GUNCELLEME_TARIHI = DateTime.Now,
                    OLUSTURAN_KULLANICI = Convert.ToInt32(Session["KulID"]),
                    OLUSTURMA_TARIHI = DateTime.Now,
                    STOK_ID = stokHareket.STOK_ID,
                    DEPO_ESLESTIRME_ID = stokHareket.DEPO_ESLESTIRME_ID,
                    SORUMLU_ID = stokHareket.SORUMLU_ID,
                    HAREKET_TIP = stokHareket.HAREKET_TIP,
                    ACIKLAMA = stokHareket.ACIKLAMA,
                    HAREKET_MIKTAR = stokHareket.HAREKET_MIKTAR = Decimal.Parse(stokHareket.HAREKET_MIKTAR.ToString().Replace('.', ',')),
                    HAREKET_TARIHI = stokHareket.HAREKET_TARIHI,
                };

                stokdata.STOK_HAREKET.Add(newHareket);
                stokdata.SaveChanges();

                TempData["SuccessMessage"] = "Stok Hareketi başarıyla eklendi.";
                return RedirectToAction("StokHareketListele");
            }

            return View();
        }


        public ActionResult StokHareketListele(string searchString)
        {
            var stokHareketleri = stokdata.STOK_HAREKET
                .Include(s => s.DEPO_ESLESTIRME)
                .Include(s => s.SORUMLU);

            if (!String.IsNullOrEmpty(searchString))
            {
                stokHareketleri = stokHareketleri.Where(s =>
                    (s.DEPO_ESLESTIRME != null && s.DEPO_ESLESTIRME.DEPO_ID.HasValue && s.DEPO_ESLESTIRME.DEPO_ID.Value.ToString().Contains(searchString))
                    || (s.HAREKET_TIP1 != null && s.HAREKET_TIP1.HAREKET_TIP_ADI != null && s.HAREKET_TIP1.HAREKET_TIP_ADI.Contains(searchString))
                    || (s.SORUMLU != null && s.SORUMLU.SORUMLU_ADI != null && s.SORUMLU.SORUMLU_ADI.Contains(searchString)));
            }

            var stokHareketListesi = stokHareketleri.ToList();

            ViewBag.SuccessMessage = TempData["SuccessMessage"];

            return View(stokHareketListesi);
        }



        public ActionResult ExportStokHareketToExcel()
        {
            var stoklar = stokdata.STOK_HAREKET.ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Stok Hareket Listesi");


                worksheet.Cells[1, 1].Value = "Hareket ID";
                worksheet.Cells[1, 2].Value = "Stok Adı";
                worksheet.Cells[1, 3].Value = "Depo Adı";
                worksheet.Cells[1, 4].Value = "Sorumlu Adı";
                worksheet.Cells[1, 5].Value = "Hareket Tipi";
                worksheet.Cells[1, 6].Value = "Açıklama";
                worksheet.Cells[1, 7].Value = "Hareket Miktarı";
                worksheet.Cells[1, 8].Value = "Hareket Tarihi";
                worksheet.Cells[1, 8].Value = "Oluşturan Kullanıcı";
                worksheet.Cells[1, 9].Value = "Oluşturma Tarihi";
                worksheet.Cells[1, 10].Value = "Güncelleyen Kullanıcı";
                worksheet.Cells[1, 11].Value = "Güncelleme Tarihi";


                for (int i = 0; i < stoklar.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cells[row, 1].Value = stoklar[i].HAREKET_ID;
                    worksheet.Cells[row, 2].Value = stoklar[i].STOK_ID;
                    worksheet.Cells[row, 3].Value = stoklar[i].DEPO_ESLESTIRME.DEPO_ID;
                    worksheet.Cells[row, 4].Value = stoklar[i].SORUMLU.SORUMLU_ADI;
                    worksheet.Cells[row, 5].Value = stoklar[i].HAREKET_TIP;
                    worksheet.Cells[row, 6].Value = stoklar[i].ACIKLAMA;
                    worksheet.Cells[row, 7].Value = stoklar[i].HAREKET_MIKTAR;
                    worksheet.Cells[row, 8].Value = stoklar[i].HAREKET_TARIHI;
                    worksheet.Cells[row, 9].Value = stoklar[i].OLUSTURAN_KULLANICI;
                    worksheet.Cells[row, 10].Value = stoklar[i].OLUSTURMA_TARIHI.HasValue ? stoklar[i].OLUSTURMA_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                    worksheet.Cells[row, 11].Value = stoklar[i].GUNCELLEYEN_KULLANICI;
                    worksheet.Cells[row, 12].Value = stoklar[i].GUNCELLEME_TARIHI.HasValue ? stoklar[i].GUNCELLEME_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                }


                using (var range = worksheet.Cells[1, 1, 1, 11])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    range.Style.Font.Color.SetColor(Color.White);
                }


                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                var content = stream.ToArray();

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StokHareketListesi.xlsx");
            }
        }

        [HttpGet]
        public ActionResult StokHareketSil(int id)
        {
            if (Session["ID"] == null)
            {
                return RedirectToAction("Giris");
            }

            var stokHareket = stokdata.STOK_HAREKET.Find(id);
            if (stokHareket == null)
            {
                return HttpNotFound();
            }

            return View(stokHareket);
        }


        [HttpPost, ActionName("StokHareketSil")]
        public ActionResult StokHareketSilConfirmed(int id)
        {
            if (Session["ID"] == null)
            {
                return RedirectToAction("Giris");
            }

            var stokHareket = stokdata.STOK_HAREKET.Find(id);
            if (stokHareket != null)
            {
                var hasReferences = stokdata.DEPO_ESLESTIRME.Any(e => e.DEPO_ID == id);

                if (hasReferences)
                {
                    stokdata.Entry(stokHareket).State = System.Data.Entity.EntityState.Modified;
                    TempData["SuccessMessage"] = "Stok hareketi başarıyla pasif duruma çekildi.";
                }
                else
                {
                    stokdata.STOK_HAREKET.Remove(stokHareket);
                    TempData["SuccessMessage"] = "Stok hareketi başarıyla silindi.";
                }

                stokdata.SaveChanges();
            }

            return RedirectToAction("StokHareketListele");
        }


        [HttpGet]
        public ActionResult StokHareketiGuncelle(int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 2)
            {
                return RedirectToAction("Giris");
            }

            var stokHareket = stokdata.STOK_HAREKET.Find(id);
            if (stokHareket == null)
            {
                return HttpNotFound();
            }

            ViewBag.Depolar = new SelectList(stokdata.DEPO, "DEPO_ID", "DEPO_ADI", stokHareket.DEPO_ESLESTIRME.DEPO_ID);

            return View(stokHareket);
        }

        [HttpPost]
        public ActionResult StokHareketiGuncelle(STOK_HAREKET stokHareket)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 2)
            {
                return RedirectToAction("Giris");
            }
            if (ModelState.IsValid)
            {
                var updateHareket = stokdata.STOK_HAREKET.Find(stokHareket.HAREKET_ID);

                if (updateHareket != null)
                {
                    updateHareket.DEPO_ESLESTIRME.DEPO_ID = stokHareket.DEPO_ESLESTIRME.DEPO_ID;
                    updateHareket.SORUMLU.SORUMLU_ADI = stokHareket.SORUMLU.SORUMLU_ADI;
                    updateHareket.HAREKET_TIP1 = stokHareket.HAREKET_TIP1;
                    updateHareket.ACIKLAMA = stokHareket.ACIKLAMA;
                    updateHareket.HAREKET_MIKTAR = stokHareket.HAREKET_MIKTAR;
                    updateHareket.HAREKET_TARIHI = stokHareket.HAREKET_TARIHI;
                    updateHareket.GUNCELLEYEN_KULLANICI = Convert.ToInt32(Session["ID"]);
                    updateHareket.GUNCELLEME_TARIHI = DateTime.Now;

                    stokdata.SaveChanges();
                    TempData["SuccessMessage"] = "Stok hareketleri başarıyla güncellendi.";
                    return RedirectToAction("StokHareketListele");
                }
            }

            ViewBag.Depolar = new SelectList(stokdata.DEPO, "DEPO_ID", "DEPO_ADI", stokHareket.DEPO_ESLESTIRME.DEPO_ID);

            return View(stokHareket);
        }


        [HttpGet]
        public ActionResult KullaniciEkleDepoYetkilisi()
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 2)
            {
                return RedirectToAction("Giris");
            }

            var model = new KULLANICI();
            return View(model);
        }

        [HttpPost]
        public ActionResult KullaniciEkleDepoYetkilisi(KULLANICI kullanici)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 2)
            {
                return RedirectToAction("Giris");
            }

            if (ModelState.IsValid)
            {
                var model = new KULLANICI()
                {
                    KUL_USERNAME = kullanici.KUL_USERNAME,
                    KUL_AD = kullanici.KUL_AD,
                    KUL_SOYAD = kullanici.KUL_SOYAD,
                    KUL_SIFRE = kullanici.KUL_SIFRE,
                    KUL_TIP = kullanici.KUL_TIP,
                    STATU = kullanici.STATU,
                    OLUSTURAN_KULLANICI = Convert.ToInt32(Session["ID"]),
                    OLUSTURMA_TARIHI = DateTime.Now,
                    GUNCELLEME_TARIHI = DateTime.Now,
                    GUNCELLEYEN_KULLANICI = Convert.ToInt32(Session["ID"]),
                };

                stokdata.KULLANICI.Add(model);
                stokdata.SaveChanges();

                TempData["SuccessMessage"] = "Kullanıcı başarıyla eklendi.";
                return RedirectToAction("KullaniciListeleDepoYetkilisi");
            }

            return View(kullanici);
        }

        [HttpGet]
        public ActionResult KullaniciListeleDepoYetkilisi(string searchString = null)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 2)
            {
                return RedirectToAction("Giris");
            }

            int currentUserId = Convert.ToInt32(Session["ID"]);
            var kullanicilar = stokdata.KULLANICI.Where(k => k.OLUSTURAN_KULLANICI == currentUserId && k.KUL_TIP == 2);

            if (!String.IsNullOrEmpty(searchString))
            {
                kullanicilar = kullanicilar.Where(s => s.KUL_USERNAME.Contains(searchString) ||
                                                       s.KUL_AD.Contains(searchString) ||
                                                       s.KUL_SOYAD.Contains(searchString) ||
                                                       s.KUL_ID.ToString().Contains(searchString) ||
                                                       s.KUL_TIP.ToString().Contains(searchString) ||
                                                       (s.STATU == true ? "true" : "false").Contains(searchString));
            }

            return View(kullanicilar.ToList());
        }


        public ActionResult ExportKullaniciDepoYetkilisiToExcel()
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 2)
            {
                return RedirectToAction("Giris");
            }

            int currentUserId = Convert.ToInt32(Session["ID"]);
            var kullanicilar = stokdata.KULLANICI.Where(k => k.OLUSTURAN_KULLANICI == currentUserId && k.KUL_TIP == 2).ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Kullanıcı Listesi");

                worksheet.Cells[1, 1].Value = "Kullanıcı ID";
                worksheet.Cells[1, 2].Value = "Kullanıcı Adı";
                worksheet.Cells[1, 3].Value = "Soyadı";
                worksheet.Cells[1, 4].Value = "Kullanıcı Tipi";
                worksheet.Cells[1, 5].Value = "Statü";
                worksheet.Cells[1, 6].Value = "Oluşturan Kullanıcı";
                worksheet.Cells[1, 7].Value = "Oluşturma Tarihi";
                worksheet.Cells[1, 8].Value = "Güncelleyen Kullanıcı";
                worksheet.Cells[1, 9].Value = "Güncelleme Tarihi";

                for (int i = 0; i < kullanicilar.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cells[row, 1].Value = kullanicilar[i].KUL_ID;
                    worksheet.Cells[row, 2].Value = kullanicilar[i].KUL_AD;
                    worksheet.Cells[row, 3].Value = kullanicilar[i].KUL_SOYAD;
                    worksheet.Cells[row, 4].Value = kullanicilar[i].KUL_TIP;
                    worksheet.Cells[row, 5].Value = kullanicilar[i].STATU == true ? "True" : "False";
                    worksheet.Cells[row, 6].Value = kullanicilar[i].OLUSTURAN_KULLANICI;
                    worksheet.Cells[row, 7].Value = kullanicilar[i].OLUSTURMA_TARIHI.HasValue ? kullanicilar[i].OLUSTURMA_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                    worksheet.Cells[row, 8].Value = kullanicilar[i].GUNCELLEYEN_KULLANICI;
                    worksheet.Cells[row, 9].Value = kullanicilar[i].GUNCELLEME_TARIHI.HasValue ? kullanicilar[i].GUNCELLEME_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                }

                using (var range = worksheet.Cells[1, 1, 1, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    range.Style.Font.Color.SetColor(Color.White);
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                var content = stream.ToArray();

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "KullaniciListeleDepoYetkilisi.xlsx");
            }
        }



        [HttpGet]
        public ActionResult KullaniciGuncelleDepoYetkilisi(int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 2)
            {
                return RedirectToAction("Giris");
            }

            var kullanici = stokdata.KULLANICI.Find(id);
            if (kullanici == null || kullanici.OLUSTURAN_KULLANICI != Convert.ToInt32(Session["ID"]))
            {
                return HttpNotFound();
            }

            return View(kullanici);
        }

        [HttpPost]
        public ActionResult KullaniciGuncelleDepoYetkilisi(KULLANICI kullanici)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 2)
            {
                return RedirectToAction("Giris");
            }

            if (ModelState.IsValid)
            {
                var updateKullanici = stokdata.KULLANICI.Find(kullanici.KUL_ID);
                if (updateKullanici != null && updateKullanici.OLUSTURAN_KULLANICI == Convert.ToInt32(Session["ID"]))
                {
                    updateKullanici.KUL_USERNAME = kullanici.KUL_USERNAME;
                    updateKullanici.KUL_AD = kullanici.KUL_AD;
                    updateKullanici.KUL_SOYAD = kullanici.KUL_SOYAD;
                    updateKullanici.KUL_SIFRE = kullanici.KUL_SIFRE;
                    updateKullanici.KUL_TIP = kullanici.KUL_TIP;
                    updateKullanici.STATU = kullanici.STATU;

                    stokdata.SaveChanges();

                    TempData["SuccessMessage"] = "Kullanıcı başarıyla güncellendi.";
                    return RedirectToAction("KullaniciListeleDepoYetkilisi");
                }
            }

            return View(kullanici);
        }

        [HttpGet]
        public ActionResult KullaniciSilDepoYetkilisi(int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 2)
            {
                return RedirectToAction("Giris");
            }

            var kullanici = stokdata.KULLANICI.Find(id);
            if (kullanici == null || kullanici.OLUSTURAN_KULLANICI != Convert.ToInt32(Session["ID"]))
            {
                return HttpNotFound();
            }

            stokdata.KULLANICI.Remove(kullanici);
            stokdata.SaveChanges();

            TempData["SuccessMessage"] = "Kullanıcı başarıyla silindi.";
            return RedirectToAction("KullaniciListeleDepoYetkilisi");
        }


        [HttpGet]
        public ActionResult KullaniciEkleRaporKullanicisi()
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 3)
            {
                return RedirectToAction("Giris");
            }

            var model = new KULLANICI();
            return View(model);
        }

        [HttpPost]
        public ActionResult KullaniciEkleRaporKullanicisi(KULLANICI kullanici)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 3)
            {
                return RedirectToAction("Giris");
            }

            if (ModelState.IsValid)
            {
                var model = new KULLANICI()
                {
                    KUL_USERNAME = kullanici.KUL_USERNAME,
                    KUL_AD = kullanici.KUL_AD,
                    KUL_SOYAD = kullanici.KUL_SOYAD,
                    KUL_SIFRE = kullanici.KUL_SIFRE,
                    KUL_TIP = kullanici.KUL_TIP,
                    STATU = kullanici.STATU,
                    OLUSTURAN_KULLANICI = Convert.ToInt32(Session["ID"]),
                    OLUSTURMA_TARIHI = DateTime.Now,
                    GUNCELLEME_TARIHI = DateTime.Now,
                    GUNCELLEYEN_KULLANICI = Convert.ToInt32(Session["ID"]),
                };

                stokdata.KULLANICI.Add(model);
                stokdata.SaveChanges();

                TempData["SuccessMessage"] = "Kullanıcı başarıyla eklendi.";
                return RedirectToAction("KullaniciListeleRaporKullanicisi");
            }

            return View(kullanici);
        }

        [HttpGet]
        public ActionResult KullaniciListeleRaporKullanicisi(string searchString = null)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 3)
            {
                return RedirectToAction("Giris");
            }

            int currentUserId = Convert.ToInt32(Session["ID"]);
            var kullanicilar = stokdata.KULLANICI.Where(k => k.OLUSTURAN_KULLANICI == currentUserId && k.KUL_TIP == 3);

            if (!String.IsNullOrEmpty(searchString))
            {
                kullanicilar = kullanicilar.Where(s => s.KUL_USERNAME.Contains(searchString) ||
                                                       s.KUL_AD.Contains(searchString) ||
                                                       s.KUL_SOYAD.Contains(searchString) ||
                                                       s.KUL_ID.ToString().Contains(searchString) ||
                                                       s.KUL_TIP.ToString().Contains(searchString) ||
                                                       (s.STATU == true ? "true" : "false").Contains(searchString));
            }

            return View(kullanicilar.ToList());
        }


        public ActionResult ExportKullaniciRaporKullanicisiToExcel()
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 3)
            {
                return RedirectToAction("Giris");
            }

            int currentUserId = Convert.ToInt32(Session["ID"]);
            var kullanicilar = stokdata.KULLANICI.Where(k => k.OLUSTURAN_KULLANICI == currentUserId && k.KUL_TIP == 3).ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Kullanıcı Listesi");

                worksheet.Cells[1, 1].Value = "Kullanıcı ID";
                worksheet.Cells[1, 2].Value = "Kullanıcı Adı";
                worksheet.Cells[1, 3].Value = "Soyadı";
                worksheet.Cells[1, 4].Value = "Kullanıcı Tipi";
                worksheet.Cells[1, 5].Value = "Statü";
                worksheet.Cells[1, 6].Value = "Oluşturan Kullanıcı";
                worksheet.Cells[1, 7].Value = "Oluşturma Tarihi";
                worksheet.Cells[1, 8].Value = "Güncelleyen Kullanıcı";
                worksheet.Cells[1, 9].Value = "Güncelleme Tarihi";

                for (int i = 0; i < kullanicilar.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cells[row, 1].Value = kullanicilar[i].KUL_ID;
                    worksheet.Cells[row, 2].Value = kullanicilar[i].KUL_AD;
                    worksheet.Cells[row, 3].Value = kullanicilar[i].KUL_SOYAD;
                    worksheet.Cells[row, 4].Value = kullanicilar[i].KUL_TIP;
                    worksheet.Cells[row, 5].Value = kullanicilar[i].STATU == true ? "True" : "False";
                    worksheet.Cells[row, 6].Value = kullanicilar[i].OLUSTURAN_KULLANICI;
                    worksheet.Cells[row, 7].Value = kullanicilar[i].OLUSTURMA_TARIHI.HasValue ? kullanicilar[i].OLUSTURMA_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                    worksheet.Cells[row, 8].Value = kullanicilar[i].GUNCELLEYEN_KULLANICI;
                    worksheet.Cells[row, 9].Value = kullanicilar[i].GUNCELLEME_TARIHI.HasValue ? kullanicilar[i].GUNCELLEME_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                }

                using (var range = worksheet.Cells[1, 1, 1, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    range.Style.Font.Color.SetColor(Color.White);
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                var content = stream.ToArray();

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "KullaniciListeleRaporKullanicisi.xlsx");
            }
        }



        [HttpGet]
        public ActionResult KullaniciGuncelleRaporKullanicisi(int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 3)
            {
                return RedirectToAction("Giris");
            }

            var kullanici = stokdata.KULLANICI.Find(id);
            if (kullanici == null || kullanici.OLUSTURAN_KULLANICI != Convert.ToInt32(Session["ID"]))
            {
                return HttpNotFound();
            }

            return View(kullanici);
        }

        [HttpPost]
        public ActionResult KullaniciGuncelleRaporKullanicisi(KULLANICI kullanici)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 3)
            {
                return RedirectToAction("Giris");
            }

            if (ModelState.IsValid)
            {
                var updateKullanici = stokdata.KULLANICI.Find(kullanici.KUL_ID);
                if (updateKullanici != null && updateKullanici.OLUSTURAN_KULLANICI == Convert.ToInt32(Session["ID"]))
                {
                    updateKullanici.KUL_USERNAME = kullanici.KUL_USERNAME;
                    updateKullanici.KUL_AD = kullanici.KUL_AD;
                    updateKullanici.KUL_SOYAD = kullanici.KUL_SOYAD;
                    updateKullanici.KUL_SIFRE = kullanici.KUL_SIFRE;
                    updateKullanici.KUL_TIP = kullanici.KUL_TIP;
                    updateKullanici.STATU = kullanici.STATU;

                    stokdata.SaveChanges();

                    TempData["SuccessMessage"] = "Kullanıcı başarıyla güncellendi.";
                    return RedirectToAction("KullaniciListeleRaporKullanicisi");
                }
            }

            return View(kullanici);
        }

        [HttpGet]
        public ActionResult KullaniciSilRaporKullanicisi(int id)
        {
            if (Session["ID"] == null || (int)Session["KullaniciTipi"] != 3)
            {
                return RedirectToAction("Giris");
            }

            var kullanici = stokdata.KULLANICI.Find(id);
            if (kullanici == null || kullanici.OLUSTURAN_KULLANICI != Convert.ToInt32(Session["ID"]))
            {
                return HttpNotFound();
            }

            stokdata.KULLANICI.Remove(kullanici);
            stokdata.SaveChanges();

            TempData["SuccessMessage"] = "Kullanıcı başarıyla silindi.";
            return RedirectToAction("KullaniciListeleRaporKullanicisi");
        }


        public ActionResult StokHareketRaporuListele(string searchString)
        {
            var stokHareketleri = stokdata.STOK_HAREKET
                .Include(s => s.DEPO_ESLESTIRME)
                .Include(s => s.SORUMLU);

            if (!String.IsNullOrEmpty(searchString))
            {
                stokHareketleri = stokHareketleri.Where(s =>
                    (s.DEPO_ESLESTIRME != null && s.DEPO_ESLESTIRME.DEPO_ID.HasValue && s.DEPO_ESLESTIRME.DEPO_ID.Value.ToString().Contains(searchString))
                    || (s.HAREKET_TIP1 != null && s.HAREKET_TIP1.HAREKET_TIP_ADI != null && s.HAREKET_TIP1.HAREKET_TIP_ADI.Contains(searchString))
                    || (s.SORUMLU != null && s.SORUMLU.SORUMLU_ADI != null && s.SORUMLU.SORUMLU_ADI.Contains(searchString)));
            }

            var stokHareketListesi = stokHareketleri.ToList();

            return View(stokHareketListesi);
        }



        public ActionResult ExportStokHareketRaporuToExcel()
        {
            var stoklar = stokdata.STOK_HAREKET.ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Stok Hareket Raporu Listesi");


                worksheet.Cells[1, 1].Value = "Hareket ID";
                worksheet.Cells[1, 2].Value = "Stok Adı";
                worksheet.Cells[1, 3].Value = "Depo Adı";
                worksheet.Cells[1, 4].Value = "Sorumlu Adı";
                worksheet.Cells[1, 5].Value = "Hareket Tipi";
                worksheet.Cells[1, 6].Value = "Açıklama";
                worksheet.Cells[1, 7].Value = "Hareket Miktarı";
                worksheet.Cells[1, 8].Value = "Hareket Tarihi";
                worksheet.Cells[1, 8].Value = "Oluşturan Kullanıcı";
                worksheet.Cells[1, 9].Value = "Oluşturma Tarihi";
                worksheet.Cells[1, 10].Value = "Güncelleyen Kullanıcı";
                worksheet.Cells[1, 11].Value = "Güncelleme Tarihi";


                for (int i = 0; i < stoklar.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cells[row, 1].Value = stoklar[i].HAREKET_ID;
                    worksheet.Cells[row, 2].Value = stoklar[i].STOK_ID;
                    worksheet.Cells[row, 3].Value = stoklar[i].DEPO_ESLESTIRME.DEPO_ID;
                    worksheet.Cells[row, 4].Value = stoklar[i].SORUMLU.SORUMLU_ADI;
                    worksheet.Cells[row, 5].Value = stoklar[i].HAREKET_TIP;
                    worksheet.Cells[row, 6].Value = stoklar[i].ACIKLAMA;
                    worksheet.Cells[row, 7].Value = stoklar[i].HAREKET_MIKTAR;
                    worksheet.Cells[row, 8].Value = stoklar[i].HAREKET_TARIHI;
                    worksheet.Cells[row, 9].Value = stoklar[i].OLUSTURAN_KULLANICI;
                    worksheet.Cells[row, 10].Value = stoklar[i].OLUSTURMA_TARIHI.HasValue ? stoklar[i].OLUSTURMA_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                    worksheet.Cells[row, 11].Value = stoklar[i].GUNCELLEYEN_KULLANICI;
                    worksheet.Cells[row, 12].Value = stoklar[i].GUNCELLEME_TARIHI.HasValue ? stoklar[i].GUNCELLEME_TARIHI.Value.ToString("dd.MM.yyyy HH:mm:ss") : "N/A";
                }


                using (var range = worksheet.Cells[1, 1, 1, 11])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    range.Style.Font.Color.SetColor(Color.White);
                }


                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                var content = stream.ToArray();

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StokHareketRaporuListesi.xlsx");
            }
        }

        [HttpGet]
        public ActionResult StokDurumRaporuEkle()
        {
            return View();
        }
        [HttpPost]
        public ActionResult StokDurumRaporuEkleUpdate(STOK_DURUM stokDurum, int stok, int depo)
        {

            var newStokDurum = new STOK_DURUM
                    {
                        STOK_ID = stok,
                        DEPO_ESLESTIRME_ID = depo,
                        DURUM_MIKTAR = stokDurum.DURUM_MIKTAR,
                        OLUSTURAN_KULLANICI = Convert.ToInt32(Session["ID"]),
                        OLUSTURMA_TARIHI = DateTime.Now,
                        GUNCELLEYEN_KULLANICI = Convert.ToInt32(Session["ID"]),
                        GUNCELLEME_TARIHI = DateTime.Now
                    };

                    stokdata.STOK_DURUM.Add(newStokDurum);
                    stokdata.SaveChanges();

                    TempData["SuccessMessage"] = "Stok durumu başarıyla eklendi.";
                    return RedirectToAction("StokDurumRaporuListele");

        }




        [HttpGet]
        public ActionResult StokDurumRaporuListele(string searchString = null)
        {
            var stokDurumlar = stokdata.STOK_DURUM.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                stokDurumlar = stokDurumlar.Where(s => s.STOK_ID.ToString().Contains(searchString) ||
                                                       s.DEPO_ESLESTIRME_ID.ToString().Contains(searchString) ||
                                                       s.DURUM_MIKTAR.ToString().Contains(searchString) ||
                                                       s.OLUSTURAN_KULLANICI.ToString().Contains(searchString) ||
                                                       s.OLUSTURMA_TARIHI.ToString().Contains(searchString) ||
                                                       s.GUNCELLEYEN_KULLANICI.ToString().Contains(searchString) ||
                                                       s.GUNCELLEME_TARIHI.ToString().Contains(searchString));
            }

            var stokDurumList = stokDurumlar.ToList();
            Console.WriteLine($"Found {stokDurumList.Count} records.");

            return View(stokDurumList);
        }


        public ActionResult ExportToExcel(string searchString = null)
        {
            var stokDurumlar = stokdata.STOK_DURUM.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                stokDurumlar = stokDurumlar.Where(s => s.STOK_ID.ToString().Contains(searchString) ||
                                                       s.DEPO_ESLESTIRME_ID.ToString().Contains(searchString) ||
                                                       s.DURUM_MIKTAR.ToString().Contains(searchString) ||
                                                       s.OLUSTURAN_KULLANICI.ToString().Contains(searchString) ||
                                                       s.OLUSTURMA_TARIHI.ToString().Contains(searchString) ||
                                                       s.GUNCELLEYEN_KULLANICI.ToString().Contains(searchString) ||
                                                       s.GUNCELLEME_TARIHI.ToString().Contains(searchString));
            }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Stok Durum Raporları");
                worksheet.Cells["A1"].Value = "DURUM ID";
                worksheet.Cells["B1"].Value = "STOK ID";
                worksheet.Cells["C1"].Value = "DEPO ESLESTIRME ID";
                worksheet.Cells["D1"].Value = "DURUM MIKTAR";
                worksheet.Cells["E1"].Value = "OLUSTURAN KULLANICI";
                worksheet.Cells["F1"].Value = "OLUSTURMA TARIHI";
                worksheet.Cells["G1"].Value = "GUNCELLEYEN KULLANICI";
                worksheet.Cells["H1"].Value = "GUNCELLEME TARIHI";

                var row = 2;
                foreach (var item in stokDurumlar)
                {
                    worksheet.Cells[$"A{row}"].Value = item.DURUM_ID;
                    worksheet.Cells[$"B{row}"].Value = item.STOK_ID;
                    worksheet.Cells[$"C{row}"].Value = item.DEPO_ESLESTIRME_ID;
                    worksheet.Cells[$"D{row}"].Value = item.DURUM_MIKTAR;
                    worksheet.Cells[$"E{row}"].Value = item.OLUSTURAN_KULLANICI;
                    worksheet.Cells[$"F{row}"].Value = item.OLUSTURMA_TARIHI;
                    worksheet.Cells[$"G{row}"].Value = item.GUNCELLEYEN_KULLANICI;
                    worksheet.Cells[$"H{row}"].Value = item.GUNCELLEME_TARIHI; 

                    row++;
                }

               
                worksheet.Column(6).Style.Numberformat.Format = "yyyy-mm-dd"; 
                worksheet.Column(8).Style.Numberformat.Format = "yyyy-mm-dd"; 

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string fileName = $"StokDurumRaporu_{DateTime.Now.ToString("yyyyMMdd")}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        public ActionResult Cikis()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index");
        }


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}