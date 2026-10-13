import DashboardIcon from "@mui/icons-material/Dashboard";
import InventoryIcon from "@mui/icons-material/Inventory";
import LocalShippingIcon from "@mui/icons-material/LocalShipping";
import LogoutIcon from "@mui/icons-material/Logout";
import PeopleIcon from "@mui/icons-material/People";
import SettingsIcon from "@mui/icons-material/Settings";
import ShoppingCartIcon from "@mui/icons-material/ShoppingCart";
import StoreIcon from "@mui/icons-material/Store";
import StyleIcon from "@mui/icons-material/Style";
import SwapHorizIcon from "@mui/icons-material/SwapHoriz";
import {
  Box,
  Divider,
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  ListSubheader,
  Toolbar,
  Typography,
} from "@mui/material";
import { useNavigate, useLocation, NavLink } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";

const DRAWER_WIDTH = 240;
const LIST_ICON_SX = {
  minWidth: 40,
  "& .MuiSvgIcon-root": { fontSize: 22 },
};

const NAV_GROUPS = [
  {
    label: null,
    items: [{ label: "Dashboard", icon: <DashboardIcon />, path: "/dashboard" }],
  },
  {
    label: "Operations",
    items: [
      { label: "Receiving", icon: <LocalShippingIcon />, path: "/goods-receipts" },
      { label: "Inventory", icon: <InventoryIcon />, path: "/inventory" },
      { label: "Stock Take", icon: <ShoppingCartIcon />, path: "/stock-takes" },
      { label: "Stock Transfers", icon: <SwapHorizIcon />, path: "/stock-transfers" },
    ],
  },
  {
    label: "Setup",
    items: [
      { label: "Products", icon: <StyleIcon />, path: "/products" },
      { label: "Stores", icon: <StoreIcon />, path: "/stores" },
      { label: "Suppliers", icon: <PeopleIcon />, path: "/suppliers" },
      { label: "Settings", icon: <SettingsIcon />, path: "/settings" },
    ],
  },
];

export function Sidebar() {
  const { logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  function handleLogout() {
    logout();
    navigate("/login");
  }

  return (
    <Drawer
      variant="permanent"
      sx={{
        width: DRAWER_WIDTH,
        flexShrink: 0,
        "& .MuiDrawer-paper": { width: DRAWER_WIDTH, boxSizing: "border-box" },
      }}
    >
      <Toolbar sx={{ justifyContent: "center" }}>
        <Typography
          variant="h6"
          noWrap
          sx={{ fontWeight: "bold", textAlign: "center" }}
        >
          Longblack
        </Typography>
      </Toolbar>
      <Divider />
      <List>
        {NAV_GROUPS.map((group, index) => (
          <Box key={group.label ?? index}>
            {group.label && (
              <ListSubheader
                disableSticky
                sx={{ lineHeight: "32px", mt: 1, bgcolor: "transparent" }}
              >
                {group.label}
              </ListSubheader>
            )}
            {group.items.map((item) => (
              <ListItem key={item.label} disablePadding>
                <ListItemButton
                  component={NavLink}
                  to={item.path}
                  selected={location.pathname.startsWith(item.path)}
                >
                  <ListItemIcon sx={LIST_ICON_SX}>{item.icon}</ListItemIcon>
                  <ListItemText primary={item.label} />
                </ListItemButton>
              </ListItem>
            ))}
          </Box>
        ))}
      </List>
      <Box sx={{ flexGrow: 1 }} />
      <Divider />
      <List>
        <ListItem disablePadding>
          <ListItemButton onClick={handleLogout}>
            <ListItemIcon sx={LIST_ICON_SX}>
              <LogoutIcon />
            </ListItemIcon>
            <ListItemText primary="Log out" />
          </ListItemButton>
        </ListItem>
      </List>
    </Drawer>
  );
}

export { DRAWER_WIDTH };
