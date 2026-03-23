import { Injectable } from '@angular/core';
import { environment } from '../../environment';
import { map, Observable } from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';

export interface Category {
  id: string;
  name: string;
  tva: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface CategoryRequestDto{
  name: string,
  tva: number
}

@Injectable({
  providedIn: 'root',
})

export class ArticlesCategoriesService {

  private readonly categoriesUrl = `${environment.routes.articles}/categories`;
  constructor(private http: HttpClient) {}

  // -------------------------------------------------------
  // CATEGORIES
  // -------------------------------------------------------

  getAllCategories(pageNumber: number = 1,
                  pageSize: number = 10): Observable<PagedResult<Category>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<Category>>(this.categoriesUrl, {params});
  }

  getAllCategoriesUnpaged(): Observable<Category[]> {
    const params = new HttpParams()
      .set('pageNumber', 1)
      .set('pageSize', 100); // covers realistic category counts
    return this.http.get<PagedResult<Category>>(this.categoriesUrl, { params }).pipe(
      map(res => res.items)
    );
  }

  getCategoryById(id: string): Observable<Category> {
    return this.http.get<Category>(`${this.categoriesUrl}/${id}`);
  }

  getCategoriesPagedByName(
    nameFilter: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<Category>> {
    const params = new HttpParams()
      .set('name', nameFilter)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<Category>>(`${this.categoriesUrl}/by-name`, { params });
  }

  getCategoriesPagedByDateRange(
    from: Date,
    to: Date,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<Category>> {
    const params = new HttpParams()
      .set('from', from.toISOString())
      .set('to', to.toISOString())
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<Category>>(`${this.categoriesUrl}/by-date-range`, { params });
  }

  createCategory(created: CategoryRequestDto): Observable<Category> {
    return this.http.post<Category>(this.categoriesUrl, created);
  }

  updateCategory(id: string, updated: CategoryRequestDto): Observable<Category> {
    return this.http.put<Category>(`${this.categoriesUrl}/${id}`, updated);
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.categoriesUrl}/${id}`);
  }
}
