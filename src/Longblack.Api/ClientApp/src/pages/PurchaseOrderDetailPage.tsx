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
import { Link, useNavigate, useParams } from "react-router-dom";
import { api } from "../api/client";
import type { GoodsReceiptDto, PurchaseOrderDto } from "../api/types";
import { ConfirmDialog } from "../components/products/ConfirmDialog";
import { AddPurchaseOrderLineRow } from "../components/purchasing/AddPurchaseOrderLineRow";
import { PurchaseOrderFormDialog } from "../components/purchasing/PurchaseOrderFormDialog";
import { PurchaseOrderLineRow } from "../components/purchasing/PurchaseOrderLineRow";
import {
  purchaseOrderStatusColor,
  purchaseOrderStatusLabel,
} from "../components/purchasing/purchaseOrderStatus";
import { SendPurchaseOrderDialog } from "../components/purchasing/SendPurchaseOrderDialog";
import { useAuth } from "../contexts/AuthContext";
import { useSnackbar } from "../contexts/SnackbarContext";

type ConfirmAction = "issue" | "cancel" | "close" | null;

export function PurchaseOrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { showSuccess, showError } = useSnackbar();
  const queryClient = useQueryClient();

  const roles = user?.roles ?? [];
  const isAdmin = roles.includes("Admin");
  const canManage = isAdmin || roles.includes("Manager");
  const canReceive = canManage || roles.includes("Staff");

  const [editOpen, setEditOpen] = useState(false);
  const [sendOpen, setSendOpen] = useState(false);
  const [confirm, setConfirm] = useState<ConfirmAction>(null);

  const {
    data: order,
    isLoading,
    isError,
  } = useQuery<PurchaseOrderDto>({
    queryKey: ["purchase-orders", id],
    queryFn: () => api.get(`/api/purchase-orders/${id}`),
  });

  function useAction(path: string, successMessage: string) {
    return useMutation({
      mutationFn: () =>
        api.post<PurchaseOrderDto>(`/api/purchase-orders/${id}/${path}`, {}),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ["purchase-orders"] });
        showSuccess(successMessage);
      },
      onError: (err: unknown) =>
        showError((err as Error).message ?? "Action failed."),
    });
  }

  const issueMutation = useAction("issue", "Purchase order issued.");
  const cancelMutation = useAction("cancel", "Purchase order cancelled.");
  const closeMutation = useAction("close", "Purchase order closed.");
  const markSentMutation = useAction("mark-sent", "Marked as sent.");

  const receiveMutation = useMutation({
    mutationFn: () =>
      api.post<GoodsReceiptDto>(
        `/api/goods-receipts/from-purchase-order/${id}`,
        {},
      ),
    onSuccess: (receipt) => {
      queryClient.invalidateQueries({ queryKey: ["goods-receipts"] });
      navigate(`/goods-receipts/${receipt.id}`);
    },
    onError: (err: unknown) =>
      showError((err as Error).message ?? "Failed to create goods receipt."),
  });

  async function downloadPdf() {
    try {
      const { blob, fileName } = await api.getBlob(
        `/api/purchase-orders/${id}/pdf`,
      );
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileName;
      link.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      showError((err as Error).message ?? "Failed to download PDF.");
    }
  }

  if (isLoading) return <CircularProgress />;
  if (isError || !order)
    return <Typography color="error">Purchase order not found.</Typography>;

  const isDraft = order.status === "Draft";
  const canReceiveAgainst =
    order.status === "Issued" || order.status === "PartiallyReceived";
  const canEditLines = isDraft && canManage;
  const anyReceived = order.lines.some((l) => l.receivedQuantity > 0);
  const hasBeenIssued = !isDraft && order.status !== "Cancelled";
  const title = order.poNumber ?? "Draft Purchase Order";

  const confirmConfig = {
    issue: {
      title: "Issue Purchase Order",
      message:
        "Issuing assigns the PO number and locks the lines. To change it afterwards you must cancel and recreate it. Continue?",
      label: "Issue",
      run: () => issueMutation.mutate(),
    },
    cancel: {
      title: "Cancel Purchase Order",
      message: `Are you sure you want to cancel "${title}"? This cannot be undone.`,
      label: "Cancel Purchase Order",
      run: () => cancelMutation.mutate(),
    },
    close: {
      title: "Close Purchase Order",
      message:
        "Closing stops any further receiving against this purchase order, for example when the supplier short-ships. Continue?",
      label: "Close",
      run: () => closeMutation.mutate(),
    },
  } as const;

  return (
    <Box>
      <Toolbar disableGutters sx={{ gap: 1, mb: 3 }}>
        <IconButton component={Link} to="/purchase-orders" size="small">
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h5">{title}</Typography>
        <Chip
          label={purchaseOrderStatusLabel(order.status)}
          size="small"
          color={purchaseOrderStatusColor(order.status)}
          sx={{ ml: 1 }}
        />
        {order.isOverdue && (
          <Chip label="Overdue" size="small" color="error" variant="outlined" />
        )}
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
        <Typography color="text.secondary">Supplier</Typography>
        <Typography>{order.supplierName}</Typography>
        <Typography color="text.secondary">Expected Delivery</Typography>
        <Typography>
          {order.expectedDeliveryDate
            ? new Date(order.expectedDeliveryDate).toLocaleDateString()
            : "—"}
        </Typography>
        <Typography color="text.secondary">Notes</Typography>
        <Typography sx={{ whiteSpace: "pre-wrap" }}>
          {order.notes ?? "—"}
        </Typography>
        <Typography color="text.secondary">Issued</Typography>
        <Typography>
          {order.issuedAt
            ? `${new Date(order.issuedAt).toLocaleString()} by ${order.issuedBy}`
            : "—"}
        </Typography>
        <Typography color="text.secondary">Sent</Typography>
        <Typography>
          {order.sentAt
            ? `${new Date(order.sentAt).toLocaleString()} by ${order.sentBy}`
            : "Not sent"}
        </Typography>
        <Typography color="text.secondary">Total</Typography>
        <Typography>${order.total.toFixed(2)}</Typography>
      </Box>

      <Box sx={{ display: "flex", gap: 1, mb: 3, flexWrap: "wrap" }}>
        {isDraft && canManage && (
          <>
            <Button variant="outlined" onClick={() => setEditOpen(true)}>
              Edit Header
            </Button>
            <Button
              variant="contained"
              onClick={() => setConfirm("issue")}
              disabled={issueMutation.isPending || order.lines.length === 0}
            >
              Issue
            </Button>
            <Button
              variant="outlined"
              color="error"
              onClick={() => setConfirm("cancel")}
            >
              Cancel
            </Button>
          </>
        )}
        {hasBeenIssued && (
          <Button variant="outlined" onClick={downloadPdf}>
            Download PDF
          </Button>
        )}
        {hasBeenIssued && canManage && (
          <>
            <Button variant="outlined" onClick={() => setSendOpen(true)}>
              {order.sends.length > 0 ? "Resend Email" : "Email Supplier"}
            </Button>
            <Button
              variant="outlined"
              onClick={() => markSentMutation.mutate()}
              disabled={markSentMutation.isPending}
            >
              Mark as Sent
            </Button>
          </>
        )}
        {canReceiveAgainst && canReceive && (
          <Button
            variant="contained"
            color="success"
            onClick={() => receiveMutation.mutate()}
            disabled={receiveMutation.isPending}
          >
            Receive Goods
          </Button>
        )}
        {canReceiveAgainst && canManage && (
          <Button variant="outlined" onClick={() => setConfirm("close")}>
            Close
          </Button>
        )}
        {order.status === "Issued" && isAdmin && !anyReceived && (
          <Button
            variant="outlined"
            color="error"
            onClick={() => setConfirm("cancel")}
          >
            Cancel
          </Button>
        )}
      </Box>

      <PurchaseOrderFormDialog
        open={editOpen}
        onClose={() => setEditOpen(false)}
        purchaseOrder={order}
      />
      <SendPurchaseOrderDialog
        open={sendOpen}
        onClose={() => setSendOpen(false)}
        purchaseOrder={order}
      />
      {confirm && (
        <ConfirmDialog
          open
          onClose={() => setConfirm(null)}
          title={confirmConfig[confirm].title}
          message={confirmConfig[confirm].message}
          confirmLabel={confirmConfig[confirm].label}
          onConfirm={confirmConfig[confirm].run}
        />
      )}

      <Divider sx={{ my: 3 }} />

      <Typography variant="h6" sx={{ mb: 2 }}>
        Lines
      </Typography>

      {order.lines.length === 0 && !canEditLines && (
        <Typography color="text.secondary">
          No lines on this purchase order.
        </Typography>
      )}

      {(order.lines.length > 0 || canEditLines) && (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>SKU</TableCell>
              <TableCell>Barcode</TableCell>
              <TableCell align="right">Ordered</TableCell>
              <TableCell align="right">Expected Cost</TableCell>
              <TableCell align="right">Line Total</TableCell>
              <TableCell align="right">Received / Outstanding</TableCell>
              {canEditLines && <TableCell align="right">Actions</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {order.lines.map((line) => (
              <PurchaseOrderLineRow
                key={line.id}
                purchaseOrderId={order.id}
                line={line}
                canEdit={canEditLines}
              />
            ))}
            {canEditLines && <AddPurchaseOrderLineRow purchaseOrderId={order.id} />}
          </TableBody>
        </Table>
      )}

      {order.goodsReceipts.length > 0 && (
        <>
          <Divider sx={{ my: 3 }} />
          <Typography variant="h6" sx={{ mb: 2 }}>
            Goods Receipts
          </Typography>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Receipt #</TableCell>
                <TableCell>Received Date</TableCell>
                <TableCell>Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {order.goodsReceipts.map((g) => (
                <TableRow
                  key={g.id}
                  hover
                  onClick={() => navigate(`/goods-receipts/${g.id}`)}
                  sx={{ cursor: "pointer" }}
                >
                  <TableCell>{g.receiptNumber}</TableCell>
                  <TableCell>
                    {new Date(g.receivedDate).toLocaleDateString()}
                  </TableCell>
                  <TableCell>{g.status}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </>
      )}

      {order.sends.length > 0 && (
        <>
          <Divider sx={{ my: 3 }} />
          <Typography variant="h6" sx={{ mb: 2 }}>
            Send History
          </Typography>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>When</TableCell>
                <TableCell>By</TableCell>
                <TableCell>To</TableCell>
                <TableCell>CC</TableCell>
                <TableCell>Result</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {order.sends.map((s) => (
                <TableRow key={s.id}>
                  <TableCell>{new Date(s.sentAt).toLocaleString()}</TableCell>
                  <TableCell>{s.sentBy}</TableCell>
                  <TableCell>{s.recipient}</TableCell>
                  <TableCell>{s.cc ?? "—"}</TableCell>
                  <TableCell>
                    {s.succeeded ? (
                      <Chip label="Sent" size="small" color="success" />
                    ) : (
                      <Chip
                        label={s.error ?? "Failed"}
                        size="small"
                        color="error"
                      />
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </>
      )}
    </Box>
  );
}
