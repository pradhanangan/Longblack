import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import {
  Box,
  Chip,
  CircularProgress,
  Divider,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { InventoryDto, InventoryTransactionDto } from '../api/types'

function useInventoryDetail(productVariantId: string) {
  return useQuery<InventoryDto>({
    queryKey: ['inventory', productVariantId],
    queryFn: () => api.get(`/api/inventory/${productVariantId}`),
  })
}

function useTransactions(productVariantId: string) {
  return useQuery<InventoryTransactionDto[]>({
    queryKey: ['inventory', productVariantId, 'transactions'],
    queryFn: () => api.get(`/api/inventory/${productVariantId}/transactions`),
  })
}

export function InventoryDetailPage() {
  const { productVariantId } = useParams<{ productVariantId: string }>()

  const { data: inventory, isLoading, isError } = useInventoryDetail(productVariantId!)
  const { data: transactions, isLoading: transactionsLoading, isError: transactionsError } =
    useTransactions(productVariantId!)

  if (isLoading) return <CircularProgress />
  if (isError || !inventory) return <Typography color="error">Variant not found.</Typography>

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
        <IconButton component={Link} to="/inventory" size="small">
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h5">{inventory.sku}</Typography>
        <Chip
          label={inventory.status}
          size="small"
          color={inventory.status === 'Active' ? 'success' : 'default'}
          sx={{ ml: 1 }}
        />
      </Box>

      <Box sx={{ display: 'grid', gridTemplateColumns: '160px 1fr', gap: 1, mb: 3, maxWidth: 600 }}>
        <Typography color="text.secondary">Product</Typography>
        <Typography>{inventory.productName}</Typography>
        <Typography color="text.secondary">Barcode</Typography>
        <Typography>{inventory.barcode ?? '—'}</Typography>
        <Typography color="text.secondary">Colour</Typography>
        <Typography>{inventory.colourName ?? '—'}</Typography>
        <Typography color="text.secondary">Size</Typography>
        <Typography>{inventory.sizeName ?? '—'}</Typography>
        <Typography color="text.secondary">Quantity</Typography>
        <Typography variant="h6">{inventory.quantity}</Typography>
        <Typography color="text.secondary">Last Updated</Typography>
        <Typography>{inventory.updatedAt ? new Date(inventory.updatedAt).toLocaleString() : '—'}</Typography>
      </Box>

      <Divider sx={{ my: 3 }} />

      <Typography variant="h6" sx={{ mb: 2 }}>Transaction History</Typography>

      {transactionsLoading && <CircularProgress size={24} />}
      {transactionsError && <Typography color="error">Failed to load transaction history.</Typography>}

      {!transactionsLoading && !transactionsError && transactions && (
        transactions.length === 0 ? (
          <Typography color="text.secondary">No transactions yet.</Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Date</TableCell>
                <TableCell>Type</TableCell>
                <TableCell align="right">Quantity Delta</TableCell>
                <TableCell>Source</TableCell>
                <TableCell>By</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {transactions.map((t) => (
                <TableRow key={t.id}>
                  <TableCell>{new Date(t.createdAt).toLocaleString()}</TableCell>
                  <TableCell>{t.type}</TableCell>
                  <TableCell align="right" sx={{ color: t.quantityDelta < 0 ? 'error.main' : 'success.main' }}>
                    {t.quantityDelta > 0 ? `+${t.quantityDelta}` : t.quantityDelta}
                  </TableCell>
                  <TableCell>{t.sourceType} ({t.sourceId})</TableCell>
                  <TableCell>{t.createdBy}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )
      )}
    </Box>
  )
}
