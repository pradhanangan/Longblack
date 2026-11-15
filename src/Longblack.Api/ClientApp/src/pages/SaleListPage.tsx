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
import type { SaleDto, StoreDto } from "../api/types";
import { SaleFormDialog } from "../components/sales/SaleFormDialog";
import { FilterPanel } from "../components/common/FilterPanel";
import { GridPagination } from "../components/common/GridPagination";
import { usePagedList } from "../components/common/usePagedList";
import { saleStatusColor } from "../components/sales/saleStatus";

function useSales(status: string, storeId: string, from: string, to: string) {
  const params = new URLSearchParams();
  if (status) params.set("status", status);
  if (storeId) params.set("storeId", storeId);
  if (from) params.set("from", new Date(`${from}T00:00:00`).toISOString());
  if (to) params.set("to", new Date(`${to}T23:59:59.999`).toISOString());
  return usePagedList<SaleDto>("sales", params);
}

export function SaleListPage() {
  const navigate = useNavigate();

  const [status, setStatus] = useState("");
  const [storeId, setStoreId] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [createOpen, setCreateOpen] = useState(false);

  const {
    items: sales,
    pagination,
    isLoading,
    isError,
  } = useSales(status, storeId, from, to);
  const { data: stores } = useQuery<StoreDto[]>({
    queryKey: ["stores", "Active"],
    queryFn: () => api.get("/api/stores?status=Active"),
    staleTime: 5 * 60_000,
  });

  return (
    <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1 }}>
      <Toolbar
        disableGutters
        sx={{ justifyContent: "space-between", gap: 1, mb: 3 }}
      >
        <Typography variant="h5">Sales</Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => setCreateOpen(true)}
        >
          New Sale
        </Button>
      </Toolbar>

      <SaleFormDialog open={createOpen} onClose={() => setCreateOpen(false)} />

      <FilterPanel
        fields={[
          {
            key: "storeId",
            label: "Store",
            kind: "select",
            allLabel: "All",
            options: stores?.map((s) => ({ value: s.id, label: s.name })),
          },
          {
            key: "status",
            label: "Status",
            kind: "select",
            allLabel: "All",
            options: ["Draft", "Completed", "Voided", "Cancelled"].map((s) => ({
              value: s,
              label: s,
            })),
          },
          { key: "from", label: "From", kind: "date" },
          { key: "to", label: "To", kind: "date" },
        ]}
        values={{ storeId, status, from, to }}
        onApply={(v) => {
          setStoreId(v.storeId);
          setStatus(v.status);
          setFrom(v.from);
          setTo(v.to);
        }}
      />

      {(isLoading || isError || sales?.length === 0) && (
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
            <Typography color="error">Failed to load Sales.</Typography>
          ) : (
            <Typography color="text.secondary">
              No Sales match the current filters.
            </Typography>
          )}
        </Box>
      )}

      {!isLoading && !isError && sales && sales.length > 0 && (
        <>
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
                    <Chip
                      label={s.status}
                      size="small"
                      color={saleStatusColor(s.status)}
                    />
                  </TableCell>
                  <TableCell align="right">${s.total.toFixed(2)}</TableCell>
                  <TableCell>
                    {new Date(s.createdAt).toLocaleString()}
                  </TableCell>
                  <TableCell>{s.createdBy}</TableCell>
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
