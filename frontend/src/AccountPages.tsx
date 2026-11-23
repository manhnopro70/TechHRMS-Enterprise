import { useState } from 'react'
import type { FormEvent } from 'react'
import { api } from './api'
import type { Employee, Role, Session } from './types'
import { Alert, Busy, Empty, Field, Heading, message, Metrics, Modal, Panel, roleName, Submit, Tag, timeText, dateText, useResource } from './ui'

type Common = { session: Session; version: number; reload: () => void; notify: (text: string, good?: boolean) => void }
type Account = { id: number; employeeId: number; employeeName: string; email: string; role: Role; isActive: boolean; mustChangePassword: boolean; createdAtUtc: string; lastLoginAtUtc: string | null }
type Profile = Employee & { role: Role; mustChangePassword: boolean }

export function PasswordForm({ forced = false, onSession }: { forced?: boolean; onSession: (value: Session) => void }) {
  const [current, setCurrent] = useState(''), [next, setNext] = useState(''), [confirm, setConfirm] = useState('')
  const [busy, setBusy] = useState(false), [error, setError] = useState(''), [success, setSuccess] = useState('')
  async function submit(event: FormEvent) {
    event.preventDefault(); setError(''); setSuccess('')
    if (next !== confirm) { setError('Hai mật khẩu mới chưa khớp nhau.'); return }
    if (next === current) { setError('Hãy chọn mật khẩu mới khác mật khẩu hiện tại.'); return }
    setBusy(true)
    try { const value = await api.changePassword(current, next); setCurrent(''); setNext(''); setConfirm(''); setSuccess('Đã đổi mật khẩu và kết thúc các phiên đăng nhập cũ.'); onSession(value) }
    catch (error) { setError(message(error)) } finally { setBusy(false) }
  }
  return <form className="v4-stack" onSubmit={submit}><Alert>{error}</Alert><Alert kind="success">{success}</Alert>
    <Field label={forced ? 'Mật khẩu tạm thời' : 'Mật khẩu hiện tại'}><input required type="password" maxLength={128} autoComplete="current-password" value={current} onChange={e => setCurrent(e.target.value)}/></Field>
    <Field label="Mật khẩu mới" hint="Từ 12 ký tự. Nên dùng một cụm từ riêng dễ nhớ."><input required minLength={12} maxLength={128} type="password" autoComplete="new-password" value={next} onChange={e => setNext(e.target.value)}/></Field>
    <Field label="Nhập lại mật khẩu mới"><input required minLength={12} maxLength={128} type="password" autoComplete="new-password" value={confirm} onChange={e => setConfirm(e.target.value)}/></Field>
    <div className="actions"><Submit busy={busy}>{forced ? 'Đổi mật khẩu & tiếp tục' : 'Cập nhật mật khẩu'}</Submit></div>
  </form>
}

export function PasswordRequired({ session, onSession, onLogout }: { session: Session; onSession: (value: Session) => void; onLogout: () => void }) {
  const [error, setError] = useState('')
  async function logout() { try { await api.logout(); onLogout() } catch (error) { setError(message(error)) } }
  return <div className="v4-auth-stage"><div className="v4-auth-card"><span className="v4-auth-symbol" aria-hidden="true">✓</span><div className="kicker">HOÀN TẤT TÀI KHOẢN</div><h1>Đặt mật khẩu của bạn</h1><p>Bạn đã đăng nhập bằng tài khoản <strong>{session.email}</strong>. Đổi mật khẩu tạm thời để bắt đầu sử dụng TechHRMS.</p><PasswordForm forced onSession={onSession}/><Alert>{error}</Alert><button className="text-button" onClick={logout}>Đăng xuất</button></div></div>
}

export function ProfilePage({ session, version, reload, notify, onSession }: Common & { onSession: (value: Session) => void }) {
  const { data, loading, error } = useResource<Profile>('/auth/profile', version)
  const [editing, setEditing] = useState(false), [name, setName] = useState(''), [busy, setBusy] = useState(false)
  async function save(event: FormEvent) {
    event.preventDefault(); setBusy(true)
    try { await api.send('PUT', '/auth/profile', { fullName: name.trim() }); const value = await api.me(); if (value) onSession(value); notify('Đã cập nhật tên hiển thị.'); setEditing(false); reload() }
    catch (error) { notify(message(error), false) } finally { setBusy(false) }
  }
  return <><Heading eyebrow="KHÔNG GIAN CÁ NHÂN" title="Tài khoản của tôi" description="Thông tin công việc, quyền truy cập và mật khẩu của bạn."/>
    <Alert>{error}</Alert>{loading ? <Busy/> : data && <div className="v4-two-col"><Panel title="Hồ sơ cá nhân" action={<button className="button secondary" onClick={() => { setName(data.fullName); setEditing(true) }}>Chỉnh sửa tên</button>}>
      <div className="v4-profile-intro"><span className="v4-profile-avatar">{data.fullName.split(' ').slice(-2).map(x => x[0]).join('')}</span><div><h2>{data.fullName}</h2><p>{data.jobTitle}</p><Tag tone="blue">{roleName[session.role]}</Tag></div></div>
      <dl className="v4-details"><div><dt>Email đăng nhập</dt><dd>{data.email}</dd></div><div><dt>Phòng ban</dt><dd>{data.department}</dd></div><div><dt>Hình thức làm việc</dt><dd>{data.workMode}</dd></div><div><dt>Ngày gia nhập</dt><dd>{dateText(data.joinedAt)}</dd></div><div><dt>Trạng thái</dt><dd><Tag status={data.status}/></dd></div></dl>
      <p className="v4-note">Liên hệ HR để cập nhật email, chức danh hoặc phòng ban.</p>
    </Panel><Panel title="Đổi mật khẩu" description="Các thiết bị khác sẽ cần đăng nhập lại sau khi đổi mật khẩu."><PasswordForm onSession={onSession}/></Panel></div>}
    {editing && <Modal title="Cập nhật tên hiển thị" onClose={() => setEditing(false)}><form className="form-body v4-stack" onSubmit={save}><Field label="Họ và tên"><input required minLength={2} maxLength={120} value={name} onChange={e => setName(e.target.value)}/></Field><Submit busy={busy}/></form></Modal>}
  </>
}

export function AccountsPage({ session, version, reload, notify, employees }: Common & { employees: Employee[] }) {
  const { data, loading, error } = useResource<Account[]>('/accounts', version)
  const accounts = data ?? []
  const [query, setQuery] = useState(''), [filter, setFilter] = useState('all')
  const [mode, setMode] = useState<'create' | 'edit' | 'reset' | null>(null), [selected, setSelected] = useState<Account | null>(null)
  const [employeeId, setEmployeeId] = useState(0), [role, setRole] = useState<Role>('Employee'), [password, setPassword] = useState(''), [active, setActive] = useState(true)
  const [busy, setBusy] = useState(false), [formError, setFormError] = useState(''), [visible, setVisible] = useState(false)
  const unlinked = employees.filter(e => e.status !== 'Inactive' && !accounts.some(a => a.employeeId === e.id))
  const shown = accounts.filter(a => `${a.employeeName} ${a.email}`.toLocaleLowerCase('vi-VN').includes(query.toLocaleLowerCase('vi-VN')) && (filter === 'all' || (filter === 'active' ? a.isActive : !a.isActive)))
  function open(kind: 'create' | 'edit' | 'reset', account: Account | null = null) { setSelected(account); setMode(kind); setEmployeeId(unlinked[0]?.id ?? 0); setRole(account?.role ?? 'Employee'); setActive(account?.isActive ?? true); setPassword(''); setVisible(false); setFormError('') }
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setFormError('')
    try {
      if (mode === 'create') await api.send('POST', '/accounts', { employeeId, role, password })
      if (mode === 'edit' && selected) await api.send('PUT', `/accounts/${selected.id}`, { role, isActive: active })
      if (mode === 'reset' && selected) await api.send('POST', `/accounts/${selected.id}/reset-password`, { password })
      notify(mode === 'create' ? 'Đã cấp tài khoản. Nhân viên cần đổi mật khẩu ở lần đăng nhập đầu.' : mode === 'reset' ? 'Đã đặt mật khẩu tạm thời và thu hồi các phiên cũ.' : 'Đã cập nhật tài khoản.'); setPassword(''); setMode(null); reload()
    } catch (error) { setFormError(message(error)) } finally { setBusy(false) }
  }
  async function revoke(account: Account) {
    if (!window.confirm(`Kết thúc mọi phiên đăng nhập của ${account.employeeName}?`)) return
    setBusy(true)
    try { await api.send('POST', `/accounts/${account.id}/revoke-sessions`, {}); notify('Đã thu hồi các phiên đăng nhập.'); reload() }
    catch (error) { notify(message(error), false) } finally { setBusy(false) }
  }
  return <><Heading eyebrow="QUẢN TRỊ / DANH TÍNH" title="Tài khoản & phân quyền" description="Cấp quyền đăng nhập riêng cho từng nhân viên. Mỗi hồ sơ liên kết với một tài khoản." action={<button className="button primary" onClick={() => open('create')}>＋ Cấp tài khoản</button>}/>
    <Metrics items={[{ label: 'Tài khoản', value: accounts.length }, { label: 'Đang hoạt động', value: accounts.filter(a => a.isActive).length }, { label: 'Hồ sơ chưa có tài khoản', value: unlinked.length }, { label: 'Cần đổi mật khẩu', value: accounts.filter(a => a.mustChangePassword).length }]}/>
    <Panel><div className="toolbar v4-toolbar"><input className="search" aria-label="Tìm tài khoản" placeholder="Tìm theo tên hoặc email…" value={query} onChange={e => setQuery(e.target.value)}/><select aria-label="Lọc trạng thái tài khoản" value={filter} onChange={e => setFilter(e.target.value)}><option value="all">Mọi trạng thái</option><option value="active">Đang hoạt động</option><option value="locked">Đã khóa</option></select></div><Alert>{error}</Alert>
      {loading ? <Busy/> : !shown.length ? <Empty title="Không có tài khoản phù hợp" description="Thử điều chỉnh bộ lọc hoặc cấp tài khoản cho một hồ sơ nhân viên."/> : <div className="table-scroll"><table><thead><tr><th>Người dùng</th><th>Vai trò</th><th>Trạng thái</th><th>Đăng nhập gần nhất</th><th>Thao tác</th></tr></thead><tbody>{shown.map(a => <tr key={a.id}><td><strong>{a.employeeName}</strong><small className="v4-cell-sub">{a.email}</small></td><td><Tag tone="blue">{roleName[a.role]}</Tag></td><td><Tag status={a.isActive ? 'Active' : 'Inactive'}>{a.isActive ? 'Hoạt động' : 'Đã khóa'}</Tag>{a.mustChangePassword && <small className="v4-cell-sub">Cần đổi mật khẩu</small>}</td><td>{timeText(a.lastLoginAtUtc)}</td><td><div className="table-actions v4-wrap"><button className="text-button" disabled={busy} onClick={() => open('edit', a)}>Phân quyền</button>{a.employeeId !== session.id && <><button className="text-button" disabled={busy} onClick={() => open('reset', a)}>Đặt lại mật khẩu</button><button className="text-button reject" disabled={busy} onClick={() => revoke(a)}>Thu hồi phiên</button></>}</div></td></tr>)}</tbody></table></div>}
    </Panel>
    {mode && <Modal title={mode === 'create' ? 'Cấp tài khoản nhân viên' : mode === 'edit' ? 'Cập nhật quyền truy cập' : 'Đặt mật khẩu tạm thời'} onClose={() => { if (!busy) { setMode(null); setPassword('') } }}><form className="form-body v4-stack" onSubmit={submit}>
      <Alert>{formError}</Alert>{mode === 'create' ? <><Field label="Hồ sơ nhân viên"><select required value={employeeId} onChange={e => setEmployeeId(Number(e.target.value))}><option value={0} disabled>Chọn hồ sơ</option>{unlinked.map(e => <option key={e.id} value={e.id}>{e.fullName} · {e.email}</option>)}</select></Field>{!unlinked.length && <Alert kind="info">Các hồ sơ đang hoạt động đã được cấp tài khoản. Hãy tạo hồ sơ mới trong mục Nhân viên.</Alert>}</> : <div className="v4-selected-person"><strong>{selected?.employeeName}</strong><span>{selected?.email}</span></div>}
      {mode !== 'reset' && <Field label="Vai trò"><select value={role} onChange={e => setRole(e.target.value as Role)} disabled={selected?.employeeId === session.id}>{Object.entries(roleName).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></Field>}
      {mode === 'edit' ? <label className="checkline"><input type="checkbox" checked={active} disabled={selected?.employeeId === session.id} onChange={e => setActive(e.target.checked)}/> Cho phép đăng nhập</label> : <><Field label="Mật khẩu tạm thời" hint="Từ 12 ký tự. Gửi riêng cho nhân viên; hệ thống yêu cầu đổi khi đăng nhập."><div className="v4-password-input"><input type={visible ? 'text' : 'password'} autoComplete="new-password" required minLength={12} maxLength={128} value={password} onChange={e => setPassword(e.target.value)}/><button className="text-button" type="button" onClick={() => setVisible(!visible)}>{visible ? 'Ẩn' : 'Hiện'}</button></div></Field></>}
      <div className="modal-actions"><button type="button" className="button secondary" disabled={busy} onClick={() => { setMode(null); setPassword('') }}>Hủy</button><Submit busy={busy || (mode === 'create' && !employeeId)}>{mode === 'create' ? 'Tạo tài khoản' : 'Lưu thay đổi'}</Submit></div>
    </form></Modal>}
  </>
}
