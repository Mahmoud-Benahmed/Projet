import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../environment';

// ========================
// Models
// ========================
export interface Article {
  id: string;
  codeRef: string;
  barCode: string
  libelle: string;
  prix: number;
  categoryId: string;
  isActive: boolean;
}

export interface ArticleStatsDto{
    TotalCount: number,
    ActiveCount: number,
    InActiveCount: number,
    CategoriesCount: number
}

export interface Category {
  id: string;
  name: string;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

export interface CreateArticleRequest {
  libelle: string;
  prix: number;
  categoryId: string;
}

export interface UpdateArticleRequest {
  libelle: string;
  prix: number;
  categoryId: string;
}
// ========================
// Service
// ========================
@Injectable({
  providedIn: 'root',
})
export class ArticleService {

  private readonly baseUrl = `${environment.apiUrl}${environment.routes.articles}`;

  private readonly articlesUrl = `${environment.apiUrl}${environment.routes.articles}`;
  private readonly categoriesUrl = `${this.articlesUrl}/categories`;

  constructor(private http: HttpClient) {}

  // -------------------------------------------------------
  // ARTICLES
  // -------------------------------------------------------

  getStats(): Observable<ArticleStatsDto> {
    return this.http.get<any>(`${this.articlesUrl}/stats`).pipe(
      map(res => ({
        TotalCount: res.totalCount,
        ActiveCount: res.activeCount,
        InActiveCount: res.inActiveCount,
        CategoriesCount: res.categoriesCount
      }))
    );
  }

  getAllArticles(): Observable<Article[]> {
    return this.http.get<Article[]>(this.articlesUrl);
  }

  getArticleById(id: string): Observable<Article> {
    return this.http.get<Article>(`${this.articlesUrl}/${id}`);
  }

  getArticleByCode(code: string): Observable<Article> {
    return this.http.get<Article>(`${this.articlesUrl}/code/${code}`);
  }

  getArticlesPagedByCategory(
    categoryId: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<Article>> {
    const params = new HttpParams()
      .set('categoryId', categoryId)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<Article>>(`${this.articlesUrl}/paged/by-category`, { params });
  }

  getArticlesPagedByStatus(
    isActive: boolean,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<Article>> {
    const params = new HttpParams()
      .set('isActive', isActive)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<Article>>(`${this.articlesUrl}/paged/by-status`, { params });
  }

  getArticlesPagedByLibelle(
    libelleFilter: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<Article>> {
    const params = new HttpParams()
      .set('libelleFilter', libelleFilter)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<Article>>(`${this.articlesUrl}/paged/by-libelle`, { params });
  }

  createArticle(request: CreateArticleRequest): Observable<Article> {
    return this.http.post<Article>(this.articlesUrl, request);
  }

  updateArticle(id: string, request: UpdateArticleRequest): Observable<Article> {
    return this.http.put<Article>(`${this.articlesUrl}/${id}`, request);
  }

  activateArticle(id: string): Observable<void> {
    return this.http.patch<void>(`${this.articlesUrl}/${id}/activate`, {});
  }

  deactivateArticle(id: string): Observable<void> {
    return this.http.patch<void>(`${this.articlesUrl}/${id}/deactivate`, {});
  }

  deleteArticle(id: string): Observable<void> {
    return this.http.delete<void>(`${this.articlesUrl}/${id}`);
  }

  // -------------------------------------------------------
  // CATEGORIES
  // -------------------------------------------------------

  getAllCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(this.categoriesUrl);
  }

  getCategoryById(id: string): Observable<Category> {
    return this.http.get<Category>(`${this.categoriesUrl}/${id}`);
  }

  getCategoryByName(name: string): Observable<Category> {
    return this.http.get<Category>(`${this.categoriesUrl}/name/${name}`);
  }

  getCategoriesPaged(
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<Category>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<Category>>(`${this.categoriesUrl}/paged`, { params });
  }

  getCategoriesPagedByName(
    nameFilter: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<Category>> {
    const params = new HttpParams()
      .set('nameFilter', nameFilter)
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

  createCategory(name: string): Observable<Category> {
    return this.http.post<Category>(this.categoriesUrl, JSON.stringify(name), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  updateCategoryName(id: string, newName: string): Observable<Category> {
    return this.http.put<Category>(`${this.categoriesUrl}/${id}/name`, JSON.stringify(newName), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.categoriesUrl}/${id}`);
  }
}
