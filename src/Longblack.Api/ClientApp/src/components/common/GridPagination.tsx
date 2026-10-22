import { TablePagination } from '@mui/material'
import { PAGE_SIZE_OPTIONS } from './pagination'

interface Props {
  total: number
  // Pages are 1-based everywhere in the app and API; MUI's zero-based page is handled here.
  page: number
  pageSize: number
  onPageChange: (page: number) => void
  onPageSizeChange: (pageSize: number) => void
}

export function GridPagination({ total, page, pageSize, onPageChange, onPageSizeChange }: Props) {
  return (
    <TablePagination
      component="div"
      count={total}
      page={page - 1}
      rowsPerPage={pageSize}
      rowsPerPageOptions={PAGE_SIZE_OPTIONS}
      showFirstButton
      showLastButton
      onPageChange={(_, zeroBased) => onPageChange(zeroBased + 1)}
      onRowsPerPageChange={(e) => onPageSizeChange(Number(e.target.value))}
    />
  )
}
