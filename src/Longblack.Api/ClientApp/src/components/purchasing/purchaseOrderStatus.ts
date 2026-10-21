export function purchaseOrderStatusColor(
  status: string,
): 'default' | 'success' | 'error' | 'warning' | 'info' {
  if (status === 'Issued') return 'info'
  if (status === 'PartiallyReceived') return 'warning'
  if (status === 'Received') return 'success'
  if (status === 'Cancelled') return 'error'
  return 'default'
}

export function purchaseOrderStatusLabel(status: string): string {
  return status === 'PartiallyReceived' ? 'Partially Received' : status
}
