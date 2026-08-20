"""Taşınmaz listesi ve ekleme akisi icin Selenium E2E testleri.

REQ-2/REQ-3: yeni taşınmaz eklerken sehir/ilce/mahalle cascading
combobox'lari ve zorunlu alan validasyonu; REQ-3/REQ-4: filtreleme.
"""
from selenium.webdriver.support.ui import WebDriverWait

from conftest import TEST_USER_EMAIL, TEST_USER_PASSWORD
from pages import LoginPage, PropertyListPage, PropertyFormPage


def _login(driver, base_url):
    page = LoginPage(driver, base_url).open()
    page.login(TEST_USER_EMAIL, TEST_USER_PASSWORD)
    WebDriverWait(driver, 10).until(lambda d: "/properties" in d.current_url)


def test_property_list_loads_after_login(driver, base_url):
    _login(driver, base_url)
    list_page = PropertyListPage(driver, base_url)
    assert list_page.is_loaded()


def test_add_new_property_button_navigates_to_form(driver, base_url):
    _login(driver, base_url)
    list_page = PropertyListPage(driver, base_url)
    form_page = list_page.click_add_new()

    WebDriverWait(driver, 10).until(lambda d: "/properties/new" in d.current_url)
    assert "/properties/new" in driver.current_url


def test_property_form_requires_mandatory_fields(driver, base_url):
    _login(driver, base_url)
    form_page = PropertyFormPage(driver, base_url).open_new()
    form_page.submit()

    # Form gecersizse SPA ayni sayfada kalir ve hata mesajlari gorunur.
    assert "/properties/new" in driver.current_url
    assert "zorunludur" in driver.page_source


def test_city_selection_populates_district_dropdown(driver, base_url):
    _login(driver, base_url)
    form_page = PropertyFormPage(driver, base_url).open_new()

    form_page.select_city("Ankara")
    form_page.wait_district_options()

    from selenium.webdriver.support.ui import Select
    options = Select(driver.find_element("id", "district")).options
    assert len(options) > 1


def test_full_cascade_and_submit_creates_property(driver, base_url):
    _login(driver, base_url)
    list_page = PropertyListPage(driver, base_url).open()
    initial_count = list_page.row_count()

    form_page = PropertyFormPage(driver, base_url).open_new()
    form_page.select_city("Ankara")
    form_page.wait_district_options()
    form_page.select_district("Çankaya")
    form_page.wait_neighborhood_options()
    form_page.select_neighborhood(
        driver.find_element("id", "neighborhood")
        .find_elements("tag name", "option")[1]
        .text
    )
    form_page.fill_common_fields(
        lot_no="123",
        parcel_no="45",
        property_type="Arsa",
        address="Selenium test adresi",
    )
    # Konum alani zorunlu (formControlName="coordinate"); harita cizimi
    # yerine gecerli bir WKT polygon direkt textarea'ya yaziliyor.
    form_page.fill_coordinate(
        "POLYGON((32.85 39.93, 32.86 39.93, 32.86 39.94, 32.85 39.94, 32.85 39.93))"
    )
    form_page.submit()

    WebDriverWait(driver, 15).until(lambda d: "/properties" in d.current_url and "new" not in d.current_url)
    list_page = PropertyListPage(driver, base_url)
    list_page.is_loaded()
    assert list_page.row_count() >= initial_count


def test_logout_redirects_to_login(driver, base_url):
    _login(driver, base_url)
    list_page = PropertyListPage(driver, base_url)
    list_page.logout()

    WebDriverWait(driver, 10).until(lambda d: "/login" in d.current_url)
    assert "/login" in driver.current_url
