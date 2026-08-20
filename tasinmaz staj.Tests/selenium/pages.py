"""REMS frontend'i icin sayfa nesneleri (Page Object Model).

Angular zoneless + sinyal tabanli oldugundan, DOM guncellemeleri
(API cagrilarindan sonra) WebDriverWait ile beklenmelidir; time.sleep
kullanilmaz.
"""
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait, Select
from selenium.webdriver.support import expected_conditions as EC

DEFAULT_TIMEOUT = 10


class BasePage:
    def __init__(self, driver, base_url):
        self.driver = driver
        self.base_url = base_url

    def wait(self, timeout=DEFAULT_TIMEOUT):
        return WebDriverWait(self.driver, timeout)

    def find(self, by, value, timeout=DEFAULT_TIMEOUT):
        return self.wait(timeout).until(EC.presence_of_element_located((by, value)))

    def click(self, by, value, timeout=DEFAULT_TIMEOUT):
        el = self.wait(timeout).until(EC.element_to_be_clickable((by, value)))
        el.click()
        return el


class LoginPage(BasePage):
    URL_PATH = "/login"

    def open(self):
        self.driver.get(self.base_url + self.URL_PATH)
        self.find(By.ID, "email")
        return self

    def login(self, email, password):
        self.find(By.ID, "email").clear()
        self.driver.find_element(By.ID, "email").send_keys(email)
        self.driver.find_element(By.ID, "password").clear()
        self.driver.find_element(By.ID, "password").send_keys(password)
        self.click(By.CSS_SELECTOR, "button.btn-login")
        return self

    def error_message(self, timeout=DEFAULT_TIMEOUT):
        el = self.find(By.CSS_SELECTOR, ".alert-danger", timeout)
        return el.text

    def forgot_password_link(self):
        return self.find(By.LINK_TEXT, "Şifremi Unuttum")


class PropertyListPage(BasePage):
    URL_PATH = "/properties"

    def open(self):
        self.driver.get(self.base_url + self.URL_PATH)
        return self

    def is_loaded(self, timeout=DEFAULT_TIMEOUT):
        self.find(By.CSS_SELECTOR, "table", timeout)
        return True

    def click_add_new(self):
        self.click(By.CSS_SELECTOR, "button.btn-primary.btn-sm")
        return PropertyFormPage(self.driver, self.base_url)

    def row_count(self):
        rows = self.driver.find_elements(By.CSS_SELECTOR, "table tbody tr")
        return len(rows)

    def filter_by_city(self, city_name):
        select = Select(self.find(By.ID, "filterCity"))
        select.select_by_visible_text(city_name)
        return self

    def logout(self):
        self.click(By.CSS_SELECTOR, "button.btn-logout")


class PropertyFormPage(BasePage):
    URL_PATH_NEW = "/properties/new"

    def open_new(self):
        self.driver.get(self.base_url + self.URL_PATH_NEW)
        self.find(By.ID, "city")
        return self

    def select_city(self, city_name):
        select = Select(self.find(By.ID, "city"))
        select.select_by_visible_text(city_name)
        return self

    def wait_district_options(self, timeout=DEFAULT_TIMEOUT):
        self.wait(timeout).until(
            lambda d: len(Select(d.find_element(By.ID, "district")).options) > 1
        )
        return self

    def select_district(self, district_name):
        select = Select(self.find(By.ID, "district"))
        select.select_by_visible_text(district_name)
        return self

    def wait_neighborhood_options(self, timeout=DEFAULT_TIMEOUT):
        self.wait(timeout).until(
            lambda d: len(Select(d.find_element(By.ID, "neighborhood")).options) > 1
        )
        return self

    def select_neighborhood(self, neighborhood_name):
        select = Select(self.find(By.ID, "neighborhood"))
        select.select_by_visible_text(neighborhood_name)
        return self

    def fill_common_fields(self, lot_no, parcel_no, property_type, address):
        self.driver.find_element(By.ID, "lotNumber").send_keys(lot_no)
        self.driver.find_element(By.ID, "parcelNumber").send_keys(parcel_no)
        self.driver.find_element(By.ID, "propertyType").send_keys(property_type)
        self.driver.find_element(By.ID, "address").send_keys(address)
        return self

    def submit(self):
        self.click(By.CSS_SELECTOR, "form button[type='submit']")
        return PropertyListPage(self.driver, self.base_url)

    def field_error_visible(self, field_id):
        errors = self.driver.find_elements(
            By.CSS_SELECTOR, f"#{field_id} ~ .text-danger, #{field_id} + .text-danger"
        )
        return len(errors) > 0
