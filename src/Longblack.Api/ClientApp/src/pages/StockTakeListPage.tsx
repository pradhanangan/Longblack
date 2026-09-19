import AddIcon from '@mui/icons-material/Add'
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
  Typography,
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { StockTakeDto } from '../api/types'
import { StockTakeFormDialog } from '../components/stockTake/StockTakeFormDialog'
import { useAuth } from '../contexts/AuthContext'

function statusColor(status: string): 'default' | 'success' | 'error' | 'warning' {
  if (status === 'Approved') return 'success'
  if (status === 'Cancelled') return 'error'
  if (status === 'InProgress') return 'warning'
  return 'default'
}

function useStockTakes(status: string) {
  const params = new URLSearchParams()
  if (status) params.set('status', status)
  const qs = params.toString()
  return useQuery<StockTakeDto[]>({
    queryKey: ['stock-takes', status],
    queryFn: () => api.get(`/api/stock-takes${qs ? `?${qs}` : ''}`),
  })
}

export function StockTakeListPage() {
  const { user } = useAuth()
  const canCreate = user?.roles.some((r) => r === 'Staff' || r === 'Manager' || r === 'Admin') ?? false

  const [status, setStatus] = useState('')
  const [createOpen, setCreateOpen] = useState(false)

  const { data: stockTakes, isLoading, isError } = useStockTakes(status)

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">Stock Takes</Typography>
        {canCreate && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
            New Stock Take
          </Button>
        )}
      </Box>

      <StockTakeFormDialog open={createOpen} onClose={() => setCreateOpen(false)} />

      <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Status</InputLabel>
          <Select value={status} label="Status" onChange={(e) => setStatus(e.target.value)}>
            <MenuItem value="">All</MenuItem>
            <MenuItem value="Draft">Draft</MenuItem>
            <MenuItem value="InProgress">InProgress</MenuItem>
            <MenuItem value="Completed">Completed</MenuItem>
            <MenuItem value="Approved">Approved</MenuItem>
            <MenuItem value="Cancelled">Cancelled</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {isLoading && <CircularProgress />}
      {isError && <Typography color="error">Failed to load Stock Takes.</Typography>}

      {!isLoading && !isError && stockTakes && (
        stockTakes.length === 0 ? (
          <Typography color="text.secondary">No Stock Takes match the current filters.</Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Reference #</TableCell>
                <TableCell>Brand</TableCell>
                <TableCell>Category</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Start Date</TableCell>
                <TableCell>Completion Date</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {stockTakes.map((s) => (
                <TableRow key={s.id} hover>
                  <TableCell>
                    <Link to={`/stock-takes/${s.id}`} style={{ textDecoration: 'none', color: 'inherit', fontWeight: 500 }}>
                      {s.referenceNumber}
                    </Link>
                  </TableCell>
                  <TableCell>{s.brandName ?? 'Any'}</TableCell>
                  <TableCell>{s.categoryName ?? 'Any'}</TableCell>
                  <TableCell>
                    <Chip label={s.status} size="small" color={statusColor(s.status)} />
                  </TableCell>
                  <TableCell>{s.startDate ? new Date(s.startDate).toLocaleDateString() : '—'}</TableCell>
                  <TableCell>{s.completionDate ? new Date(s.completionDate).toLocaleDateString() : '—'}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )
      )}
    </Box>
  )
}
