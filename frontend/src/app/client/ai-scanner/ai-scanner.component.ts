import { Component, ElementRef, ViewChild, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AiScannerService } from '../../core/services/ai-scanner.service';
import {
  ScannedInvoice,
  ConfirmScannedOrderRequest,
  ConfirmScannedOrderResponse
} from '../../core/models/ai-scanner.model';

@Component({
  selector: 'app-ai-scanner',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './ai-scanner.component.html',
  styleUrl: './ai-scanner.component.scss'
})
export class AiScannerComponent implements OnDestroy {
  private scannerService = inject(AiScannerService);
  private router = inject(Router);

  @ViewChild('videoElement') videoElement?: ElementRef<HTMLVideoElement>;
  @ViewChild('canvasElement') canvasElement?: ElementRef<HTMLCanvasElement>;
  @ViewChild('fileInput') fileInput?: ElementRef<HTMLInputElement>;

  // Tab State
  activeTab: 'camera' | 'upload' = 'camera';

  // Camera state
  isCameraActive = false;
  cameraStream: MediaStream | null = null;
  cameraError = '';

  // File upload state
  selectedFile: File | null = null;
  previewImageUrl: string | null = null;

  // Processing state
  scanning = false;
  confirming = false;
  errorMsg = '';
  successResponse: ConfirmScannedOrderResponse | null = null;

  // AI Extracted Result
  scannedResult: ScannedInvoice | null = null;

  ngOnDestroy(): void {
    this.stopCamera();
  }

  // --- TAB SWITCHING ---
  switchTab(tab: 'camera' | 'upload'): void {
    this.activeTab = tab;
    if (tab === 'upload') {
      this.stopCamera();
    } else {
      if (!this.previewImageUrl && !this.scannedResult) {
        void this.startCamera();
      }
    }
  }

  // --- CAMERA CONTROL ---
  async startCamera(): Promise<void> {
    this.cameraError = '';
    this.errorMsg = '';
    this.successResponse = null;

    try {
      this.cameraStream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 720 } }
      });

      this.isCameraActive = true;

      setTimeout(() => {
        if (this.videoElement && this.cameraStream) {
          this.videoElement.nativeElement.srcObject = this.cameraStream;
          void this.videoElement.nativeElement.play();
        }
      }, 100);
    } catch (err: any) {
      this.cameraError = 'Không thể truy cập camera. Vui lòng cấp quyền trình duyệt hoặc sử dụng tính năng tải ảnh.';
      this.isCameraActive = false;
    }
  }

  stopCamera(): void {
    if (this.cameraStream) {
      this.cameraStream.getTracks().forEach(track => track.stop());
      this.cameraStream = null;
    }
    this.isCameraActive = false;
  }

  captureAndScan(): void {
    if (!this.videoElement || !this.canvasElement) return;

    const video = this.videoElement.nativeElement;
    const canvas = this.canvasElement.nativeElement;

    canvas.width = video.videoWidth || 640;
    canvas.height = video.videoHeight || 480;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
    this.previewImageUrl = canvas.toDataURL('image/jpeg');

    canvas.toBlob((blob) => {
      if (blob) {
        const file = new File([blob], `scan-${Date.now()}.jpg`, { type: 'image/jpeg' });
        this.selectedFile = file;
        this.stopCamera();
        this.executeScan(file);
      }
    }, 'image/jpeg', 0.9);
  }

  // --- FILE UPLOAD ---
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      this.setFile(input.files[0]);
    }
  }

  onDropFile(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer && event.dataTransfer.files.length > 0) {
      this.setFile(event.dataTransfer.files[0]);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  private setFile(file: File): void {
    this.selectedFile = file;
    this.stopCamera();
    this.successResponse = null;

    const reader = new FileReader();
    reader.onload = () => {
      this.previewImageUrl = reader.result as string;
    };
    reader.readAsDataURL(file);

    this.executeScan(file);
  }

  // --- QUICK DEMO SAMPLES ---
  loadSampleInvoice(type: 'fashion' | 'tech'): void {
    this.stopCamera();
    this.previewImageUrl = null;
    this.selectedFile = null;
    this.successResponse = null;

    // Trigger AI Scan trực tiếp không cần file thực (sử dụng fallback AI demo từ kho hàng)
    this.executeScan();
  }

  // --- CALL BACKEND AI SCAN ---
  executeScan(file?: File): void {
    this.scanning = true;
    this.errorMsg = '';
    this.scannedResult = null;

    this.scannerService.scanInvoice(file).subscribe({
      next: (result: any) => {
        // Chuẩn hóa dữ liệu tương thích cả PascalCase và camelCase
        this.scannedResult = {
          customerName: result.customerName ?? result.CustomerName ?? 'Khách Hàng AI',
          phone: result.phone ?? result.Phone ?? '0908889999',
          address: result.address ?? result.Address ?? 'Hồ Chí Minh',
          invoiceCode: result.invoiceCode ?? result.InvoiceCode ?? null,
          items: (result.items ?? result.Items ?? []).map((i: any) => ({
            productName: i.productName ?? i.ProductName ?? 'Sản phẩm',
            quantity: i.quantity ?? i.Quantity ?? 1,
            unitPrice: i.unitPrice ?? i.UnitPrice ?? 0,
            total: i.total ?? i.Total ?? (i.quantity * i.unitPrice),
            matchedProductId: i.matchedProductId ?? i.MatchedProductId ?? null,
            matchedProductSku: i.matchedProductSku ?? i.MatchedProductSku ?? null,
            currentStock: i.currentStock ?? i.CurrentStock ?? 0,
            isMatched: i.isMatched ?? i.IsMatched ?? false
          })),
          totalAmount: result.totalAmount ?? result.TotalAmount ?? 0,
          aiConfidence: result.aiConfidence ?? result.AiConfidence ?? 98.5,
          aiSource: result.aiSource ?? result.AiSource ?? 'Gemini 2.5 Flash'
        };
        this.scanning = false;
      },
      error: (err) => {
        this.scanning = false;
        this.errorMsg = err.error?.error || 'Không thể trích xuất hóa đơn bằng AI. Vui lòng thử lại.';
      }
    });
  }

  // --- CONFIRM & CREATE ORDER ---
  onConfirmOrder(): void {
    if (!this.scannedResult) return;

    this.confirming = true;
    this.errorMsg = '';

    const req: ConfirmScannedOrderRequest = {
      customerName: this.scannedResult.customerName || 'Khách Hàng AI',
      phone: this.scannedResult.phone || '0908889999',
      address: this.scannedResult.address || 'Hồ Chí Minh',
      items: this.scannedResult.items.map(i => ({
        productId: i.matchedProductId || '',
        productName: i.productName,
        quantity: i.quantity,
        unitPrice: i.unitPrice
      }))
    };

    this.scannerService.confirmScannedOrder(req).subscribe({
      next: (res: any) => {
        this.confirming = false;
        this.successResponse = {
          orderId: res.orderId ?? res.OrderId,
          orderCode: res.orderCode ?? res.OrderCode,
          totalAmount: res.totalAmount ?? res.TotalAmount,
          status: res.status ?? res.Status ?? 'Confirmed',
          customerName: res.customerName ?? res.CustomerName,
          syncedToOutbox: res.syncedToOutbox ?? res.SyncedToOutbox ?? true
        };
        this.scannedResult = null;
      },
      error: (err) => {
        this.confirming = false;
        this.errorMsg = err.error?.error || 'Có lỗi xảy ra khi tạo đơn hàng từ hóa đơn quét.';
      }
    });
  }

  // --- REMOVE / RECALCULATE ITEM ---
  removeItem(index: number): void {
    if (!this.scannedResult) return;
    this.scannedResult.items.splice(index, 1);
    this.recalculateTotal();
  }

  recalculateTotal(): void {
    if (!this.scannedResult) return;
    this.scannedResult.totalAmount = this.scannedResult.items.reduce(
      (sum, item) => sum + (item.quantity * item.unitPrice),
      0
    );
  }

  resetScan(): void {
    this.scannedResult = null;
    this.successResponse = null;
    this.previewImageUrl = null;
    this.selectedFile = null;
    this.errorMsg = '';
    if (this.fileInput) {
      this.fileInput.nativeElement.value = '';
    }
  }
}
