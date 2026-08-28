import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useEffect } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { configureApiClient } from './api/client'
import { AppLayout } from './components/layout/AppLayout'
import { ProtectedRoute } from './components/layout/ProtectedRoute'
import { AuthProvider, useAuth } from './contexts/AuthContext'
import { SnackbarProvider } from './contexts/SnackbarContext'
import { LoginPage } from './pages/LoginPage'
import { ProductDetailPage } from './pages/ProductDetailPage'
import { ProductListPage } from './pages/ProductListPage'

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000 } },
})

function ApiClientBootstrap({ children }: { children: React.ReactNode }) {
  const { token, logout } = useAuth()
  useEffect(() => {
    configureApiClient(
      () => token,
      () => { logout() },
    )
  }, [token, logout])
  return <>{children}</>
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <SnackbarProvider>
          <ApiClientBootstrap>
            <BrowserRouter>
              <Routes>
                <Route path="/login" element={<LoginPage />} />
                <Route element={<ProtectedRoute />}>
                  <Route element={<AppLayout />}>
                    <Route index element={<Navigate to="/products" replace />} />
                    <Route path="/products" element={<ProductListPage />} />
                    <Route path="/products/:id" element={<ProductDetailPage />} />
                  </Route>
                </Route>
              </Routes>
            </BrowserRouter>
          </ApiClientBootstrap>
        </SnackbarProvider>
      </AuthProvider>
    </QueryClientProvider>
  )
}

