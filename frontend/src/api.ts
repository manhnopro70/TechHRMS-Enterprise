import type { Session } from './types'

export class ApiError extends Error {
  status: number
  code?: string
  constructor(message: string, status: number, code?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}
let authGeneration = 0
let csrfToken: string | null = null
let csrfRequest: Promise<string> | null = null
function clearCsrf() { csrfToken = null; csrfRequest = null }
async function getCsrf(): Promise<string> {
  if (csrfToken) return csrfToken
  if (!csrfRequest) csrfRequest = fetch('/api/auth/csrf', { credentials: 'same-origin', cache: 'no-store' })
    .then(async response => {
      if (!response.ok) throw new ApiError('Không thể khởi tạo phiên làm việc. Vui lòng tải lại trang.', response.status)
      const result = await response.json() as { token: string }
      csrfToken = result.token
      return result.token
    }).finally(() => { csrfRequest = null })
  return csrfRequest
}
async function request<T>(path: string, options: RequestInit = {}, retry = true): Promise<T> {
  const generation = authGeneration
  const method = options.method ?? 'GET'
  const headers = new Headers(options.headers)
  if (options.body !== undefined) headers.set('Content-Type', 'application/json')
  const controller = new AbortController()
  let response: Response
  const timeout = window.setTimeout(() => controller.abort(), 90000)
  try {
    if (method !== 'GET' && method !== 'HEAD') headers.set('X-CSRF-TOKEN', await getCsrf())
    response = await fetch(`/api${path}`, { ...options, headers, credentials: 'same-origin', cache: 'no-store', signal: controller.signal })
  } catch (error) {
    if (error instanceof ApiError) throw error
    throw new ApiError(controller.signal.aborted ? 'Máy chủ phản hồi quá lâu. Vui lòng thử lại sau ít phút.' : 'Chưa kết nối được máy chủ. Kiểm tra Internet rồi thử lại.', 0)
  } finally { window.clearTimeout(timeout) }
  if (!response.ok) {
    const body = await response.json().catch(() => ({})) as { error?: string; title?: string; code?: string }
    if ([400, 403].includes(response.status) && body.code === 'CSRF_INVALID' && retry) { clearCsrf(); return request<T>(path, options, false) }
    if (response.status === 401 && generation === authGeneration && !['/auth/login', '/auth/me'].includes(path)) { clearCsrf(); window.dispatchEvent(new Event('techhrms:session-expired')) }
    if (body.code === 'PASSWORD_CHANGE_REQUIRED' && generation === authGeneration) window.dispatchEvent(new Event('techhrms:password-required'))
    const fallback = response.status === 401 ? 'Phiên đăng nhập đã kết thúc. Vui lòng đăng nhập lại.' : response.status === 403 ? 'Bạn chưa có quyền thực hiện thao tác này.' : `Yêu cầu chưa hoàn tất (HTTP ${response.status}).`
    throw new ApiError(body.error ?? body.title ?? fallback, response.status, body.code)
  }
  if (response.status === 204) return undefined as T
  const text = await response.text()
  if (!text) return undefined as T
  try { return JSON.parse(text) as T } catch { throw new ApiError('Máy chủ trả về dữ liệu không hợp lệ. Vui lòng thử lại.', response.status) }
}
export const api = {
  get: <T,>(path: string) => request<T>(path),
  send: <T,>(method: 'POST' | 'PUT' | 'DELETE', path: string, data?: unknown) => request<T>(path, { method, body: data === undefined ? undefined : JSON.stringify(data) }),
  me: async (): Promise<Session | null> => { try { return await request<Session>('/auth/me') } catch (e) { if (e instanceof ApiError && e.status === 401) { clearCsrf(); return null } throw e } },
  login: async (email: string, password: string) => { const value = await request<Session>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }); authGeneration += 1; clearCsrf(); return value },
  logout: async () => { const value = await request<{ message: string }>('/auth/logout', { method: 'POST' }); authGeneration += 1; clearCsrf(); return value },
  changePassword: async (currentPassword: string, newPassword: string) => { const value = await request<Session>('/auth/change-password', { method: 'POST', body: JSON.stringify({ currentPassword, newPassword }) }); authGeneration += 1; clearCsrf(); return value },
}
