import AddIcon from "@mui/icons-material/Add";
import UploadFileIcon from "@mui/icons-material/UploadFile";
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
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { api } from "../api/client";
import type { GoodsReceiptDto, SupplierDto } from "../api/types";
import { GoodsReceiptFormDialog } from "../components/receiving/GoodsReceiptFormDialog";
import { GoodsReceiptUploadDialog } from "../components/receiving/GoodsReceiptUploadDialog";
import { FilterPanel } from "../components/common/FilterPanel";
import { GridPagination } from "../components/common/GridPagination";
import { usePagedList } from "../components/common/usePagedList";
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
  return usePagedList<GoodsReceiptDto>("goods-receipts", params);
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
  const [uploadOpen, setUploadOpen] = useState(false);

  const {
    items: receipts,
    pagination,
    isLoading,
    isError,
  } = useGoodsReceipts(status, supplierId);
  const { data: suppliers } = useSuppliers();

  return (
    <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1 }}>
      <Toolbar
        disableGutters
        sx={{ justifyContent: "space-between", gap: 1, mb: 3 }}
      >
        <Typography variant="h5">Goods Receipts</Typography>
        {canCreate && (
          <Box sx={{ display: "flex", gap: 1 }}>
            <Button
              variant="outlined"
              startIcon={<UploadFileIcon />}
              onClick={() => setUploadOpen(true)}
            >
              Upload CSV
            </Button>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => setCreateOpen(true)}
            >
              New Receipt
            </Button>
          </Box>
        )}
      </Toolbar>

      <GoodsReceiptFormDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
      />
      <GoodsReceiptUploadDialog
        open={uploadOpen}
        onClose={() => setUploadOpen(false)}
      />

      <FilterPanel
        fields={[
          {
            key: "supplierId",
            label: "Supplier",
            kind: "select",
            allLabel: "All suppliers",
            options: suppliers?.map((s) => ({ value: s.id, label: s.name })),
            minWidth: 180,
          },
          {
            key: "status",
            label: "Status",
            kind: "select",
            allLabel: "All",
            options: ["Draft", "Received", "Cancelled"].map((s) => ({
              value: s,
              label: s,
            })),
          },
        ]}
        values={{ supplierId, status }}
        onApply={(v) => {
          setSupplierId(v.supplierId);
          setStatus(v.status);
        }}
      />

      {(isLoading || isError || receipts?.length === 0) && (
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
              Failed to load goods receipts.
            </Typography>
          ) : (
            <Typography color="text.secondary">
              No goods receipts match the current filters.
            </Typography>
          )}
        </Box>
      )}

      {!isLoading && !isError && receipts && receipts.length > 0 && (
        <>
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
          <GridPagination {...pagination} />
        </>
      )}
    </Box>
  );
}
