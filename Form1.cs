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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Firmalytics
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        private CancellationTokenSource cts;

        public Form1()
        {
            InitializeComponent();
            GridAyarla();
            gridViewSirketler.MouseDown += gridViewSirketler_MouseDown;
            spinAramaDerinligi.Properties.Mask.EditMask = "d";
            spinAramaDerinligi.Properties.IsFloatValue = false;
        }

        private async void btnAramayiBaslat_Click(object sender, EventArgs e)
        {
            string konum = txtKonum.Text.Trim();
            string anahtarKelime = txtAnahtarKelime.Text.Trim();
            int maksSonuc = (int)spinAramaDerinligi.Value;
            bool epostaAra = checkEditEpostaAra.Checked;
            bool tarayiciGoster = checkEditTarayiciGoster.Checked;

            int websiteTimeout = (int)spinWebsiteTimeout.Value;

            if (string.IsNullOrEmpty(konum) || string.IsNullOrEmpty(anahtarKelime))
            {
                MessageBox.Show("Lütfen Konum ve Anahtar Kelime alanlarını doldurun.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AramaDurumunuAyarla(true);
            cts = new CancellationTokenSource();

            try
            {
                var scraper = new ScraperService();
                scraper.OnLogMessage += LogYaz;
                scraper.OnProgressUpdate += ProgressGuncelle;

                var sirketListesi = await Task.Run(() => scraper.GoogleAramaYapAsync(konum, anahtarKelime, maksSonuc, epostaAra, websiteTimeout, cts.Token, tarayiciGoster));

                if (cts.IsCancellationRequested)
                {
                    LogYaz("Arama iptal edildi. Mevcut sonuçlar listeleniyor.");
                }
                else
                {
                    LogYaz($"{sirketListesi.Count} adet işletme bulundu. Arama tamamlandı.");
                }

                gridControlSirketler.DataSource = sirketListesi;
                gridControlSirketler.RefreshDataSource();
                KolonlariDuzenle();
            }
            catch (Exception ex)
            {
                LogYaz($"Beklenmedik bir hata oluştu: {ex.Message}");
                MessageBox.Show("Arama sırasında ciddi bir hata oluştu. Detaylar için log penceresini kontrol edin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                AramaDurumunuAyarla(false);
            }
        }

        private void btnDurdur_Click(object sender, EventArgs e)
        {
            cts?.Cancel();
            LogYaz("Arama durduruluyor...");
            btnDurdur.Enabled = false;
        }

        private void AramaDurumunuAyarla(bool basladiMi)
        {
            btnAramayiBaslat.Enabled = !basladiMi;
            btnDurdur.Enabled = basladiMi;
            txtKonum.Enabled = !basladiMi;
            txtAnahtarKelime.Enabled = !basladiMi;
            spinAramaDerinligi.Enabled = !basladiMi;
            spinWebsiteTimeout.Enabled = !basladiMi;
            checkEditEpostaAra.Enabled = !basladiMi;
            checkEditTarayiciGoster.Enabled = !basladiMi;

            if (basladiMi)
            {
                progressBar.Value = 0;
            }
            else
            {
                progressBar.Value = 100;
            }
        }

        #region UI ve Grid Metotları (Değişiklik Yok)

        private void LogYaz(string mesaj)
        {
            if (memoLogPenceresi.InvokeRequired)
            {
                memoLogPenceresi.Invoke(new Action(() => LogYaz(mesaj)));
            }
            else
            {
                memoLogPenceresi.AppendText($"[{DateTime.Now:HH:mm:ss}] {mesaj}{Environment.NewLine}");
                memoLogPenceresi.SelectionStart = memoLogPenceresi.Text.Length;
                memoLogPenceresi.ScrollToCaret();
            }
        }

        private void ProgressGuncelle(int progress)
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action(() => progressBar.Value = progress));
            }
            else
            {
                progressBar.Value = progress;
            }
        }

        private void GridAyarla()
        {
            var gridView = gridViewSirketler;
            gridView.OptionsView.ColumnAutoWidth = false;
            gridView.OptionsView.AllowHtmlDrawHeaders = true;
            gridView.Appearance.HeaderPanel.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            gridView.OptionsView.RowAutoHeight = true;
        }

        private void KolonlariDuzenle()
        {
            var gridView = gridViewSirketler;

            // BeginUpdate/EndUpdate performans için iyidir.
            gridView.BeginUpdate();
            try
            {
                // Kolonların var olup olmadığını kontrol ederek ilerle.
                // Bu, hata almayı engeller.
                if (gridView.Columns["IsletmeAdi"] != null)
                    gridView.Columns["IsletmeAdi"].Caption = "İşletme Adı";

                if (gridView.Columns["Adres"] != null)
                    gridView.Columns["Adres"].Caption = "Adres";

                if (gridView.Columns["Telefon"] != null)
                    gridView.Columns["Telefon"].Caption = "Telefon Numarası";

                if (gridView.Columns["WebSitesi"] != null)
                    gridView.Columns["WebSitesi"].Caption = "Web Sitesi";

                if (gridView.Columns["Puan"] != null)
                    gridView.Columns["Puan"].Caption = "Puan";

                if (gridView.Columns["YorumSayisi"] != null)
                    gridView.Columns["YorumSayisi"].Caption = "Yorum Sayısı";

                if (gridView.Columns["HaritaLinki"] != null)
                    gridView.Columns["HaritaLinki"].Caption = "Google Haritalar Linki";

                if (gridView.Columns["Enlem"] != null)
                    gridView.Columns["Enlem"].Caption = "Enlem";

                if (gridView.Columns["Boylam"] != null)
                    gridView.Columns["Boylam"].Caption = "Boylam";

                if (gridView.Columns["Eposta"] != null)
                    gridView.Columns["Eposta"].Caption = "E-Posta";

                if (gridView.Columns["LinkedIn"] != null)
                    gridView.Columns["LinkedIn"].Caption = "LinkedIn";

                // 'Notlar' kolonu Sirket sınıfında var mı? Varsa bu satırı aktif edin.
                // if (gridView.Columns["Notlar"] != null)
                //     gridView.Columns["Notlar"].Caption = "Notlar";

                // Tüm kolonları içeriğe göre en uygun genişliğe ayarla
                gridView.BestFitColumns();

                // Ardından özel genişlikleri ayarla
                if (gridView.Columns["IsletmeAdi"] != null) gridView.Columns["IsletmeAdi"].Width = 250;
                if (gridView.Columns["Adres"] != null) gridView.Columns["Adres"].Width = 300;
                if (gridView.Columns["HaritaLinki"] != null) gridView.Columns["HaritaLinki"].Width = 200;
                if (gridView.Columns["Eposta"] != null) gridView.Columns["Eposta"].Width = 200;
                if (gridView.Columns["LinkedIn"] != null) gridView.Columns["LinkedIn"].Width = 200;
            }
            catch (Exception ex)
            {
                LogYaz("Kolon başlıkları ayarlanırken hata oluştu: " + ex.Message);
            }
            finally
            {
                // Güncellemeyi bitir ve değişiklikleri ekrana yansıt.
                gridView.EndUpdate();
            }
        }

        // --- Sağ Tık Menüleri ve Kaydet/Aç Fonksiyonları (Bu kodları olduğu gibi bırakabilirsiniz) ---

        private void gridViewSirketler_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var view = sender as GridView;
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
                    // --- YENİ EKLENEN KISIM ---
                    // E-posta Kopyala butonu kontrolü
                    var epostaButonu = popupMenuSirket.Manager.Items["menuEpostaKopyala"];
                    if (epostaButonu != null)
                    {
                        epostaButonu.Enabled = !string.IsNullOrEmpty(sirket.Eposta) && sirket.Eposta != "Bulunamadı" && sirket.Eposta != "Aranmadı";
                    }

                    // LinkedIn Kopyala butonu kontrolü
                    var linkedinButonu = popupMenuSirket.Manager.Items["menuLinkedInKopyala"];
                    if (linkedinButonu != null)
                    {
                        linkedinButonu.Enabled = !string.IsNullOrEmpty(sirket.LinkedIn) && sirket.LinkedIn != "Bulunamadı" && sirket.LinkedIn != "Aranmadı";
                    }
                    popupMenuSirket.ShowPopup(gridControlSirketler.PointToScreen(e.Location));
                }
            }
        }
        private void menuEpostaKopyala_ItemClick(object sender, ItemClickEventArgs e)
        {
            Sirket sirket = gridViewSirketler.GetFocusedRow() as Sirket;
            if (sirket != null && !string.IsNullOrEmpty(sirket.Eposta) && sirket.Eposta != "Bulunamadı" && sirket.Eposta != "Aranmadı")
            {
                Clipboard.SetText(sirket.Eposta);
            }
        }

        private void menuLinkedInKopyala_ItemClick(object sender, ItemClickEventArgs e)
        {
            Sirket sirket = gridViewSirketler.GetFocusedRow() as Sirket;
            if (sirket != null && !string.IsNullOrEmpty(sirket.LinkedIn) && sirket.LinkedIn != "Bulunamadı" && sirket.LinkedIn != "Aranmadı")
            {
                Clipboard.SetText(sirket.LinkedIn);
            }
        }

        private void menuAdresKopyala_ItemClick(object sender, ItemClickEventArgs e)
        {
            Sirket sirket = gridViewSirketler.GetFocusedRow() as Sirket;
            if (sirket != null && !string.IsNullOrEmpty(sirket.Adres) && sirket.Adres != "Bulunamadı")
                Clipboard.SetText(sirket.Adres);
        }

        private void menuTelefonKopyala_ItemClick(object sender, ItemClickEventArgs e)
        {
            Sirket sirket = gridViewSirketler.GetFocusedRow() as Sirket;
            if (sirket != null && !string.IsNullOrEmpty(sirket.Telefon) && sirket.Telefon != "Bulunamadı")
                Clipboard.SetText(sirket.Telefon);
        }

        private void menuGoogleAdresiniKopyala_ItemClick(object sender, ItemClickEventArgs e)
        {
            Sirket sirket = gridViewSirketler.GetFocusedRow() as Sirket;
            if (sirket != null)
                Clipboard.SetText(sirket.HaritaLinki);
        }

        private void menuWebSitesiGit_ItemClick(object sender, ItemClickEventArgs e)
        {
            Sirket sirket = gridViewSirketler.GetFocusedRow() as Sirket;
            if (sirket != null && !string.IsNullOrEmpty(sirket.WebSitesi) && sirket.WebSitesi != "Bulunamadı")
            {
                string webSitesiUrl = sirket.WebSitesi;
                if (!webSitesiUrl.StartsWith("http"))
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

        private void btnExcelAktar_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
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
        }

        private void btnProjeyiKaydet_Click(object sender, EventArgs e)
        {
            var data = gridControlSirketler.DataSource as List<Sirket>;
            if (data == null || !data.Any())
            {
                MessageBox.Show("Kaydedilecek veri bulunmuyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Kurumsal Keşif Projesi (*.json)|*.json";
                saveFileDialog.Title = "Projeyi Kaydet";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    File.WriteAllText(saveFileDialog.FileName, json);
                    MessageBox.Show("Proje başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void btnProjeyiAc_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Kurumsal Keşif Projesi (*.json)|*.json";
                openFileDialog.Title = "Proje Aç";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string json = File.ReadAllText(openFileDialog.FileName);
                    var data = JsonConvert.DeserializeObject<List<Sirket>>(json);

                    gridControlSirketler.DataSource = data;
                    gridControlSirketler.RefreshDataSource();
                    KolonlariDuzenle();
                    LogYaz($"{data?.Count ?? 0} kayıt içeren proje dosyası yüklendi.");
                }
            }
        }

        #endregion
    }
}
