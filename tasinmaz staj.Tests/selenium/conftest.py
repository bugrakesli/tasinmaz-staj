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


# Bir test basarisiz oldugunda ekran goruntusu ve sayfa kaynagini
# tasinmaz staj.Tests/selenium/failures/ altina kaydeder; CI/lokal
# debug icin faydali.
@pytest.hookimpl(tryfirst=True, hookwrapper=True)
def pytest_runtest_makereport(item, call):
    outcome = yield
    report = outcome.get_result()

    if report.when == "call" and report.failed:
        drv = item.funcargs.get("driver")
        if drv is not None:
            failures_dir = os.path.join(os.path.dirname(__file__), "failures")
            os.makedirs(failures_dir, exist_ok=True)
            safe_name = item.name.replace("/", "_")

            screenshot_path = os.path.join(failures_dir, f"{safe_name}.png")
            html_path = os.path.join(failures_dir, f"{safe_name}.html")

            try:
                drv.save_screenshot(screenshot_path)
            except Exception:
                pass

            try:
                with open(html_path, "w", encoding="utf-8") as f:
                    f.write(drv.page_source)
            except Exception:
                pass
