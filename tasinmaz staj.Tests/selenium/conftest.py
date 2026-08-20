import os
import pytest
from selenium import webdriver
from selenium.webdriver.chrome.options import Options

# Testler calisan bir frontend (varsayilan: http://localhost:4200) ve
# calisan bir backend API'ye (varsayilan: http://localhost:5000) ihtiyac duyar.
BASE_URL = os.environ.get("REMS_BASE_URL", "http://localhost:4200")

# Test kullanicisi bilgileri (mevcut bir DB kaydiyla eslesmeli).
TEST_USER_EMAIL = os.environ.get("REMS_TEST_EMAIL", "bugra@rems.com")
TEST_USER_PASSWORD = os.environ.get("REMS_TEST_PASSWORD", "bugr4@rems")
TEST_ADMIN_EMAIL = os.environ.get("REMS_ADMIN_EMAIL", "admin@rems.com")
TEST_ADMIN_PASSWORD = os.environ.get("REMS_ADMIN_PASSWORD", "Admin123!")


@pytest.fixture
def base_url():
    return BASE_URL


@pytest.fixture
def driver():
    options = Options()
    if os.environ.get("REMS_HEADLESS", "1") == "1":
        options.add_argument("--headless=new")
    options.add_argument("--window-size=1400,1000")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")

    drv = webdriver.Chrome(options=options)
    drv.implicitly_wait(2)
    yield drv
    drv.quit()
