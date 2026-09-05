export interface GoogleSheetStatus {
  isConnected: boolean;
  status: 'Disconnected' | 'Connected' | 'SyncError';
  spreadsheetId: string;
  sheetName: string;
  lastSyncAt: string | null;
  lastErrorMessage: string | null;
}

export interface SyncHistoryItem {
  id: string;
  type: string;
  orderCode: string;
  customerName: string;
  productsSummary: string;
  totalAmount: number;
  currency: string;
  status: 'Success' | 'Pending' | 'Retrying' | 'Failed';
  retryCount: number;
  error: string | null;
  occurredAt: string;
  processedAt: string | null;
}

export interface ConnectGoogleSheetRequest {
  spreadsheetId: string;
  sheetName: string;
  refreshToken?: string;
}
