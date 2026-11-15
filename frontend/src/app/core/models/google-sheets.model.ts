export interface GoogleSheetStatus {
  isConnected: boolean;
  status: 'Disconnected' | 'Connected' | 'SyncError' | 'ReauthorizationRequired';
  spreadsheetId: string;
  sheetName: string;
  lastSyncAt: string | null;
  lastErrorMessage: string | null;
  /** true khi server chưa cấu hình Google OAuth — chỉ giả lập, không ghi Sheet thật */
  isMockMode: boolean;
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

export interface SaveGoogleSheetConfigRequest {
  spreadsheetId: string;
  sheetName: string;
}
