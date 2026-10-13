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
import type { StockTransferDto } from "../api/types";
import { ConfirmDialog } from "../components/products/ConfirmDialog";
import { AddStockTransferLineRow } from "../components/stockTransfers/AddStockTransferLineRow";
import { ReceiveStockTransferDialog } from "../components/stockTransfers/ReceiveStockTransferDialog";
import { StockTransferLineRow } from "../components/stockTransfers/StockTransferLineRow";
import { useAuth } from "../contexts/AuthContext";
import { useSnackbar } from "../contexts/SnackbarContext";

function statusColor(status: string): "default" | "success" | "error" | "warning" {
  if (status === "Received") return "success";
  if (status === "Cancelled") return "error";
  if (status === "Dispatched") return "warning";
  return "default";
}

function useStockTransfer(id: string) {
  return useQuery<StockTransferDto>({
    queryKey: ["stock-transfers", id],
    queryFn: () => api.get(`/api/stock-transfers/${id}`),
  });
}

export function StockTransferDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const { showSuccess, showError } = useSnackbar();
  const queryClient = useQueryClient();

  const canWrite =
    user?.roles.some((r) => r === "Staff" || r === "Manager" || r === "Admin") ?? false;
  const canDispatchOrReceive =
    user?.roles.some((r) => r === "Manager" || r === "Admin") ?? false;

  const [cancelConfirmOpen, setCancelConfirmOpen] = useState(false);
  const [dispatchConfirmOpen, setDispatchConfirmOpen] = useState(false);
  const [receiveOpen, setReceiveOpen] = useState(false);

  const { data: transfer, isLoading, isError } = useStockTransfer(id!);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["stock-transfers"] });

  const dispatchMutation = useMutation({
    mutationFn: () => api.post<StockTransferDto>(`/api/stock-transfers/${id}/dispatch`, {}),
    onSuccess: () => {
      invalidate();
      showSuccess("Stock Transfer dispatched. Source Inventory has been reduced.");
    },
    onError: (err: unknown) => showError((err as Error).message ?? "Failed to dispatch."),
  });

  const cancelMutation = useMutation({
    mutationFn: () => api.post<StockTransferDto>(`/api/stock-transfers/${id}/cancel`, {}),
    onSuccess: () => {
      invalidate();
      showSuccess("Stock Transfer cancelled.");
    },
    onError: (err: unknown) => showError((err as Error).message ?? "Failed to cancel."),
  });

  if (isLoading) return <CircularProgress />;
  if (isError || !transfer)
    return <Typography color="error">Stock Transfer not found.</Typography>;

  const isDraft = transfer.status === "Draft";
  const isDispatched = transfer.status === "Dispatched";
  const isReceived = transfer.status === "Received";
  const canEditLines = isDraft && canWrite;

  return (
    <Box>
      <Toolbar disableGutters sx={{ gap: 1, mb: 3 }}>
        <IconButton component={Link} to="/stock-transfers" size="small">
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h5">{transfer.transferNumber}</Typography>
        <Chip
          label={transfer.status}
          size="small"
          color={statusColor(transfer.status)}
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
        <Typography color="text.secondary">Source Store</Typography>
        <Typography>{transfer.sourceStoreName}</Typography>
        <Typography color="text.secondary">Destination Store</Typography>
        <Typography>{transfer.destinationStoreName}</Typography>
        <Typography color="text.secondary">Dispatched</Typography>
        <Typography>
          {transfer.dispatchedDate
            ? `${new Date(transfer.dispatchedDate).toLocaleString()} by ${transfer.dispatchedBy}`
            : "—"}
        </Typography>
        <Typography color="text.secondary">Received</Typography>
        <Typography>
          {transfer.receivedDate
            ? `${new Date(transfer.receivedDate).toLocaleString()} by ${transfer.receivedBy}`
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
            Cancel Transfer
          </Button>
        )}
        {isDraft && canDispatchOrReceive && (
          <Button
            variant="contained"
            onClick={() => setDispatchConfirmOpen(true)}
            disabled={dispatchMutation.isPending || transfer.lines.length === 0}
          >
            Dispatch
          </Button>
        )}
        {isDispatched && canDispatchOrReceive && (
          <Button
            variant="contained"
            color="success"
            onClick={() => setReceiveOpen(true)}
          >
            Receive
          </Button>
        )}
      </Box>

      <ConfirmDialog
        open={cancelConfirmOpen}
        onClose={() => setCancelConfirmOpen(false)}
        title="Cancel Stock Transfer"
        message={`Are you sure you want to cancel "${transfer.transferNumber}"? This cannot be undone.`}
        confirmLabel="Cancel Transfer"
        onConfirm={() => cancelMutation.mutate()}
      />
      <ConfirmDialog
        open={dispatchConfirmOpen}
        onClose={() => setDispatchConfirmOpen(false)}
        title="Dispatch Stock Transfer"
        message="This will reduce the source Store's Inventory for every line and lock the transfer from further line edits. Continue?"
        confirmLabel="Dispatch"
        onConfirm={() => dispatchMutation.mutate()}
      />
      <ReceiveStockTransferDialog
        open={receiveOpen}
        onClose={() => setReceiveOpen(false)}
        stockTransfer={transfer}
      />

      <Divider sx={{ my: 3 }} />

      <Typography variant="h6" sx={{ mb: 2 }}>
        Lines
      </Typography>

      {transfer.lines.length === 0 && !canEditLines && (
        <Typography color="text.secondary">No lines on this transfer.</Typography>
      )}

      {(transfer.lines.length > 0 || canEditLines) && (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>SKU</TableCell>
              <TableCell>Barcode</TableCell>
              <TableCell>Product</TableCell>
              <TableCell align="right">Dispatched Qty</TableCell>
              {(isDispatched || isReceived) && (
                <>
                  <TableCell align="right">Received Qty</TableCell>
                  <TableCell align="right">Variance</TableCell>
                </>
              )}
              {canEditLines && <TableCell align="right">Actions</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {transfer.lines.map((line) => (
              <StockTransferLineRow
                key={line.id}
                stockTransferId={transfer.id}
                line={line}
                canEdit={canEditLines}
                showReceivedColumns={isDispatched || isReceived}
              />
            ))}
            {canEditLines && <AddStockTransferLineRow stockTransferId={transfer.id} />}
          </TableBody>
        </Table>
      )}
    </Box>
  );
}
