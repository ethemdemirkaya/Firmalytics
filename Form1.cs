using DevExpress.Utils.Menu;
using DevExpress.XtraBars;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Firmalytics
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        public Form1()
        {
            InitializeComponent();
            GridAyarla();
            gridViewSirketler.MouseDown += gridViewSirketler_MouseDown;
            spinAramaDerinligi.Properties.Mask.EditMask = "d";
            spinAramaDerinligi.Properties.IsFloatValue = false;
        }

        private void menuAdresKopyala_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Sirket sirket = gridViewSirketler.GetFocusedRow() as Sirket;
            if (sirket != null && sirket.Adres != "Bulunamadı")
            {
                Clipboard.SetText(sirket.Adres);
            }
        }
        private void gridViewSirketler_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var view = gridViewSirketler;
                GridHitInfo hitInfo = view.CalcHitInfo(e.Location);

                if (hitInfo.InRow)
                {
                    view.FocusedRowHandle = hitInfo.RowHandle;

                    Sirket sirket = view.GetRow(hitInfo.RowHandle) as Sirket;
                    if (sirket != null)
                    {
                        var webSitesiButonu = popupMenuSirket.Manager.Items["menuWebSitesiGit"];
                        if (webSitesiButonu != null)
                        {
                            webSitesiButonu.Enabled = !string.IsNullOrEmpty(sirket.WebSitesi) && sirket.WebSitesi != "Bulunamadı";
                        }
                    }
                    popupMenuSirket.ShowPopup(gridControlSirketler.PointToScreen(e.Location));
                }
            }
        }
        private void menuTelefonKopyala_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Sirket sirket = gridViewSirketler.GetFocusedRow() as Sirket;
            if (sirket != null && sirket.Telefon != "Bulunamadı")
            {
                Clipboard.SetText(sirket.Telefon);
            }
        }

        private void menuGoogleAdresiniKopyala_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Sirket sirket = gridViewSirketler.GetFocusedRow() as Sirket;
            if (sirket != null)
            {
                Clipboard.SetText(sirket.HaritaLinki);
            }
        }

        private void menuWebSitesiGit_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Sirket sirket = gridViewSirketler.GetFocusedRow() as Sirket;

            if (sirket != null && !string.IsNullOrEmpty(sirket.WebSitesi) && sirket.WebSitesi != "Bulunamadı")
            {
                string webSitesiUrl = sirket.WebSitesi;
                if (!webSitesiUrl.StartsWith("http://") && !webSitesiUrl.StartsWith("https://"))
                {
                    webSitesiUrl = "https://" + webSitesiUrl;
                }

                try
                {
                    System.Diagnostics.Process.Start(webSitesiUrl);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Web sitesi açılamadı: {webSitesiUrl}\n\nHata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void GridAyarla()
        {
            var gridView = gridViewSirketler;
            gridView.OptionsView.ColumnAutoWidth = false;
            gridView.BestFitColumns();
            gridView.OptionsView.AllowHtmlDrawHeaders = true; 
            gridView.Appearance.HeaderPanel.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            gridView.OptionsView.RowAutoHeight = true;
        }
        private void KolonlariDuzenle()
        {
            var gridView = gridViewSirketler;

            gridView.BestFitColumns();
            try
            {
                gridView.Columns["IsletmeAdi"].Caption = "İşletme Adı";
                gridView.Columns["Adres"].Caption = "Adres";
                gridView.Columns["Telefon"].Caption = "Telefon Numarası";
                gridView.Columns["WebSitesi"].Caption = "Web Sitesi";
                gridView.Columns["Puan"].Caption = "Puan";
                gridView.Columns["YorumSayisi"].Caption = "Yorum Sayısı";
                gridView.Columns["HaritaLinki"].Caption = "Google Haritalar Linki";
                gridView.Columns["Enlem"].Caption = "Enlem";
                gridView.Columns["Boylam"].Caption = "Boylam";
                gridView.Columns["Eposta"].Caption = "E-Posta"; 
                gridView.Columns["LinkedIn"].Caption = "LinkedIn";
                gridView.Columns["Notlar"].Caption = "Notlar";

                gridView.Columns["IsletmeAdi"].Width = 250;
                gridView.Columns["Adres"].Width = 300;
                gridView.Columns["HaritaLinki"].Width = 350;

            }
            catch (Exception ex)
            {
                LogYaz("Kolon başlıkları ayarlanırken hata oluştu: " + ex.Message);
            }
        }
        private async void btnAramayiBaslat_Click(object sender, EventArgs e)
        {
            string konum = txtKonum.Text.Trim();
            string anahtarKelime = txtAnahtarKelime.Text.Trim();
            int maksSonuc = (int)spinAramaDerinligi.Value;

            if (string.IsNullOrEmpty(konum) || string.IsNullOrEmpty(anahtarKelime))
            {
                MessageBox.Show("Lütfen Konum ve Anahtar Kelime alanlarını doldurun.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LogYaz("Arama başlatılıyor...");
            btnAramayiBaslat.Enabled = false;
            progressBar.Value = 0;

            var sirketListesi = await Task.Run(() => GoogleAramaYap(konum, anahtarKelime, maksSonuc));

            gridControlSirketler.DataSource = sirketListesi;
            gridControlSirketler.RefreshDataSource();
            KolonlariDuzenle();
            LogYaz($"{sirketListesi.Count} adet işletme bulundu. Arama tamamlandı.");
            progressBar.Value = 100;
            btnAramayiBaslat.Enabled = true;
        }

        private void LogYaz(string mesaj)
        {
            if (memoLogPenceresi.InvokeRequired)
            {
                memoLogPenceresi.Invoke(new Action(() => {
                    memoLogPenceresi.Text += $"[{DateTime.Now:HH:mm:ss}] {mesaj}{Environment.NewLine}";
                    memoLogPenceresi.SelectionStart = memoLogPenceresi.Text.Length;
                    memoLogPenceresi.ScrollToCaret();
                }));
            }
            else
            {
                memoLogPenceresi.Text += $"[{DateTime.Now:HH:mm:ss}] {mesaj}{Environment.NewLine}";
                memoLogPenceresi.SelectionStart = memoLogPenceresi.Text.Length;
                memoLogPenceresi.ScrollToCaret();
            }
        }

        private List<Sirket> GoogleAramaYap(string konum, string anahtarKelime, int maksSonuc)
        {
            var sirketler = new List<Sirket>();

            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            chromeOptions.AddArgument("--disable-gpu");
            chromeOptions.AddArgument("--log-level=3");
            chromeOptions.AddArgument("--lang=tr-TR");

            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;

            using (var driver = new ChromeDriver(driverService, chromeOptions))
            {
                driver.Navigate().GoToUrl("https://www.google.com/maps");
                System.Threading.Thread.Sleep(3000);

                try
                {
                    var aramaKutusu = driver.FindElement(By.Id("searchboxinput"));
                    aramaKutusu.SendKeys($"{konum} {anahtarKelime}");
                    aramaKutusu.SendKeys(OpenQA.Selenium.Keys.Enter);
                    System.Threading.Thread.Sleep(5000);
                }
                catch (Exception ex)
                {
                    LogYaz($"Arama kutusu bulunamadı veya arama yapılamadı: {ex.Message}");
                    return sirketler;
                }

                IWebElement scrollablePanel;
                try
                {
                    scrollablePanel = driver.FindElement(By.XPath("//div[contains(@aria-label, 'için sonuçlar')]"));
                }
                catch
                {
                    LogYaz("Sonuç listesi paneli ('için sonuçlar' içeren) bulunamadı. Arama sonlandırılıyor.");
                    return sirketler;
                }

                var mevcutKartSayisi = 0;
                while (true)
                {
                    var isletmeKartlari = driver.FindElements(By.CssSelector("a.hfpxzc"));
                    if (isletmeKartlari.Count >= maksSonuc || isletmeKartlari.Count == mevcutKartSayisi)
                    {
                        break;
                    }
                    mevcutKartSayisi = isletmeKartlari.Count;
                    LogYaz($"{mevcutKartSayisi} işletme yüklendi, daha fazlası için sayfa kaydırılıyor...");
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight", scrollablePanel);
                    System.Threading.Thread.Sleep(2500);
                }
                LogYaz("Tüm işletme linkleri toplanıyor...");
                var tumKartElementleri = driver.FindElements(By.CssSelector("a.hfpxzc")).Take(maksSonuc).ToList();
                var sirketLinkleri = new List<string>();
                foreach (var kart in tumKartElementleri)
                {
                    try
                    {
                        sirketLinkleri.Add(kart.GetAttribute("href"));
                    }
                    catch {}
                }

                LogYaz($"Toplam {sirketLinkleri.Count} işletmenin detayları çekilecek...");
                int islenen = 0;

                foreach (var link in sirketLinkleri)
                {
                    try
                    {
                        driver.Navigate().GoToUrl(link);
                        System.Threading.Thread.Sleep(2000);

                        Sirket yeniSirket = new Sirket();

                        yeniSirket.IsletmeAdi = CekVeri(driver, By.CssSelector("h1.DUwDvf"));
                        yeniSirket.Adres = CekVeri(driver, By.CssSelector("button[data-item-id='address'] div.fontBodyMedium"));
                        yeniSirket.Telefon = CekVeri(driver, By.CssSelector("button[data-item-id*='phone:tel:'] div.fontBodyMedium"));
                        yeniSirket.WebSitesi = CekVeri(driver, By.CssSelector("a[data-item-id='authority'] div.fontBodyMedium"));

                        string puanText = CekVeri(driver, By.CssSelector("div.F7nice span[aria-hidden]"));
                        string yorumSayisiText = CekVeri(driver, By.CssSelector("div.F7nice button.DkEaL"));
                        yeniSirket.Puan = puanText;
                        yeniSirket.YorumSayisi = yorumSayisiText.Replace("(", "").Replace(")", "");

                        yeniSirket.HaritaLinki = driver.Url;
                        var koordinatlar = KoordinatCek(driver.Url);
                        yeniSirket.Enlem = koordinatlar.Item1;
                        yeniSirket.Boylam = koordinatlar.Item2;

                        if (!sirketler.Any(s => s.IsletmeAdi == yeniSirket.IsletmeAdi && s.Adres == yeniSirket.Adres))
                        {
                            sirketler.Add(yeniSirket);
                            LogYaz($"Bulundu: {yeniSirket.IsletmeAdi}");
                        }

                        islenen++;
                        int progress = (int)((double)islenen / sirketLinkleri.Count * 100);
                        progressBar.Invoke(new Action(() => progressBar.Value = progress));
                    }
                    catch (Exception ex)
                    {
                        LogYaz($"Bir işletme işlenirken hata oluştu: {ex.Message}.");
                    }
                }
            }
            return sirketler;
        }



        private string CekVeri(IWebDriver driver, By by)
        {
            try
            {
                return driver.FindElement(by).Text;
            }
            catch (NoSuchElementException)
            {
                return "Bulunamadı";
            }
        }
        private void btnExcelAktar_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel Dosyası (*.xlsx)|*.xlsx";
            saveFileDialog.Title = "Sonuçları Kaydet";
            saveFileDialog.FileName = "sirket_listesi.xlsx";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    gridControlSirketler.ExportToXlsx(saveFileDialog.FileName);
                    MessageBox.Show("Veriler başarıyla Excel'e aktarıldı!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Dışa aktarma sırasında bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnProjeyiKaydet_Click(object sender, EventArgs e)
        {
            var data = gridControlSirketler.DataSource as List<Sirket>;
            if (data == null || data.Count == 0)
            {
                MessageBox.Show("Kaydedilecek veri bulunmuyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Kurumsal Keşif Projesi (*.json)|*.json";
            saveFileDialog.Title = "Projeyi Kaydet";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(saveFileDialog.FileName, json);
                MessageBox.Show("Proje başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnProjeyiAc_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Kurumsal Keşif Projesi (*.json)|*.json";
            openFileDialog.Title = "Proje Aç";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string json = File.ReadAllText(openFileDialog.FileName);
                var data = JsonConvert.DeserializeObject<List<Sirket>>(json);

                gridControlSirketler.DataSource = data;
                gridControlSirketler.RefreshDataSource();
                KolonlariDuzenle();
                LogYaz($"{data.Count} kayıt içeren proje dosyası yüklendi.");
            }
        }
        private Tuple<double, double> KoordinatCek(string url)
        {
            try
            {
                string koordinatBlogu = url.Split(new[] { "/@" }, StringSplitOptions.None)[1].Split('/')[0];
                string[] parcalar = koordinatBlogu.Split(',');

                double enlem = double.Parse(parcalar[0], CultureInfo.InvariantCulture);
                double boylam = double.Parse(parcalar[1], CultureInfo.InvariantCulture);

                return new Tuple<double, double>(enlem, boylam);
            }
            catch
            {
                return new Tuple<double, double>(0, 0);
            }
        }
    }
}
