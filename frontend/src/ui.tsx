import { useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { api } from './api'

export function useResource<T>(path: string | null, version = 0, interval = 0) {
  const [data, setData] = useState<T | null>(null)
  const [loading, setLoading] = useState(Boolean(path))
  const [error, setError] = useState('')
  useEffect(() => {
    if (!path) return
    let active = true
    const get = (background = false) => {
      if (!background) setLoading(true)
      api.get<T>(path).then(value => { if (active) { setData(value); setError('') } })
        .catch(e => { if (active) setError(message(e)) })
        .finally(() => { if (active) setLoading(false) })
    }
    get()
    const timer = interval ? window.setInterval(() => { if (!document.hidden) get(true) }, interval) : undefined
    return () => { active = false; window.clearInterval(timer) }
  }, [path, version, interval])
  return { data, loading, error }
}
export const message = (error: unknown) => error instanceof Error ? error.message : 'Không thể hoàn tất thao tác. Vui lòng thử lại.'
export const dateText = (value: string | null | undefined) => value ? new Date(value.slice(0, 10) + 'T00:00:00').toLocaleDateString('vi-VN') : '—'
export const timeText = (value: string | null | undefined) => value ? new Date(value).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' }) : 'Chưa đăng nhập'
export const money = (value: number) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value)
export const today = () => new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date())
export const monthNow = () => today().slice(0, 7)
export const roleName: Record<string, string> = { Admin: 'Quản trị viên', HR: 'Nhân sự', Manager: 'Quản lý', Employee: 'Nhân viên' }
export const statusName: Record<string, string> = { Pending: 'Chờ duyệt', Submitted: 'Chờ duyệt', Approved: 'Đã duyệt', Rejected: 'Từ chối', Cancelled: 'Đã hủy', Draft: 'Bản nháp', Published: 'Đã phát hành', Active: 'Đang hoạt động', Done: 'Hoàn thành', InProgress: 'Đang thực hiện' }
export function Heading({ title, description, action, eyebrow = 'TECHHRMS / WORKSPACE' }: { title: string; description: string; action?: ReactNode; eyebrow?: string }) {
  return <div className="page-heading v4-heading"><div><div className="kicker">{eyebrow}</div><h1>{title}</h1><p>{description}</p></div>{action}</div>
}
export function Panel({ children, title, description, action, className = '' }: { children: ReactNode; title?: string; description?: string; action?: ReactNode; className?: string }) {
  return <section className={`panel v4-panel ${className}`}>{title && <div className="panel-heading"><div><h2>{title}</h2>{description && <p>{description}</p>}</div>{action}</div>}{children}</section>
}
export function Field({ label, children, hint }: { label: string; children: ReactNode; hint?: string }) {
  return <label className="field"><span>{label}</span>{children}{hint && <small className="v4-field-hint">{hint}</small>}</label>
}
export function Alert({ children, kind = 'error' }: { children?: ReactNode; kind?: 'error' | 'success' | 'info' }) {
  return children ? <div className={`v4-alert v4-alert-${kind}`} role={kind === 'error' ? 'alert' : 'status'}>{children}</div> : null
}
export function Busy({ children = 'Đang tải dữ liệu…' }: { children?: ReactNode }) { return <div className="v4-state" role="status"><span className="v4-spinner"/>{children}</div> }
export function Empty({ title = 'Chưa có dữ liệu', description = 'Dữ liệu sẽ hiển thị tại đây sau khi được tạo.', action }: { title?: string; description?: string; action?: ReactNode }) {
  return <div className="v4-empty"><span className="v4-empty-icon" aria-hidden="true">▤</span><h3>{title}</h3><p>{description}</p>{action}</div>
}
export function Tag({ status, children, tone }: { status?: string; children?: ReactNode; tone?: string }) {
  const type = tone ?? (['Approved', 'Done', 'Published', 'Active'].includes(status ?? '') ? 'good' : ['Rejected', 'Cancelled', 'Inactive'].includes(status ?? '') ? 'bad' : 'warn')
  return <span className={`badge badge-${type}`}>{children ?? statusName[status ?? ''] ?? status}</span>
}
export function Modal({ title, onClose, children, wide = false }: { title: string; onClose: () => void; children: ReactNode; wide?: boolean }) {
  const ref = useRef<HTMLDivElement>(null)
  const closeRef = useRef(onClose)
  useEffect(() => { closeRef.current = onClose }, [onClose])
  useEffect(() => {
    const previous = document.activeElement as HTMLElement | null
    const bodyOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const focusable = () => Array.from(ref.current?.querySelectorAll<HTMLElement>('button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), a[href], [tabindex="0"]') ?? [])
    focusable()[0]?.focus()
    const key = (event: KeyboardEvent) => {
      if (event.key === 'Escape') closeRef.current()
      if (event.key !== 'Tab') return
      const items = focusable()
      const first = items[0], last = items[items.length - 1]
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus() }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus() }
    }
    document.addEventListener('keydown', key)
    return () => { document.body.style.overflow = bodyOverflow; document.removeEventListener('keydown', key); previous?.focus() }
  }, [])
  return <div className="modal-backdrop v4-modal-backdrop" onMouseDown={e => { if (e.target === e.currentTarget) onClose() }}><div ref={ref} className={`modal-card v4-modal ${wide ? 'v4-modal-wide' : ''}`} role="dialog" aria-modal="true" aria-label={title}><div className="modal-header"><h2>{title}</h2><button type="button" className="icon-button" onClick={onClose} aria-label="Đóng hộp thoại">×</button></div>{children}</div></div>
}
export function Submit({ busy, children = 'Lưu thay đổi' }: { busy: boolean; children?: ReactNode }) { return <button type="submit" disabled={busy} className="button primary">{busy ? 'Đang xử lý…' : children}</button> }
export function Metrics({ items }: { items: { label: string; value: ReactNode; detail?: string }[] }) {
  return <div className="v4-metrics">{items.map(item => <div className="v4-metric" key={item.label}><span>{item.label}</span><strong>{item.value}</strong>{item.detail && <small>{item.detail}</small>}</div>)}</div>
}
