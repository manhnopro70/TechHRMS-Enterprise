"""Browser smoke checks against a disposable local API + SQLite database.

Requires: Python playwright package, Chromium, built frontend/dist and backend Release.
Usage: python tests/ui-smoke.py --dotnet dotnet --chromium /usr/bin/chromium
Screenshots/logs are internal review files outside the deliverable repository by default.
"""
from __future__ import annotations
import argparse
import json
import os
from pathlib import Path
import tempfile

from playwright.sync_api import sync_playwright, expect
from run_integration import Server, Client, ROOT, DEMO_PASSWORD, TEMP_PASSWORD, STAFF_PASSWORD


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--dotnet', default='dotnet')
    parser.add_argument('--chromium', default=None, help='Optional installed Chromium/Chrome executable; otherwise Playwright managed browser')
    parser.add_argument('--screenshots', default=str(ROOT.parent / 'ui-review'))
    args = parser.parse_args()
    output = Path(args.screenshots)
    output.mkdir(parents=True, exist_ok=True)
    os.environ['ASPNETCORE_WEBROOT'] = str(ROOT / 'frontend/dist')
    results = []
    errors = []
    failed_http = []
    def checked(condition, label):
        assert condition, label
        results.append(label)
        print('PASS ' + label, flush=True)
    def install_listeners(page):
        page.on('pageerror', lambda error: errors.append(str(error)))
        page.on('response', lambda response: failed_http.append(f'{response.status} {response.url}') if response.status >= 500 else None)
    def login(page, email, password):
        page.get_by_label('Email công việc', exact=True).fill(email)
        page.get_by_label('Mật khẩu', exact=True).fill(password)
        page.get_by_role('button', name='Đăng nhập vào TechHRMS', exact=False).click()
    def open_module(page, key):
        page.evaluate('(key) => { window.location.hash = "/" + key }', key)
        page.wait_for_load_state('networkidle')
        expect(page.locator('main h1').first).to_be_visible()
        alerts = page.locator('.error-banner, .v4-alert-error, .v3-feedback.is-error')
        checked(alerts.count() == 0, f'Admin module {key}: data loads without error banners')
    with tempfile.TemporaryDirectory(prefix='techhrms-ui-') as directory:
        directory = Path(directory)
        server = Server(args.dotnet, directory / 'ui.db', output / 'api-ui.log')
        try:
            server.start()
            admin_api = Client(server.base)
            admin_api.login('admin@techhrms.local')
            employees = admin_api.call('GET', '/employees')
            linh = next(e for e in employees if e['email'] == 'linh@techhrms.local')
            with sync_playwright() as pw:
                browser = pw.chromium.launch(executable_path=args.chromium, headless=True, args=['--no-sandbox'])
                context = browser.new_context(viewport={'width':1440,'height':1000}, locale='vi-VN', timezone_id='Asia/Ho_Chi_Minh')
                page = context.new_page()
                install_listeners(page)
                page.set_default_timeout(15000)
                page.goto(server.base)
                expect(page.get_by_role('heading', name='Chào mừng trở lại')).to_be_visible()
                login(page, 'admin@techhrms.local', DEMO_PASSWORD)
                expect(page.get_by_role('navigation')).to_be_visible()
                checked(page.get_by_text('Không gian Quản trị', exact=False).count() > 0, 'Admin UI login succeeds through CSRF client')
                keys = ['dashboard','profile','notifications','employees','departments','organization','contracts','onboarding','attendance','requests','skills','projects','timesheets','assets','candidates','objectives','learning','payroll','insights','accounts','audit','roadmap']
                for key in keys:
                    open_module(page, key)
                checked(not errors, 'All Admin modules have no JavaScript page errors')
                checked(not failed_http, 'All Admin modules have no HTTP 5xx')
                open_module(page, 'dashboard')
                page.screenshot(path=str(output / 'dashboard-desktop.png'), full_page=True)
                open_module(page, 'accounts')
                page.get_by_role('button', name='Cấp tài khoản', exact=False).click()
                dialog = page.get_by_role('dialog', name='Cấp tài khoản nhân viên')
                dialog.get_by_label('Hồ sơ nhân viên').select_option(str(linh['id']))
                dialog.get_by_label('Vai trò', exact=True).select_option('Employee')
                dialog.get_by_label('Mật khẩu tạm thời', exact=False).fill(TEMP_PASSWORD)
                dialog.get_by_role('button', name='Tạo tài khoản', exact=True).click()
                expect(dialog).not_to_be_visible()
                expect(page.get_by_text('linh@techhrms.local', exact=True)).to_be_visible()
                account = next(a for a in admin_api.call('GET','/accounts') if a['employeeId']==linh['id'])
                checked(account['mustChangePassword'], 'Admin UI creates linked employee account persisted in database')
                page.screenshot(path=str(output / 'accounts-desktop.png'), full_page=True)
                # Hash links, reload, and browser history.
                open_module(page, 'employees')
                page.reload(); expect(page.get_by_role('heading', name='Quản lý nhân sự', exact=True)).to_be_visible()
                checked(page.url.endswith('#/employees'), 'Refresh keeps employee route and authenticated session')
                open_module(page, 'profile')
                page.go_back(); expect(page.get_by_role('heading', name='Quản lý nhân sự', exact=True)).to_be_visible()
                checked(page.url.endswith('#/employees'), 'Back navigation restores previous module')
                # Independent staff session, forced password change, and permission-aware menus.
                staff_context = browser.new_context(viewport={'width':1280,'height':900}, locale='vi-VN', timezone_id='Asia/Ho_Chi_Minh')
                staff = staff_context.new_page(); staff.set_default_timeout(15000); install_listeners(staff)
                staff.goto(server.base); login(staff, linh['email'], TEMP_PASSWORD)
                expect(staff.get_by_role('heading', name='Đặt mật khẩu của bạn')).to_be_visible()
                checked(staff.get_by_role('navigation').count()==0, 'First login gated before workspace renders')
                staff.get_by_label('Mật khẩu tạm thời', exact=True).fill(TEMP_PASSWORD)
                staff.get_by_label('Mật khẩu mới', exact=False).first.fill(STAFF_PASSWORD)
                staff.get_by_label('Nhập lại mật khẩu mới', exact=True).fill(STAFF_PASSWORD)
                staff.get_by_role('button', name='Đổi mật khẩu & tiếp tục').click()
                expect(staff.get_by_role('navigation')).to_be_visible()
                checked(staff.get_by_role('navigation').get_by_role('button',name='Tài khoản & phân quyền',exact=True).count()==0, 'Employee menu hides Admin account management')
                checked(staff.get_by_role('navigation').get_by_role('button',name='Nhật ký hệ thống',exact=True).count()==0, 'Employee menu hides Admin audit')
                open_module(staff, 'employees')
                checked(staff.locator('tbody tr').count()==1, 'Employee UI only shows own employee profile')
                staff.reload(); expect(staff.get_by_role('heading', name='Quản lý nhân sự',exact=True)).to_be_visible()
                # Mobile viewport: all main employee pages stay inside viewport; table regions scroll independently.
                mobile_context = browser.new_context(viewport={'width':390,'height':844}, device_scale_factor=1, is_mobile=True, has_touch=True, locale='vi-VN', timezone_id='Asia/Ho_Chi_Minh')
                mobile = mobile_context.new_page(); install_listeners(mobile); mobile.set_default_timeout(15000)
                mobile.goto(server.base); login(mobile,linh['email'],STAFF_PASSWORD)
                expect(mobile.locator('main h1').first).to_be_visible()
                mobile.get_by_role('button',name='Mở menu điều hướng').click()
                expect(mobile.get_by_role('navigation')).to_be_visible()
                mobile.get_by_role('navigation').get_by_role('button',name='Tài khoản của tôi',exact=True).click()
                expect(mobile.get_by_role('heading',name='Tài khoản của tôi',exact=True)).to_be_visible()
                for key in ['dashboard','profile','employees','requests','timesheets','payroll','notifications']:
                    open_module(mobile,key)
                    sizes=mobile.evaluate('({viewport:innerWidth,scroll:document.documentElement.scrollWidth})')
                    checked(sizes['scroll']<=sizes['viewport']+1,f'Mobile {key}: no document horizontal overflow ({sizes})')
                open_module(mobile,'profile')
                mobile.screenshot(path=str(output / 'profile-mobile.png'),full_page=True)
                # Session revocation must leave protected workspace on next call, without refresh workaround.
                admin_api.call('POST',f"/accounts/{account['id']}/revoke-sessions",{})
                staff.evaluate('location.hash="/profile"')
                expect(staff.get_by_role('heading',name='Chào mừng trở lại')).to_be_visible()
                expect(staff.get_by_text('Phiên đăng nhập đã kết thúc.',exact=False)).to_be_visible()
                checked(True,'Revoked session automatically returns UI to Login with expiry message')
                checked(not errors,'No JavaScript errors in Admin, staff, mobile, or forced-password flows')
                checked(not failed_http,'No HTTP5xx during browser interactions')
                browser.close()
        finally:
            server.stop()
    summary={'checks':len(results),'passed':results,'javascriptErrors':errors,'serverErrors':failed_http}
    (output/'ui-smoke-result.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2))
    print(json.dumps({'checks':len(results),'result':'PASS','review_directory':str(output)},ensure_ascii=False),flush=True)

if __name__=='__main__': main()
