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
  TextField,
  Toolbar,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api/client";
import type { PurchaseOrderDto, SupplierDto } from "../api/types";
import { PurchaseOrderFormDialog } from "../components/purchasing/PurchaseOrderFormDialog";
import { GridPagination } from "../components/common/GridPagination";
import { usePagedList } from "../components/common/usePagedList";
import {
  purchaseOrderStatusColor,
  purchaseOrderStatusLabel,
} from "../components/purchasing/purchaseOrderStatus";
import { useAuth } from "../contexts/AuthContext";

const STATUSES = [
  "Draft",
  "Issued",
  "PartiallyReceived",
  "Received",
  "Closed",
  "Cancelled",
];

function usePurchaseOrders(
  status: string,
  supplierId: string,
  from: string,
  to: string,
) {
  const params = new URLSearchParams();
  if (status) params.set("status", status);
  if (supplierId) params.set("supplierId", supplierId);
  if (from) params.set("from", new Date(from).toISOString());
  if (to) {
    // Include the whole end day.
    const end = new Date(to);
    end.setHours(23, 59, 59, 999);
    params.set("to", end.toISOString());
  }
  return usePagedList<PurchaseOrderDto>("purchase-orders", params);
}

export function PurchaseOrderListPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const canCreate =
    user?.roles.some((r) => r === "Manager" || r === "Admin") ?? false;

  const [status, setStatus] = useState("");
  const [supplierId, setSupplierId] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [createOpen, setCreateOpen] = useState(false);

  const {
    items: orders,
    pagination,
    isLoading,
    isError,
  } = usePurchaseOrders(status, supplierId, from, to);
  const { data: suppliers } = useQuery<SupplierDto[]>({
    queryKey: ["suppliers"],
    queryFn: () => api.get("/api/suppliers"),
    staleTime: 5 * 60_000,
  });

  return (
    <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1 }}>
      <Toolbar
        disableGutters
        sx={{ justifyContent: "space-between", gap: 1, mb: 3 }}
      >
        <Typography variant="h5">Purchase Orders</Typography>
        {canCreate && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setCreateOpen(true)}
          >
            New Purchase Order
          </Button>
        )}
      </Toolbar>

      <PurchaseOrderFormDialog
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
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel>Status</InputLabel>
          <Select
            value={status}
            label="Status"
            onChange={(e) => setStatus(e.target.value)}
          >
            <MenuItem value="">All</MenuItem>
            {STATUSES.map((s) => (
              <MenuItem key={s} value={s}>
                {purchaseOrderStatusLabel(s)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <TextField
          label="Created from"
          type="date"
          size="small"
          value={from}
          onChange={(e) => setFrom(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          label="Created to"
          type="date"
          size="small"
          value={to}
          onChange={(e) => setTo(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
      </Box>

      {(isLoading || isError || orders?.length === 0) && (
        <Box
          sx={{
            flexGrow: 1,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            textAlign: "center",
          }}
        >
          {isLoading ? (
            <CircularProgress />
          ) : isError ? (
            <Typography color="error">
              Failed to load purchase orders.
            </Typography>
          ) : (
            <Typography color="text.secondary">
              No purchase orders match the current filters.
            </Typography>
          )}
        </Box>
      )}

      {!isLoading && !isError && orders && orders.length > 0 && (
        <>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>PO #</TableCell>
                <TableCell>Supplier</TableCell>
                <TableCell>Expected Delivery</TableCell>
                <TableCell align="right">Total</TableCell>
                <TableCell>Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {orders.map((o) => (
                <TableRow
                  key={o.id}
                  hover
                  onClick={() => navigate(`/purchase-orders/${o.id}`)}
                  sx={{ cursor: "pointer" }}
                >
                  <TableCell>{o.poNumber ?? "Draft"}</TableCell>
                  <TableCell>{o.supplierName}</TableCell>
                  <TableCell>
                    {o.expectedDeliveryDate
                      ? new Date(o.expectedDeliveryDate).toLocaleDateString()
                      : "—"}
                  </TableCell>
                  <TableCell align="right">${o.total.toFixed(2)}</TableCell>
                  <TableCell>
                    <Chip
                      label={purchaseOrderStatusLabel(o.status)}
                      size="small"
                      color={purchaseOrderStatusColor(o.status)}
                    />
                    {o.isOverdue && (
                      <Chip
                        label="Overdue"
                        size="small"
                        color="error"
                        variant="outlined"
                        sx={{ ml: 1 }}
                      />
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <GridPagination {...pagination} />
        </>
      )}
    </Box>
  );
}
