import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Toolbar,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../api/client";
import type { SaleDto } from "../api/types";
import { ConfirmDialog } from "../components/products/ConfirmDialog";
import { AddSaleLineRow } from "../components/sales/AddSaleLineRow";
import { CompleteSaleDialog } from "../components/sales/CompleteSaleDialog";
import { SaleLineRow } from "../components/sales/SaleLineRow";
import { useAuth } from "../contexts/AuthContext";
import { useSnackbar } from "../contexts/SnackbarContext";
import { saleStatusColor } from "../components/sales/saleStatus";

export function SaleDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const { showSuccess, showError } = useSnackbar();
  const queryClient = useQueryClient();

  const canWrite =
    user?.roles.some((r) => r === "Staff" || r === "Manager" || r === "Admin") ?? false;
  const canVoid = user?.roles.some((r) => r === "Manager" || r === "Admin") ?? false;

  const [cancelConfirmOpen, setCancelConfirmOpen] = useState(false);
  const [voidConfirmOpen, setVoidConfirmOpen] = useState(false);
  const [completeOpen, setCompleteOpen] = useState(false);

  const {
    data: sale,
    isLoading,
    isError,
  } = useQuery<SaleDto>({
    queryKey: ["sales", id],
    queryFn: () => api.get(`/api/sales/${id}`),
  });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["sales"] });
    queryClient.invalidateQueries({ queryKey: ["dashboard"] });
  };

  const cancelMutation = useMutation({
    mutationFn: () => api.post<SaleDto>(`/api/sales/${id}/cancel`, {}),
    onSuccess: () => {
      invalidate();
      showSuccess("Sale cancelled.");
    },
    onError: (err: unknown) => showError((err as Error).message ?? "Failed to cancel."),
  });

  const voidMutation = useMutation({
    mutationFn: () => api.post<SaleDto>(`/api/sales/${id}/void`, {}),
    onSuccess: () => {
      invalidate();
      showSuccess("Sale voided. Store Inventory has been restored.");
    },
    onError: (err: unknown) => showError((err as Error).message ?? "Failed to void."),
  });

  if (isLoading) return <CircularProgress />;
  if (isError || !sale) return <Typography color="error">Sale not found.</Typography>;

  const isDraft = sale.status === "Draft";
  const isCompleted = sale.status === "Completed";
  const canEditLines = isDraft && canWrite;

  return (
    <Box>
      <Toolbar disableGutters sx={{ gap: 1, mb: 3 }}>
        <IconButton component={Link} to="/sales" size="small">
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h5">{sale.saleNumber}</Typography>
        <Chip
          label={sale.status}
          size="small"
          color={saleStatusColor(sale.status)}
          sx={{ ml: 1 }}
        />
      </Toolbar>

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: "160px 1fr",
          gap: 1,
          mb: 3,
          maxWidth: 600,
        }}
      >
        <Typography color="text.secondary">Store</Typography>
        <Typography>{sale.storeName}</Typography>
        <Typography color="text.secondary">Total (GST incl.)</Typography>
        <Typography>${sale.total.toFixed(2)}</Typography>
        <Typography color="text.secondary">Payment</Typography>
        <Typography>
          {sale.paymentType
            ? `${sale.paymentType} — $${(sale.paymentAmount ?? 0).toFixed(2)}`
            : "—"}
        </Typography>
        <Typography color="text.secondary">Completed</Typography>
        <Typography>
          {sale.completedDate
            ? `${new Date(sale.completedDate).toLocaleString()} by ${sale.completedBy}`
            : "—"}
        </Typography>
        <Typography color="text.secondary">Voided</Typography>
        <Typography>
          {sale.voidedDate
            ? `${new Date(sale.voidedDate).toLocaleString()} by ${sale.voidedBy}`
            : "—"}
        </Typography>
      </Box>

      <Box sx={{ display: "flex", gap: 1, mb: 3, flexWrap: "wrap" }}>
        {isDraft && canWrite && (
          <Button
            variant="outlined"
            color="error"
            onClick={() => setCancelConfirmOpen(true)}
            disabled={cancelMutation.isPending}
          >
            Cancel Sale
          </Button>
        )}
        {isDraft && canWrite && (
          <Button
            variant="contained"
            color="success"
            onClick={() => setCompleteOpen(true)}
            disabled={sale.lines.length === 0}
          >
            Complete
          </Button>
        )}
        {isCompleted && canVoid && (
          <Button
            variant="outlined"
            color="warning"
            onClick={() => setVoidConfirmOpen(true)}
            disabled={voidMutation.isPending}
          >
            Void Sale
          </Button>
        )}
      </Box>

      <ConfirmDialog
        open={cancelConfirmOpen}
        onClose={() => setCancelConfirmOpen(false)}
        title="Cancel Sale"
        message={`Are you sure you want to cancel "${sale.saleNumber}"? This cannot be undone.`}
        confirmLabel="Cancel Sale"
        onConfirm={() => cancelMutation.mutate()}
      />
      <ConfirmDialog
        open={voidConfirmOpen}
        onClose={() => setVoidConfirmOpen(false)}
        title="Void Sale"
        message={`This will void "${sale.saleNumber}" and return every line's stock to ${sale.storeName}. This cannot be undone. Continue?`}
        confirmLabel="Void Sale"
        onConfirm={() => voidMutation.mutate()}
      />
      <CompleteSaleDialog
        open={completeOpen}
        onClose={() => setCompleteOpen(false)}
        sale={sale}
      />

      <Divider sx={{ my: 3 }} />

      <Typography variant="h6" sx={{ mb: 2 }}>
        Lines
      </Typography>

      {sale.lines.length === 0 && !canEditLines && (
        <Typography color="text.secondary">No lines on this Sale.</Typography>
      )}

      {(sale.lines.length > 0 || canEditLines) && (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>SKU</TableCell>
              <TableCell>Barcode</TableCell>
              <TableCell>Product</TableCell>
              <TableCell align="right">Qty</TableCell>
              <TableCell align="right">Unit Price</TableCell>
              <TableCell align="right">Discount</TableCell>
              <TableCell align="right">Line Total</TableCell>
              {canEditLines && <TableCell align="right">Actions</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {sale.lines.map((line) => (
              <SaleLineRow key={line.id} saleId={sale.id} line={line} canEdit={canEditLines} />
            ))}
            {canEditLines && <AddSaleLineRow saleId={sale.id} storeId={sale.storeId} />}
          </TableBody>
        </Table>
      )}
    </Box>
  );
}
