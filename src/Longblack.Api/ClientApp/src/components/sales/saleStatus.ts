export function saleStatusColor(
  status: string,
): 'default' | 'success' | 'error' | 'warning' {
  if (status === 'Completed') return 'success'
  if (status === 'Cancelled') return 'error'
  if (status === 'Voided') return 'warning'
  return 'default'
}
