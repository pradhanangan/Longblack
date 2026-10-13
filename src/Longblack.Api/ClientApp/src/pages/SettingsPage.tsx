import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Select,
  Tab,
  Tabs,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Toolbar,
  Typography,
  Chip,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { api } from "../api/client";
import { useAuth } from "../contexts/AuthContext";
import { useSnackbar } from "../contexts/SnackbarContext";

const ROLES = ["Admin", "Manager", "Staff"];

interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  isActive: boolean;
}

function MyAccountTab() {
  const { user } = useAuth();
  const { showSuccess, showError } = useSnackbar();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");

  const change = useMutation({
    mutationFn: () =>
      api.post("/api/users/me/password", { currentPassword, newPassword }),
    onSuccess: () => {
      showSuccess("Password changed.");
      setCurrentPassword("");
      setNewPassword("");
    },
    onError: (e: Error) => showError(e.message),
  });

  return (
    <Box sx={{ maxWidth: 400, display: "flex", flexDirection: "column", gap: 2 }}>
      <Typography>
        {user?.name} ({user?.email}) · {user?.roles.join(", ")}
      </Typography>
      <TextField
        label="Current password"
        type="password"
        value={currentPassword}
        onChange={(e) => setCurrentPassword(e.target.value)}
      />
      <TextField
        label="New password"
        type="password"
        value={newPassword}
        onChange={(e) => setNewPassword(e.target.value)}
        helperText="At least 8 characters, with a digit, an uppercase letter and a symbol."
      />
      <Button
        variant="contained"
        disabled={!currentPassword || !newPassword || change.isPending}
        onClick={() => change.mutate()}
      >
        Change password
      </Button>
    </Box>
  );
}

function CreateUserDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const { showSuccess, showError } = useSnackbar();
  const [form, setForm] = useState({
    email: "",
    password: "",
    firstName: "",
    lastName: "",
    role: "Staff",
  });

  const create = useMutation({
    mutationFn: () => api.post("/api/users", form),
    onSuccess: () => {
      showSuccess("User created.");
      qc.invalidateQueries({ queryKey: ["users"] });
      setForm({ email: "", password: "", firstName: "", lastName: "", role: "Staff" });
      onClose();
    },
    onError: (e: Error) => showError(e.message),
  });

  const set = (k: keyof typeof form) => (e: { target: { value: string } }) =>
    setForm({ ...form, [k]: e.target.value });

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>New user</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 2 }}>
        <TextField label="First name" value={form.firstName} onChange={set("firstName")} sx={{ mt: 1 }} />
        <TextField label="Last name" value={form.lastName} onChange={set("lastName")} />
        <TextField label="Email" value={form.email} onChange={set("email")} />
        <TextField label="Password" type="password" value={form.password} onChange={set("password")} />
        <Select value={form.role} onChange={set("role")}>
          {ROLES.map((r) => (
            <MenuItem key={r} value={r}>
              {r}
            </MenuItem>
          ))}
        </Select>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          disabled={!form.email || !form.password || !form.firstName || create.isPending}
          onClick={() => create.mutate()}
        >
          Create
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function SetPasswordDialog({
  user,
  onClose,
}: {
  user: UserDto | null;
  onClose: () => void;
}) {
  const { showSuccess, showError } = useSnackbar();
  const [password, setPassword] = useState("");

  const save = useMutation({
    mutationFn: () =>
      api.put(`/api/users/${user!.id}/password`, { newPassword: password }),
    onSuccess: () => {
      showSuccess("Password updated.");
      setPassword("");
      onClose();
    },
    onError: (e: Error) => showError(e.message),
  });

  return (
    <Dialog open={!!user} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>Set password for {user?.email}</DialogTitle>
      <DialogContent>
        <TextField
          label="New password"
          type="password"
          fullWidth
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          sx={{ mt: 1 }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          disabled={!password || save.isPending}
          onClick={() => save.mutate()}
        >
          Save
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function UsersTab() {
  const qc = useQueryClient();
  const { user: me } = useAuth();
  const { showError } = useSnackbar();
  const [creating, setCreating] = useState(false);
  const [passwordUser, setPasswordUser] = useState<UserDto | null>(null);

  const { data: users, isLoading } = useQuery<UserDto[]>({
    queryKey: ["users"],
    queryFn: () => api.get("/api/users"),
  });

  const refresh = () => qc.invalidateQueries({ queryKey: ["users"] });

  const changeRole = useMutation({
    mutationFn: (v: { id: string; role: string }) =>
      api.put(`/api/users/${v.id}/role`, { role: v.role }),
    onSuccess: refresh,
    onError: (e: Error) => showError(e.message),
  });

  const toggleActive = useMutation({
    mutationFn: (u: UserDto) =>
      api.post(`/api/users/${u.id}/${u.isActive ? "deactivate" : "activate"}`, {}),
    onSuccess: refresh,
    onError: (e: Error) => showError(e.message),
  });

  return (
    <Box>
      <Toolbar disableGutters sx={{ mb: 1 }}>
        <Box sx={{ flexGrow: 1 }} />
        <Button variant="contained" onClick={() => setCreating(true)}>
          New user
        </Button>
      </Toolbar>
      {!isLoading && users && (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Email</TableCell>
              <TableCell>Role</TableCell>
              <TableCell>Status</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {users.map((u) => {
              const isMe = u.id === me?.id;
              return (
                <TableRow key={u.id}>
                  <TableCell>{`${u.firstName} ${u.lastName}`.trim()}</TableCell>
                  <TableCell>{u.email}</TableCell>
                  <TableCell>
                    <Select
                      size="small"
                      value={u.role}
                      disabled={isMe}
                      onChange={(e) =>
                        changeRole.mutate({ id: u.id, role: e.target.value })
                      }
                    >
                      {ROLES.map((r) => (
                        <MenuItem key={r} value={r}>
                          {r}
                        </MenuItem>
                      ))}
                    </Select>
                  </TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      label={u.isActive ? "Active" : "Deactivated"}
                      color={u.isActive ? "success" : "default"}
                    />
                  </TableCell>
                  <TableCell align="right">
                    <Button size="small" onClick={() => setPasswordUser(u)}>
                      Set password
                    </Button>
                    <Button
                      size="small"
                      disabled={isMe && u.isActive}
                      onClick={() => toggleActive.mutate(u)}
                    >
                      {u.isActive ? "Deactivate" : "Activate"}
                    </Button>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      )}
      <CreateUserDialog open={creating} onClose={() => setCreating(false)} />
      <SetPasswordDialog user={passwordUser} onClose={() => setPasswordUser(null)} />
    </Box>
  );
}

export function SettingsPage() {
  const { user } = useAuth();
  const isAdmin = user?.roles.includes("Admin") ?? false;
  const [tab, setTab] = useState(0);

  return (
    <Box>
      <Toolbar disableGutters sx={{ mb: 1 }}>
        <Typography variant="h5">Settings</Typography>
      </Toolbar>
      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 3 }}>
        <Tab label="My account" />
        {isAdmin && <Tab label="Users" />}
      </Tabs>
      {tab === 0 && <MyAccountTab />}
      {tab === 1 && isAdmin && <UsersTab />}
    </Box>
  );
}
