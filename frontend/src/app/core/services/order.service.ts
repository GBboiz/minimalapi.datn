import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { OrderSummary, OrderDetail, CreateOrderRequest } from '../models/order.model';
import { PagedResult } from '../models/paged-result.model';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/orders`;

  getAll(page: number = 1, pageSize: number = 10, search?: string, status?: string): Observable<PagedResult<OrderSummary>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) {
      params = params.set('search', search);
    }
    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<PagedResult<OrderSummary>>(this.apiUrl, { params });
  }

  getById(id: string): Observable<OrderDetail> {
    return this.http.get<OrderDetail>(`${this.apiUrl}/${id}`);
  }

  create(data: CreateOrderRequest): Observable<string> {
    return this.http.post<string>(this.apiUrl, data);
  }

  confirm(id: string): Observable<{ orderId: string; message: string }> {
    return this.http.put<{ orderId: string; message: string }>(`${this.apiUrl}/${id}/confirm`, {});
  }

  cancel(id: string): Observable<{ orderId: string; message: string }> {
    return this.http.put<{ orderId: string; message: string }>(`${this.apiUrl}/${id}/cancel`, {});
  }

  complete(id: string): Observable<{ orderId: string; message: string }> {
    return this.http.put<{ orderId: string; message: string }>(`${this.apiUrl}/${id}/complete`, {});
  }
}
