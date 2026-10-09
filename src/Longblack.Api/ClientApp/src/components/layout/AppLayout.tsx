import { Box, GlobalStyles } from "@mui/material";
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
          px: 3,
          pb: 3,
          minWidth: 0,
          width: `calc(100% - ${DRAWER_WIDTH}px)`,
        }}
      >
        <Outlet />
      </Box>
    </Box>
  );
}
