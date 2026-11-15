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
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import type { StoreDto } from "../api/types";
import { StoreFormDialog } from "../components/stores/StoreFormDialog";
import { FilterPanel } from "../components/common/FilterPanel";
import { GridPagination } from "../components/common/GridPagination";
import { usePagedList } from "../components/common/usePagedList";
import { useAuth } from "../contexts/AuthContext";

function useStores(status: string) {
  const params = new URLSearchParams();
  if (status) params.set("status", status);
  return usePagedList<StoreDto>("stores", params);
}

export function StoreListPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const isAdmin = user?.roles.some((r) => r === "Admin") ?? false;

  const [status, setStatus] = useState("");
  const [createOpen, setCreateOpen] = useState(false);

  const { items: stores, pagination, isLoading, isError } = useStores(status);

  return (
    <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1 }}>
      <Toolbar
        disableGutters
        sx={{ justifyContent: "space-between", gap: 1, mb: 3 }}
      >
        <Typography variant="h5">Stores</Typography>
        {isAdmin && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setCreateOpen(true)}
          >
            New Store
          </Button>
        )}
      </Toolbar>

      <StoreFormDialog open={createOpen} onClose={() => setCreateOpen(false)} />

      <FilterPanel
        fields={[
          {
            key: "status",
            label: "Status",
            kind: "select",
            allLabel: "All",
            options: [
              { value: "Active", label: "Active" },
              { value: "Closed", label: "Closed" },
            ],
          },
        ]}
        values={{ status }}
        onApply={(v) => setStatus(v.status)}
      />

      {(isLoading || isError || stores?.length === 0) && (
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
            <Typography color="error">Failed to load stores.</Typography>
          ) : (
            <Typography color="text.secondary">
              No stores match the current filters.
            </Typography>
          )}
        </Box>
      )}

      {!isLoading && !isError && stores && stores.length > 0 && (
        <>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {stores.map((s) => (
                <TableRow
                  key={s.id}
                  hover
                  onClick={() => navigate(`/stores/${s.id}`)}
                  sx={{ cursor: "pointer" }}
                >
                  <TableCell>{s.name}</TableCell>
                  <TableCell>{s.type}</TableCell>
                  <TableCell>
                    <Chip
                      label={s.status}
                      size="small"
                      color={s.status === "Active" ? "success" : "default"}
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
