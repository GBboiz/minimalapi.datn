import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GoogleSheetsService } from '../../../core/services/google-sheets.service';
import { GoogleSheetStatus, SyncHistoryItem } from '../../../core/models/google-sheets.model';

@Component({
  selector: 'app-google-sheets-dashboard',
  imports: [CommonModule, FormsModule],
  templateUrl: './google-sheets-dashboard.component.html',
  styleUrl: './google-sheets-dashboard.component.scss'
})
export class GoogleSheetsDashboardComponent implements OnInit {
  private sheetsService = inject(GoogleSheetsService);

  status: GoogleSheetStatus | null = null;
  history: SyncHistoryItem[] = [];

  spreadsheetId = '';
  sheetName = 'Đơn hàng';

  loading = false;
  connecting = false;
  historyLoading = false;
  successMessage = '';
  errorMessage = '';

  ngOnInit(): void {
    this.loadStatus();
    this.loadHistory();
  }

  loadStatus(): void {
    this.loading = true;
    this.sheetsService.getStatus().subscribe({
      next: (res) => {
        this.status = res;
        this.spreadsheetId = res.spreadsheetId || '';
        this.sheetName = res.sheetName || 'Đơn hàng';
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || err?.error || 'Không thể tải trạng thái Google Sheets.';
        this.loading = false;
      }
    });
  }

  loadHistory(): void {
    this.historyLoading = true;
    this.sheetsService.getHistory(20).subscribe({
      next: (res) => {
        this.history = res;
        this.historyLoading = false;
      },
      error: () => {
        this.historyLoading = false;
      }
    });
  }

  saveConfig(): void {
    if (!this.spreadsheetId.trim()) {
      this.errorMessage = 'Vui lòng nhập Spreadsheet ID.';
      return;
    }

    this.connecting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.sheetsService.connect({
      spreadsheetId: this.spreadsheetId.trim(),
      sheetName: this.sheetName.trim()
    }).subscribe({
      next: (res) => {
        this.connecting = false;
        this.successMessage = res.message || 'Lưu cấu hình Google Sheets thành công!';
        this.loadStatus();
      },
      error: (err) => {
        this.connecting = false;
        this.errorMessage = err?.error?.detail || err?.error || 'Lỗi khi kết nối Google Sheets.';
      }
    });
  }

  fillDemo(): void {
    this.spreadsheetId = '1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms';
    this.sheetName = 'Đơn hàng';
    this.saveConfig();
  }

  disconnect(): void {
    if (!confirm('Bạn có chắc muốn hủy kết nối Google Sheets không?')) return;

    this.connecting = true;
    this.sheetsService.disconnect().subscribe({
      next: () => {
        this.connecting = false;
        this.successMessage = 'Đã ngắt kết nối Google Sheets.';
        this.loadStatus();
      },
      error: (err) => {
        this.connecting = false;
        this.errorMessage = err?.error?.detail || err?.error || 'Lỗi khi ngắt kết nối.';
      }
    });
  }

  retrySync(id: string): void {
    this.sheetsService.retry(id).subscribe({
      next: () => {
        this.successMessage = 'Đã kích hoạt thử lại đồng bộ cho đơn hàng.';
        this.loadHistory();
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || err?.error || 'Không thể kích hoạt thử lại.';
      }
    });
  }

  openOAuthConsent(): void {
    this.sheetsService.getOAuthUrl().subscribe({
      next: (res) => {
        window.location.href = res.url;
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'Không thể lấy URL cấp quyền Google OAuth.';
      }
    });
  }
}
