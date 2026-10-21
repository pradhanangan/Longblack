import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider, createTheme } from "@mui/material/styles";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AppLayout } from "./components/layout/AppLayout";
import { ProtectedRoute } from "./components/layout/ProtectedRoute";
import { AuthProvider } from "./contexts/AuthContext";
import { SnackbarProvider } from "./contexts/SnackbarContext";
import { DashboardPage } from "./pages/DashboardPage";
import { GoodsReceiptDetailPage } from "./pages/GoodsReceiptDetailPage";
import { GoodsReceiptListPage } from "./pages/GoodsReceiptListPage";
import { InventoryDetailPage } from "./pages/InventoryDetailPage";
import { InventoryListPage } from "./pages/InventoryListPage";
import { LoginPage } from "./pages/LoginPage";
import { ProductDetailPage } from "./pages/ProductDetailPage";
import { ProductListPage } from "./pages/ProductListPage";
import { PurchaseOrderDetailPage } from "./pages/PurchaseOrderDetailPage";
import { PurchaseOrderListPage } from "./pages/PurchaseOrderListPage";
import { SaleDetailPage } from "./pages/SaleDetailPage";
import { SaleListPage } from "./pages/SaleListPage";
import { StockTakeDetailPage } from "./pages/StockTakeDetailPage";
import { StockTakeListPage } from "./pages/StockTakeListPage";
import { StockTransferDetailPage } from "./pages/StockTransferDetailPage";
import { StockTransferListPage } from "./pages/StockTransferListPage";
import { StoreDetailPage } from "./pages/StoreDetailPage";
import { StoreListPage } from "./pages/StoreListPage";
import { SupplierListPage } from "./pages/SupplierListPage";
import { SettingsPage } from "./pages/SettingsPage";

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000 } },
});

const theme = createTheme({
  typography: {
    fontFamily: '"Source Sans 3", sans-serif',
  },
  components: {
    // MuiButton: {
    //   styleOverrides: {
    //     root: { textTransform: "none" },
    //   },
    // },
    MuiTextField: {
      defaultProps: { size: "small" },
    },
    MuiAutocomplete: {
      defaultProps: { size: "small" },
    },
    MuiFormControl: {
      defaultProps: { size: "small" },
    },
    MuiSelect: {
      defaultProps: { size: "small" },
    },
  },
});

export default function App() {
  return (
    <ThemeProvider theme={theme}>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <SnackbarProvider>
            <BrowserRouter>
              <Routes>
                <Route path="/login" element={<LoginPage />} />
                <Route element={<ProtectedRoute />}>
                  <Route element={<AppLayout />}>
                    <Route
                      index
                      element={<Navigate to="/dashboard" replace />}
                    />
                    <Route path="/dashboard" element={<DashboardPage />} />
                    <Route path="/products" element={<ProductListPage />} />
                    <Route
                      path="/products/:id"
                      element={<ProductDetailPage />}
                    />
                    <Route
                      path="/purchase-orders"
                      element={<PurchaseOrderListPage />}
                    />
                    <Route
                      path="/purchase-orders/:id"
                      element={<PurchaseOrderDetailPage />}
                    />
                    <Route
                      path="/goods-receipts"
                      element={<GoodsReceiptListPage />}
                    />
                    <Route
                      path="/goods-receipts/:id"
                      element={<GoodsReceiptDetailPage />}
                    />
                    <Route path="/inventory" element={<InventoryListPage />} />
                    <Route
                      path="/inventory/:productVariantId"
                      element={<InventoryDetailPage />}
                    />
                    <Route
                      path="/stock-takes"
                      element={<StockTakeListPage />}
                    />
                    <Route
                      path="/stock-takes/:id"
                      element={<StockTakeDetailPage />}
                    />
                    <Route path="/stores" element={<StoreListPage />} />
                    <Route path="/stores/:id" element={<StoreDetailPage />} />
                    <Route
                      path="/stock-transfers"
                      element={<StockTransferListPage />}
                    />
                    <Route
                      path="/stock-transfers/:id"
                      element={<StockTransferDetailPage />}
                    />
                    <Route path="/sales" element={<SaleListPage />} />
                    <Route path="/sales/:id" element={<SaleDetailPage />} />
                    <Route path="/suppliers" element={<SupplierListPage />} />
                    <Route path="/settings" element={<SettingsPage />} />
                  </Route>
                </Route>
              </Routes>
            </BrowserRouter>
          </SnackbarProvider>
        </AuthProvider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}
