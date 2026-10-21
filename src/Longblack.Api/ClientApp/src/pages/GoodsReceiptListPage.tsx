import AddIcon from "@mui/icons-material/Add";
import {
  Box,
  Button,
  Chip,
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
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { api } from "../api/client";
import type { GoodsReceiptDto, SupplierDto } from "../api/types";
import { GoodsReceiptFormDialog } from "../components/receiving/GoodsReceiptFormDialog";
import { useAuth } from "../contexts/AuthContext";

function statusColor(status: string): "default" | "success" | "error" {
  if (status === "Received") return "success";
  if (status === "Cancelled") return "error";
  return "default";
}

function useGoodsReceipts(status: string, supplierId: string) {
  const params = new URLSearchParams();
  if (status) params.set("status", status);
  if (supplierId) params.set("supplierId", supplierId);
  const qs = params.toString();
  return useQuery<GoodsReceiptDto[]>({
    queryKey: ["goods-receipts", status, supplierId],
    queryFn: () => api.get(`/api/goods-receipts${qs ? `?${qs}` : ""}`),
  });
}

function useSuppliers() {
  return useQuery<SupplierDto[]>({
    queryKey: ["suppliers", "Active"],
    queryFn: () => api.get("/api/suppliers?status=Active"),
    staleTime: 5 * 60_000,
  });
}

export function GoodsReceiptListPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const canCreate =
    user?.roles.some(
      (r) => r === "Staff" || r === "Manager" || r === "Admin",
    ) ?? false;

  const [status, setStatus] = useState("");
  const [supplierId, setSupplierId] = useState("");
  const [createOpen, setCreateOpen] = useState(false);

  const {
    data: receipts,
    isLoading,
    isError,
  } = useGoodsReceipts(status, supplierId);
  const { data: suppliers } = useSuppliers();

  return (
    <Box>
      <Toolbar
        disableGutters
        sx={{ justifyContent: "space-between", gap: 1, mb: 3 }}
      >
        <Typography variant="h5">Goods Receipts</Typography>
        {canCreate && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setCreateOpen(true)}
          >
            New Receipt
          </Button>
        )}
      </Toolbar>

      <GoodsReceiptFormDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
      />

      <Box sx={{ display: "flex", gap: 2, mb: 3, flexWrap: "wrap" }}>
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel>Supplier</InputLabel>
          <Select
            value={supplierId}
            label="Supplier"
            onChange={(e) => setSupplierId(e.target.value)}
          >
            <MenuItem value="">All suppliers</MenuItem>
            {suppliers?.map((s) => (
              <MenuItem key={s.id} value={s.id}>
                {s.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 140 }}>
          <InputLabel>Status</InputLabel>
          <Select
            value={status}
            label="Status"
            onChange={(e) => setStatus(e.target.value)}
          >
            <MenuItem value="">All</MenuItem>
            <MenuItem value="Draft">Draft</MenuItem>
            <MenuItem value="Received">Received</MenuItem>
            <MenuItem value="Cancelled">Cancelled</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {isLoading && <CircularProgress />}
      {isError && (
        <Typography color="error">Failed to load goods receipts.</Typography>
      )}

      {!isLoading &&
        !isError &&
        receipts &&
        (receipts.length === 0 ? (
          <Typography color="text.secondary">
            No goods receipts match the current filters.
          </Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Receipt #</TableCell>
                <TableCell>Supplier</TableCell>
                <TableCell>Purchase Order</TableCell>
                <TableCell>Received Date</TableCell>
                <TableCell>Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {receipts.map((r) => (
                <TableRow
                  key={r.id}
                  hover
                  onClick={() => navigate(`/goods-receipts/${r.id}`)}
                  sx={{ cursor: "pointer" }}
                >
                  <TableCell>{r.receiptNumber}</TableCell>
                  <TableCell>{r.supplierName}</TableCell>
                  <TableCell>{r.purchaseOrderNumber ?? "—"}</TableCell>
                  <TableCell>
                    {new Date(r.receivedDate).toLocaleDateString()}
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={r.status}
                      size="small"
                      color={statusColor(r.status)}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ))}
    </Box>
  );
}

