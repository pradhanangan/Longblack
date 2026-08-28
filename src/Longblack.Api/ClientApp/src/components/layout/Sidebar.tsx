import DashboardIcon from '@mui/icons-material/Dashboard'
import InventoryIcon from '@mui/icons-material/Inventory'
import LocalShippingIcon from '@mui/icons-material/LocalShipping'
import LogoutIcon from '@mui/icons-material/Logout'
import PeopleIcon from '@mui/icons-material/People'
import SettingsIcon from '@mui/icons-material/Settings'
import ShoppingCartIcon from '@mui/icons-material/ShoppingCart'
import StyleIcon from '@mui/icons-material/Style'
import {
  Box,
  Chip,
  Divider,
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
} from '@mui/material'
import { useNavigate, useLocation, NavLink } from 'react-router-dom'
import { useAuth } from '../../contexts/AuthContext'

const DRAWER_WIDTH = 240

const NAV_ITEMS = [
  { label: 'Dashboard', icon: <DashboardIcon />, path: null },
  { label: 'Products', icon: <StyleIcon />, path: '/products' },
  { label: 'Receiving', icon: <LocalShippingIcon />, path: null },
  { label: 'Inventory', icon: <InventoryIcon />, path: null },
  { label: 'Stock Take', icon: <ShoppingCartIcon />, path: null },
  { label: 'Suppliers', icon: <PeopleIcon />, path: null },
  { label: 'Settings', icon: <SettingsIcon />, path: null },
]

export function Sidebar() {
  const { logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  function handleLogout() {
    logout()
    navigate('/login')
  }

  return (
    <Drawer
      variant="permanent"
      sx={{
        width: DRAWER_WIDTH,
        flexShrink: 0,
        '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box' },
      }}
    >
      <Toolbar>
        <Typography variant="h6" noWrap sx={{ fontWeight: 'bold' }}>
          Longblack
        </Typography>
      </Toolbar>
      <Divider />
      <List>
        {NAV_ITEMS.map((item) => {
          const active = item.path ? location.pathname.startsWith(item.path) : false
          return (
            <ListItem key={item.label} disablePadding>
              <ListItemButton
                component={item.path ? NavLink : 'div'}
                to={item.path ?? undefined}
                disabled={!item.path}
                selected={active}
                sx={{ opacity: item.path ? 1 : 0.5 }}
              >
                <ListItemIcon>{item.icon}</ListItemIcon>
                <ListItemText primary={item.label} />
                {!item.path && (
                  <Chip label="Soon" size="small" variant="outlined" sx={{ fontSize: 10 }} />
                )}
              </ListItemButton>
            </ListItem>
          )
        })}
      </List>
      <Box sx={{ flexGrow: 1 }} />
      <Divider />
      <List>
        <ListItem disablePadding>
          <ListItemButton onClick={handleLogout}>
            <ListItemIcon>
              <LogoutIcon />
            </ListItemIcon>
            <ListItemText primary="Log out" />
          </ListItemButton>
        </ListItem>
      </List>
    </Drawer>
  )
}

export { DRAWER_WIDTH }
