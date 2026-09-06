import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { InventorySummary, AdjustInventoryRequest } from '../models/inventory.model';

@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/inventory`;

  getInventory(search?: string, categoryId?: string, lowStockOnly?: boolean): Observable<InventorySummary> {
    let params = new HttpParams();
    if (search) {
      params = params.set('search', search);
    }
    if (categoryId) {
      params = params.set('categoryId', categoryId);
    }
    if (lowStockOnly !== undefined) {
      params = params.set('lowStockOnly', lowStockOnly.toString());
    }
    return this.http.get<InventorySummary>(this.apiUrl, { params });
  }

  adjustInventory(data: AdjustInventoryRequest): Observable<{ stockQuantity: number }> {
    return this.http.post<{ stockQuantity: number }>(`${this.apiUrl}/adjust`, data);
  }
}
