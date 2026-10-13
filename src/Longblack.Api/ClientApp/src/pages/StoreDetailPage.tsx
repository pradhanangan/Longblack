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
import type { InventoryDto, StoreDto } from "../api/types";
import { ConfirmDialog } from "../components/products/ConfirmDialog";
import { StoreFormDialog } from "../components/stores/StoreFormDialog";
import { useAuth } from "../contexts/AuthContext";
import { useSnackbar } from "../contexts/SnackbarContext";

function useStore(id: string) {
  return useQuery<StoreDto>({
    queryKey: ["stores", id],
    queryFn: () => api.get(`/api/stores/${id}`),
  });
}

// "All" status so a closed Store's historical (zero) Inventory rows are still visible here.
function useStoreInventory(storeId: string) {
  return useQuery<InventoryDto[]>({
    queryKey: ["inventory", "store", storeId],
    queryFn: () => api.get(`/api/inventory?storeId=${storeId}&status=All`),
  });
}

export function StoreDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const { showSuccess, showError } = useSnackbar();
  const queryClient = useQueryClient();
  const isAdmin = user?.roles.some((r) => r === "Admin") ?? false;

  const [editOpen, setEditOpen] = useState(false);
  const [closeConfirmOpen, setCloseConfirmOpen] = useState(false);

  const { data: store, isLoading, isError } = useStore(id!);
  const {
    data: inventory,
    isLoading: inventoryLoading,
    isError: inventoryError,
  } = useStoreInventory(id!);

  const closeMutation = useMutation({
    mutationFn: () =>
      api.patch<StoreDto>(`/api/stores/${id}/status`, { status: "Closed" }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["stores"] });
      showSuccess("Store closed.");
    },
    onError: (err: unknown) =>
      showError((err as Error).message ?? "Failed to close store."),
  });

  if (isLoading) return <CircularProgress />;
  if (isError || !store)
    return <Typography color="error">Store not found.</Typography>;

  const isActive = store.status === "Active";
  const nonzeroItems = (inventory ?? []).filter((i) => i.quantity !== 0);

  return (
    <Box>
      <Toolbar disableGutters sx={{ gap: 1, mb: 3 }}>
        <IconButton component={Link} to="/stores" size="small">
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h5">{store.name}</Typography>
        <Chip
          label={store.status}
          size="small"
          color={isActive ? "success" : "default"}
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
        <Typography color="text.secondary">Type</Typography>
        <Typography>{store.type}</Typography>
      </Box>

      {isAdmin && (
        <Box sx={{ display: "flex", gap: 1, mb: 3 }}>
          <Button variant="outlined" onClick={() => setEditOpen(true)}>
            Edit
          </Button>
          {isActive && (
            <Button
              variant="outlined"
              color="error"
              onClick={() => setCloseConfirmOpen(true)}
              disabled={closeMutation.isPending}
            >
              Close Store
            </Button>
          )}
        </Box>
      )}

      <StoreFormDialog open={editOpen} onClose={() => setEditOpen(false)} store={store} />
      <ConfirmDialog
        open={closeConfirmOpen}
        onClose={() => setCloseConfirmOpen(false)}
        title="Close Store"
        message={
          nonzeroItems.length > 0
            ? `"${store.name}" still holds nonzero Inventory for ${nonzeroItems.length} variant(s) — closing will be rejected until it is zero everywhere.`
            : `Are you sure you want to close "${store.name}"? It will no longer be selectable for new Stock Transfers or Stock Takes.`
        }
        confirmLabel="Close Store"
        onConfirm={() => closeMutation.mutate()}
      />

      <Divider sx={{ my: 3 }} />

      <Typography variant="h6" sx={{ mb: 2 }}>
        Current Inventory
      </Typography>

      {inventoryLoading && <CircularProgress size={24} />}
      {inventoryError && (
        <Typography color="error">Failed to load inventory.</Typography>
      )}

      {!inventoryLoading &&
        !inventoryError &&
        inventory &&
        (inventory.length === 0 ? (
          <Typography color="text.secondary">
            No inventory recorded at this store.
          </Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>SKU</TableCell>
                <TableCell>Product</TableCell>
                <TableCell>Colour</TableCell>
                <TableCell>Size</TableCell>
                <TableCell align="right">Quantity</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {inventory.map((i) => (
                <TableRow key={i.productVariantId}>
                  <TableCell>
                    <Link
                      to={`/inventory/${i.productVariantId}?storeId=${id}`}
                      style={{ textDecoration: "none", color: "inherit" }}
                    >
                      {i.sku}
                    </Link>
                  </TableCell>
                  <TableCell>{i.productName}</TableCell>
                  <TableCell>{i.colourName ?? "—"}</TableCell>
                  <TableCell>{i.sizeName ?? "—"}</TableCell>
                  <TableCell align="right">{i.quantity}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ))}
    </Box>
  );
}
