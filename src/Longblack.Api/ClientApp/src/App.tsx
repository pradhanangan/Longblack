import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider, createTheme } from "@mui/material/styles";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AppLayout } from "./components/layout/AppLayout";
import { AdminRoute, ManagerRoute, ProtectedRoute } from "./components/layout/ProtectedRoute";
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
import { BrandListPage } from "./pages/BrandListPage";
import { CategoryListPage } from "./pages/CategoryListPage";
import { SettingsPage } from "./pages/SettingsPage";
import { ReportsPage } from "./pages/ReportsPage";

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
    // Form dialogs (fullWidth) go full-screen on phones; ConfirmDialog stays compact.
    MuiDialog: {
      styleOverrides: {
        paperFullWidth: ({ theme }) => ({
          [theme.breakpoints.down("sm")]: {
            margin: 0,
            width: "100%",
            maxWidth: "100%",
            height: "100%",
            maxHeight: "none",
            borderRadius: 0,
          },
        }),
      },
    },
    // Below sm, tables render each row as a card (labels set by useStackedTableLabels).
    // Tables marked data-no-stack (wide report/matrix grids) scroll horizontally instead.
    MuiTable: {
      styleOverrides: {
        root: ({ theme }) => ({
          [theme.breakpoints.down("sm")]: {
            "&[data-no-stack]": { display: "block", overflowX: "auto" },
            "&:not([data-no-stack])": {
              display: "block",
              "& thead": { display: "none" },
              "& tbody, & tfoot": { display: "block" },
              "& tr": {
                display: "block",
                border: `1px solid ${theme.palette.divider}`,
                borderRadius: theme.shape.borderRadius,
                marginBottom: theme.spacing(1.5),
                padding: theme.spacing(0.5, 1),
              },
              "& td": {
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                gap: theme.spacing(2),
                border: 0,
                padding: theme.spacing(0.75, 0.5),
                textAlign: "right",
                "&::before": {
                  content: "attr(data-label)",
                  fontWeight: 600,
                  color: theme.palette.text.secondary,
                  textAlign: "left",
                  flexShrink: 0,
                },
                '&[data-label=""]': { justifyContent: "flex-start" },
                '&[data-label=""]::before': { display: "none" },
                // Inputs inside cards fill the row instead of keeping desktop fixed widths.
                "& .MuiFormControl-root, & .MuiAutocomplete-root": {
                  flex: 1,
                  minWidth: 0,
                  width: "auto",
                },
              },
            },
          },
        }),
      },
    },
    MuiToolbar: {
      styleOverrides: {
        root: ({ theme }) => ({
          [theme.breakpoints.down("sm")]: { flexWrap: "wrap", rowGap: theme.spacing(1) },
        }),
      },
    },
    // Keeps actions pinned (and visually separated) under scrolling full-screen dialog content.
    MuiDialogActions: {
      styleOverrides: {
        root: ({ theme }) => ({
          [theme.breakpoints.down("sm")]: {
            borderTop: `1px solid ${theme.palette.divider}`,
            backgroundColor: theme.palette.background.paper,
            paddingBottom: "max(8px, env(safe-area-inset-bottom))",
          },
        }),
      },
    },
    MuiTablePagination: {
      styleOverrides: {
        toolbar: ({ theme }) => ({
          [theme.breakpoints.down("sm")]: {
            flexWrap: "wrap",
            justifyContent: "center",
            paddingLeft: theme.spacing(1),
          },
        }),
        spacer: ({ theme }) => ({
          [theme.breakpoints.down("sm")]: { display: "none" },
        }),
      },
    },
    // Minimum 44px touch targets on touch devices.
    MuiButton: {
      styleOverrides: {
        root: { "@media (pointer: coarse)": { minHeight: 44 } },
      },
    },
    MuiIconButton: {
      styleOverrides: {
        root: { "@media (pointer: coarse)": { minWidth: 44, minHeight: 44 } },
      },
    },
    MuiListItemButton: {
      styleOverrides: {
        root: { "@media (pointer: coarse)": { minHeight: 44 } },
      },
    },
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
                    <Route element={<ManagerRoute />}>
                      <Route path="/reports" element={<ReportsPage />} />
                    </Route>
                    <Route element={<AdminRoute />}>
                      <Route path="/brands" element={<BrandListPage />} />
                      <Route path="/categories" element={<CategoryListPage />} />
                    </Route>
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
