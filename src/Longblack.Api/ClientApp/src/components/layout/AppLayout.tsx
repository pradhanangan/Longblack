import MenuIcon from "@mui/icons-material/Menu";
import {
  AppBar,
  Box,
  GlobalStyles,
  IconButton,
  Toolbar,
  Typography,
} from "@mui/material";
import { useState } from "react";
import { Outlet } from "react-router-dom";
import { useStackedTableLabels } from "../common/useStackedTableLabels";
import { Sidebar, DRAWER_WIDTH } from "./Sidebar";

export function AppLayout() {
  const [mobileOpen, setMobileOpen] = useState(false);
  useStackedTableLabels();

  return (
    <Box sx={{ display: "flex" }}>
      <GlobalStyles styles={{ body: { margin: 0 } }} />
      <AppBar
        position="fixed"
        elevation={1}
        sx={{ display: { xs: "block", md: "none" } }}
      >
        <Toolbar>
          <IconButton
            color="inherit"
            edge="start"
            aria-label="Open navigation"
            onClick={() => setMobileOpen(true)}
            sx={{ mr: 1 }}
          >
            <MenuIcon />
          </IconButton>
          <Typography variant="h6" noWrap sx={{ fontWeight: "bold" }}>
            Longblack
          </Typography>
        </Toolbar>
      </AppBar>
      <Sidebar
        mobileOpen={mobileOpen}
        onMobileClose={() => setMobileOpen(false)}
      />
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          minWidth: 0,
          width: { xs: "100%", md: `calc(100% - ${DRAWER_WIDTH}px)` },
          minHeight: "100vh",
          display: "flex",
          flexDirection: "column",
        }}
      >
        <Toolbar sx={{ display: { xs: "block", md: "none" } }} />
        <Box
          sx={{
            flexGrow: 1,
            display: "flex",
            flexDirection: "column",
            px: { xs: 1.5, sm: 3 },
            pb: 3,
          }}
        >
          <Outlet />
        </Box>
        <Box
          component="footer"
          sx={{
            px: { xs: 1.5, sm: 3 },
            py: 1,
            bgcolor: "background.paper",
            textAlign: "right",
          }}
        >
          <Typography variant="body2" color="text.secondary">
            © 2026 NPradhananga
          </Typography>
        </Box>
      </Box>
    </Box>
  );
}
