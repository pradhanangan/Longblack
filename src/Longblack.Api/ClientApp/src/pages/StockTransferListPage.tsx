import AddIcon from "@mui/icons-material/Add";
import {
  Box,
  Button,
  Chip,
  CircularProgress,
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
import { FilterPanel } from "../components/common/FilterPanel";
import { GridPagination } from "../components/common/GridPagination";
import { usePagedList } from "../components/common/usePagedList";
import { useAuth } from "../contexts/AuthContext";

function statusColor(
  status: string,
): "default" | "success" | "error" | "warning" {
  if (status === "Received") return "success";
  if (status === "Cancelled") return "error";
  if (status === "Dispatched") return "warning";
  return "default";
}

function useStockTransfers(
  status: string,
  sourceStoreId: string,
  destinationStoreId: string,
) {
  const params = new URLSearchParams();
  if (status) params.set("status", status);
  if (sourceStoreId) params.set("sourceStoreId", sourceStoreId);
  if (destinationStoreId) params.set("destinationStoreId", destinationStoreId);
  return usePagedList<StockTransferDto>("stock-transfers", params);
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
    user?.roles.some(
      (r) => r === "Staff" || r === "Manager" || r === "Admin",
    ) ?? false;

  const [status, setStatus] = useState("");
  const [sourceStoreId, setSourceStoreId] = useState("");
  const [destinationStoreId, setDestinationStoreId] = useState("");
  const [createOpen, setCreateOpen] = useState(false);

  const {
    items: transfers,
    pagination,
    isLoading,
    isError,
  } = useStockTransfers(status, sourceStoreId, destinationStoreId);
  const { data: stores } = useStores();

  return (
    <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1 }}>
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

      <StockTransferFormDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
      />

      <FilterPanel
        fields={[
          {
            key: "sourceStoreId",
            label: "Source Store",
            kind: "select",
            allLabel: "Any",
            options: stores?.map((s) => ({ value: s.id, label: s.name })),
          },
          {
            key: "destinationStoreId",
            label: "Destination Store",
            kind: "select",
            allLabel: "Any",
            options: stores?.map((s) => ({ value: s.id, label: s.name })),
          },
          {
            key: "status",
            label: "Status",
            kind: "select",
            allLabel: "All",
            options: ["Draft", "Dispatched", "Received", "Cancelled"].map(
              (s) => ({ value: s, label: s }),
            ),
          },
        ]}
        values={{ sourceStoreId, destinationStoreId, status }}
        onApply={(v) => {
          setSourceStoreId(v.sourceStoreId);
          setDestinationStoreId(v.destinationStoreId);
          setStatus(v.status);
        }}
      />

      {(isLoading || isError || transfers?.length === 0) && (
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
              Failed to load Stock Transfers.
            </Typography>
          ) : (
            <Typography color="text.secondary">
              No Stock Transfers match the current filters.
            </Typography>
          )}
        </Box>
      )}

      {!isLoading && !isError && transfers && transfers.length > 0 && (
        <>
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
          <GridPagination {...pagination} />
        </>
      )}
    </Box>
  );
}
