import AddIcon from "@mui/icons-material/Add";
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
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
import type { StoreDto } from "../api/types";
import { StoreFormDialog } from "../components/stores/StoreFormDialog";
import { useAuth } from "../contexts/AuthContext";

function useStores(status: string) {
  const params = new URLSearchParams();
  if (status) params.set("status", status);
  const qs = params.toString();
  return useQuery<StoreDto[]>({
    queryKey: ["stores", status],
    queryFn: () => api.get(`/api/stores${qs ? `?${qs}` : ""}`),
  });
}

export function StoreListPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const isAdmin = user?.roles.some((r) => r === "Admin") ?? false;

  const [status, setStatus] = useState("");
  const [createOpen, setCreateOpen] = useState(false);

  const { data: stores, isLoading, isError } = useStores(status);

  return (
    <Box>
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
            <MenuItem value="Closed">Closed</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {isLoading && <CircularProgress />}
      {isError && (
        <Typography color="error">Failed to load stores.</Typography>
      )}

      {!isLoading &&
        !isError &&
        stores &&
        (stores.length === 0 ? (
          <Typography color="text.secondary">
            No stores match the current filters.
          </Typography>
        ) : (
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
        ))}
    </Box>
  );
}
