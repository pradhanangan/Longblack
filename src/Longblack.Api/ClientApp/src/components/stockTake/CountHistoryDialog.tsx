import {
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import type { StockTakeCountDto } from '../../api/types'

interface Props {
  open: boolean
  onClose: () => void
  stockTakeId: string
  itemId: string
  sku: string
}

export function CountHistoryDialog({ open, onClose, stockTakeId, itemId, sku }: Props) {
  const { data: counts, isLoading, isError } = useQuery<StockTakeCountDto[]>({
    queryKey: ['stock-takes', stockTakeId, 'items', itemId, 'counts'],
    queryFn: () => api.get(`/api/stock-takes/${stockTakeId}/items/${itemId}/counts`),
    enabled: open,
  })

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Count History — {sku}</DialogTitle>
      <DialogContent>
        {isLoading && <CircularProgress size={24} />}
        {isError && <Typography color="error">Failed to load count history.</Typography>}
        {!isLoading && !isError && counts && (
          counts.length === 0 ? (
            <Typography color="text.secondary">No counts recorded yet.</Typography>
          ) : (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>#</TableCell>
                  <TableCell align="right">Quantity</TableCell>
                  <TableCell>Counted At</TableCell>
                  <TableCell>By</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {counts.map((c, index) => (
                  <TableRow key={c.id}>
                    <TableCell>{index === 0 ? 'Initial' : `Recount ${index}`}</TableCell>
                    <TableCell align="right">{c.quantity}</TableCell>
                    <TableCell>{new Date(c.countedAt).toLocaleString()}</TableCell>
                    <TableCell>{c.countedBy}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  )
}
