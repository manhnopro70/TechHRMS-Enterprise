import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api } from './api'
import type { Employee, Session } from './types'
import { OnboardingPage } from './EnterprisePages'
import { Alert, Busy, dateText, Empty, Field, Heading, message, Metrics, Modal, Panel, Submit, Tag, today, useResource } from './ui'

type ExitRecord = { id: number; employeeId: number; employee: string; lastWorkingDate: string; reason: string; status: string; checklist: { id: number; title: string; done: boolean }[]; completedAtUtc: string | null }
type Props = { session: Session; version: number; reload: () => void; notify: (text: string, good?: boolean) => void; employees: Employee[] }
export function LifecyclePage(props: Props) {
  const [tab, setTab] = useState(location.hash.includes('offboarding') ? 'offboarding' : 'onboarding')
  useEffect(() => { const update = () => setTab(location.hash.includes('offboarding') ? 'offboarding' : 'onboarding'); window.addEventListener('hashchange', update); return () => window.removeEventListener('hashchange', update) }, [])
  return <><div className="v4-tabs" role="tablist" aria-label="Vòng đời nhân viên"><button role="tab" aria-selected={tab === 'onboarding'} className={tab === 'onboarding' ? 'active' : ''} onClick={() => setTab('onboarding')}>Tiếp nhận nhân viên</button><button role="tab" aria-selected={tab === 'offboarding'} className={tab === 'offboarding' ? 'active' : ''} onClick={() => setTab('offboarding')}>Nghỉ việc & bàn giao</button></div>{tab === 'onboarding' ? <OnboardingPage {...props}/> : <OffboardingPage {...props}/>}</>
}
function OffboardingPage({ session, version, reload, notify, employees }: Props) {
  const hr = ['Admin', 'HR'].includes(session.role)
  const { data, loading, error } = useResource<ExitRecord[]>('/offboarding', version)
  const [open, setOpen] = useState(false), [busy, setBusy] = useState(false), [formError, setFormError] = useState('')
  const [form, setForm] = useState({ employeeId: 0, lastWorkingDate: today(), reason: '' })
  async function create(event: FormEvent) {
    event.preventDefault(); setBusy(true); setFormError('')
    try { await api.send('POST', '/offboarding', form); setOpen(false); notify('Đã tạo quy trình bàn giao.'); reload() } catch (error) { setFormError(message(error)) } finally { setBusy(false) }
  }
  async function check(record: ExitRecord, id: number, done: boolean) {
    setBusy(true); try { await api.send('PUT', `/offboarding/${record.id}/tasks/${id}`, { done }); reload() } catch (error) { notify(message(error), false) } finally { setBusy(false) }
  }
  async function complete(record: ExitRecord) {
    if (!window.confirm(`Hoàn tất nghỉ việc của ${record.employee}? Hệ thống sẽ khóa tài khoản HRM, ngừng hồ sơ, thu hồi thiết bị trong HRM và giải phóng phân bổ dự án. Bạn đã kiểm tra đầy đủ bàn giao?`)) return
    setBusy(true); try { await api.send('POST', `/offboarding/${record.id}/complete`, {}); notify('Đã hoàn tất bàn giao và cập nhật nhân sự.'); reload() } catch (error) { notify(message(error), false) } finally { setBusy(false) }
  }
  return <><Heading eyebrow="CON NGƯỜI / BÀN GIAO" title="Nghỉ việc & thu hồi" description="Kiểm tra từng bước bàn giao trước khi kết thúc quyền truy cập hệ thống." action={hr && <button className="button primary" onClick={() => { setFormError(''); setOpen(true) }}>＋ Tạo quy trình nghỉ việc</button>}/><Metrics items={[{ label: 'Tổng quy trình', value: data?.length ?? 0 }, { label: 'Đang bàn giao', value: data?.filter(x => !x.completedAtUtc).length ?? 0 }, { label: 'Đã hoàn tất', value: data?.filter(x => x.completedAtUtc).length ?? 0 }]}/><Alert>{error}</Alert>{loading ? <Busy/> : !data?.length ? <Panel><Empty title="Chưa có quy trình nghỉ việc" description="HR có thể tạo quy trình để theo dõi bàn giao công việc, tài sản và tài khoản."/></Panel> : <div className="v4-exit-grid">{data.map(record => <Panel key={record.id} title={record.employee} description={`Ngày làm việc cuối: ${dateText(record.lastWorkingDate)}`} action={<Tag status={record.completedAtUtc ? 'Done' : 'InProgress'}/>}><p className="v4-exit-reason">{record.reason}</p><div className="v4-checklist">{record.checklist.map(task => <label key={task.id} className={`v4-checklist-row ${task.done ? 'done' : ''}`}><input type="checkbox" checked={task.done} disabled={busy || !hr || Boolean(record.completedAtUtc)} onChange={e => check(record, task.id, e.target.checked)}/><span>{task.title}</span></label>)}</div>{hr && !record.completedAtUtc && <button className="button primary" disabled={busy || !record.checklist.every(x => x.done) || record.employeeId === session.id} onClick={() => complete(record)}>Hoàn tất bàn giao</button>}<p className="v4-note">Quyền GitHub, Jira, Slack hoặc email cần được bộ phận IT thu hồi riêng.</p></Panel>)}</div>}
    {open && <Modal title="Tạo quy trình nghỉ việc" onClose={() => { if (!busy) setOpen(false) }}><form className="form-body v4-stack" onSubmit={create}><Alert>{formError}</Alert><Field label="Nhân viên"><select required value={form.employeeId} onChange={e => setForm({ ...form, employeeId: Number(e.target.value) })}><option value={0} disabled>Chọn nhân viên</option>{employees.filter(e => e.status !== 'Inactive' && e.id !== session.id).map(e => <option key={e.id} value={e.id}>{e.fullName}</option>)}</select></Field><Field label="Ngày làm việc cuối"><input type="date" required value={form.lastWorkingDate} onChange={e => setForm({ ...form, lastWorkingDate: e.target.value })}/></Field><Field label="Lý do / Ghi chú"><textarea rows={3} required minLength={5} maxLength={500} value={form.reason} onChange={e => setForm({ ...form, reason: e.target.value })}/></Field><Submit busy={busy || !form.employeeId}>Tạo checklist bàn giao</Submit></form></Modal>}
  </>
}
