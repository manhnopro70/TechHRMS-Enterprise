import { useEffect, useState } from 'react'
import type { FormEvent, ReactNode, CSSProperties } from 'react'
import { api } from './api'
import type { Employee, Role, Session } from './types'

type Common = { version: number; reload: () => void; notify: (s: string, ok?: boolean) => void; session: Session }
type Contract = { id: number; employeeId: number; employee: string; contractCode: string; contractType: string; startDate: string; endDate: string | null; status: string; notes: string }
type Onboarding = { id: number; employeeId: number; employee: string; title: string; category: string; ownerRole: string; dueDate: string | null; status: string }
type TimeEntry = { id: number; employeeId: number; employee: string; workDate: string; mode: string; checkInUtc: string; checkOutUtc: string | null; note: string }
type Course = { id: number; code: string; title: string; category: string; durationHours: number; status: string }
type Enrollment = { id: number; courseId: number; course: string; employeeId: number; employee: string; status: string; enrolledAtUtc: string }
type Org = { id: number; code: string; name: string; total: number; active: number; remote: number }
type Insight = { headcount: number; active: number; hybrid: number; remote: number; onsite: number; bench: number; openRequests: number; attendanceThisMonth: number; certificates: number; expiringContracts: number; departments: { name: string; value: number }[] }

const hr = (r: Role) => r === 'Admin' || r === 'HR'
const dateText = (v: string | null) => v ? new Date(v.slice(0, 10) + 'T00:00:00').toLocaleDateString('vi-VN') : '—'
const timeText = (v: string | null) => v ? new Date(v).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' }) : '—'

function useData<T>(path: string, version: number) {
  const [items, setItems] = useState<T[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  useEffect(() => {
    let active = true
    setLoading(true); setError('')
    api.get<T[]>(path).then(data => { if (active) setItems(data) })
      .catch(e => { if (active) setError(e instanceof Error ? e.message : 'Không thể tải dữ liệu.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [path, version])
  return { items, loading, error }
}
function Header({ eyebrow, title, description, action }: { eyebrow: string; title: string; description: string; action?: ReactNode }) {
  return <div className="v3-page-head"><div><span className="v3-eyebrow">{eyebrow}</span><h1>{title}</h1><p>{description}</p></div>{action}</div>
}
function Surface({ children, title, subtitle }: { children: ReactNode; title?: string; subtitle?: string }) {
  return <section className="v3-surface">{title && <div className="v3-section-title"><h3>{title}</h3>{subtitle && <p>{subtitle}</p>}</div>}{children}</section>
}
function Status({ children, kind = 'info' }: { children: ReactNode; kind?: 'info' | 'positive' | 'warn' }) {
  return <span className={`v3-tag v3-${kind}`}>{children}</span>
}
function Feedback({ loading, error, empty, children }: { loading: boolean; error: string; empty: boolean; children: ReactNode }) {
  if (loading) return <div className="v3-feedback">Đang tải dữ liệu…</div>
  if (error) return <div className="v3-feedback is-error">{error}</div>
  if (empty) return <div className="v3-feedback">Chưa có dữ liệu. Hãy bắt đầu bằng thao tác đầu tiên.</div>
  return <>{children}</>
}
function EditPanel({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) {
  return <div className="v3-modal-overlay" onMouseDown={onClose}><section className="v3-modal" role="dialog" aria-modal="true" aria-label={title} onMouseDown={e => e.stopPropagation()}>
    <header><h2>{title}</h2><button className="v3-icon-close" onClick={onClose} aria-label="Đóng">×</button></header>{children}</section></div>
}
function SelectEmployee({ employees, value, onChange }: { employees: Employee[]; value: number; onChange: (id: number) => void }) {
  return <label className="v3-field">Nhân viên <select required value={value} onChange={e => onChange(Number(e.target.value))}><option value={0}>Chọn nhân viên</option>{employees.map(e => <option key={e.id} value={e.id}>{e.fullName} · {e.department}</option>)}</select></label>
}
function Submit({ busy, children = 'Lưu thay đổi' }: { busy: boolean; children?: ReactNode }) { return <button type="submit" className="v3-primary" disabled={busy}>{busy ? 'Đang lưu…' : children}</button> }

export function OrganizationPage({ version }: { version: number }) {
  const { items, loading, error } = useData<Org>('/organization', version)
  const total = items.reduce((n, d) => n + d.total, 0)
  return <><Header eyebrow="TỔ CHỨC" title="Sơ đồ phòng ban" description="Nhìn nhanh cấu trúc nhân sự theo phòng ban. Bản này chưa hỗ trợ nhiều pháp nhân và cây tổ chức nhiều cấp."/>
    <Surface title="Toàn cảnh tổ chức" subtitle={`${items.length} phòng ban · ${total} hồ sơ nhân sự`}><Feedback loading={loading} error={error} empty={!items.length}>
      <div className="v3-org-grid">{items.map((d, i) => <article className="v3-org-card" key={d.id}>
        <div className="v3-org-symbol" style={{ '--i': i % 5 } as CSSProperties}>{d.code.slice(0, 2)}</div>
        <span className="v3-org-code">{d.code}</span><h3>{d.name}</h3>
        <div className="v3-org-numbers"><span><strong>{d.total}</strong> nhân sự</span><span><strong>{d.active}</strong> đang làm</span><span><strong>{d.remote}</strong> remote</span></div>
      </article>)}</div></Feedback></Surface>
  </>
}

export function ContractsPage({ session, version, reload, notify, employees }: Common & { employees: Employee[] }) {
  const { items, loading, error } = useData<Contract>('/contracts', version)
  const [open, setOpen] = useState(false)
  const [busy, setBusy] = useState(false)
  const [form, setForm] = useState({ employeeId: 0, contractCode: '', contractType: 'FixedTerm', startDate: '', endDate: '', status: 'Draft', notes: '' })
  async function add(e: FormEvent) {
    e.preventDefault(); setBusy(true)
    try { await api.send('POST', '/contracts', { ...form, endDate: form.endDate || null }); notify('Đã lưu hợp đồng giả lập.'); setOpen(false); reload() }
    catch (x) { notify(x instanceof Error ? x.message : 'Không thể lưu hợp đồng.', false) }
    finally { setBusy(false) }
  }
  async function update(id: number, status: string) {
    try { await api.send('PUT', `/contracts/${id}/status`, { status }); notify('Đã cập nhật trạng thái hợp đồng.'); reload() }
    catch (x) { notify(x instanceof Error ? x.message : 'Có lỗi.', false) }
  }
  const statuses: Record<string, string> = { Draft: 'Bản nháp', Active: 'Đang hiệu lực', Expired: 'Hết hạn', Terminated: 'Đã chấm dứt' }
  const types: Record<string, string> = { FixedTerm: 'Xác định thời hạn', Probation: 'Thử việc', Indefinite: 'Không xác định thời hạn', Internship: 'Thực tập', Contractor: 'Cộng tác / hợp đồng' }
  return <><Header eyebrow="CORE HR · HỢP ĐỒNG" title="Hợp đồng lao động" description="Quản lý danh mục hợp đồng và thời hạn. Chưa bao gồm văn bản pháp lý, chữ ký số hoặc dữ liệu lương."
    action={hr(session.role) && <button className="v3-primary" onClick={() => setOpen(true)}>+ Thêm hợp đồng</button>}/>
    <Surface title="Danh sách hợp đồng" subtitle={hr(session.role) ? 'HR xem hợp đồng toàn hệ thống' : 'Chỉ hiển thị hợp đồng của bạn'}>
      <Feedback loading={loading} error={error} empty={!items.length}><div className="v3-table-scroll"><table className="v3-table"><thead><tr><th>Mã / Nhân viên</th><th>Loại hợp đồng</th><th>Ngày bắt đầu</th><th>Hết hạn</th><th>Trạng thái</th></tr></thead><tbody>
        {items.map(x => <tr key={x.id}><td><strong>{x.contractCode}</strong><small>{x.employee}</small></td><td>{types[x.contractType] ?? x.contractType}</td><td>{dateText(x.startDate)}</td><td>{dateText(x.endDate)}</td><td>{hr(session.role) ? <select className="v3-small-select" value={x.status} aria-label={`Trạng thái ${x.contractCode}`} onChange={e => update(x.id, e.target.value)}>{Object.entries(statuses).map(([k, v]) => <option key={k} value={k}>{v}</option>)}</select> : <Status kind={x.status === 'Active' ? 'positive' : 'warn'}>{statuses[x.status] ?? x.status}</Status>}</td></tr>)}
      </tbody></table></div></Feedback></Surface>
    {open && <EditPanel title="Tạo hợp đồng mẫu" onClose={() => setOpen(false)}><form className="v3-form" onSubmit={add}>
      <SelectEmployee employees={employees} value={form.employeeId} onChange={employeeId => setForm(v => ({ ...v, employeeId }))}/>
      <label className="v3-field">Mã hợp đồng<input required minLength={4} maxLength={48} placeholder="DEMO-HD-002" value={form.contractCode} onChange={e => setForm(v => ({ ...v, contractCode: e.target.value }))}/></label>
      <label className="v3-field">Loại<select value={form.contractType} onChange={e => setForm(v => ({ ...v, contractType: e.target.value }))}>{Object.entries(types).map(([k, v]) => <option value={k} key={k}>{v}</option>)}</select></label>
      <label className="v3-field">Ngày bắt đầu<input required type="date" value={form.startDate} onChange={e => setForm(v => ({ ...v, startDate: e.target.value }))}/></label>
      <label className="v3-field">Ngày kết thúc (nếu có)<input type="date" min={form.startDate} value={form.endDate} onChange={e => setForm(v => ({ ...v, endDate: e.target.value }))}/></label>
      <label className="v3-field">Ghi chú<textarea maxLength={500} value={form.notes} onChange={e => setForm(v => ({ ...v, notes: e.target.value }))}/></label>
      <p className="v3-helper">Chỉ sử dụng dữ liệu giả. Không đưa hợp đồng thật hoặc thông tin ngân hàng vào bản demo.</p><Submit busy={busy}>Lưu hợp đồng</Submit></form></EditPanel>}
  </>
}

export function OnboardingPage({ session, version, reload, notify, employees }: Common & { employees: Employee[] }) {
  const { items, loading, error } = useData<Onboarding>('/onboarding', version)
  const [open, setOpen] = useState(false)
  const [busy, setBusy] = useState(false)
  const [form, setForm] = useState({ employeeId: 0, title: '', category: 'HR', ownerRole: 'Employee', dueDate: '' })
  async function add(e: FormEvent) {
    e.preventDefault(); setBusy(true)
    try { await api.send('POST', '/onboarding', { ...form, dueDate: form.dueDate || null }); notify('Đã thêm công việc onboarding.'); reload(); setOpen(false) }
    catch (x) { notify(x instanceof Error ? x.message : 'Không thể tạo checklist.', false) }
    finally { setBusy(false) }
  }
  async function toggle(x: Onboarding) {
    try { await api.send('PUT', `/onboarding/${x.id}/status`, { status: x.status === 'Pending' ? 'Done' : 'Pending' }); notify('Đã cập nhật checklist.'); reload() }
    catch (e) { notify(e instanceof Error ? e.message : 'Bạn không có quyền cập nhật.', false) }
  }
  const done = items.filter(x => x.status === 'Done').length
  return <><Header eyebrow="EMPLOYEE LIFECYCLE" title="Onboarding & bàn giao" description="Checklist điều phối giữa HR, quản lý và nhân viên, giúp không bỏ sót công việc khi tiếp nhận nhân sự."
    action={hr(session.role) && <button className="v3-primary" onClick={() => setOpen(true)}>+ Tạo checklist</button>}/>
    <div className="v3-mini-overview"><div><strong>{items.length}</strong><span>Tổng công việc</span></div><div><strong>{done}</strong><span>Đã hoàn thành</span></div><div><strong>{items.length - done}</strong><span>Đang xử lý</span></div></div>
    <Surface title="Danh sách bàn giao"><Feedback loading={loading} error={error} empty={!items.length}><div className="v3-task-list">
      {items.map(x => <article className="v3-task" key={x.id}><button className={`v3-task-check ${x.status === 'Done' ? 'is-done' : ''}`} onClick={() => toggle(x)} title="Đổi trạng thái công việc" aria-label={`Đánh dấu ${x.title}`}>
        {x.status === 'Done' ? '✓' : '○'}</button><div><strong className={x.status === 'Done' ? 'v3-completed' : ''}>{x.title}</strong><p>{x.employee} · {x.category} · Phụ trách: {x.ownerRole} · Hạn {dateText(x.dueDate)}</p></div><Status kind={x.status === 'Done' ? 'positive' : 'warn'}>{x.status === 'Done' ? 'Hoàn thành' : 'Chưa xong'}</Status></article>)}
    </div></Feedback></Surface>
    {open && <EditPanel title="Thêm công việc onboarding" onClose={() => setOpen(false)}><form className="v3-form" onSubmit={add}>
      <SelectEmployee employees={employees} value={form.employeeId} onChange={employeeId => setForm(v => ({ ...v, employeeId }))}/>
      <label className="v3-field">Công việc<input required minLength={5} maxLength={200} placeholder="Cấp tài khoản máy tính" value={form.title} onChange={e => setForm(v => ({ ...v, title: e.target.value }))}/></label>
      <label className="v3-field">Nhóm việc<select value={form.category} onChange={e => setForm(v => ({ ...v, category: e.target.value }))}>{['HR', 'IT', 'Training', 'Operations'].map(x => <option key={x}>{x}</option>)}</select></label>
      <label className="v3-field">Người phụ trách<select value={form.ownerRole} onChange={e => setForm(v => ({ ...v, ownerRole: e.target.value }))}><option value="Employee">Nhân viên</option><option value="Manager">Quản lý</option><option value="HR">HR</option></select></label>
      <label className="v3-field">Hạn thực hiện<input type="date" value={form.dueDate} onChange={e => setForm(v => ({ ...v, dueDate: e.target.value }))}/></label><Submit busy={busy}>Tạo công việc</Submit></form></EditPanel>}
  </>
}

export function AttendancePage({ session, version, reload, notify }: Common) {
  const [month, setMonth] = useState(new Date().toISOString().slice(0, 7))
  const { items, loading, error } = useData<TimeEntry>(`/attendance?month=${month}`, version)
  const [mode, setMode] = useState('Hybrid')
  const [note, setNote] = useState('')
  const [busy, setBusy] = useState(false)
  const ownOpen = items.some(x => x.employeeId === session.id && !x.checkOutUtc)
  async function action(kind: 'check-in' | 'check-out') {
    setBusy(true)
    try { await api.send('POST', `/attendance/${kind}`, kind === 'check-in' ? { mode, note } : {});
      notify(kind === 'check-in' ? 'Đã ghi nhận check-in.' : 'Đã ghi nhận check-out.'); reload(); setNote('') }
    catch (e) { notify(e instanceof Error ? e.message : 'Có lỗi khi ghi nhận công.', false) }
    finally { setBusy(false) }
  }
  return <><Header eyebrow="WORKSPACE · TIMESHEET" title="Chấm công linh hoạt" description="Nhân viên tự check-in/check-out; quản lý xem theo phạm vi được phân quyền. Chưa xác thực GPS/IP hay tính lương."/>
    <div className="v3-attendance-top"><div><span className="v3-eyebrow">HÔM NAY</span><h2>Ghi nhận ngày làm việc</h2><p>Thao tác lưu thời điểm thật lên Backend. Mỗi nhân viên check-in tối đa một lần mỗi ngày.</p></div>
      <div className="v3-attendance-actions"><select aria-label="Mô hình làm việc" value={mode} onChange={e => setMode(e.target.value)}><option value="Hybrid">Hybrid</option><option value="Remote">Remote</option><option value="Onsite">Văn phòng</option></select><input placeholder="Ghi chú (tùy chọn)" maxLength={240} value={note} onChange={e => setNote(e.target.value)}/>
        <button className="v3-primary" disabled={busy || ownOpen} onClick={() => action('check-in')}>Bắt đầu ngày làm việc</button><button className="v3-secondary" disabled={busy || !ownOpen} onClick={() => action('check-out')}>Kết thúc ngày làm việc</button></div></div>
    <Surface title="Lịch sử chấm công"><div className="v3-toolbar"><span>{hr(session.role) || session.role === 'Manager' ? 'Bản ghi trong phạm vi bạn được xem' : 'Chỉ hiển thị lịch sử của bạn'}</span><input type="month" aria-label="Chọn tháng" value={month} onChange={e => setMonth(e.target.value)}/></div>
      <Feedback loading={loading} error={error} empty={!items.length}><div className="v3-table-scroll"><table className="v3-table"><thead><tr><th>Nhân viên</th><th>Ngày</th><th>Mô hình</th><th>Check-in</th><th>Check-out</th></tr></thead><tbody>
        {items.map(x => <tr key={x.id}><td><strong>{x.employee}</strong></td><td>{dateText(x.workDate)}</td><td><Status>{x.mode}</Status></td><td>{timeText(x.checkInUtc)}</td><td>{x.checkOutUtc ? timeText(x.checkOutUtc) : <Status kind="warn">Chưa check-out</Status>}</td></tr>)}
      </tbody></table></div></Feedback></Surface>
  </>
}

export function LearningPage({ session, version, reload, notify }: Common) {
  const courses = useData<Course>('/courses', version)
  const enrollments = useData<Enrollment>('/enrollments', version)
  const [open, setOpen] = useState(false)
  const [busy, setBusy] = useState(false)
  const [form, setForm] = useState({ code: '', title: '', category: 'Technical', durationHours: 4 })
  async function create(e: FormEvent) {
    e.preventDefault(); setBusy(true)
    try { await api.send('POST', '/courses', form); notify('Đã tạo khóa học.'); reload(); setOpen(false) }
    catch (x) { notify(x instanceof Error ? x.message : 'Không thể tạo khóa học.', false) }
    finally { setBusy(false) }
  }
  async function enroll(id: number) {
    try { await api.send('POST', `/courses/${id}/enroll`, {}); notify('Đã đăng ký khóa học.'); reload() }
    catch (e) { notify(e instanceof Error ? e.message : 'Đăng ký thất bại.', false) }
  }
  async function advance(id: number, current: string) {
    const next = current === 'Enrolled' ? 'InProgress' : 'Completed'
    try { await api.send('PUT', `/enrollments/${id}/status`, { status: next }); notify('Đã cập nhật tiến độ học.'); reload() }
    catch (e) { notify(e instanceof Error ? e.message : 'Có lỗi cập nhật.', false) }
  }
  const own = enrollments.items.filter(x => x.employeeId === session.id)
  return <><Header eyebrow="LEARNING & DEVELOPMENT" title="Học tập & phát triển" description="Danh mục khóa học, đăng ký tự phục vụ và ghi nhận hoàn thành. Chưa kết nối LMS hoặc xác thực chứng chỉ."
    action={hr(session.role) && <button className="v3-primary" onClick={() => setOpen(true)}>+ Thêm khóa học</button>}/>
    <Surface title="Khóa học dành cho bạn" subtitle="Chọn khóa học phù hợp với định hướng nghề nghiệp"><Feedback loading={courses.loading} error={courses.error} empty={!courses.items.length}>
      <div className="v3-course-grid">{courses.items.map((x, i) => <article className="v3-course" key={x.id}><div className={`v3-course-art art-${i % 4}`}><span>{x.code}</span><span>✦</span></div><div className="v3-course-body"><Status>{x.category}</Status><h3>{x.title}</h3><p>◷ {x.durationHours} giờ · {x.status === 'Published' ? 'Đang mở' : 'Tạm ẩn'}</p>
        {own.some(e => e.courseId === x.id) ? <Status kind="positive">Đã đăng ký</Status> : <button className="v3-secondary" onClick={() => enroll(x.id)}>Đăng ký học →</button>}</div></article>)}</div>
    </Feedback></Surface>
    <Surface title="Lộ trình học của tôi"><Feedback loading={enrollments.loading} error={enrollments.error} empty={!own.length}>
      <div className="v3-task-list">{own.map(x => <div className="v3-task" key={x.id}><div className="v3-task-check">✓</div><div><strong>{x.course}</strong><p>Đã đăng ký {timeText(x.enrolledAtUtc)}</p></div>{x.status === 'Completed' ? <Status kind="positive">Hoàn thành (tự khai)</Status> : <button className="v3-secondary" onClick={() => advance(x.id, x.status)}>{x.status === 'Enrolled' ? 'Bắt đầu' : 'Đánh dấu hoàn thành'}</button>}</div>)}</div>
    </Feedback></Surface>
    {open && <EditPanel title="Thêm khóa học" onClose={() => setOpen(false)}><form className="v3-form" onSubmit={create}>
      <label className="v3-field">Mã khóa học<input required minLength={3} maxLength={32} placeholder="TECH-101" value={form.code} onChange={e => setForm(v => ({ ...v, code: e.target.value }))}/></label>
      <label className="v3-field">Tên khóa học<input required minLength={5} maxLength={160} value={form.title} onChange={e => setForm(v => ({ ...v, title: e.target.value }))}/></label>
      <label className="v3-field">Nhóm<select value={form.category} onChange={e => setForm(v => ({ ...v, category: e.target.value }))}><option value="Technical">Công nghệ</option><option value="SoftSkill">Kỹ năng mềm</option><option value="Compliance">Tuân thủ</option><option value="Leadership">Lãnh đạo</option></select></label>
      <label className="v3-field">Số giờ<input type="number" min={1} max={500} required value={form.durationHours} onChange={e => setForm(v => ({ ...v, durationHours: Number(e.target.value) }))}/></label><Submit busy={busy}>Lưu khóa học</Submit></form></EditPanel>}
  </>
}

export function InsightsPage({ version, session }: { version: number; session: Session }) {
  const [data, setData] = useState<Insight | null>(null)
  const [error, setError] = useState('')
  useEffect(() => { let active = true; setError(''); api.get<Insight>('/insights').then(x => { if (active) setData(x) })
    .catch(e => { if (active) setError(e instanceof Error ? e.message : 'Không tải được số liệu.') }); return () => { active = false } }, [version])
  const total = Math.max(data?.headcount ?? 0, 1)
  const peak = Math.max(1, ...(data?.departments.map(d => d.value) ?? [1]))
  return <><Header eyebrow="PEOPLE ANALYTICS" title="Báo cáo nhân sự" description="Số liệu trực tiếp từ Database theo quyền truy cập. Không suy diễn turnover hay chi phí lương khi thiếu dữ liệu lịch sử."/>
    {error ? <div className="v3-feedback is-error">{error}</div> : !data ? <div className="v3-feedback">Đang tổng hợp số liệu…</div> : <>
      <div className="v3-insight-cards">{[
        ['Tổng nhân sự', data.headcount, 'Hồ sơ trong phạm vi được phép'],
        ['Đang làm việc', data.active, 'Trạng thái Active'],
        ['Đơn chờ xử lý', data.openRequests, 'Leave / WFH / OT'],
        ['Bản ghi công tháng', data.attendanceThisMonth, 'Số lượt check-in đã lưu'],
        ['Chứng chỉ khai báo', data.certificates, 'Dữ liệu Skill Matrix'],
        ['Hợp đồng sắp hết hạn', hr(session.role) ? data.expiringContracts : '—', '30 ngày tới · chỉ HR/Admin']
      ].map(([label, value, tip]) => <div className="v3-insight-card" key={String(label)}><span>{label}</span><strong>{value}</strong><small>{tip}</small></div>)}</div>
      <div className="v3-dual"><Surface title="Phân bổ hình thức làm việc"><div className="v3-modes">{[
        ['Hybrid', data.hybrid, '#5d65f1'],['Remote', data.remote, '#22b8a3'],['Onsite', data.onsite, '#f5a65e']
      ].map(([label, value, color]) => <div className="v3-mode-row" key={String(label)}><div><span>{label}</span><strong>{value} · {Math.round(Number(value) / total * 100)}%</strong></div><div className="v3-track"><i style={{ width: `${Number(value) / total * 100}%`, background: String(color) }}/></div></div>)}</div></Surface>
      <Surface title="Cơ cấu phòng ban"><div className="v3-modes">{data.departments.map((d, i) => <div className="v3-mode-row" key={d.name}><div><span>{d.name}</span><strong>{d.value} người</strong></div><div className="v3-track"><i style={{ width: `${d.value / peak * 100}%`, background: ['#5d65f1','#22b8a3','#f5a65e','#c17ed8'][i % 4] }}/></div></div>)}</div></Surface></div>
    </>}
  </>
}

export function RoadmapPage() {
  const groups = [
    { title: 'Đã có nghiệp vụ & API', badge: 'Có thể kiểm thử', items: 'Core HR, phòng ban, yêu cầu Leave/WFH/OT, Skill Matrix, phân bổ dự án, IT Assets, ATS cơ bản, OKR, hợp đồng cơ bản, checklist onboarding, check-in/out web, đăng ký khóa học, báo cáo tổng hợp, nhật ký thay đổi.' },
    { title: 'Cần hoàn thiện quy tắc và kiểm thử', badge: 'Giai đoạn tiếp theo', items: 'Quy trình nghỉ phép nhiều cấp, phép tồn, ca làm, OT theo hệ số, kỳ công, quy trình offboarding, hợp đồng có hiệu lực theo thời gian, phân quyền chi tiết theo phòng ban/pháp nhân, import/export, tích hợp email.' },
    { title: 'Chưa đưa vào vận hành', badge: 'Yêu cầu thiết kế riêng', items: 'Payroll/BHXH/thuế TNCN, ký số, SSO/MFA, quản trị đa pháp nhân, workflow engine cấu hình, access provisioning, ngân hàng, Face ID/GPS, tài liệu nhân sự nhạy cảm, hạ tầng HA, backup/restore và kiểm thử bảo mật.' },
  ]
  return <><Header eyebrow="PROJECT TRANSPARENCY" title="Phạm vi & lộ trình" description="Theo dõi rõ phần nào chạy được, phần nào mới ở mức demo. Đây là bản trình diễn học thuật, không phải phần mềm HR có thể vận hành ngay với dữ liệu thật."/>
    <div className="v3-roadmap">{groups.map((g, i) => <section className="v3-roadmap-card" key={g.title}><div className="v3-roadmap-index">0{i + 1}</div><Status kind={i === 0 ? 'positive' : 'warn'}>{g.badge}</Status><h2>{g.title}</h2><p>{g.items}</p></section>)}</div>
  </>
}
