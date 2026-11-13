import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Switch,
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
import { api } from "../api/client";
import type { BrandDto } from "../api/types";
import { BrandFormDialog } from "../components/brands/BrandFormDialog";
import { useAuth } from "../contexts/AuthContext";
import { useSnackbar } from "../contexts/SnackbarContext";

export function BrandListPage() {
  const { user } = useAuth();
  const { showSuccess, showError } = useSnackbar();
  const queryClient = useQueryClient();
  const isAdmin = user?.roles.some((r) => r === "Admin") ?? false;

  const [status, setStatus] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [editBrand, setEditBrand] = useState<BrandDto | undefined>();

  const {
    data: allBrands,
    isLoading,
    isError,
  } = useQuery<BrandDto[]>({
    queryKey: ["brands", "all"],
    queryFn: () => api.get("/api/brands?status=All"),
  });

  const brands = allBrands?.filter((b) => !status || b.status === status);

  const setStatusMutation = useMutation({
    mutationFn: (vars: { id: string; status: string }) =>
      api.patch<BrandDto>(`/api/brands/${vars.id}/status`, {
        status: vars.status,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["brands"] });
      showSuccess("Brand status updated.");
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
        <Typography variant="h5">Brands</Typography>
        {isAdmin && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setCreateOpen(true)}
          >
            New Brand
          </Button>
        )}
      </Toolbar>

      <BrandFormDialog open={createOpen} onClose={() => setCreateOpen(false)} />
      <BrandFormDialog
        open={!!editBrand}
        onClose={() => setEditBrand(undefined)}
        brand={editBrand}
      />

      <Box sx={{ display: "flex", gap: 2, mb: 3, flexWrap: "wrap" }}>
        <FormControl size="small" sx={{ minWidth: 140 }}>
          <InputLabel>Status</InputLabel>
          <Select
            value={status}
            label="Status"
            onChange={(e) => setStatus(e.target.value)}
          >
            <MenuItem value="">All</MenuItem>
            <MenuItem value="Active">Active</MenuItem>
            <MenuItem value="Inactive">Inactive</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {(isLoading || isError || brands?.length === 0) && (
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
            <Typography color="error">Failed to load brands.</Typography>
          ) : (
            <Typography color="text.secondary">
              No brands match the current filters.
            </Typography>
          )}
        </Box>
      )}

      {!isLoading && !isError && brands && brands.length > 0 && (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Code</TableCell>
              <TableCell>Name</TableCell>
              <TableCell>Status</TableCell>
              {isAdmin && <TableCell align="right">Actions</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {brands.map((b) => (
              <TableRow key={b.id} hover>
                <TableCell>{b.code}</TableCell>
                <TableCell>{b.name}</TableCell>
                <TableCell>
                  <Chip
                    label={b.status}
                    size="small"
                    color={b.status === "Active" ? "success" : "default"}
                  />
                </TableCell>
                {isAdmin && (
                  <TableCell align="right">
                    <IconButton
                      size="small"
                      onClick={() => setEditBrand(b)}
                      aria-label="Edit brand"
                    >
                      <EditIcon fontSize="small" />
                    </IconButton>
                    <Switch
                      size="small"
                      checked={b.status === "Active"}
                      disabled={setStatusMutation.isPending}
                      onChange={(e) =>
                        setStatusMutation.mutate({
                          id: b.id,
                          status: e.target.checked ? "Active" : "Inactive",
                        })
                      }
                      aria-label="Toggle brand status"
                    />
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Box>
  );
}
