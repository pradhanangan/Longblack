import {
  Box,
  Card,
  CardActionArea,
  CardContent,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Toolbar,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api/client";
import type { StoreDto } from "../api/types";

interface DashboardTransactionDto {
  id: string;
  productVariantId: string;
  sku: string;
  productName: string;
  storeName: string;
  type: string;
  quantityDelta: number;
  createdAt: string;
  createdBy: string;
}

interface DashboardDto {
  totalUnitsOnHand: number;
  variantsAtZeroOrBelow: number;
  draftGoodsReceipts: number;
  transfersInTransit: number;
  stockTakesOpen: number;
  salesTotalLast7Days: number;
  recentSales: DashboardSaleDto[];
  recentTransactions: DashboardTransactionDto[];
}

interface DashboardSaleDto {
  id: string;
  saleNumber: string;
  storeName: string;
  total: number;
  completedDate: string;
  completedBy: string;
}

function StatCard({
  label,
  value,
  to,
}: {
  label: string;
  value: number;
  to: string;
}) {
  const navigate = useNavigate();
  return (
    <Card variant="outlined" sx={{ minWidth: 200, flex: "1 1 200px" }}>
      <CardActionArea onClick={() => navigate(to)}>
        <CardContent>
          <Typography variant="h4">{value}</Typography>
          <Typography color="text.secondary">{label}</Typography>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}

export function DashboardPage() {
  const navigate = useNavigate();
  const [storeId, setStoreId] = useState("");

  const { data: stores } = useQuery<StoreDto[]>({
    queryKey: ["stores", "Active"],
    queryFn: () => api.get("/api/stores?status=Active"),
    staleTime: 5 * 60_000,
  });

  const {
    data: dashboard,
    isLoading,
    isError,
  } = useQuery<DashboardDto>({
    queryKey: ["dashboard", storeId],
    queryFn: () =>
      api.get(`/api/dashboard${storeId ? `?storeId=${storeId}` : ""}`),
  });

  return (
    <Box>
      <Toolbar disableGutters sx={{ mb: 3, gap: 2 }}>
        <Typography variant="h5" sx={{ flexGrow: 1 }}>
          Dashboard
        </Typography>
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel>Store</InputLabel>
          <Select
            value={storeId}
            label="Store"
            onChange={(e) => setStoreId(e.target.value)}
          >
            <MenuItem value="">All stores</MenuItem>
            {stores?.map((s) => (
              <MenuItem key={s.id} value={s.id}>
                {s.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Toolbar>

      {isLoading && <CircularProgress />}
      {isError && (
        <Typography color="error">Failed to load dashboard.</Typography>
      )}

      {dashboard && (
        <>
          <Box sx={{ display: "flex", gap: 2, mb: 2, flexWrap: "wrap" }}>
            <StatCard
              label="Units on hand"
              value={dashboard.totalUnitsOnHand}
              to="/inventory"
            />
            <StatCard
              label="Variants at zero or below"
              value={dashboard.variantsAtZeroOrBelow}
              to="/inventory"
            />
          </Box>
          <Box sx={{ display: "flex", gap: 2, mb: 4, flexWrap: "wrap" }}>
            <StatCard
              label="Draft goods receipts"
              value={dashboard.draftGoodsReceipts}
              to="/goods-receipts"
            />
            <StatCard
              label="Transfers in transit"
              value={dashboard.transfersInTransit}
              to="/stock-transfers"
            />
            <StatCard
              label="Stock takes awaiting approval or in progress"
              value={dashboard.stockTakesOpen}
              to="/stock-takes"
            />
          </Box>

          <Typography variant="h6" sx={{ mb: 1 }}>
            Sales
          </Typography>
          <Box sx={{ display: "flex", gap: 2, mb: 2, flexWrap: "wrap" }}>
            <Card variant="outlined" sx={{ minWidth: 200, flex: "1 1 200px" }}>
              <CardActionArea onClick={() => navigate("/sales")}>
                <CardContent>
                  <Typography variant="h4">
                    ${dashboard.salesTotalLast7Days.toFixed(2)}
                  </Typography>
                  <Typography color="text.secondary">
                    Sales, last 7 days (GST incl.)
                  </Typography>
                </CardContent>
              </CardActionArea>
            </Card>
          </Box>
          {dashboard.recentSales.length === 0 ? (
            <Typography color="text.secondary" sx={{ mb: 4 }}>
              No completed sales yet.
            </Typography>
          ) : (
            <Table size="small" sx={{ mb: 4 }}>
              <TableHead>
                <TableRow>
                  <TableCell>Completed</TableCell>
                  <TableCell>Sale #</TableCell>
                  <TableCell>Store</TableCell>
                  <TableCell align="right">Total</TableCell>
                  <TableCell>User</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {dashboard.recentSales.map((s) => (
                  <TableRow
                    key={s.id}
                    hover
                    onClick={() => navigate(`/sales/${s.id}`)}
                    sx={{ cursor: "pointer" }}
                  >
                    <TableCell>
                      {new Date(s.completedDate).toLocaleString()}
                    </TableCell>
                    <TableCell>{s.saleNumber}</TableCell>
                    <TableCell>{s.storeName}</TableCell>
                    <TableCell align="right">${s.total.toFixed(2)}</TableCell>
                    <TableCell>{s.completedBy}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}

          <Typography variant="h6" sx={{ mb: 1 }}>
            Recent activity
          </Typography>
          {dashboard.recentTransactions.length === 0 ? (
            <Typography color="text.secondary">
              No inventory activity yet.
            </Typography>
          ) : (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Date</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>SKU</TableCell>
                  <TableCell>Product</TableCell>
                  <TableCell>Store</TableCell>
                  <TableCell align="right">Change</TableCell>
                  <TableCell>User</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {dashboard.recentTransactions.map((t) => (
                  <TableRow key={t.id}>
                    <TableCell>{new Date(t.createdAt).toLocaleString()}</TableCell>
                    <TableCell>{t.type}</TableCell>
                    <TableCell>{t.sku}</TableCell>
                    <TableCell>{t.productName}</TableCell>
                    <TableCell>{t.storeName}</TableCell>
                    <TableCell align="right">
                      {t.quantityDelta > 0
                        ? `+${t.quantityDelta}`
                        : t.quantityDelta}
                    </TableCell>
                    <TableCell>{t.createdBy}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </>
      )}
    </Box>
  );
}
