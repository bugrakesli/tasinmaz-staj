# Selenium E2E Testleri

Bu klasördeki `test_*_selenium.py` ve `selenium-test1.py` dosyaları
REMS frontend'ini gerçek bir tarayıcı üzerinden test eder (Page Object
Model: `pages.py`, ortak fixture'lar: `conftest.py`).

## Kurulum

```bash
pip install -r requirements-selenium.txt --break-system-packages
```

Chrome/Chromium ve uyumlu chromedriver PATH üzerinde bulunmalıdır
(Selenium Manager 4.6+ genelde otomatik indirir).

## Çalıştırmadan önce

1. Backend'i başlatın: `dotnet run` (varsayılan `http://localhost:5000`)
2. Frontend'i başlatın: `ng serve` (varsayılan `http://localhost:4200`)
3. Testlerin login olabilmesi için DB'de geçerli bir kullanıcı olmalı;
   aşağıdaki ortam değişkenleriyle belirtin:

```bash
export REMS_BASE_URL=http://localhost:4200
export REMS_TEST_EMAIL=bugra@rems.com
export REMS_TEST_PASSWORD='bugr4@rems'
export REMS_HEADLESS=1   # 0 yaparsanız tarayıcı görünür çalışır
```

## Çalıştırma

```bash
cd "tasinmaz staj.Tests"
pytest test_login_selenium.py test_property_selenium.py -v
```

## Kapsam

- `test_login_selenium.py`: boş alan validasyonu, hatalı giriş,
  başarılı giriş + `/properties` yönlendirmesi, "Şifremi Unuttum" linki
- `test_property_selenium.py`: liste yükleme, "Yeni Taşınmaz Ekle"
  navigasyonu, zorunlu alan validasyonu, Şehir→İlçe→Mahalle cascading
  combobox, uçtan uca taşınmaz oluşturma, çıkış (logout)

## Notlar

- Angular zoneless + signal mimarisi nedeniyle `time.sleep` yerine
  `WebDriverWait` / `expected_conditions` kullanılıyor.
- `test_full_cascade_and_submit_creates_property` gerçek bir kayıt
  oluşturduğundan test veritabanında çalıştırılması önerilir.
