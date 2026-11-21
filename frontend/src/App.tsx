import { useEffect, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { api } from './api'
import type { Session, Page, Department, Employee, Dashboard, WorkRequest, Skill, Asset, Project, Allocation, Candidate, Objective, Audit, Role } from './types'
import './App.css'
import './premium.css'
import './enterprise.css'
import './refined.css'
import { OrganizationPage, ContractsPage, OnboardingPage, AttendancePage, LearningPage, InsightsPage, RoadmapPage } from './EnterprisePages'

function Glyph({ name, size = 18 }: { name: string; size?: number }) {
  const shapes: Record<string, ReactNode> = {
    home: <><rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/></>,
    users: <><circle cx="9" cy="8" r="3"/><path d="M3 20v-2a6 6 0 0 1 12 0v2"/><path d="M16 5a3 3 0 0 1 0 6M17 14a5 5 0 0 1 4 5v1"/></>,
    building: <><rect x="3" y="4" width="18" height="17" rx="2"/><path d="M8 9h2M14 9h2M8 13h2M14 13h2M10 21v-4h4v4"/></>,
    calendar: <><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M7 3v4M17 3v4M3 10h18"/></>,
    sparkles: <><path d="m12 2 1.8 6.2L20 10l-6.2 1.8L12 18l-1.8-6.2L4 10l6.2-1.8z"/><path d="m19 17 .7 2.3L22 20l-2.3.7L19 23l-.7-2.3L16 20l2.3-.7z"/></>,
    projects: <><path d="M3 7h7l2 2h9v11H3z"/><path d="M3 7V5a2 2 0 0 1 2-2h5l2 2"/></>,
    laptop: <><rect x="4" y="4" width="16" height="12" rx="2"/><path d="m2 20 2-4h16l2 4z"/></>,
    briefcase: <><rect x="3" y="7" width="18" height="14" rx="2"/><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 13c5 3 13 3 18 0"/></>,
    target: <><circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="5"/><circle cx="12" cy="12" r="1"/></>,
    history: <><path d="M3 11a9 9 0 1 1 3 7M3 4v7h7"/><path d="M12 7v5l3 2"/></>,
    search: <><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 5 5"/></>,
    bell: <><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4"/></>,
    chevron: <path d="m7 10 5 5 5-5"/>,
    arrow: <><path d="M5 12h14M13 6l6 6-6 6"/></>,
    arrowup: <><path d="m6 17 5-5 4 4 5-7"/><path d="M15 9h5v5"/></>,
    shield: <><path d="m12 2 8 4v6c0 5-4 8.5-8 10-4-1.5-8-5-8-10V6z"/><path d="m8 12 3 3 5-6"/></>,
    logout: <><path d="M10 4H5a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h5M16 17l5-5-5-5M21 12H9"/></>,
    menu: <><path d="M4 6h16M4 12h16M4 18h16"/></>,
    close: <><path d="M5 5l14 14M19 5 5 19"/></>,
    check: <><path d="m4 12 5 5L20 6"/></>,
    clock: <><circle cx="12" cy="12" r="9"/><path d="M12 7v5l4 3"/></>,
    sliders: <><path d="M4 7h16M4 17h16"/><circle cx="9" cy="7" r="2" fill="currentColor"/><circle cx="16" cy="17" r="2" fill="currentColor"/></>,
    pulse: <><path d="M2 12h5l3-7 4 14 3-7h5"/></>,
    layers: <><path d="m12 3 9 5-9 5-9-5zM3 12l9 5 9-5M3 16l9 5 9-5"/></>,
    globe: <><circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3a15 15 0 0 1 0 18M12 3a15 15 0 0 0 0 18"/></>,
    contract: <><path d="M6 2h9l4 4v16H6zM15 2v5h4M9 12h7M9 16h7"/></>,
    task: <><rect x="4" y="3" width="16" height="19" rx="2"/><path d="m8 12 2 2 4-4M8 18h8"/></>,
    checkin: <><circle cx="12" cy="12" r="9"/><path d="M12 7v5l4 2"/></>,
    learning: <><path d="m2 6 10-4 10 4-10 4-10-4M5 8v9c5 3 9 3 14 0V8M22 6v10"/></>,
    analytics: <><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M7 17v-5M12 17V7M17 17v-8"/></>,
    roadmap: <><circle cx="6" cy="5" r="2"/><circle cx="18" cy="12" r="2"/><circle cx="6" cy="19" r="2"/><path d="M8 5h5a5 5 0 0 1 0 10H8"/></>,
  }
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{shapes[name] ?? shapes.home}</svg>
}

const menu: { id: Page; label: string; icon: string; roles?: Role[] }[] = [
  { id: 'dashboard', label: 'Tổng quan', icon: 'home' },
  { id: 'employees', label: 'Hồ sơ nhân viên', icon: 'users' },
  { id: 'departments', label: 'Phòng ban', icon: 'building', roles: ['Admin', 'HR', 'Manager'] },
  { id: 'organization', label: 'Sơ đồ tổ chức', icon: 'layers' },
  { id: 'contracts', label: 'Hợp đồng', icon: 'contract' },
  { id: 'onboarding', label: 'Onboarding', icon: 'task' },
  { id: 'attendance', label: 'Chấm công', icon: 'checkin' },
  { id: 'requests', label: 'Nghỉ phép · WFH · OT', icon: 'calendar' },
  { id: 'skills', label: 'Ma trận kỹ năng', icon: 'sparkles' },
  { id: 'projects', label: 'Dự án & nguồn lực', icon: 'projects' },
  { id: 'assets', label: 'Tài sản IT', icon: 'laptop' },
  { id: 'candidates', label: 'Tuyển dụng ATS', icon: 'briefcase', roles: ['Admin', 'HR'] },
  { id: 'objectives', label: 'OKR & hiệu suất', icon: 'target' },
  { id: 'learning', label: 'Đào tạo & khóa học', icon: 'learning' },
  { id: 'insights', label: 'Báo cáo nhân sự', icon: 'analytics', roles: ['Admin', 'HR', 'Manager'] },
  { id: 'audit', label: 'Nhật ký hệ thống', icon: 'history', roles: ['Admin'] },
  { id: 'roadmap', label: 'Phạm vi dự án', icon: 'roadmap' },
]
const canManage = (role: Role) => role === 'Admin' || role === 'HR'
const canApprove = (role: Role) => canManage(role) || role === 'Manager'
const formatDate = (value: string) => value ? new Date(value.slice(0, 10) + 'T00:00:00').toLocaleDateString('vi-VN') : '—'
const initials = (name: string) => name.split(' ').slice(-2).map(p => p[0]).join('').toUpperCase()
const badge = (status: string) => {
  const map: Record<string, [string, string]> = {
    Active: ['Đang làm', 'good'], Inactive: ['Ngừng làm', 'muted'], OnLeave: ['Đang nghỉ', 'warn'],
    Pending: ['Chờ duyệt', 'warn'], Approved: ['Đã duyệt', 'good'], Rejected: ['Từ chối', 'bad'],
    Hybrid: ['Hybrid', 'blue'], Remote: ['Remote', 'purple'], Onsite: ['Onsite', 'muted'],
    Assigned: ['Đã cấp', 'blue'], Available: ['Sẵn có', 'good'],
  }
  const [label, tone] = map[status] ?? [status, 'muted']
  return <span className={`badge badge-${tone}`}>{label}</span>
}

function useList<T>(url: string, version: number) {
  const [items, setItems] = useState<T[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  useEffect(() => {
    let active = true
    setLoading(true); setError('')
    api.get<T[]>(url).then(data => { if (active) setItems(data) })
      .catch(e => { if (active) setError(e instanceof Error ? e.message : 'Không tải được dữ liệu.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [url, version])
  return { items, loading, error }
}

function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) {
  useEffect(() => {
    function esc(e: KeyboardEvent) { if (e.key === 'Escape') onClose() }
    window.addEventListener('keydown', esc)
    return () => window.removeEventListener('keydown', esc)
  }, [onClose])
  return <div className="modal-backdrop" onMouseDown={onClose}>
    <div className="modal-card" role="dialog" aria-modal="true" aria-label={title} onMouseDown={e => e.stopPropagation()}>
      <div className="modal-header"><h2>{title}</h2><button className="icon-button" aria-label="Đóng" onClick={onClose}>×</button></div>
      {children}
    </div>
  </div>
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return <label className="field"><span>{label}</span>{children}</label>
}

function Empty({ text = 'Chưa có dữ liệu.' }: { text?: string }) { return <div className="empty">✧<p>{text}</p></div> }
function Loading() { return <div className="loading">Đang tải dữ liệu…</div> }
function ErrorMessage({ text }: { text: string }) { return text ? <p className="error-banner">{text}</p> : null }
function PageTitle({ kicker, title, subtitle, action }: { kicker: string; title: string; subtitle: string; action?: ReactNode }) {
  return <div className="page-heading"><div><div className="kicker">{kicker}</div><h1>{title}</h1><p>{subtitle}</p></div>{action}</div>
}
function Avatar({ name }: { name: string }) { return <span className="avatar" aria-hidden="true">{initials(name)}</span> }
function CsvButton({ rows }: { rows: Employee[] }) {
  const download = () => {
    const columns = ['ID', 'Họ tên', 'Email', 'Chức danh', 'Phòng ban', 'Mô hình', 'Trạng thái']
    const values = rows.map(e => [e.id, e.fullName, e.email, e.jobTitle, e.department, e.workMode, e.status])
    const excelSafe = (value: string | number) => {
      const raw = String(value)
      return /^[=+@\-\t\r]/.test(raw.trimStart()) ? "'" + raw : raw
    }
    const csv = '\uFEFF' + [columns, ...values].map(row => row.map(c => `"${excelSafe(c).replaceAll('"', '""')}"`).join(',')).join('\r\n')
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a'); link.href = url; link.download = 'techhrms-nhan-su.csv'; link.click()
    URL.revokeObjectURL(url)
  }
  return <button className="button secondary" onClick={download}>↓ Xuất CSV</button>
}

function Login({ onLogin }: { onLogin: (session: Session) => void }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  async function submit(e: FormEvent) {
    e.preventDefault(); setError(''); setBusy(true)
    try { onLogin(await api.login(email, password)) }
    catch (err) { setError(err instanceof Error ? err.message : 'Đăng nhập thất bại.') }
    finally { setBusy(false) }
  }
  return <div className="login-screen">
    <div className="login-hero"><div className="hero-grid"/>
      <div className="brand brand-light"><span className="brand-square"><Glyph name="sparkles" size={21}/></span><strong>TechHRMS</strong></div>
      <div className="login-hero-text"><span className="eyebrow">TECHHRMS · PEOPLE OPERATIONS</span>
        <h1>Chăm sóc con người.<br/><em>Thúc đẩy hiệu suất.</em></h1>
        <p>Quản lý nhân sự, chấm công, phát triển năng lực và vận hành đội ngũ công nghệ trong một nền tảng thống nhất.</p>
        <div className="login-features"><span>✓ Cổng nhân viên & quản trị</span><span>✓ Kỹ năng & nguồn lực</span><span>✓ Làm việc Hybrid</span></div>
      </div><div className="login-bottom">TECHHRMS V3 · NỀN TẢNG NHÂN SỰ CHO CÔNG TY TECH</div>
    </div>
    <div className="login-panel"><form className="login-form" onSubmit={submit}>
      <div className="mobile-brand brand"><span className="brand-square"><Glyph name="sparkles" size={21}/></span><strong>TechHRMS</strong></div>
      <span className="eyebrow blue-text">ĐĂNG NHẬP HỆ THỐNG</span>
      <h2>Chào mừng trở lại</h2><p>Đăng nhập để vào cổng thông tin phù hợp với vai trò của bạn.</p>
      <Field label="Email công việc"><input type="email" required autoComplete="username" value={email} onChange={e => setEmail(e.target.value)} placeholder="you@company.com" /></Field>
      <Field label="Mật khẩu"><input type="password" required autoComplete="current-password" value={password} onChange={e => setPassword(e.target.value)} placeholder="Nhập mật khẩu" /></Field>
      <ErrorMessage text={error}/>
      <button type="submit" className="button primary wide" disabled={busy}>{busy ? 'Đang xác thực…' : 'Đăng nhập vào TechHRMS  →'}</button>
      <div className="login-note">Phiên bản thử nghiệm. Không nhập dữ liệu nhân sự thật.</div>
    </form></div>
  </div>
}

function DashboardPage({ session, version, onNavigate }: { session: Session; version: number; onNavigate: (page: Page) => void }) {
  const [data, setData] = useState<Dashboard | null>(null)
  const [error, setError] = useState('')
  const requests = useList<WorkRequest>('/requests', version)
  useEffect(() => { let active = true
    api.get<Dashboard>('/dashboard').then(d => { if (active) { setData(d); setError('') } })
      .catch(e => { if (active) setError(e instanceof Error ? e.message : 'Không tải được Dashboard.') })
    return () => { active = false }
  }, [version])
  const role = session.role === 'Employee' ? 'Nhân viên' : session.role === 'Manager' ? 'Quản lý nhóm' : 'Quản trị nhân sự'
  const salutation = new Date().getHours() < 12 ? 'Chào buổi sáng' : new Date().getHours() < 18 ? 'Chào buổi chiều' : 'Chào buổi tối'
  const cards = [
    { label: data?.isPersonal ? 'Hồ sơ của tôi' : 'Tổng nhân sự', number: data?.totalEmployees ?? 0, icon: 'users', tip: 'Dữ liệu theo quyền xem', tint: 'lilac' },
    { label: 'Phòng ban', number: data?.departments ?? 0, icon: 'building', tip: 'Cấu trúc tổ chức', tint: 'sky' },
    { label: 'Đơn chờ xử lý', number: data?.pendingRequests ?? 0, icon: 'calendar', tip: 'Yêu cầu đang chờ', tint: 'peach' },
    { label: data?.isPersonal ? 'Thiết bị của tôi' : 'Nhân sự Bench', number: data?.isPersonal ? (data?.assets ?? 0) : (data?.benchEmployees ?? 0), icon: 'projects', tip: 'Theo dữ liệu hiện tại', tint: 'mint' },
  ]
  const peak = Math.max(1, ...(data?.workforceByDepartment.map(x => x.count) ?? [1]))
  const remoteRatio = data?.totalEmployees ? Math.round(data.remoteEmployees / data.totalEmployees * 100) : 0
  return <div className="v3-dashboard">
    <div className="v3-welcome">
      <div className="v3-welcome-copy"><span className="v3-bubble">✦ {role} · TechHRMS Workspace</span>
        <h1>{salutation}!<br/><em>Cùng làm việc hiệu quả.</em></h1>
        <p>Mọi thứ về con người, công việc và phát triển đội ngũ — trong một không gian dễ sử dụng.</p>
        <div className="v3-welcome-actions"><button className="v3-primary" onClick={() => onNavigate('requests')}>Tạo / xem đơn từ <Glyph name="arrow" size={17}/></button>
          <button className="v3-ghost" onClick={() => onNavigate('attendance')}><Glyph name="clock" size={17}/> Chấm công hôm nay</button></div>
      </div>
      <div className="v3-hero-visual" aria-hidden="true"><div className="v3-orbit o1"/><div className="v3-orbit o2"/>
        <span className="v3-hero-circle a">HR</span><span className="v3-hero-circle b">DEV</span><span className="v3-hero-circle c">UX</span>
        <div className="v3-hero-core"><Glyph name="users" size={36}/><span>People<br/>in sync</span></div>
        <div className="v3-hero-label">✦ Teamwork made simple</div></div>
    </div>
    <div className="v3-between"><div><span className="v3-eyebrow">DỮ LIỆU HIỆN TẠI</span><h2>Bảng điều khiển</h2></div><span className="v3-live"><i/> Kết nối API TechHRMS</span></div>
    <ErrorMessage text={error}/>
    {!data ? <Loading/> : <>
      <div className="v3-metrics">{cards.map(c => <article className={`v3-metric ${c.tint}`} key={c.label}>
        <div className="v3-metric-symbol"><Glyph name={c.icon} size={22}/></div><span>{c.label}</span><strong>{c.number.toLocaleString('vi-VN')}</strong><small>{c.tip}</small></article>)}</div>
      <div className="v3-dashboard-columns">
        <section className="v3-panel v3-workforce"><div className="v3-panel-heading"><div><span className="v3-eyebrow">TỔ CHỨC</span><h3>Nhân sự theo phòng ban</h3><p>Thống kê trực tiếp từ dữ liệu bạn được phép xem.</p></div><button onClick={() => onNavigate('organization')}>Xem sơ đồ <Glyph name="arrow" size={15}/></button></div>
          {data.workforceByDepartment.length ? data.workforceByDepartment.map((x,i) => <div className="v3-bar-item" key={x.name}><div><span>{x.name}</span><strong>{x.count}</strong></div><div className="v3-bar-track"><i className={`tone-${i%4}`} style={{ width: `${x.count / peak * 100}%` }}/></div></div>) : <Empty text="Chưa có dữ liệu nhân sự."/>}</section>
        <section className="v3-panel v3-flex-card"><div className="v3-panel-heading"><div><span className="v3-eyebrow">WORK MODE</span><h3>Đội ngũ linh hoạt</h3><p>Tỷ lệ nhân viên Remote</p></div></div>
          <div className="v3-ring" style={{ background: `conic-gradient(#0b9b84 ${remoteRatio}%, #e6f1ee ${remoteRatio}% 100%)` }}><div><strong>{remoteRatio}%</strong><small>Remote</small></div></div>
          <div className="v3-ring-caption"><span><i/> {data.remoteEmployees} Remote</span><span>{Math.max(0, data.totalEmployees-data.remoteEmployees)} khác</span></div>
        </section>
      </div>
      <div className="v3-dashboard-columns v3-bottom-columns">
        <section className="v3-panel"><div className="v3-panel-heading"><div><span className="v3-eyebrow">LUỒNG CÔNG VIỆC</span><h3>Đơn từ gần đây</h3><p>Hoạt động trong phạm vi quyền của bạn</p></div><button onClick={() => onNavigate('requests')}>Xem tất cả <Glyph name="arrow" size={15}/></button></div>
          {requests.loading ? <Loading/> : requests.error ? <ErrorMessage text={requests.error}/> : requests.items.length === 0 ? <Empty text="Chưa có đơn từ nào."/> : <div className="v3-recent-list">{requests.items.slice(0,4).map(x => <div key={x.id} className="v3-recent-row"><Avatar name={x.employee}/><div><strong>{x.employee}</strong><small>{requestLabel[x.type] ?? x.type} · {formatDate(x.startDate)}</small></div>{badge(x.status)}</div>)}</div>}
        </section>
        <section className="v3-panel"><div className="v3-panel-heading"><div><span className="v3-eyebrow">BẮT ĐẦU NHANH</span><h3>Bạn muốn làm gì?</h3><p>Truy cập tác vụ thường dùng</p></div></div>
          <div className="v3-shortcuts">{[
            ['Hồ sơ nhân viên', 'Tìm nhân sự và phòng ban', 'employees', 'users'],
            ['Kế hoạch onboarding', 'Theo dõi việc tiếp nhận', 'onboarding', 'task'],
            ['Đào tạo nội bộ', 'Khám phá khóa học', 'learning', 'learning'],
          ].map(([label,desc,to,icon]) => <button key={to} onClick={() => onNavigate(to as Page)}><span><Glyph name={icon} size={18}/></span><div><strong>{label}</strong><small>{desc}</small></div><Glyph name="arrow" size={16}/></button>)}</div>
        </section>
      </div>
    </>}
  </div>
}

function EmployeesPage({ session, departments, version, reload, notify }: { session: Session; departments: Department[]; version: number; reload: () => void; notify: (s: string, good?: boolean) => void }) {
  const { items, loading, error } = useList<Employee>('/employees', version)
  const [query, setQuery] = useState('')
  const blank = { fullName: '', email: '', jobTitle: '', departmentId: 0, workMode: 'Hybrid', status: 'Active', isBench: false }
  const [form, setForm] = useState(blank)
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<number | null>(null)
  const [busy, setBusy] = useState(false)
  const filtered = items.filter(x => `${x.fullName} ${x.email} ${x.jobTitle} ${x.department}`.toLowerCase().includes(query.toLowerCase()))
  const launch = (employee?: Employee) => {
    setEditing(employee?.id ?? null)
    setForm(employee ? { fullName: employee.fullName, email: employee.email, jobTitle: employee.jobTitle, departmentId: employee.departmentId,
      workMode: employee.workMode, status: employee.status, isBench: employee.isBench } : { ...blank, departmentId: departments[0]?.id ?? 0 })
    setOpen(true)
  }
  async function save(e: FormEvent) {
    e.preventDefault(); setBusy(true)
    try { await api.send(editing ? 'PUT' : 'POST', editing ? `/employees/${editing}` : '/employees', form)
      notify(editing ? 'Đã cập nhật nhân viên.' : 'Đã thêm hồ sơ nhân viên.'); setOpen(false); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không lưu được.', false) }
    finally { setBusy(false) }
  }
  return <>
    <PageTitle kicker="PEOPLE / DIRECTORY" title="Quản lý nhân sự" subtitle="Hồ sơ đội ngũ, mô hình làm việc, trạng thái và nguồn lực Bench."
      action={<div className="actions"><CsvButton rows={filtered}/>{canManage(session.role) && <button className="button primary" onClick={() => launch()}>＋ Thêm nhân viên</button>}</div>}/>
    <div className="panel"><div className="toolbar"><input className="search" placeholder="⌕ Tìm theo tên, email, kỹ năng chức danh…" value={query} onChange={e => setQuery(e.target.value)}/><span className="result-count">{filtered.length} nhân sự</span></div>
      <ErrorMessage text={error}/>{loading ? <Loading/> : filtered.length === 0 ? <Empty text="Không tìm thấy nhân viên phù hợp."/> :
        <div className="table-scroll"><table><thead><tr><th>Nhân viên</th><th>Chức danh</th><th>Phòng ban</th><th>Hình thức</th><th>Trạng thái</th>{canManage(session.role) && <th>Thao tác</th>}</tr></thead>
          <tbody>{filtered.map(x => <tr key={x.id}><td><div className="person"><Avatar name={x.fullName}/><div><strong>{x.fullName}</strong><small>{x.email}</small></div></div></td><td>{x.jobTitle}{x.isBench && <span className="mini-tag">Bench</span>}</td><td>{x.department}</td><td>{badge(x.workMode)}</td><td>{badge(x.status)}</td>{canManage(session.role) && <td><button className="text-button" onClick={() => launch(x)}>Chỉnh sửa</button></td>}</tr>)}</tbody></table></div>}
    </div>
    {open && <Modal title={editing ? 'Chỉnh sửa nhân viên' : 'Thêm nhân viên mới'} onClose={() => setOpen(false)}>
      <form onSubmit={save} className="form-body"><div className="form-grid">
        <Field label="Họ và tên"><input required minLength={2} maxLength={120} value={form.fullName} onChange={e => setForm({ ...form, fullName: e.target.value })}/></Field>
        <Field label="Email công việc"><input required type="email" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })}/></Field>
        <Field label="Chức danh"><input required maxLength={120} value={form.jobTitle} onChange={e => setForm({ ...form, jobTitle: e.target.value })}/></Field>
        <Field label="Phòng ban"><select required value={form.departmentId} onChange={e => setForm({ ...form, departmentId: Number(e.target.value) })}><option value={0} disabled>Chọn phòng ban</option>{departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}</select></Field>
        <Field label="Mô hình làm việc"><select value={form.workMode} onChange={e => setForm({ ...form, workMode: e.target.value })}><option value="Hybrid">Hybrid</option><option value="Remote">Remote</option><option value="Onsite">Onsite</option></select></Field>
        <Field label="Trạng thái"><select value={form.status} onChange={e => setForm({ ...form, status: e.target.value })}><option value="Active">Đang làm</option><option value="OnLeave">Đang nghỉ</option><option value="Inactive">Ngừng làm</option></select></Field>
      </div><label className="checkline"><input type="checkbox" checked={form.isBench} onChange={e => setForm({ ...form, isBench: e.target.checked })}/> Nhân viên đang On Bench</label>
      <div className="form-info">Lưu hồ sơ chưa tự tạo tài khoản đăng nhập. Quy trình cấp tài khoản sẽ bổ sung trong giai đoạn tiếp theo.</div>
      <div className="modal-actions"><button type="button" className="button secondary" onClick={() => setOpen(false)}>Hủy</button><button disabled={busy} className="button primary" type="submit">{busy ? 'Đang lưu…' : 'Lưu thông tin'}</button></div></form>
    </Modal>}
  </>
}

function DepartmentsPage({ departments, session, reload, notify }: { departments: Department[]; session: Session; reload: () => void; notify: (s: string, good?: boolean) => void }) {
  const [open, setOpen] = useState(false)
  const [name, setName] = useState(''); const [code, setCode] = useState('')
  async function submit(e: FormEvent) {
    e.preventDefault()
    try { await api.send('POST', '/departments', { name, code }); notify('Đã tạo phòng ban.'); setOpen(false); setName(''); setCode(''); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không tạo được phòng ban.', false) }
  }
  return <><PageTitle kicker="PEOPLE / ORGANIZATION" title="Phòng ban" subtitle="Cơ cấu tổ chức và quy mô nhân sự theo từng bộ phận."
    action={canManage(session.role) && <button className="button primary" onClick={() => setOpen(true)}>＋ Thêm phòng ban</button>}/>
    <div className="department-grid">{departments.map(dep => <div className="department-card" key={dep.id}><div className="department-icon">▦</div><span className="dept-code">{dep.code}</span><h3>{dep.name}</h3><div className="metric-inline"><span>Nhân viên</span><strong>{dep.employeeCount}</strong></div></div>)}</div>
    {open && <Modal title="Thêm phòng ban" onClose={() => setOpen(false)}><form className="form-body" onSubmit={submit}>
      <Field label="Mã phòng ban"><input required minLength={2} maxLength={12} value={code} onChange={e => setCode(e.target.value)} placeholder="VD: QA"/></Field>
      <Field label="Tên phòng ban"><input required minLength={2} maxLength={100} value={name} onChange={e => setName(e.target.value)} placeholder="VD: Quality Assurance"/></Field>
      <div className="modal-actions"><button type="button" className="button secondary" onClick={() => setOpen(false)}>Hủy</button><button className="button primary">Tạo phòng ban</button></div>
    </form></Modal>}
  </>
}

const requestLabel: Record<string, string> = { Leave: 'Nghỉ phép', WFH: 'Làm việc từ xa', OT: 'Làm thêm giờ' }
function RequestsPage({ session, version, reload, notify }: { session: Session; version: number; reload: () => void; notify: (s: string, good?: boolean) => void }) {
  const { items, error, loading } = useList<WorkRequest>('/requests', version)
  const today = new Date().toISOString().slice(0, 10)
  const [open, setOpen] = useState(false)
  const [form, setForm] = useState({ type: 'Leave', startDate: today, endDate: today, reason: '' })
  async function submit(e: FormEvent) {
    e.preventDefault()
    try { await api.send('POST', '/requests', form); notify('Đã gửi yêu cầu chờ phê duyệt.'); setOpen(false); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không gửi được yêu cầu.', false) }
  }
  async function decide(item: WorkRequest, status: string) {
    if (!window.confirm(`${status === 'Approved' ? 'Duyệt' : 'Từ chối'} đơn của ${item.employee}?`)) return
    try { await api.send('POST', `/requests/${item.id}/decision`, { status, note: '' }); notify('Đã xử lý đơn.'); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không xử lý được đơn.', false) }
  }
  return <><PageTitle kicker="WORKSPACE / WORKFLOW" title="Đơn từ & phê duyệt" subtitle="Nghỉ phép, làm việc từ xa và OT theo quy trình có trạng thái."
    action={<button className="button primary" onClick={() => setOpen(true)}>＋ Tạo yêu cầu</button>}/>
    <div className="panel"><div className="panel-heading"><div><h3>Lịch sử yêu cầu</h3><p>Chỉ hiển thị đơn bạn có quyền xem</p></div><span className="small-pill">{items.filter(x => x.status === 'Pending').length} chờ duyệt</span></div>
      <ErrorMessage text={error}/>{loading ? <Loading/> : !items.length ? <Empty text="Chưa có đơn nào."/> :
        <div className="table-scroll"><table><thead><tr><th>Người gửi</th><th>Loại đơn</th><th>Thời gian</th><th>Lý do</th><th>Trạng thái</th>{canApprove(session.role) && <th>Phê duyệt</th>}</tr></thead><tbody>
          {items.map(x => <tr key={x.id}><td><strong>{x.employee}</strong></td><td>{requestLabel[x.type] ?? x.type}</td><td>{formatDate(x.startDate)}{x.endDate !== x.startDate ? ` – ${formatDate(x.endDate)}` : ''}</td><td className="truncate-cell" title={x.reason}>{x.reason}</td><td>{badge(x.status)}</td>
            {canApprove(session.role) && <td>{x.status === 'Pending' && x.employeeId !== session.id ? <div className="table-actions"><button className="text-button approve" onClick={() => decide(x, 'Approved')}>Duyệt</button><button className="text-button reject" onClick={() => decide(x, 'Rejected')}>Từ chối</button></div> : <span className="muted">—</span>}</td>}</tr>)}</tbody></table></div>}
    </div>
    {open && <Modal title="Gửi yêu cầu mới" onClose={() => setOpen(false)}><form onSubmit={submit} className="form-body">
      <Field label="Loại yêu cầu"><select value={form.type} onChange={e => setForm({ ...form, type: e.target.value })}><option value="Leave">Nghỉ phép</option><option value="WFH">Làm việc từ xa (WFH)</option><option value="OT">Làm thêm giờ (OT)</option></select></Field>
      <div className="form-grid"><Field label="Từ ngày"><input type="date" required value={form.startDate} onChange={e => setForm({ ...form, startDate: e.target.value })}/></Field><Field label="Đến ngày"><input type="date" required min={form.startDate} value={form.endDate} onChange={e => setForm({ ...form, endDate: e.target.value })}/></Field></div>
      <Field label="Lý do"><textarea required minLength={5} maxLength={500} rows={3} value={form.reason} onChange={e => setForm({ ...form, reason: e.target.value })}/></Field>
      <div className="form-info">Bản demo ghi nhận và phê duyệt đơn. Chưa tính hạn mức nghỉ phép hoặc tiền OT thực tế.</div>
      <div className="modal-actions"><button type="button" className="button secondary" onClick={() => setOpen(false)}>Hủy</button><button className="button primary">Gửi yêu cầu</button></div>
    </form></Modal>}
  </>
}

function SkillsPage({ session, employees, version, reload, notify }: { session: Session; employees: Employee[]; version: number; reload: () => void; notify: (s: string, good?: boolean) => void }) {
  const { items, loading, error } = useList<Skill>('/skills', version)
  const [query, setQuery] = useState('')
  const [level, setLevel] = useState('All')
  const [benchOnly, setBenchOnly] = useState(false)
  const [open, setOpen] = useState(false)
  const [form, setForm] = useState({ employeeId: session.id, name: '', level: 'Intermediate', certificate: '' })
  const visible = items.filter(x => `${x.name} ${x.employee} ${x.level}`.toLowerCase().includes(query.toLowerCase())
    && (level === 'All' || x.level === level)
    && (!benchOnly || employees.some(emp => emp.id === x.employeeId && emp.isBench)))
  async function submit(e: FormEvent) {
    e.preventDefault()
    try { await api.send('POST', '/skills', form); notify('Đã thêm kỹ năng.'); setOpen(false); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không lưu được kỹ năng.', false) }
  }
  async function remove(id: number) {
    if (!window.confirm('Xóa kỹ năng này?')) return
    try { await api.send('DELETE', `/skills/${id}`); notify('Đã xóa kỹ năng.'); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không xóa được.', false) }
  }
  return <><PageTitle kicker="TALENT / SKILLS" title="Ma trận kỹ năng" subtitle="Tìm kiếm nguồn lực theo công nghệ, cấp độ và chứng chỉ."
    action={<button className="button primary" onClick={() => setOpen(true)}>＋ Thêm kỹ năng</button>}/>
    <div className="panel"><div className="toolbar"><input className="search" placeholder="⌕ Tìm React, .NET, Docker, nhân viên..." value={query} onChange={e => setQuery(e.target.value)}/><select className="table-select" aria-label="Lọc cấp độ" value={level} onChange={e => setLevel(e.target.value)}><option value="All">Mọi cấp độ</option><option>Beginner</option><option>Intermediate</option><option>Advanced</option><option>Senior</option></select><label className="checkline"><input type="checkbox" checked={benchOnly} onChange={e => setBenchOnly(e.target.checked)}/> Chỉ Bench</label><span className="result-count">{visible.length} kỹ năng</span></div>
      <ErrorMessage text={error}/>{loading ? <Loading/> : !visible.length ? <Empty text="Chưa có kỹ năng phù hợp."/> : <div className="table-scroll"><table><thead><tr><th>Nhân viên</th><th>Kỹ năng</th><th>Cấp độ</th><th>Chứng chỉ</th><th></th></tr></thead><tbody>
        {visible.map(x => <tr key={x.id}><td><div className="person"><Avatar name={x.employee}/><strong>{x.employee}</strong></div></td><td><span className="skill-pill">{x.name}</span></td><td>{badge(x.level)}</td><td>{x.certificate || <span className="muted">—</span>}</td><td>{(canManage(session.role) || x.employeeId === session.id) && <button className="text-button reject" onClick={() => remove(x.id)}>Xóa</button>}</td></tr>)}
      </tbody></table></div>}
    </div>
    {open && <Modal title="Thêm kỹ năng" onClose={() => setOpen(false)}><form className="form-body" onSubmit={submit}>
      {canManage(session.role) && <Field label="Nhân viên"><select value={form.employeeId} onChange={e => setForm({ ...form, employeeId: Number(e.target.value) })}>{employees.map(x => <option key={x.id} value={x.id}>{x.fullName}</option>)}</select></Field>}
      <Field label="Tên kỹ năng"><input required minLength={2} maxLength={80} placeholder="VD: React, Go, Kubernetes" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })}/></Field>
      <Field label="Cấp độ"><select value={form.level} onChange={e => setForm({ ...form, level: e.target.value })}><option>Beginner</option><option>Intermediate</option><option>Advanced</option><option>Senior</option></select></Field>
      <Field label="Chứng chỉ (nếu có)"><input maxLength={150} value={form.certificate} onChange={e => setForm({ ...form, certificate: e.target.value })}/></Field>
      <div className="modal-actions"><button className="button secondary" type="button" onClick={() => setOpen(false)}>Hủy</button><button className="button primary">Lưu kỹ năng</button></div>
    </form></Modal>}
  </>
}

function AssetsPage({ session, employees, version, reload, notify }: { session: Session; employees: Employee[]; version: number; reload: () => void; notify: (s: string, good?: boolean) => void }) {
  const { items, loading, error } = useList<Asset>('/assets', version)
  const [open, setOpen] = useState(false)
  const [form, setForm] = useState({ assetTag: '', name: '', assignedToEmployeeId: 0 })
  async function add(e: FormEvent) {
    e.preventDefault()
    try { await api.send('POST', '/assets', { ...form, assignedToEmployeeId: form.assignedToEmployeeId || null }); notify('Đã thêm thiết bị.'); setOpen(false); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không lưu được.', false) }
  }
  async function assign(id: number, value: number) {
    try { await api.send('PUT', `/assets/${id}/assign`, { employeeId: value || null }); notify('Đã cập nhật cấp phát.'); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không cấp phát được.', false) }
  }
  return <><PageTitle kicker="OPERATIONS / IT ASSETS" title="Thiết bị công nghệ" subtitle="Theo dõi laptop, màn hình và lịch sử cấp phát hiện tại."
    action={canManage(session.role) && <button className="button primary" onClick={() => setOpen(true)}>＋ Thêm thiết bị</button>}/>
    <div className="panel"><div className="panel-heading"><h3>Danh sách tài sản</h3><span className="small-pill">{items.length} thiết bị</span></div><ErrorMessage text={error}/>
      {loading ? <Loading/> : !items.length ? <Empty/> : <div className="table-scroll"><table><thead><tr><th>Mã tài sản</th><th>Thiết bị</th><th>Trạng thái</th><th>Được cấp cho</th>{canManage(session.role) && <th>Đổi người nhận</th>}</tr></thead><tbody>
        {items.map(x => <tr key={x.id}><td><strong>{x.assetTag}</strong></td><td>{x.name}</td><td>{badge(x.status)}</td><td>{x.assignedTo || 'Chưa cấp phát'}</td>{canManage(session.role) && <td><select className="table-select" value={x.assignedToEmployeeId ?? 0} onChange={e => assign(x.id, Number(e.target.value))}><option value={0}>Chưa cấp</option>{employees.filter(emp => emp.status === 'Active').map(emp => <option key={emp.id} value={emp.id}>{emp.fullName}</option>)}</select></td>}</tr>)}
      </tbody></table></div>}
    </div>
    {open && <Modal title="Thêm thiết bị IT" onClose={() => setOpen(false)}><form className="form-body" onSubmit={add}>
      <Field label="Mã tài sản"><input required minLength={3} maxLength={30} placeholder="VD: LAP-004" value={form.assetTag} onChange={e => setForm({ ...form, assetTag: e.target.value })}/></Field>
      <Field label="Tên thiết bị"><input required minLength={3} maxLength={120} placeholder="VD: MacBook Air M3" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })}/></Field>
      <Field label="Cấp phát cho"><select value={form.assignedToEmployeeId} onChange={e => setForm({ ...form, assignedToEmployeeId: Number(e.target.value) })}><option value={0}>Chưa cấp phát</option>{employees.filter(x => x.status === 'Active').map(x => <option key={x.id} value={x.id}>{x.fullName}</option>)}</select></Field>
      <div className="modal-actions"><button type="button" className="button secondary" onClick={() => setOpen(false)}>Hủy</button><button className="button primary">Lưu thiết bị</button></div>
    </form></Modal>}
  </>
}

function ProjectsPage({ session, employees, version, reload, notify }: { session: Session; employees: Employee[]; version: number; reload: () => void; notify: (s: string, good?: boolean) => void }) {
  const projects = useList<Project>('/projects', version)
  const allocations = useList<Allocation>('/allocations', version)
  const [open, setOpen] = useState(false)
  const [assignOpen, setAssignOpen] = useState(false)
  const [newProject, setNewProject] = useState({ code: '', name: '' })
  const [assignment, setAssignment] = useState({ techProjectId: 0, employeeId: 0, percent: 50 })
  const canEdit = canManage(session.role) || session.role === 'Manager'
  async function addProject(e: FormEvent) {
    e.preventDefault()
    try { await api.send('POST', '/projects', newProject); notify('Đã tạo dự án.'); setOpen(false); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không tạo được dự án.', false) }
  }
  async function addAllocation(e: FormEvent) {
    e.preventDefault()
    try { await api.send('POST', '/allocations', assignment); notify('Đã phân bổ nhân sự.'); setAssignOpen(false); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không phân bổ được.', false) }
  }
  async function remove(id: number) {
    if (!window.confirm('Thu hồi phân bổ này?')) return
    try { await api.send('DELETE', `/allocations/${id}`); notify('Đã thu hồi phân bổ.'); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không thực hiện được.', false) }
  }
  return <><PageTitle kicker="DELIVERY / RESOURCES" title="Dự án & nguồn lực" subtitle="Theo dõi dự án, tỷ lệ phân bổ và nhân sự đang On Bench."
    action={canEdit && <div className="actions"><button className="button secondary" onClick={() => { setAssignment({ ...assignment, techProjectId: projects.items[0]?.id ?? 0, employeeId: employees.find(x => x.status === 'Active' && (canManage(session.role) || x.id !== session.id))?.id ?? 0 }); setAssignOpen(true) }}>＋ Phân bổ</button><button className="button primary" onClick={() => setOpen(true)}>＋ Tạo dự án</button></div>}/>
    <div className="project-grid">{projects.items.map(p => <div className="project-card" key={p.id}><span className="dept-code">{p.code}</span><h3>{p.name}</h3>{badge(p.status)}<div className="metric-inline"><span>Phân bổ nguồn lực</span><strong>{p.allocationCount}</strong></div></div>)}</div>
    <div className="panel"><div className="panel-heading"><div><h3>Danh sách phân bổ</h3><p>Ràng buộc tổng công suất không vượt 100%</p></div></div>
      <ErrorMessage text={projects.error || allocations.error}/>{allocations.loading ? <Loading/> : !allocations.items.length ? <Empty/> :
        <div className="table-scroll"><table><thead><tr><th>Nhân viên</th><th>Dự án</th><th>Phân bổ (%)</th>{canEdit && <th></th>}</tr></thead><tbody>
          {allocations.items.map(a => <tr key={a.id}><td><strong>{a.employee}</strong></td><td>{a.project}</td><td><div className="progress-row"><div className="progress-track"><div style={{ width: `${a.percent}%` }}/></div><b>{a.percent}%</b></div></td>{canEdit && <td><button className="text-button reject" onClick={() => remove(a.id)}>Thu hồi</button></td>}</tr>)}
        </tbody></table></div>}
    </div>
    {open && <Modal title="Tạo dự án" onClose={() => setOpen(false)}><form className="form-body" onSubmit={addProject}>
      <Field label="Mã dự án"><input required minLength={3} maxLength={30} value={newProject.code} onChange={e => setNewProject({ ...newProject, code: e.target.value })} placeholder="VD: PROJ-03"/></Field>
      <Field label="Tên dự án"><input required minLength={3} maxLength={120} value={newProject.name} onChange={e => setNewProject({ ...newProject, name: e.target.value })} placeholder="VD: AI Assistant"/></Field>
      <div className="modal-actions"><button type="button" className="button secondary" onClick={() => setOpen(false)}>Hủy</button><button className="button primary">Tạo dự án</button></div></form></Modal>}
    {assignOpen && <Modal title="Phân bổ nguồn lực" onClose={() => setAssignOpen(false)}><form className="form-body" onSubmit={addAllocation}>
      <Field label="Dự án"><select required value={assignment.techProjectId} onChange={e => setAssignment({ ...assignment, techProjectId: Number(e.target.value) })}><option value={0} disabled>Chọn dự án</option>{projects.items.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></Field>
      <Field label="Nhân viên"><select required value={assignment.employeeId} onChange={e => setAssignment({ ...assignment, employeeId: Number(e.target.value) })}><option value={0} disabled>Chọn nhân viên</option>{employees.filter(x => x.status === 'Active' && (canManage(session.role) || x.id !== session.id)).map(x => <option key={x.id} value={x.id}>{x.fullName}</option>)}</select></Field>
      <Field label="Tỷ lệ công suất (%)"><input type="number" min={1} max={100} required value={assignment.percent} onChange={e => setAssignment({ ...assignment, percent: Number(e.target.value) })}/></Field>
      <div className="modal-actions"><button type="button" className="button secondary" onClick={() => setAssignOpen(false)}>Hủy</button><button className="button primary">Lưu phân bổ</button></div></form></Modal>}
  </>
}

const stages = [
  { code: 'Applied', name: 'Nộp hồ sơ' }, { code: 'TechnicalTest', name: 'Test kỹ thuật' },
  { code: 'Interview', name: 'Phỏng vấn' }, { code: 'Offer', name: 'Offer' },
  { code: 'Hired', name: 'Đã tuyển' }, { code: 'Rejected', name: 'Từ chối' },
]
function CandidatesPage({ version, reload, notify }: { version: number; reload: () => void; notify: (s: string, good?: boolean) => void }) {
  const { items, loading, error } = useList<Candidate>('/candidates', version)
  const [open, setOpen] = useState(false)
  const [form, setForm] = useState({ fullName: '', email: '', position: '' })
  async function add(e: FormEvent) {
    e.preventDefault()
    try { await api.send('POST', '/candidates', form); notify('Đã thêm ứng viên.'); setOpen(false); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không thêm được ứng viên.', false) }
  }
  async function move(id: number, stage: string) {
    try { await api.send('PUT', `/candidates/${id}/stage`, { stage }); notify('Đã chuyển vòng tuyển dụng.'); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không chuyển được vòng.', false) }
  }
  return <><PageTitle kicker="TALENT / RECRUITMENT" title="Tuyển dụng ATS" subtitle="Theo dõi ứng viên qua các vòng tuyển dụng bằng bảng Kanban."
    action={<button className="button primary" onClick={() => setOpen(true)}>＋ Thêm ứng viên</button>}/>
    <ErrorMessage text={error}/>{loading ? <Loading/> : <div className="kanban">{stages.map(stage => <section className="kanban-column" key={stage.code}>
      <div className="kanban-heading"><h3>{stage.name}</h3><span>{items.filter(x => x.stage === stage.code).length}</span></div>
      {items.filter(x => x.stage === stage.code).map(c => <article className="candidate-card" key={c.id}><div className="person"><Avatar name={c.fullName}/><strong>{c.fullName}</strong></div>
        <p>{c.position}</p><small>{c.email}</small><select aria-label={`Chuyển vòng ${c.fullName}`} value={c.stage} onChange={e => move(c.id, e.target.value)}>{stages.map(s => <option key={s.code} value={s.code}>{s.name}</option>)}</select>
      </article>)}
    </section>)}</div>}
    {open && <Modal title="Thêm ứng viên" onClose={() => setOpen(false)}><form className="form-body" onSubmit={add}>
      <Field label="Họ và tên"><input required minLength={2} value={form.fullName} onChange={e => setForm({ ...form, fullName: e.target.value })}/></Field>
      <Field label="Email"><input type="email" required value={form.email} onChange={e => setForm({ ...form, email: e.target.value })}/></Field>
      <Field label="Vị trí ứng tuyển"><input required minLength={2} value={form.position} onChange={e => setForm({ ...form, position: e.target.value })}/></Field>
      <div className="modal-actions"><button type="button" className="button secondary" onClick={() => setOpen(false)}>Hủy</button><button className="button primary">Thêm ứng viên</button></div></form></Modal>}
  </>
}

function ObjectivesPage({ session, employees, version, reload, notify }: { session: Session; employees: Employee[]; version: number; reload: () => void; notify: (s: string, good?: boolean) => void }) {
  const { items, loading, error } = useList<Objective>('/objectives', version)
  const [open, setOpen] = useState(false)
  const [form, setForm] = useState({ employeeId: session.id, title: '', quarter: 'Q4-2026' })
  const [updates, setUpdates] = useState<Record<number, number>>({})
  async function add(e: FormEvent) {
    e.preventDefault()
    try { await api.send('POST', '/objectives', form); notify('Đã tạo mục tiêu OKR.'); setOpen(false); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không tạo được OKR.', false) }
  }
  async function setProgress(item: Objective) {
    const progress = updates[item.id] ?? item.progress
    try { await api.send('PUT', `/objectives/${item.id}/progress`, { progress }); notify('Đã cập nhật tiến độ.'); reload() }
    catch (err) { notify(err instanceof Error ? err.message : 'Không cập nhật được.', false) }
  }
  return <><PageTitle kicker="PERFORMANCE / OBJECTIVES" title="OKR & tiến độ" subtitle="Đặt mục tiêu theo quý và theo dõi tỷ lệ hoàn thành."
    action={<button className="button primary" onClick={() => setOpen(true)}>＋ Tạo mục tiêu</button>}/>
    <ErrorMessage text={error}/>{loading ? <Loading/> : !items.length ? <Empty text="Chưa có mục tiêu OKR."/> : <div className="objective-grid">{items.map(x => <article className="panel objective-card" key={x.id}>
      <div className="panel-heading"><span className="dept-code">{x.quarter}</span><span className="small-pill">{x.employee}</span></div><h3>{x.title}</h3>
      <div className="progress-row"><div className="progress-track"><div style={{ width: `${x.progress}%` }}/></div><strong>{x.progress}%</strong></div>
      {(canManage(session.role) || x.employeeId === session.id || session.role === 'Manager') && <div className="objective-controls"><input type="number" min={0} max={100} aria-label={`Tiến độ ${x.title}`} value={updates[x.id] ?? x.progress} onChange={e => setUpdates({ ...updates, [x.id]: Number(e.target.value) })}/><button className="button secondary" onClick={() => setProgress(x)}>Cập nhật</button></div>}
    </article>)}</div>}
    {open && <Modal title="Tạo mục tiêu OKR" onClose={() => setOpen(false)}><form className="form-body" onSubmit={add}>
      {(canManage(session.role) || session.role === 'Manager') && <Field label="Nhân viên"><select value={form.employeeId} onChange={e => setForm({ ...form, employeeId: Number(e.target.value) })}>{employees.map(x => <option key={x.id} value={x.id}>{x.fullName}</option>)}</select></Field>}
      <Field label="Tên mục tiêu"><input required minLength={5} maxLength={200} value={form.title} onChange={e => setForm({ ...form, title: e.target.value })}/></Field>
      <Field label="Quý"><input required maxLength={15} placeholder="Q4-2026" value={form.quarter} onChange={e => setForm({ ...form, quarter: e.target.value })}/></Field>
      <div className="modal-actions"><button type="button" className="button secondary" onClick={() => setOpen(false)}>Hủy</button><button className="button primary">Lưu mục tiêu</button></div></form></Modal>}
  </>
}

function AuditPage({ version }: { version: number }) {
  const { items, loading, error } = useList<Audit>('/audit', version)
  return <><PageTitle kicker="SYSTEM / AUDIT TRAIL" title="Nhật ký hoạt động" subtitle="Lịch sử thay đổi cơ bản trên hệ thống, chỉ Admin được xem."/>
    <div className="panel"><ErrorMessage text={error}/>{loading ? <Loading/> : !items.length ? <Empty/> : <div className="table-scroll"><table><thead><tr><th>Thời điểm (UTC)</th><th>Nhân viên thực hiện</th><th>Hành động</th><th>Đối tượng</th><th>Chi tiết</th></tr></thead><tbody>
    {items.map(x => <tr key={x.id}><td>{new Date(x.atUtc).toLocaleString('vi-VN')}</td><td>#{x.actorEmployeeId}</td><td>{x.action}</td><td>{x.entity}</td><td>{x.details}</td></tr>)}
    </tbody></table></div>}</div>
  </>
}

function Shell({ session, onLogout }: { session: Session; onLogout: () => void }) {
  const [page, setPage] = useState<Page>('dashboard')
  const [version, setVersion] = useState(0)
  const [toast, setToast] = useState<{ text: string; good: boolean } | null>(null)
  const [menuOpen, setMenuOpen] = useState(false)
  const [searchOpen, setSearchOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [notificationsOpen, setNotificationsOpen] = useState(false)
  const deps = useList<Department>('/departments', version)
  const emps = useList<Employee>('/employees', version)
  const inbox = useList<WorkRequest>('/requests', version)
  const available = menu.filter(x => !x.roles || x.roles.includes(session.role))
  const pending = inbox.items.filter(x => x.status === 'Pending')
  const navGroups = [
    { label: 'KHÔNG GIAN', pages: ['dashboard'] },
    { label: 'CON NGƯỜI', pages: ['employees', 'departments', 'organization', 'contracts', 'onboarding'] },
    { label: 'CÔNG VIỆC', pages: ['attendance', 'requests', 'skills', 'projects', 'assets'] },
    { label: 'PHÁT TRIỂN', pages: ['candidates', 'objectives', 'learning'] },
    { label: 'DỮ LIỆU & QUẢN TRỊ', pages: ['insights', 'audit', 'roadmap'] },
  ]
  function notify(text: string, good = true) { setToast({ text, good }) }
  function reload() { setVersion(x => x + 1) }
  function navigate(destination: Page) { setPage(destination); setMenuOpen(false); setSearchOpen(false); setNotificationsOpen(false); setToast(null) }
  useEffect(() => {
    const listener = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') { event.preventDefault(); setSearchOpen(value => !value) }
      if (event.key === 'Escape') { setSearchOpen(false); setNotificationsOpen(false) }
    }
    window.addEventListener('keydown', listener)
    return () => window.removeEventListener('keydown', listener)
  }, [])
  async function logout() {
    try { await api.logout(); onLogout() }
    catch (e) { notify(e instanceof Error ? e.message : 'Could not sign out.', false) }
  }
  return <div className="app-shell premium-shell refined-shell">
    {menuOpen && <button className="mobile-overlay" aria-label="Close menu" onClick={() => setMenuOpen(false)}/>}
    <aside className={`sidebar ${menuOpen ? 'sidebar-open' : ''}`}>
      <div className="brand"><span className="brand-square"><Glyph name="sparkles" size={21}/></span><strong>Tech<span>HRMS</span></strong><span className="brand-v2">V3.1</span></div>
      <div className="sidebar-workspace"><span className="workspace-logo"><Glyph name="layers" size={18}/></span><div><strong>Tech Enterprise</strong><small>{session.role === 'Employee' ? 'Employee portal' : session.role === 'Manager' ? 'Manager portal' : 'HR & Admin portal'}</small></div><Glyph name="chevron" size={15}/></div>
      <nav aria-label="Primary navigation">{navGroups.map(group => {
        const items = available.filter(item => group.pages.includes(item.id))
        if (!items.length) return null
        return <div key={group.label} className="nav-group"><div className="menu-heading">{group.label}</div>{items.map(item =>
          <button key={item.id} className={`nav-link ${page === item.id ? 'active' : ''}`} onClick={() => navigate(item.id)}><span className="nav-icon"><Glyph name={item.icon} size={18}/></span><span>{item.label}</span>{page === item.id && <span className="nav-indicator"/>}</button>)}</div>
      })}</nav>
      <div className="sidebar-foot">
        <div className="sidebar-tools" aria-label="Công cụ nhanh">
          <div className="sidebar-tools-label">CÔNG CỤ NHANH</div>
          <button className="sidebar-tool search-tool" onClick={() => { setSearchOpen(true); setQuery(''); setMenuOpen(false) }} aria-label="Tìm nhanh phân hệ">
            <Glyph name="search" size={18}/><span>Tìm phân hệ</span><kbd>Ctrl K</kbd>
          </button>
          <div className="notifications-box sidebar-notifications">
            <button className="sidebar-tool" aria-label={`Thông báo, ${pending.length} đơn đang chờ`} aria-expanded={notificationsOpen} onClick={() => setNotificationsOpen(x => !x)}>
              <Glyph name="bell" size={18}/><span>Thông báo</span>{pending.length > 0 && <b className="sidebar-count">{pending.length}</b>}
            </button>
            {notificationsOpen && <div className="notification-panel" role="region" aria-label="Thông báo đơn từ">
              <div><strong>Đơn từ đang chờ</strong><span>{pending.length} đang chờ</span></div>
              {pending.length ? pending.slice(0,3).map(x => <button key={x.id} onClick={() => navigate('requests')}><span className="notice-pip"/><span><strong>{x.employee}</strong><small>{x.type} · {formatDate(x.startDate)}</small></span></button>) : <p>Chưa có đơn nào chờ xử lý.</p>}
              <button className="notice-all" onClick={() => navigate('requests')}>Xem đơn từ <Glyph name="arrow" size={15}/></button>
            </div>}
          </div>
          <button className="sidebar-tool signout-tool" onClick={logout} aria-label="Đăng xuất"><Glyph name="logout" size={18}/><span>Đăng xuất</span></button>
        </div>
        <button className="v3-sidebar-help" onClick={() => navigate('roadmap')}><Glyph name="shield" size={17}/> Xem phạm vi & độ tin cậy</button>
        <div className="sidebar-version">TECHHRMS · ENTERPRISE DEMO V3.1</div>
      </div>
    </aside>
    <div className="main-area"><header className="topbar">
      <div className="topbar-left"><button className="icon-button mobile-menu" onClick={() => setMenuOpen(true)} aria-label="Open navigation"><Glyph name="menu" size={22}/></button><div className="location"><span>TECHHRMS / WORKSPACE</span><strong>{available.find(x => x.id === page)?.label}</strong></div></div>
      <div className="topbar-status"><span className="system-dot" aria-hidden="true"/><span>Không gian <strong>{session.role === 'Employee' ? 'Nhân viên' : session.role === 'Manager' ? 'Quản lý' : session.role === 'HR' ? 'Nhân sự' : 'Quản trị'}</strong></span><span className="topbar-demo">DEMO</span></div>
    </header>
    <main className="content">
      {page === 'dashboard' && <DashboardPage session={session} version={version} onNavigate={navigate}/>}
      {page === 'employees' && <EmployeesPage session={session} departments={deps.items} version={version} reload={reload} notify={notify}/>}
      {page === 'departments' && <DepartmentsPage session={session} departments={deps.items} reload={reload} notify={notify}/>}
      {page === 'requests' && <RequestsPage session={session} version={version} reload={reload} notify={notify}/>}
      {page === 'skills' && <SkillsPage session={session} employees={emps.items} version={version} reload={reload} notify={notify}/>}
      {page === 'projects' && <ProjectsPage session={session} employees={emps.items} version={version} reload={reload} notify={notify}/>}
      {page === 'assets' && <AssetsPage session={session} employees={emps.items} version={version} reload={reload} notify={notify}/>}
      {page === 'candidates' && <CandidatesPage version={version} reload={reload} notify={notify}/>}
      {page === 'objectives' && <ObjectivesPage session={session} employees={emps.items} version={version} reload={reload} notify={notify}/>}
      {page === 'audit' && <AuditPage version={version}/>}
      {page === 'organization' && <OrganizationPage version={version}/>}
      {page === 'contracts' && <ContractsPage session={session} version={version} reload={reload} notify={notify} employees={emps.items}/>}
      {page === 'onboarding' && <OnboardingPage session={session} version={version} reload={reload} notify={notify} employees={emps.items}/>}
      {page === 'attendance' && <AttendancePage session={session} version={version} reload={reload} notify={notify}/>}
      {page === 'learning' && <LearningPage session={session} version={version} reload={reload} notify={notify}/>}
      {page === 'insights' && <InsightsPage version={version} session={session}/>}
      {page === 'roadmap' && <RoadmapPage/>}
      <footer className="content-footer"><span>TECHHRMS / ENTERPRISE V3.1</span><span>Phiên bản thử nghiệm · Chỉ dùng dữ liệu giả lập</span></footer>
    </main></div>
    {searchOpen && <Modal title="Tìm nhanh phân hệ" onClose={() => setSearchOpen(false)}><div className="command-search"><div className="command-input"><Glyph name="search" size={19}/><input autoFocus placeholder="Tìm chức năng, nghiệp vụ..." value={query} onChange={e => setQuery(e.target.value)}/></div><div className="command-result-label">CÁC PHÂN HỆ BẠN CÓ THỂ TRUY CẬP</div>{available.filter(x => x.label.toLowerCase().includes(query.toLowerCase()) || x.id.includes(query.toLowerCase())).map(item => <button key={item.id} onClick={() => navigate(item.id)}><Glyph name={item.icon} size={19}/><span>{item.label}</span><Glyph name="arrow" size={17}/></button>)}<p>Mẹo: Nhấn Ctrl+K để mở hộp tìm kiếm ở bất kỳ trang nào.</p></div></Modal>}
    {toast && <div className={`toast ${toast.good ? 'success' : 'failure'}`} role="status"><span>{toast.good ? <Glyph name="check" size={19}/> : '!'}</span><p>{toast.text}</p><button aria-label="Close notification" onClick={() => setToast(null)}>×</button></div>}
  </div>
}

function App() {
  const [session, setSession] = useState<Session | null | undefined>(undefined)
  const [startupError, setStartupError] = useState('')
  useEffect(() => { let active = true; api.me().then(data => { if (active) setSession(data) })
    .catch(e => { if (active) { setStartupError(e instanceof Error ? e.message : 'Có lỗi khi kết nối.'); setSession(null) } })
    return () => { active = false } }, [])
  if (session === undefined) return <div className="boot"><div className="brand"><span className="brand-square"><Glyph name="sparkles" size={21}/></span><strong>TechHRMS</strong></div><Loading/></div>
  if (session === null) return <>{startupError && <div className="startup-warning">{startupError}</div>}<Login onLogin={value => { setStartupError(''); setSession(value) }}/></>
  return <Shell session={session} onLogout={() => setSession(null)}/>
}

export default App
