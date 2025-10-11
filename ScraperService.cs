using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace Firmalytics
{
    public class ScraperService
    {
        public event Action<string> OnLogMessage;
        public event Action<int> OnProgressUpdate;
        private int _islenenSirketSayisi = 0;

        public async Task<List<Sirket>> GoogleAramaYapAsync(string konum, string anahtarKelime, int maksSonuc, bool ePostaAramasiYapilsin, int websiteTimeout, CancellationToken token, bool tarayiciGoster, int paralelGorevSayisi)
        {
            new DriverManager().SetUpDriver(new ChromeConfig());

            var sirketLinkleri = new List<string>();
            var sirketler = new ConcurrentBag<Sirket>(); 

            try
            {
                var ilkChromeOptions = new ChromeOptions();
                if (!tarayiciGoster) ilkChromeOptions.AddArgument("--headless");
                ilkChromeOptions.AddArgument("--disable-gpu");
                ilkChromeOptions.AddArgument("--log-level=3");
                ilkChromeOptions.AddArgument("--lang=tr-TR");

                var driverService = ChromeDriverService.CreateDefaultService();
                driverService.HideCommandPromptWindow = true;

                using (var driver = new ChromeDriver(driverService, ilkChromeOptions)) 
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
                        return new List<Sirket>();
                    }

                    int mevcutKartSayisi = 0;
                    while (sirketLinkleri.Count < maksSonuc)
                    {
                        token.ThrowIfCancellationRequested(); 

                        var isletmeKartlari = driver.FindElements(By.CssSelector("a.hfpxzc"));
                        if (isletmeKartlari.Count == mevcutKartSayisi)
                        {
                            Log("Daha fazla sonuç bulunamadı.");
                            break; 
                        }

                        mevcutKartSayisi = isletmeKartlari.Count;
                        Log($"{mevcutKartSayisi} işletme yüklendi, daha fazlası için sayfa kaydırılıyor...");

                        var yeniLinkler = isletmeKartlari
                                           .Select(k => k.GetAttribute("href"))
                                           .Where(h => !string.IsNullOrEmpty(h))
                                           .ToList();

                        sirketLinkleri.AddRange(yeniLinkler);
                        sirketLinkleri = sirketLinkleri.Distinct().ToList(); 

                        if (sirketLinkleri.Count >= maksSonuc) break;

                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight", scrollablePanel);
                        await Task.Delay(2500, token);
                    }

                    sirketLinkleri = sirketLinkleri.Take(maksSonuc).ToList();
                } 
            }
            catch (OperationCanceledException)
            {
                Log("Link toplama aşaması kullanıcı tarafından iptal edildi. Mevcut linklerle devam ediliyor.");
            }
            catch (Exception ex)
            {
                Log($"Link toplama aşamasında hata: {ex.Message}");
                return sirketler.ToList(); 
            }

            if (!sirketLinkleri.Any())
            {
                Log("İşletme detayı alınacak link bulunamadı.");
                return sirketler.ToList();
            }
            Log($"Toplam {sirketLinkleri.Count} işletmenin detayları {paralelGorevSayisi} koldan çekilecek...");
            _islenenSirketSayisi = 0;

            using (var semaphore = new SemaphoreSlim(paralelGorevSayisi))
            {
                var tasks = sirketLinkleri.Select(async link =>
                {
                    await semaphore.WaitAsync(token);
                    try
                    {
                        if (token.IsCancellationRequested) return;

                        var chromeOptions = new ChromeOptions();
                        if (!tarayiciGoster) chromeOptions.AddArgument("--headless");
                        chromeOptions.AddArgument("--disable-gpu");
                        chromeOptions.AddArgument("--log-level=3");
                        chromeOptions.AddArgument("--lang=tr-TR");
                        var parallelDriverService = ChromeDriverService.CreateDefaultService();
                        parallelDriverService.HideCommandPromptWindow = true;

                        using (var driver = new ChromeDriver(parallelDriverService, chromeOptions)) 
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

                            if (ePostaAramasiYapilsin && !string.IsNullOrEmpty(yeniSirket.WebSitesi) && yeniSirket.WebSitesi != "Bulunamadı")
                            {
                                await IletisimBilgileriniCekAsync(driver, yeniSirket, websiteTimeout, token);
                            }

                            if (!sirketler.Any(s => s.IsletmeAdi == yeniSirket.IsletmeAdi && s.Adres == yeniSirket.Adres))
                            {
                                sirketler.Add(yeniSirket);
                                Log($"Bulundu: {yeniSirket.IsletmeAdi} (E-posta: {yeniSirket.Eposta})");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Log("Bir görev iptal edildi.");
                    }
                    catch (Exception ex)
                    {
                        Log($"Bir işletme işlenirken hata oluştu ({link}): {ex.Message.Split('\n')[0]}");
                    }
                    finally
                    {
                        Interlocked.Increment(ref _islenenSirketSayisi);
                        ProgressGuncelle((int)((double)_islenenSirketSayisi / sirketLinkleri.Count * 100));
                        semaphore.Release();
                    }
                });

                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException)
                {
                    Log("Detay çekme aşaması kullanıcı tarafından iptal edildi.");
                }
            }

            return sirketler.ToList();
        }

        #region Mevcut Yardımcı Metotlar (Değişiklik Yok)
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
                    .Where(href => href != null && keywords.Any(kw => href.ToLower().Contains(kw)))
                    .Take(10)
                    .ToList();

                Log($"{contactLinks.Count} potansiyel iletişim sayfası bulundu. Taranıyor...");

                foreach (var link in contactLinks)
                {
                    token.ThrowIfCancellationRequested();
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
            catch (OperationCanceledException)
            {
                Log("Web sitesi taraması iptal edildi.");
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
        private bool SayfadanEpostaCek(string pageSource, Sirket sirket)
        {
            pageSource = pageSource.ToLower();

            var emailRegex = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
            var emailMatches = emailRegex.Matches(pageSource);
            var ignoreList = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg", "wixpress.com" };
            var validEmails = emailMatches.Cast<Match>()
                                          .Select(m => m.Value)
                                          .Where(m => !ignoreList.Any(ext => m.EndsWith(ext)))
                                          .Distinct()
                                          .ToList();

            if (validEmails.Any())
            {
                sirket.Eposta = string.Join(", ", validEmails);
            }

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
        private string CekVeriWithRetry(IWebDriver driver, By by, int retries = 2)
        {
            for (int i = 0; i <= retries; i++)
            {
                try { return driver.FindElement(by).Text; }
                catch (NoSuchElementException) { if (i == retries) return "Bulunamadı"; Thread.Sleep(500); }
                catch (StaleElementReferenceException) { if (i == retries) return "Bulunamadı"; Thread.Sleep(500); }
            }
            return "Bulunamadı";
        }
        private IWebElement BulmayaCalis(IWebDriver driver, By by, int retries = 2)
        {
            for (int i = 0; i <= retries; i++)
            {
                try { return driver.FindElement(by); }
                catch (NoSuchElementException) { if (i == retries) return null; Thread.Sleep(1000); }
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
        private void Log(string message) => OnLogMessage?.Invoke(message);
        private void ProgressGuncelle(int progress) => OnProgressUpdate?.Invoke(progress);
        #endregion
    }
}