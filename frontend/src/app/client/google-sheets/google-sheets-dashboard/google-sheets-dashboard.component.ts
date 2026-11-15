import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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
  private route = inject(ActivatedRoute);
  private router = inject(Router);

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
    this.handleOAuthRedirect();
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

  /** Lưu cấu hình bảng tính rồi chuyển sang Google để cấp quyền (OAuth). */
  saveConfig(): void {
    const spreadsheetId = this.extractSpreadsheetId(this.spreadsheetId);
    if (!spreadsheetId) {
      this.errorMessage = 'Vui lòng nhập Spreadsheet ID hoặc link bảng tính.';
      return;
    }
    this.spreadsheetId = spreadsheetId;

    this.connecting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.sheetsService.saveConfig({
      spreadsheetId,
      sheetName: this.sheetName.trim()
    }).subscribe({
      next: () => this.openOAuthConsent(),
      error: (err) => {
        this.connecting = false;
        this.errorMessage = err?.error?.error || err?.error?.detail || 'Lỗi khi lưu cấu hình Google Sheets.';
      }
    });
  }

  /** Chỉ dùng ở chế độ giả lập (server chưa cấu hình Google OAuth). */
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
    this.connecting = true;
    this.sheetsService.getOAuthUrl().subscribe({
      next: (res) => {
        window.location.href = res.url;
      },
      error: (err) => {
        this.connecting = false;
        this.errorMessage = err?.error?.detail || 'Không thể lấy URL cấp quyền Google OAuth.';
      }
    });
  }

  /** Cho phép dán cả link https://docs.google.com/spreadsheets/d/{ID}/edit */
  private extractSpreadsheetId(input: string): string {
    const value = input.trim();
    const match = value.match(/\/spreadsheets\/d\/([a-zA-Z0-9_-]+)/);
    return match ? match[1] : value;
  }

  /** Đọc kết quả Google redirect về (?connected=true hoặc ?error=...) rồi xóa khỏi URL. */
  private handleOAuthRedirect(): void {
    const params = this.route.snapshot.queryParamMap;
    if (params.get('connected') === 'true') {
      this.successMessage = 'Kết nối Google Sheets thành công. Đơn hàng mới sẽ được tự động đồng bộ.';
    } else if (params.get('error')) {
      this.errorMessage = params.get('error')!;
    } else {
      return;
    }
    void this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });
  }
}
