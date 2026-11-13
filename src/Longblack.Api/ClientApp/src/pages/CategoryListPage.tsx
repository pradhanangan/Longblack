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
import type { CategoryDto } from "../api/types";
import { CategoryFormDialog } from "../components/categories/CategoryFormDialog";
import { orderCategoriesForDropdown } from "../components/categories/CategorySelectItems";
import { useAuth } from "../contexts/AuthContext";
import { useSnackbar } from "../contexts/SnackbarContext";

export function CategoryListPage() {
  const { user } = useAuth();
  const { showSuccess, showError } = useSnackbar();
  const queryClient = useQueryClient();
  const isAdmin = user?.roles.some((r) => r === "Admin") ?? false;

  const [status, setStatus] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [editCategory, setEditCategory] = useState<CategoryDto | undefined>();

  const {
    data: allCategories,
    isLoading,
    isError,
  } = useQuery<CategoryDto[]>({
    queryKey: ["categories", "all"],
    queryFn: () => api.get("/api/categories?status=All"),
  });

  const parentNames = new Map(allCategories?.map((c) => [c.id, c.name]));
  const categories = orderCategoriesForDropdown(allCategories)
    .map((o) => o.category)
    .filter((c) => !status || c.status === status);

  const setStatusMutation = useMutation({
    mutationFn: (vars: { id: string; status: string }) =>
      api.patch<CategoryDto>(`/api/categories/${vars.id}/status`, {
        status: vars.status,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["categories"] });
      showSuccess("Category status updated.");
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
        <Typography variant="h5">Categories</Typography>
        {isAdmin && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setCreateOpen(true)}
          >
            New Category
          </Button>
        )}
      </Toolbar>

      <CategoryFormDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        allCategories={allCategories ?? []}
      />
      <CategoryFormDialog
        open={!!editCategory}
        onClose={() => setEditCategory(undefined)}
        category={editCategory}
        allCategories={allCategories ?? []}
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

      {(isLoading || isError || categories.length === 0) && (
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
            <Typography color="error">Failed to load categories.</Typography>
          ) : (
            <Typography color="text.secondary">
              No categories match the current filters.
            </Typography>
          )}
        </Box>
      )}

      {!isLoading && !isError && categories.length > 0 && (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Code</TableCell>
              <TableCell>Name</TableCell>
              <TableCell>Parent</TableCell>
              <TableCell>Status</TableCell>
              {isAdmin && <TableCell align="right">Actions</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {categories.map((c) => (
              <TableRow key={c.id} hover>
                <TableCell>{c.code}</TableCell>
                <TableCell sx={{ pl: c.parentCategoryId ? 5 : 2 }}>
                  {c.name}
                </TableCell>
                <TableCell>
                  {c.parentCategoryId ? parentNames.get(c.parentCategoryId) : "—"}
                </TableCell>
                <TableCell>
                  <Chip
                    label={c.status}
                    size="small"
                    color={c.status === "Active" ? "success" : "default"}
                  />
                </TableCell>
                {isAdmin && (
                  <TableCell align="right">
                    <IconButton
                      size="small"
                      onClick={() => setEditCategory(c)}
                      aria-label="Edit category"
                    >
                      <EditIcon fontSize="small" />
                    </IconButton>
                    <Switch
                      size="small"
                      checked={c.status === "Active"}
                      disabled={setStatusMutation.isPending}
                      onChange={(e) =>
                        setStatusMutation.mutate({
                          id: c.id,
                          status: e.target.checked ? "Active" : "Inactive",
                        })
                      }
                      aria-label="Toggle category status"
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
