import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../../contexts/AuthContext'

export function ProtectedRoute() {
  const { token } = useAuth()
  if (!token) return <Navigate to="/login" replace />
  return <Outlet />
}

export function AdminRoute() {
  const { user } = useAuth()
  if (!user?.roles.includes('Admin')) return <Navigate to="/dashboard" replace />
  return <Outlet />
}
