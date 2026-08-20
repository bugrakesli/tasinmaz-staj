"""Login akisi icin Selenium E2E testleri (REQ: kimlik dogrulama).

Calistirmadan once frontend'in (ng serve) ve backend'in (dotnet run)
ayakta olmasi ve REMS_TEST_EMAIL / REMS_TEST_PASSWORD ortam
degiskenlerinin gecerli bir kullaniciyi isaret etmesi gerekir.
"""
import pytest

from conftest import TEST_USER_EMAIL, TEST_USER_PASSWORD
from pages import LoginPage


def test_login_page_loads(driver, base_url):
    page = LoginPage(driver, base_url).open()
    assert "Taşınmaz" in driver.page_source


def test_login_with_empty_fields_shows_validation_errors(driver, base_url):
    page = LoginPage(driver, base_url).open()
    page.driver.find_element("css selector", "button.btn-login").click()

    assert "zorunludur" in driver.page_source


def test_login_with_invalid_credentials_shows_error(driver, base_url):
    page = LoginPage(driver, base_url).open()
    page.login("nonexistent-user@example.com", "WrongPassword1!")

    assert "hatalı" in page.error_message().lower()


def test_login_with_valid_credentials_redirects_to_properties(driver, base_url):
    page = LoginPage(driver, base_url).open()
    page.login(TEST_USER_EMAIL, TEST_USER_PASSWORD)

    WebDriverWaitAssert = pytest.importorskip("selenium.webdriver.support.ui")
    from selenium.webdriver.support.ui import WebDriverWait

    WebDriverWait(driver, 10).until(lambda d: "/properties" in d.current_url)
    assert "/properties" in driver.current_url


def test_forgot_password_link_navigates(driver, base_url):
    page = LoginPage(driver, base_url).open()
    page.forgot_password_link().click()

    from selenium.webdriver.support.ui import WebDriverWait
    WebDriverWait(driver, 10).until(lambda d: "/forgot-password" in d.current_url)
    assert "/forgot-password" in driver.current_url
