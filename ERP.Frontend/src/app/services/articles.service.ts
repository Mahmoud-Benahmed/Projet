import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../environment';
import { ArticleCategoryResponseDto } from './articles/categories.service';

// ========================
// Models
// ========================
export interface ArticleResponseDto {
  id: string;
  codeRef: string;
  barCode: string
  libelle: string;
  tva: number
  prix: number;
  category: ArticleCategoryResponseDto;
  isDeleted: boolean;
}

export interface ArticleStatsDto{
    TotalCount: number,
    ActiveCount: number,
    DeletedCount: number,
    CategoriesCount: number
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

  private readonly articlesUrl = environment.apiUrl + environment.routes.articles;
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
        DeletedCount: res.deletedCount,
        CategoriesCount: res.categoriesCount
      }))
    );
  }

  getAllArticles(pageNumber: number = 1,
                  pageSize: number = 10): Observable<PagedResult<ArticleResponseDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<ArticleResponseDto>>(this.articlesUrl, {params});
  }

  getDeletedArticles(pageNumber: number = 1,
                  pageSize: number = 10): Observable<PagedResult<ArticleResponseDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<ArticleResponseDto>>(`${this.articlesUrl}/deleted`, {params});
  }

  getArticleById(id: string): Observable<ArticleResponseDto> {
    return this.http.get<ArticleResponseDto>(`${this.articlesUrl}/${id}`);
  }

  getArticleByCode(code: string): Observable<ArticleResponseDto> {
    return this.http.get<ArticleResponseDto>(`${this.articlesUrl}/code/${code}`);
  }

  getArticlesPagedByCategory(
    categoryId: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<ArticleResponseDto>> {
    const params = new HttpParams()
      .set('categoryId', categoryId)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<ArticleResponseDto>>(`${this.articlesUrl}/paged/by-ArticleCategoryResponseDto`, { params });
  }

  getArticlesPagedByLibelle(
    libelleFilter: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<ArticleResponseDto>> {
    const params = new HttpParams()
      .set('libelleFilter', libelleFilter)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<ArticleResponseDto>>(`${this.articlesUrl}/paged/by-libelle`, { params });
  }

  createArticle(request: CreateArticleRequest): Observable<ArticleResponseDto> {
    return this.http.post<ArticleResponseDto>(this.articlesUrl, request);
  }

  updateArticle(id: string, request: UpdateArticleRequest): Observable<ArticleResponseDto> {
    return this.http.put<ArticleResponseDto>(`${this.articlesUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.articlesUrl}/delete/${id}`);
  }

  restore(id: string): Observable<void> {
    return this.http.patch<void>(`${this.articlesUrl}/restore/${id}`, {});
  }


  // -------------------------------------------------------
  // CATEGORIES
  // -------------------------------------------------------

  getAllCategories(): Observable<ArticleCategoryResponseDto[]> {
    return this.http.get<ArticleCategoryResponseDto[]>(this.categoriesUrl);
  }

  getCategoryById(id: string): Observable<ArticleCategoryResponseDto> {
    return this.http.get<ArticleCategoryResponseDto>(`${this.categoriesUrl}/${id}`);
  }

  getCategoryByName(name: string): Observable<ArticleCategoryResponseDto> {
    return this.http.get<ArticleCategoryResponseDto>(`${this.categoriesUrl}/name/${name}`);
  }

  getCategoriesPaged(
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<ArticleCategoryResponseDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<ArticleCategoryResponseDto>>(`${this.categoriesUrl}/paged`, { params });
  }

  getCategoriesPagedByName(
    nameFilter: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<ArticleCategoryResponseDto>> {
    const params = new HttpParams()
      .set('nameFilter', nameFilter)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<ArticleCategoryResponseDto>>(`${this.categoriesUrl}/by-name`, { params });
  }

  getCategoriesPagedByDateRange(
    from: Date,
    to: Date,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<ArticleCategoryResponseDto>> {
    const params = new HttpParams()
      .set('from', from.toISOString())
      .set('to', to.toISOString())
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<ArticleCategoryResponseDto>>(`${this.categoriesUrl}/by-date-range`, { params });
  }

  createCategory(name: string): Observable<ArticleCategoryResponseDto> {
    return this.http.post<ArticleCategoryResponseDto>(this.categoriesUrl, JSON.stringify(name), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  updateCategoryName(id: string, newName: string): Observable<ArticleCategoryResponseDto> {
    return this.http.put<ArticleCategoryResponseDto>(`${this.categoriesUrl}/${id}/name`, JSON.stringify(newName), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.categoriesUrl}/${id}`);
  }
}
