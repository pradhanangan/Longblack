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
import type { SaleDto, StoreDto } from "../api/types";
import { SaleFormDialog } from "../components/sales/SaleFormDialog";
import { saleStatusColor } from "../components/sales/saleStatus";

function useSales(status: string, storeId: string, from: string, to: string) {
  const params = new URLSearchParams();
  if (status) params.set("status", status);
  if (storeId) params.set("storeId", storeId);
  if (from) params.set("from", new Date(`${from}T00:00:00`).toISOString());
  if (to) params.set("to", new Date(`${to}T23:59:59.999`).toISOString());
  const qs = params.toString();
  return useQuery<SaleDto[]>({
    queryKey: ["sales", status, storeId, from, to],
    queryFn: () => api.get(`/api/sales${qs ? `?${qs}` : ""}`),
  });
}

export function SaleListPage() {
  const navigate = useNavigate();

  const [status, setStatus] = useState("");
  const [storeId, setStoreId] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [createOpen, setCreateOpen] = useState(false);

  const { data: sales, isLoading, isError } = useSales(status, storeId, from, to);
  const { data: stores } = useQuery<StoreDto[]>({
    queryKey: ["stores", "Active"],
    queryFn: () => api.get("/api/stores?status=Active"),
    staleTime: 5 * 60_000,
  });

  return (
    <Box>
      <Toolbar disableGutters sx={{ justifyContent: "space-between", gap: 1, mb: 3 }}>
        <Typography variant="h5">Sales</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
          New Sale
        </Button>
      </Toolbar>

      <SaleFormDialog open={createOpen} onClose={() => setCreateOpen(false)} />

      <Box sx={{ display: "flex", gap: 2, mb: 3, flexWrap: "wrap" }}>
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Store</InputLabel>
          <Select value={storeId} label="Store" onChange={(e) => setStoreId(e.target.value)}>
            <MenuItem value="">All</MenuItem>
            {stores
              ?.filter((s) => s.type !== "Warehouse")
              .map((s) => (
                <MenuItem key={s.id} value={s.id}>
                  {s.name}
                </MenuItem>
              ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 140 }}>
          <InputLabel>Status</InputLabel>
          <Select value={status} label="Status" onChange={(e) => setStatus(e.target.value)}>
            <MenuItem value="">All</MenuItem>
            <MenuItem value="Draft">Draft</MenuItem>
            <MenuItem value="Completed">Completed</MenuItem>
            <MenuItem value="Voided">Voided</MenuItem>
            <MenuItem value="Cancelled">Cancelled</MenuItem>
          </Select>
        </FormControl>
        <TextField
          size="small"
          type="date"
          label="From"
          value={from}
          onChange={(e) => setFrom(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          size="small"
          type="date"
          label="To"
          value={to}
          onChange={(e) => setTo(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
      </Box>

      {isLoading && <CircularProgress />}
      {isError && <Typography color="error">Failed to load Sales.</Typography>}

      {!isLoading &&
        !isError &&
        sales &&
        (sales.length === 0 ? (
          <Typography color="text.secondary">No Sales match the current filters.</Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Sale #</TableCell>
                <TableCell>Store</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Total</TableCell>
                <TableCell>Created</TableCell>
                <TableCell>Created By</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {sales.map((s) => (
                <TableRow
                  key={s.id}
                  hover
                  onClick={() => navigate(`/sales/${s.id}`)}
                  sx={{ cursor: "pointer" }}
                >
                  <TableCell>{s.saleNumber}</TableCell>
                  <TableCell>{s.storeName}</TableCell>
                  <TableCell>
                    <Chip label={s.status} size="small" color={saleStatusColor(s.status)} />
                  </TableCell>
                  <TableCell align="right">${s.total.toFixed(2)}</TableCell>
                  <TableCell>{new Date(s.createdAt).toLocaleString()}</TableCell>
                  <TableCell>{s.createdBy}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ))}
    </Box>
  );
}
