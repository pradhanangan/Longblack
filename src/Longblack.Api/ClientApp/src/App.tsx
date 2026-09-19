import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/layout/AppLayout'
import { ProtectedRoute } from './components/layout/ProtectedRoute'
import { AuthProvider } from './contexts/AuthContext'
import { SnackbarProvider } from './contexts/SnackbarContext'
import { GoodsReceiptDetailPage } from './pages/GoodsReceiptDetailPage'
import { GoodsReceiptListPage } from './pages/GoodsReceiptListPage'
import { InventoryDetailPage } from './pages/InventoryDetailPage'
import { InventoryListPage } from './pages/InventoryListPage'
import { LoginPage } from './pages/LoginPage'
import { ProductDetailPage } from './pages/ProductDetailPage'
import { ProductListPage } from './pages/ProductListPage'
import { StockTakeDetailPage } from './pages/StockTakeDetailPage'
import { StockTakeListPage } from './pages/StockTakeListPage'

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000 } },
})

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <SnackbarProvider>
          <BrowserRouter>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route element={<ProtectedRoute />}>
                <Route element={<AppLayout />}>
                  <Route index element={<Navigate to="/products" replace />} />
                  <Route path="/products" element={<ProductListPage />} />
                  <Route path="/products/:id" element={<ProductDetailPage />} />
                  <Route path="/goods-receipts" element={<GoodsReceiptListPage />} />
                  <Route path="/goods-receipts/:id" element={<GoodsReceiptDetailPage />} />
                  <Route path="/inventory" element={<InventoryListPage />} />
                  <Route path="/inventory/:productVariantId" element={<InventoryDetailPage />} />
                  <Route path="/stock-takes" element={<StockTakeListPage />} />
                  <Route path="/stock-takes/:id" element={<StockTakeDetailPage />} />
                </Route>
              </Route>
            </Routes>
          </BrowserRouter>
        </SnackbarProvider>
      </AuthProvider>
    </QueryClientProvider>
  )
}

