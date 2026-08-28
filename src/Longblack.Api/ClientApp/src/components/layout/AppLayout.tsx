import { Box } from '@mui/material'
import { Outlet } from 'react-router-dom'
import { Sidebar, DRAWER_WIDTH } from './Sidebar'

export function AppLayout() {
  return (
    <Box sx={{ display: 'flex' }}>
      <Sidebar />
      <Box component="main" sx={{ flexGrow: 1, p: 3, width: `calc(100% - ${DRAWER_WIDTH}px)` }}>
        <Outlet />
      </Box>
    </Box>
  )
}
