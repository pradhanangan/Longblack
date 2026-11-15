import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  IconButton,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Toolbar,
  Typography,
} from "@mui/material";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { api } from "../api/client";
import type { SupplierDto } from "../api/types";
import { SupplierFormDialog } from "../components/suppliers/SupplierFormDialog";
import { FilterPanel } from "../components/common/FilterPanel";
import { GridPagination } from "../components/common/GridPagination";
import { usePagedList } from "../components/common/usePagedList";
import { useAuth } from "../contexts/AuthContext";
import { useSnackbar } from "../contexts/SnackbarContext";

function useSuppliers(status: string) {
  const params = new URLSearchParams();
  if (status) params.set("status", status);
  return usePagedList<SupplierDto>("suppliers", params);
}

export function SupplierListPage() {
  const { user } = useAuth();
  const { showSuccess, showError } = useSnackbar();
  const queryClient = useQueryClient();
  const isAdmin = user?.roles.some((r) => r === "Admin") ?? false;

  const [status, setStatus] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [editSupplier, setEditSupplier] = useState<SupplierDto | undefined>();

  const {
    items: suppliers,
    pagination,
    isLoading,
    isError,
  } = useSuppliers(status);

  const setStatusMutation = useMutation({
    mutationFn: (vars: { id: string; status: string }) =>
      api.patch<SupplierDto>(`/api/suppliers/${vars.id}/status`, {
        status: vars.status,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["suppliers"] });
      showSuccess("Supplier status updated.");
    },
    onError: (err: unknown) =>
      showError((err as Error).message ?? "Failed to update status."),
  });

  return (
    <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1 }}>
      <Toolbar
        disableGutters
        sx={{ justifyContent: "space-between", gap: 1, mb: 3 }}
      >
        <Typography variant="h5">Suppliers</Typography>
        {isAdmin && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setCreateOpen(true)}
          >
            New Supplier
          </Button>
        )}
      </Toolbar>

      <SupplierFormDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
      />
      <SupplierFormDialog
        open={!!editSupplier}
        onClose={() => setEditSupplier(undefined)}
        supplier={editSupplier}
      />

      <FilterPanel
        fields={[
          {
            key: "status",
            label: "Status",
            kind: "select",
            allLabel: "All",
            options: [
              { value: "Active", label: "Active" },
              { value: "Inactive", label: "Inactive" },
            ],
          },
        ]}
        values={{ status }}
        onApply={(v) => setStatus(v.status)}
      />

      {(isLoading || isError || suppliers?.length === 0) && (
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
            <Typography color="error">Failed to load suppliers.</Typography>
          ) : (
            <Typography color="text.secondary">
              No suppliers match the current filters.
            </Typography>
          )}
        </Box>
      )}

      {!isLoading && !isError && suppliers && suppliers.length > 0 && (
        <>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Code</TableCell>
                <TableCell>Name</TableCell>
                <TableCell>Email</TableCell>
                <TableCell>Phone</TableCell>
                <TableCell>Status</TableCell>
                {isAdmin && <TableCell align="right">Actions</TableCell>}
              </TableRow>
            </TableHead>
            <TableBody>
              {suppliers.map((s) => (
                <TableRow key={s.id} hover>
                  <TableCell>{s.code}</TableCell>
                  <TableCell>{s.name}</TableCell>
                  <TableCell>{s.email}</TableCell>
                  <TableCell>{s.phone}</TableCell>
                  <TableCell>
                    <Chip
                      label={s.status}
                      size="small"
                      color={s.status === "Active" ? "success" : "default"}
                    />
                  </TableCell>
                  {isAdmin && (
                    <TableCell align="right">
                      <IconButton
                        size="small"
                        onClick={() => setEditSupplier(s)}
                        aria-label="Edit supplier"
                      >
                        <EditIcon fontSize="small" />
                      </IconButton>
                      <Switch
                        size="small"
                        checked={s.status === "Active"}
                        disabled={setStatusMutation.isPending}
                        onChange={(e) =>
                          setStatusMutation.mutate({
                            id: s.id,
                            status: e.target.checked ? "Active" : "Inactive",
                          })
                        }
                        aria-label="Toggle supplier status"
                      />
                    </TableCell>
                  )}
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
