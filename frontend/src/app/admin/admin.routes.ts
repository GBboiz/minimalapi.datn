import { Routes } from '@angular/router';
import { StoreListComponent } from './stores/store-list.component';

export const ADMIN_ROUTES: Routes = [
  { path: 'stores', component: StoreListComponent },
  { path: '', redirectTo: 'stores', pathMatch: 'full' }
];
