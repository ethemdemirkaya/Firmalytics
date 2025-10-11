# Firmalytics | Gelişmiş Kurumsal Keşif Aracı

Firmalytics, C# WinForms ve Selenium teknolojileri kullanılarak geliştirilmiş, Google Haritalar üzerinden hedef şirket ve işletmelerin temel bilgilerini toplayan ve ardından web sitelerini tarayarak e-posta ve LinkedIn gibi kritik iletişim bilgilerini çıkaran güçlü bir otomasyon aracıdır. Özellikle B2B satış, pazarlama ve iş geliştirme profesyonelleri için potansiyel müşteri (lead generation) sürecini otomatize etmek ve zenginleştirmek amacıyla tasarlanmıştır.


![Firmalytics Arayüz Görüntüsü](./assets/Firmalytics-1.2.0.png)

---

## 🚀 Yenilikler (v1.2.0)

### ⚡ Paralel Veri Çekme Motoru (Multi-Threading)
Arama sürecinin en zaman alıcı kısmı olan şirket detaylarını (adres, telefon, web sitesi, e-posta vb.) çekme işlemi artık çoklu kanaldan (multi-thread) paralel olarak gerçekleştiriliyor. Bu, özellikle yüzlerce sonuç içeren aramalarda bekleme süresini **önemli ölçüde** kısaltır.
- **Performans Optimizasyonu:** Arayüzden "Paralel Görev Sayısı"nı ayarlayarak sisteminizin kapasitesine göre performansı optimize edebilirsiniz.
- **Daha Hızlı Sonuçlar:** Daha fazla işletme verisine çok daha kısa sürede ulaşın.

### ⚙️ Otomatik Sürücü Yönetimi (WebDriverManager)
`WebDriverManager` entegrasyonu sayesinde, artık `chromedriver.exe` dosyasını manuel olarak indirip projenize eklemenize gerek kalmadı. Program, her zaman doğru Chrome sürücüsünü otomatik olarak indirip kurar, bu da kurulumu ve bakımı basitleştirir.

---

## ✨ Temel Özellikler

- **Detaylı Bilgi Toplama:** Google Haritalar ve şirket web siteleri üzerinden aşağıdaki verileri otomatik olarak çeker:
  - İşletme Adı
  - Tam Adres
  - Telefon Numarası
  - Web Sitesi Adresi
  - **E-Posta Adres(ler)i**
  - **LinkedIn Şirket Profili**
  - Google Puanı ve Yorum Sayısı
  - Google Haritalar Linki
  - Coğrafi Koordinatlar (Enlem, Boylam)

- **Akıllı Arama:** Belirtilen şehir ve anahtar kelimeye göre (örn: "Ankara" + "Yazılım Şirketleri") hedefe yönelik arama yapar.

- **Veri Yönetimi:**
  - Toplanan verileri modern ve kullanışlı bir grid üzerinde listeler.
  - Sonuçları tek tıkla **Excel (.xlsx)** formatında dışa aktarma.
  - Mevcut arama oturumunu **JSON (.json)** formatında proje olarak kaydetme ve daha sonra tekrar açma.

- **Kullanıcı Dostu Arayüz:**
  - DevExpress UI kütüphanesi ile şık ve modern bir tasarım.
  - İşlem adımlarını anlık olarak gösteren log penceresi.
  - Arama sürecini gösteren ilerleme çubuğu (progress bar).
  - Grid üzerinde sağ tık menüsü ile "Adresi Kopyala", "Website'ye Git", "E-posta Kopyala" gibi hızlı eylemler.

- **Sağlam ve Güvenilir Altyapı:**
  - **İki Aşamalı Mimari:** "Önce Topla, Sonra İşle" mantığı ile önce Google Haritalar'dan tüm işletme linklerini toplar, ardından bu linkleri **paralel olarak** işleyerek veri kaybı riskini minimize eder ve hızı maksimize eder.
  - Google'ın dinamik sayfa yapısına karşı dayanıklı veri kazıma motoru.
  - Yaygın otomasyon hatalarına karşı güçlendirilmiş, zaman aşımları ve yeniden deneme mekanizmaları içeren yapı.

---

## 🛠️ Kullanılan Teknolojiler

- **Platform:** .NET Framework
- **Dil:** C#
- **Arayüz:** Windows Forms (WinForms)
- **UI Kütüphaneleri:** DevExpress WinForms Controls
- **Web Otomasyonu:** Selenium WebDriver
- **Sürücü Yönetimi:** WebDriverManager.Net
- **Veri İşleme:** Newtonsoft.Json

---

## ⚙️ Kurulum ve Kullanım

1.  **Releases** sayfasından en son sürümü (`.zip` dosyası) indirin.
2.  İndirilen `.zip` dosyasını bir klasöre çıkartın.
3.  `Firmalytics.exe` dosyasını çalıştırın.
4.  "Konum" ve "Anahtar Kelime" alanlarını doldurun.
5.  Maksimum kaç sonuç istediğinizi belirtin.
6.  Gelişmiş seçenekleri (E-posta Ara, Tarayıcıyı Göster, Paralel Görev Sayısı vb.) isteğinize göre yapılandırın.
7.  "Aramayı Başlat" butonuna tıklayın ve arkanıza yaslanın!

---

## 📜 Lisans

Bu proje MIT Lisansı altında lisanslanmıştır. Detaylar için `LICENSE` dosyasına bakınız.