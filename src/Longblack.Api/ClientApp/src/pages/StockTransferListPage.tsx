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
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api/client";
import type { StockTransferDto, StoreDto } from "../api/types";
import { StockTransferFormDialog } from "../components/stockTransfers/StockTransferFormDialog";
import { useAuth } from "../contexts/AuthContext";

function statusColor(status: string): "default" | "success" | "error" | "warning" {
  if (status === "Received") return "success";
  if (status === "Cancelled") return "error";
  if (status === "Dispatched") return "warning";
  return "default";
}

function useStockTransfers(status: string, sourceStoreId: string, destinationStoreId: string) {
  const params = new URLSearchParams();
  if (status) params.set("status", status);
  if (sourceStoreId) params.set("sourceStoreId", sourceStoreId);
  if (destinationStoreId) params.set("destinationStoreId", destinationStoreId);
  const qs = params.toString();
  return useQuery<StockTransferDto[]>({
    queryKey: ["stock-transfers", status, sourceStoreId, destinationStoreId],
    queryFn: () => api.get(`/api/stock-transfers${qs ? `?${qs}` : ""}`),
  });
}

function useStores() {
  return useQuery<StoreDto[]>({
    queryKey: ["stores", "Active"],
    queryFn: () => api.get("/api/stores?status=Active"),
    staleTime: 5 * 60_000,
  });
}

export function StockTransferListPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const canCreate =
    user?.roles.some((r) => r === "Staff" || r === "Manager" || r === "Admin") ?? false;

  const [status, setStatus] = useState("");
  const [sourceStoreId, setSourceStoreId] = useState("");
  const [destinationStoreId, setDestinationStoreId] = useState("");
  const [createOpen, setCreateOpen] = useState(false);

  const {
    data: transfers,
    isLoading,
    isError,
  } = useStockTransfers(status, sourceStoreId, destinationStoreId);
  const { data: stores } = useStores();

  return (
    <Box>
      <Toolbar
        disableGutters
        sx={{ justifyContent: "space-between", gap: 1, mb: 3 }}
      >
        <Typography variant="h5">Stock Transfers</Typography>
        {canCreate && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setCreateOpen(true)}
          >
            New Transfer
          </Button>
        )}
      </Toolbar>

      <StockTransferFormDialog open={createOpen} onClose={() => setCreateOpen(false)} />

      <Box sx={{ display: "flex", gap: 2, mb: 3, flexWrap: "wrap" }}>
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Source Store</InputLabel>
          <Select
            value={sourceStoreId}
            label="Source Store"
            onChange={(e) => setSourceStoreId(e.target.value)}
          >
            <MenuItem value="">Any</MenuItem>
            {stores?.map((s) => (
              <MenuItem key={s.id} value={s.id}>
                {s.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Destination Store</InputLabel>
          <Select
            value={destinationStoreId}
            label="Destination Store"
            onChange={(e) => setDestinationStoreId(e.target.value)}
          >
            <MenuItem value="">Any</MenuItem>
            {stores?.map((s) => (
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
            <MenuItem value="Dispatched">Dispatched</MenuItem>
            <MenuItem value="Received">Received</MenuItem>
            <MenuItem value="Cancelled">Cancelled</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {isLoading && <CircularProgress />}
      {isError && (
        <Typography color="error">Failed to load Stock Transfers.</Typography>
      )}

      {!isLoading &&
        !isError &&
        transfers &&
        (transfers.length === 0 ? (
          <Typography color="text.secondary">
            No Stock Transfers match the current filters.
          </Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Transfer #</TableCell>
                <TableCell>Source</TableCell>
                <TableCell>Destination</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Dispatched Date</TableCell>
                <TableCell>Received Date</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {transfers.map((t) => (
                <TableRow
                  key={t.id}
                  hover
                  onClick={() => navigate(`/stock-transfers/${t.id}`)}
                  sx={{ cursor: "pointer" }}
                >
                  <TableCell>{t.transferNumber}</TableCell>
                  <TableCell>{t.sourceStoreName}</TableCell>
                  <TableCell>{t.destinationStoreName}</TableCell>
                  <TableCell>
                    <Chip
                      label={t.status}
                      size="small"
                      color={statusColor(t.status)}
                    />
                  </TableCell>
                  <TableCell>
                    {t.dispatchedDate
                      ? new Date(t.dispatchedDate).toLocaleDateString()
                      : "—"}
                  </TableCell>
                  <TableCell>
                    {t.receivedDate
                      ? new Date(t.receivedDate).toLocaleDateString()
                      : "—"}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ))}
    </Box>
  );
}
