import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import {
  Box,
  Button,
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
import type { ProductDto, ProductVariantDto } from '../api/types'
import { useAuth } from '../contexts/AuthContext'

function useProduct(id: string) {
  return useQuery<ProductDto>({
    queryKey: ['products', id],
    queryFn: () => api.get(`/api/products/${id}`),
  })
}

function useVariants(productId: string) {
  return useQuery<ProductVariantDto[]>({
    queryKey: ['products', productId, 'variants'],
    queryFn: () => api.get(`/api/products/${productId}/variants?status=`),
  })
}

export function ProductDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { user } = useAuth()
  const canWrite = user?.roles.some((r) => r === 'Manager' || r === 'Admin') ?? false

  const { data: product, isLoading: productLoading, isError: productError } = useProduct(id!)
  const { data: variants, isLoading: variantsLoading, isError: variantsError } = useVariants(id!)

  if (productLoading) return <CircularProgress />
  if (productError || !product) return <Typography color="error">Product not found.</Typography>

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
        <IconButton component={Link} to="/products" size="small">
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h5">{product.name}</Typography>
        <Chip
          label={product.status}
          size="small"
          color={product.status === 'Active' ? 'success' : 'default'}
          sx={{ ml: 1 }}
        />
      </Box>

      {/* Product fields */}
      <Box sx={{ display: 'grid', gridTemplateColumns: '160px 1fr', gap: 1, mb: 3, maxWidth: 600 }}>
        <Typography color="text.secondary">Product Code</Typography>
        <Typography>{product.productCode}</Typography>
        <Typography color="text.secondary">Description</Typography>
        <Typography>{product.description ?? '—'}</Typography>
        <Typography color="text.secondary">Brand</Typography>
        <Typography>{product.brandName ?? '—'}</Typography>
        <Typography color="text.secondary">Category</Typography>
        <Typography>{product.categoryName ?? '—'}</Typography>
      </Box>

      {/* Action buttons (stubbed) */}
      {canWrite && (
        <Box sx={{ display: 'flex', gap: 1, mb: 3 }}>
          <Button variant="outlined" disabled>Edit Product</Button>
          {product.status === 'Active' ? (
            <Button variant="outlined" color="warning" disabled>Deactivate</Button>
          ) : (
            <Button variant="outlined" color="success" disabled>Reactivate</Button>
          )}
        </Box>
      )}

      <Divider sx={{ my: 3 }} />

      {/* Variants section */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">Variants</Typography>
        {canWrite && (
          <Button variant="contained" size="small" disabled>Add Variant</Button>
        )}
      </Box>

      {variantsLoading && <CircularProgress size={24} />}
      {variantsError && <Typography color="error">Failed to load variants.</Typography>}

      {!variantsLoading && !variantsError && variants && (
        variants.length === 0 ? (
          <Typography color="text.secondary">No variants yet.</Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>SKU</TableCell>
                <TableCell>Barcode</TableCell>
                <TableCell>Colour</TableCell>
                <TableCell>Size</TableCell>
                <TableCell align="right">Selling Price</TableCell>
                <TableCell>Status</TableCell>
                {canWrite && <TableCell align="right">Actions</TableCell>}
              </TableRow>
            </TableHead>
            <TableBody>
              {variants.map((v) => (
                <TableRow key={v.id} sx={{ opacity: v.status === 'Inactive' ? 0.5 : 1 }}>
                  <TableCell>{v.sku}</TableCell>
                  <TableCell>{v.barcode ?? '—'}</TableCell>
                  <TableCell>{v.colourName ?? '—'}</TableCell>
                  <TableCell>{v.sizeName ?? '—'}</TableCell>
                  <TableCell align="right">${v.sellingPrice.toFixed(2)}</TableCell>
                  <TableCell>
                    <Chip
                      label={v.status}
                      size="small"
                      color={v.status === 'Active' ? 'success' : 'default'}
                    />
                  </TableCell>
                  {canWrite && (
                    <TableCell align="right">
                      <Button size="small" disabled>Edit</Button>
                      <Button size="small" color={v.status === 'Active' ? 'warning' : 'success'} disabled>
                        {v.status === 'Active' ? 'Deactivate' : 'Reactivate'}
                      </Button>
                    </TableCell>
                  )}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )
      )}
    </Box>
  )
}

