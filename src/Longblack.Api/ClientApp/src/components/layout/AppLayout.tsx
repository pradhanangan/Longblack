import { Box, GlobalStyles, Typography } from "@mui/material";
import { Outlet } from "react-router-dom";
import { Sidebar, DRAWER_WIDTH } from "./Sidebar";

export function AppLayout() {
  return (
    <Box sx={{ display: "flex" }}>
      <GlobalStyles styles={{ body: { margin: 0 } }} />
      <Sidebar />
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          minWidth: 0,
          width: `calc(100% - ${DRAWER_WIDTH}px)`,
          minHeight: "100vh",
          display: "flex",
          flexDirection: "column",
        }}
      >
        <Box sx={{ flexGrow: 1, px: 3, pb: 3 }}>
          <Outlet />
        </Box>
        <Box
          component="footer"
          sx={{
            px: 3,
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
