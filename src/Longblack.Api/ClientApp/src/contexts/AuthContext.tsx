import { jwtDecode } from 'jwt-decode'
import { createContext, useCallback, useContext, useState } from 'react'

interface JwtPayload {
  sub: string
  email: string
  name?: string
  role?: string | string[]
}

interface AuthUser {
  id: string
  email: string
  name: string
  roles: string[]
}

interface AuthContextValue {
  token: string | null
  user: AuthUser | null
  login: (token: string) => void
  logout: () => void
}

const TOKEN_KEY = 'lb_token'

function decodeUser(token: string): AuthUser | null {
  try {
    const payload = jwtDecode<JwtPayload>(token)
    const roles = Array.isArray(payload.role)
      ? payload.role
      : payload.role
        ? [payload.role]
        : []
    return {
      id: payload.sub,
      email: payload.email,
      name: payload.name ?? payload.email,
      roles,
    }
  } catch {
    return null
  }
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(TOKEN_KEY))
  const [user, setUser] = useState<AuthUser | null>(() => {
    const stored = localStorage.getItem(TOKEN_KEY)
    return stored ? decodeUser(stored) : null
  })

  const login = useCallback((newToken: string) => {
    localStorage.setItem(TOKEN_KEY, newToken)
    setToken(newToken)
    setUser(decodeUser(newToken))
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY)
    setToken(null)
    setUser(null)
  }, [])

  return (
    <AuthContext.Provider value={{ token, user, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
