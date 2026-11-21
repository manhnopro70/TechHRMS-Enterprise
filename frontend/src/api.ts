import type { Session } from './types'

export class ApiError extends Error {
  constructor(message: string, public status: number) { super(message) }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      credentials: 'same-origin',
      ...options,
      headers: { 'Content-Type': 'application/json', ...options.headers },
    })
  } catch {
    throw new ApiError('Không kết nối được Backend. Hãy chạy dotnet run trong thư mục backend.', 0)
  }
  if (!response.ok) {
    const body = await response.json().catch(() => ({})) as { error?: string; title?: string }
    throw new ApiError(body.error ?? body.title ?? `Yêu cầu thất bại (HTTP ${response.status}).`, response.status)
  }
  return await response.json() as T
}

export const api = {
  get: <T,>(path: string) => request<T>(path),
  send: <T,>(method: 'POST' | 'PUT' | 'DELETE', path: string, data?: unknown) =>
    request<T>(path, { method, body: data === undefined ? undefined : JSON.stringify(data) }),
  me: async (): Promise<Session | null> => {
    try { return await request<Session>('/auth/me') }
    catch (error) { if (error instanceof ApiError && error.status === 401) return null; throw error }
  },
  login: (email: string, password: string) => request<Session>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  logout: () => request<{ message: string }>('/auth/logout', { method: 'POST' }),
}
