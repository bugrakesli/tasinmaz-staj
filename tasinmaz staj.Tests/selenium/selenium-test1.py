"""Basit ortam dogrulama testi: Chrome WebDriver kurulumunun calisir
oldugunu ve frontend'in acilabildigini kontrol eder. Diger senaryolar
icin test_login_selenium.py ve test_property_selenium.py dosyalarina
bakiniz.
"""
from pages import LoginPage


def test_environment_smoke(driver, base_url):
    LoginPage(driver, base_url).open()
    assert "Taşınmaz Yönetim Sistemi" in driver.page_source
