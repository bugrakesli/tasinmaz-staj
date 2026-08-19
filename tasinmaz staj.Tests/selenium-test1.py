from selenium import webdriver
from selenium.webdriver.common.by import By
import time

# 1. Tarayıcıyı başlat (Burada Chrome kullanıyoruz)
driver = webdriver.Chrome()

# 2. İlgili web sitesine git
driver.get("https://www.google.com")

# 3. Sitenin başlığını ekrana yazdır
print("Sayfa Başlığı:", driver.title)

# Tarayıcının hemen kapanmaması için 3 saniye bekle
time.sleep(3)

# 4. Tarayıcıyı kapat
driver.quit()