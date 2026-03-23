import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../environment';
import { Category } from './categories.service';

// ========================
// Models
// ========================
export interface Article {
  id: string;
  codeRef: string;
  barCode: string;
  libelle: string;
  prix: number;
  tva: number;
  categoryId: string;
  categoryName: string;
  category: Category
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface ArticleStatsDto {
  totalCount: number;
  activeCount: number;
  deletedCount: number;
  categoriesCount: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface CreateArticleRequest {
  libelle: string;
  prix: number;
  categoryId: string;
  barCode: string;
  tva: number | null;
}

export interface UpdateArticleRequest {
  libelle: string;
  prix: number;
  categoryId: string;
  barCode: string | null;
  tva: number | null;
}
// ========================
// Service
// ========================
@Injectable({
  providedIn: 'root',
})
export class ArticleService {

  private readonly articlesUrl = `${environment.apiUrl}${environment.routes.articles}`;

  constructor(private http: HttpClient) {}

  // -------------------------------------------------------
  // ARTICLES
  // -------------------------------------------------------

  getStats(): Observable<ArticleStatsDto> {
    return this.http.get<ArticleStatsDto>(`${this.articlesUrl}/stats`);
  }

  getAllArticles(pageNumber: number = 1,
                  pageSize: number = 10): Observable<PagedResult<Article>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<Article>>(this.articlesUrl, {params});
  }

  getDeletedArticles(pageNumber: number = 1,
                  pageSize: number = 10): Observable<PagedResult<Article>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<Article>>(`${this.articlesUrl}/deleted`, {params});
  }

  getArticleById(id: string): Observable<Article> {
    return this.http.get<Article>(`${this.articlesUrl}/${id}`);
  }

  getArticleByCode(code: string): Observable<Article> {
    const params = new HttpParams().set('code', code);
    return this.http.get<Article>(`${this.articlesUrl}/by-code`, {params});
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
    return this.http.get<PagedResult<Article>>(`${this.articlesUrl}/by-category`, { params });
  }

  getArticlesPagedByLibelle(
    libelleFilter: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<Article>> {
    const params = new HttpParams()
      .set('libelle', libelleFilter)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<Article>>(`${this.articlesUrl}/by-libelle`, { params });
  }

  createArticle(request: CreateArticleRequest): Observable<Article> {
    return this.http.post<Article>(this.articlesUrl, request);
  }

  updateArticle(id: string, request: UpdateArticleRequest): Observable<Article> {
    return this.http.put<Article>(`${this.articlesUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.articlesUrl}/${id}`);
  }

  restore(id: string): Observable<void> {
    return this.http.patch<void>(`${this.articlesUrl}/restore/${id}`, {});
  }


}
