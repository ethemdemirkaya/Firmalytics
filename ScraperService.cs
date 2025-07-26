using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace Firmalytics
{
    public class ScraperService
    {
        public event Action<string> OnLogMessage;
        public event Action<int> OnProgressUpdate;

        public async Task<List<Sirket>> GoogleAramaYapAsync(string konum, string anahtarKelime, int maksSonuc, bool ePostaAramasiYapilsin, int websiteTimeout, CancellationToken token,bool tarayiciGoster)
        {
            var sirketler = new List<Sirket>();
            var chromeOptions = new ChromeOptions();
            if(!tarayiciGoster) chromeOptions.AddArgument("--headless");
            chromeOptions.AddArgument("--disable-gpu");
            chromeOptions.AddArgument("--log-level=3");
            chromeOptions.AddArgument("--lang=tr-TR");

            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;

            using (var driver = new ChromeDriver(driverService, chromeOptions))
            {
                try
                {
                    driver.Navigate().GoToUrl("https://www.google.com/maps");
                    await Task.Delay(3000, token);

                    Log("Google Haritalar açıldı. Arama yapılıyor...");
                    var aramaKutusu = driver.FindElement(By.Id("searchboxinput"));
                    aramaKutusu.SendKeys($"{konum} {anahtarKelime}");
                    aramaKutusu.SendKeys(Keys.Enter);
                    await Task.Delay(5000, token);

                    IWebElement scrollablePanel = BulmayaCalis(driver, By.XPath("//div[contains(@aria-label, 'için sonuçlar')] | //div[contains(@aria-label, 'Results for')]"));
                    if (scrollablePanel == null)
                    {
                        Log("Sonuç listesi paneli bulunamadı. Arama sonlandırılıyor.");
                        return sirketler;
                    }

                    // Sayfayı kaydırarak tüm sonuçları yükle
                    int mevcutKartSayisi = 0;
                    while (true)
                    {
                        if (token.IsCancellationRequested) break;
                        var isletmeKartlari = driver.FindElements(By.CssSelector("a.hfpxzc"));
                        if (isletmeKartlari.Count >= maksSonuc || isletmeKartlari.Count == mevcutKartSayisi)
                        {
                            break;
                        }
                        mevcutKartSayisi = isletmeKartlari.Count;
                        Log($"{mevcutKartSayisi} işletme yüklendi, daha fazlası için sayfa kaydırılıyor...");
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight", scrollablePanel);
                        await Task.Delay(2500, token);
                    }

                    var sirketLinkleri = driver.FindElements(By.CssSelector("a.hfpxzc"))
                                               .Take(maksSonuc)
                                               .Select(k => k.GetAttribute("href"))
                                               .Distinct()
                                               .ToList();

                    Log($"Toplam {sirketLinkleri.Count} işletmenin detayları çekilecek...");
                    int islenen = 0;

                    foreach (var link in sirketLinkleri)
                    {
                        if (token.IsCancellationRequested) break;

                        try
                        {
                            driver.Navigate().GoToUrl(link);
                            await Task.Delay(2000, token);

                            Sirket yeniSirket = new Sirket
                            {
                                IsletmeAdi = CekVeriWithRetry(driver, By.CssSelector("h1.DUwDvf")),
                                Adres = CekVeriWithRetry(driver, By.CssSelector("button[data-item-id='address'] div.fontBodyMedium")),
                                Telefon = CekVeriWithRetry(driver, By.CssSelector("button[data-item-id*='phone:tel:'] div.fontBodyMedium")),
                                WebSitesi = CekVeriWithRetry(driver, By.CssSelector("a[data-item-id='authority'] div.fontBodyMedium")),
                                Puan = CekVeriWithRetry(driver, By.CssSelector("div.F7nice span[aria-hidden]")),
                                YorumSayisi = CekVeriWithRetry(driver, By.CssSelector("div.F7nice button.DkEaL")).Replace("(", "").Replace(")", ""),
                                HaritaLinki = driver.Url
                            };

                            var koordinatlar = KoordinatCek(driver.Url);
                            yeniSirket.Enlem = koordinatlar.Item1;
                            yeniSirket.Boylam = koordinatlar.Item2;

                            // E-posta ve LinkedIn Arama (isteğe bağlı)
                            if (ePostaAramasiYapilsin && !string.IsNullOrEmpty(yeniSirket.WebSitesi) && yeniSirket.WebSitesi != "Bulunamadı")
                            {
                                // --- DEĞİŞİKLİK 2: 'websiteTimeout' parametresi buraya da iletildi.
                                await IletisimBilgileriniCekAsync(driver, yeniSirket, websiteTimeout, token);
                            }

                            if (!sirketler.Any(s => s.IsletmeAdi == yeniSirket.IsletmeAdi && s.Adres == yeniSirket.Adres))
                            {
                                sirketler.Add(yeniSirket);
                                Log($"Bulundu: {yeniSirket.IsletmeAdi} (E-posta: {yeniSirket.Eposta})");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Bir işletme işlenirken hata oluştu: {ex.Message}");
                        }
                        finally
                        {
                            islenen++;
                            ProgressGuncelle((int)((double)islenen / sirketLinkleri.Count * 100));
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Log("Arama kullanıcı tarafından iptal edildi.");
                }
                catch (Exception ex)
                {
                    Log($"Genel bir hata oluştu: {ex.Message}");
                }
            }
            return sirketler;
        }
        private async Task IletisimBilgileriniCekAsync(IWebDriver driver, Sirket sirket, int websiteTimeout, CancellationToken token)
        {
            Log($"'{sirket.IsletmeAdi}' için web sitesi taranıyor: {sirket.WebSitesi}");
            sirket.Eposta = "Bulunamadı";
            sirket.LinkedIn = "Bulunamadı";
            string anaSayfaUrl = sirket.WebSitesi;

            if (!anaSayfaUrl.StartsWith("http"))
            {
                anaSayfaUrl = "https://" + anaSayfaUrl;
            }
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(websiteTimeout);
            try
            {
                driver.Navigate().GoToUrl(anaSayfaUrl);
                await Task.Delay(3000, token);

                // Ana sayfada e-posta var mı diye hızlıca kontrol et
                if (SayfadanEpostaCek(driver.PageSource, sirket))
                {
                    Log("E-posta ana sayfada bulundu.");
                    return;
                }

                var allLinks = driver.FindElements(By.TagName("a"))
                                     .Select(a => a.GetAttribute("href"))
                                     .Where(href => !string.IsNullOrEmpty(href))
                                     .Distinct()
                                     .ToList();

                string[] keywords = { "iletisim", "contact", "bize-ulasin", "kunye", "impressum", "legal" };

                var contactLinks = allLinks
                    .Where(href => keywords.Any(kw => href.ToLower().Contains(kw)))
                    .Take(10) 
                    .ToList();

                Log($"{contactLinks.Count} potansiyel iletişim sayfası bulundu. Taranıyor...");

                foreach (var link in contactLinks)
                {
                    if (token.IsCancellationRequested) return;
                    try
                    {
                        Log($"Taranıyor: {link}");
                        driver.Navigate().GoToUrl(link);
                        await Task.Delay(1500, token);
                        if (SayfadanEpostaCek(driver.PageSource, sirket))
                        {
                            Log($"E-posta bulundu: {link}");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Linke gidilemedi ({link}): {ex.Message.Split('\n')[0]}");
                    }
                }

                Log("Potansiyel iletişim sayfalarında e-posta bulunamadı.");
            }
            catch (Exception ex)
            {
                Log($"Web sitesi taranırken hata: {ex.Message.Split('\n')[0]}");
            }
            finally
            {
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
            }
        }

        // E-posta ve LinkedIn çeken yardımcı metot
        private bool SayfadanEpostaCek(string pageSource, Sirket sirket)
        {
            pageSource = pageSource.ToLower();

            // E-posta ara
            var emailRegex = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
            var emailMatches = emailRegex.Matches(pageSource);
            if (emailMatches.Count > 0)
            {
                sirket.Eposta = string.Join(", ", emailMatches.Cast<Match>().Select(m => m.Value).Distinct());
            }

            // LinkedIn ara (sadece henüz bulunmadıysa)
            if (sirket.LinkedIn == "Bulunamadı")
            {
                var linkedinRegex = new Regex(@"https?://[a-z.]*linkedin\.com/company/[a-zA-Z0-9_-]+");
                var linkedinMatch = linkedinRegex.Match(pageSource);
                if (linkedinMatch.Success)
                {
                    sirket.LinkedIn = linkedinMatch.Value;
                }
            }

            return sirket.Eposta != "Bulunamadı";
        }


        // Elementi bulamazsa tekrar deneyen, daha sağlam bir metot
        private string CekVeriWithRetry(IWebDriver driver, By by, int retries = 2)
        {
            for (int i = 0; i <= retries; i++)
            {
                try
                {
                    return driver.FindElement(by).Text;
                }
                catch (NoSuchElementException)
                {
                    if (i == retries) return "Bulunamadı";
                    Thread.Sleep(500); // Kısa bir süre bekle ve tekrar dene
                }
            }
            return "Bulunamadı";
        }

        private IWebElement BulmayaCalis(IWebDriver driver, By by, int retries = 2)
        {
            for (int i = 0; i <= retries; i++)
            {
                try { return driver.FindElement(by); }
                catch (NoSuchElementException)
                {
                    if (i == retries) return null;
                    Thread.Sleep(1000);
                }
            }
            return null;
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
            catch { return new Tuple<double, double>(0, 0); }
        }

        // Event'leri tetikleyen yardımcı metotlar
        private void Log(string message) => OnLogMessage?.Invoke(message);
        private void ProgressGuncelle(int progress) => OnProgressUpdate?.Invoke(progress);
    }
}
