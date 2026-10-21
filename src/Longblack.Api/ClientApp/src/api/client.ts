export const TOKEN_KEY = 'lb_token'

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem(TOKEN_KEY)
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(init.headers as Record<string, string> | undefined),
  }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(path, { ...init, headers })

  if (res.status === 401) {
    window.dispatchEvent(new Event('lb:unauthorized'))
    throw new Error('Unauthorized')
  }

  if (!res.ok) {
    let message = `Request failed: ${res.status}`
    try {
      const body = await res.json()
      message = body?.message ?? message
    } catch { /* ignore parse errors */ }
    const err = Object.assign(new Error(message), { status: res.status })
    throw err
  }

  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}

export const api = {
  getBlob: async (path: string): Promise<{ blob: Blob; fileName: string }> => {
    const token = localStorage.getItem(TOKEN_KEY)
    const res = await fetch(path, { headers: token ? { Authorization: `Bearer ${token}` } : {} })
    if (res.status === 401) {
      window.dispatchEvent(new Event('lb:unauthorized'))
      throw new Error('Unauthorized')
    }
    if (!res.ok) {
      let message = `Request failed: ${res.status}`
      try {
        message = (await res.json())?.message ?? message
      } catch { /* ignore parse errors */ }
      throw new Error(message)
    }
    const disposition = res.headers.get('Content-Disposition') ?? ''
    const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition)
    return { blob: await res.blob(), fileName: match ? decodeURIComponent(match[1]) : 'download.pdf' }
  },
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  patch: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PATCH', body: JSON.stringify(body) }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
}
