import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../environment';

// ── Enums ─────────────────────────────────────────────────────────────────────
export enum RetourSourceType {
  BonEntre  = 'BonEntre',
  BonSortie = 'BonSortie',
}

// ── Shared ────────────────────────────────────────────────────────────────────
export interface PagedResult<T> {
  items:      T[];
  totalCount: number;
  pageNumber: number;
  pageSize:   number;
  totalPages: number;
}

export interface FournisseurStatsDto {
  totalFournisseurs:  number;
  activeFournisseurs: number;
  blockedFournisseurs: number;
  deletedFournisseurs: number;
}

// ── Fournisseur ───────────────────────────────────────────────────────────────
export interface FournisseurResponse {
  id:         string;
  name:       string;
  address:    string;
  phone:      string;
  email:      string | null;
  taxNumber:  string;
  rib:        string;
  isDeleted:  boolean;
  isBlocked:  boolean;
  createdAt:  string;
  updatedAt:  string | null;
}

export interface CreateFournisseurRequest {
  name:       string;
  address:    string;
  phone:      string;
  taxNumber:  string;
  rib:        string;
  email?:     string | null;
}

export interface UpdateFournisseurRequest {
  name:       string;
  address:    string;
  phone:      string;
  taxNumber:  string;
  rib:        string;
  email?:     string | null;
}

// ── Lignes ────────────────────────────────────────────────────────────────────
export interface LigneResponseDto{
  id: string,
  articleId: string,
  quantity: number;
  price: number;
  remarque: string | null;
  total: number;
}
export interface LigneRequestDto{
  articleId: string,
  quantity: number;
  price: number;
  remarque: string | null;
}

// ── BonEntre ──────────────────────────────────────────────────────────────────
export interface BonEntreResponse {
  id:              string;
  fournisseurId:   string;
  fournisseurName: string;
  numero:          string;
  observation:     string | null;
  isDeleted:       boolean;
  createdAt:       string;
  updatedAt:       string | null;
  lignes:          LigneResponseDto[];
  total:           number;
}

export interface CreateBonEntreRequest {
  numero:        string;
  fournisseurId: string;
  observation?:  string | null;
  lignes?:       LigneRequestDto[] | null;
}

export interface UpdateBonEntreRequest {
  fournisseurId:string;
  observation?: string | null;
  lignes?:       LigneRequestDto[] | null;
}

// ── BonSortie ─────────────────────────────────────────────────────────────────
export interface BonSortieResponse {
  id:          string;
  clientId:    string;
  numero:      string;
  observation: string | null;
  isDeleted:   boolean;
  createdAt:   string;
  updatedAt:   string | null;
  lignes:      LigneResponseDto[];
  total:       number;
}

export interface CreateBonSortieRequest {
  clientId:     string;
  observation?: string | null;
  lignes?:      LigneRequestDto[] | null;
}

export interface UpdateBonSortieRequest {
  clientId:     string;
  observation?: string | null;
}

// ── BonRetour ─────────────────────────────────────────────────────────────────
export interface BonRetourResponse {
  id:          string;
  sourceId:    string;
  sourceType:  RetourSourceType;
  motif:       string;
  numero:      string;
  observation: string | null;
  isDeleted:   boolean;
  createdAt:   string;
  updatedAt:   string | null;
  lignes:      LigneResponseDto[];
  total:       number;
}

export interface BonStatsDto {
  totalCount:  number;
  activeCount: number;
  deletedCount: number;
}

export interface CreateBonRetourRequest {
  sourceId:     string;
  sourceType:   RetourSourceType;
  motif:        string;
  observation?: string | null;
  lignes?:      LigneRequestDto[] | null;
}

export interface UpdateBonRetourRequest {
  sourceId:     string;
  motif:        string;
  observation?: string | null;
}

export type BonRecord = BonEntreResponse | BonSortieResponse | BonRetourResponse;

// ─────────────────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class StockService {
  private readonly base = `${environment.apiUrl}${environment.routes.stock}`;

  constructor(private http: HttpClient) {}

  // ── Helpers ─────────────────────────────────────────────────────────────────
  private pagedParams(page: number, size: number): HttpParams {
    return new HttpParams().set('page', page).set('size', size);
  }

  // ===========================================================================
  // FOURNISSEURS
  // ===========================================================================

  getFournisseurs(page = 1, size = 10): Observable<PagedResult<FournisseurResponse>> {
    return this.http.get<PagedResult<FournisseurResponse>>(
      `${this.base}/fournisseurs`,
      { params: this.pagedParams(page, size) }
    );
  }

  getBlockedFournisseurs(page = 1, size = 10): Observable<PagedResult<FournisseurResponse>> {
    return this.http.get<PagedResult<FournisseurResponse>>(
      `${this.base}/fournisseurs`,
      { params: this.pagedParams(page, size) }
    ).pipe(
      map((res) => ({
        ...res,
        items: res.items.filter((f) => f.isBlocked)
      }))
    );
  }

  getFournisseurById(id: string): Observable<FournisseurResponse> {
    return this.http.get<FournisseurResponse>(`${this.base}/fournisseurs/${id}`);
  }

  getDeletedFournisseurs(page = 1, size = 10): Observable<PagedResult<FournisseurResponse>> {
    return this.http.get<PagedResult<FournisseurResponse>>(
      `${this.base}/fournisseurs/deleted`,
      { params: this.pagedParams(page, size) }
    );
  }

  getFournisseursByName(name: string, page = 1, size = 10): Observable<PagedResult<FournisseurResponse>> {
    const params = this.pagedParams(page, size).set('name', name);
    return this.http.get<PagedResult<FournisseurResponse>>(
      `${this.base}/fournisseurs/by-name`,
      { params }
    );
  }

  getFournisseurStats(): Observable<FournisseurStatsDto> {
    return this.http.get<FournisseurStatsDto>(`${this.base}/fournisseurs/stats`);
  }

  createFournisseur(dto: CreateFournisseurRequest): Observable<FournisseurResponse> {
    return this.http.post<FournisseurResponse>(`${this.base}/fournisseurs`, dto);
  }

  updateFournisseur(id: string, dto: UpdateFournisseurRequest): Observable<FournisseurResponse> {
    return this.http.put<FournisseurResponse>(`${this.base}/fournisseurs/${id}`, dto);
  }

  deleteFournisseur(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/fournisseurs/${id}`);
  }

  restoreFournisseur(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/fournisseurs/${id}/restore`, null);
  }

  blockFournisseur(id: string): Observable<FournisseurResponse> {
    return this.http.patch<FournisseurResponse>(`${this.base}/fournisseurs/${id}/block`, null);
  }

  unblockFournisseur(id: string): Observable<FournisseurResponse> {
    return this.http.patch<FournisseurResponse>(`${this.base}/fournisseurs/${id}/unblock`, null);
  }

  toggleBlock(fournisseur: FournisseurResponse): Observable<FournisseurResponse> {
    return fournisseur.isBlocked ? this.unblockFournisseur(fournisseur.id) : this.blockFournisseur(fournisseur.id);
  }

  // ===========================================================================
  // BON ENTRES
  // ===========================================================================

  getBonEntres(page = 1, size = 10): Observable<PagedResult<BonEntreResponse>> {
    return this.http.get<PagedResult<BonEntreResponse>>(
      `${this.base}/bon-entres`,
      { params: this.pagedParams(page, size) }
    );
  }

  getBonEntreById(id: string): Observable<BonEntreResponse> {
    return this.http.get<BonEntreResponse>(`${this.base}/bon-entres/${id}`);
  }

  getDeletedBonEntres(page = 1, size = 10): Observable<PagedResult<BonEntreResponse>> {
    return this.http.get<PagedResult<BonEntreResponse>>(
      `${this.base}/bon-entres/deleted`,
      { params: this.pagedParams(page, size) }
    );
  }

  getBonEntresByFournisseur(fournisseurId: string, page = 1, size = 10): Observable<PagedResult<BonEntreResponse>> {
    return this.http.get<PagedResult<BonEntreResponse>>(
      `${this.base}/bon-entres/by-fournisseur/${fournisseurId}`,
      { params: this.pagedParams(page, size) }
    );
  }

  getBonEntresByDateRange(
    from: Date,
    to: Date,
    page = 1,
    size = 10
  ): Observable<PagedResult<BonEntreResponse>> {

    const params = this.pagedParams(page, size)
      .set('from', from.toISOString().split('T')[0])
      .set('to', to.toISOString().split('T')[0]);

    return this.http.get<PagedResult<BonEntreResponse>>(
      `${this.base}/bon-entres/by-date-range`,
      { params }
    );
  }

  createBonEntre(dto: CreateBonEntreRequest): Observable<BonEntreResponse> {
    return this.http.post<BonEntreResponse>(`${this.base}/bon-entres`, dto);
  }

  updateBonEntre(id: string, dto: UpdateBonEntreRequest): Observable<BonEntreResponse> {
    return this.http.put<BonEntreResponse>(`${this.base}/bon-entres/${id}`, dto);
  }

  deleteBonEntre(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/bon-entres/${id}`);
  }
  // ===========================================================================
  // BON SORTIES
  // ===========================================================================

  getBonSorties(page = 1, size = 10): Observable<PagedResult<BonSortieResponse>> {
    return this.http.get<PagedResult<BonSortieResponse>>(
      `${this.base}/bon-sorties`,
      { params: this.pagedParams(page, size) }
    );
  }

  getBonSortieById(id: string): Observable<BonSortieResponse> {
    return this.http.get<BonSortieResponse>(`${this.base}/bon-sorties/${id}`);
  }

  getDeletedBonSorties(page = 1, size = 10): Observable<PagedResult<BonSortieResponse>> {
    return this.http.get<PagedResult<BonSortieResponse>>(
      `${this.base}/bon-sorties/deleted`,
      { params: this.pagedParams(page, size) }
    );
  }

  getBonSortiesByClient(clientId: string, page = 1, size = 10): Observable<PagedResult<BonSortieResponse>> {
    return this.http.get<PagedResult<BonSortieResponse>>(
      `${this.base}/bon-sorties/by-client/${clientId}`,
      { params: this.pagedParams(page, size) }
    );
  }

  getBonSortiesByDateRange(from: Date, to: Date, page = 1, size = 10): Observable<PagedResult<BonSortieResponse>> {
    const params = this.pagedParams(page, size)
      .set('from', from.toISOString().split('T')[0])
      .set('to', to.toISOString().split('T')[0]);
    return this.http.get<PagedResult<BonSortieResponse>>(
      `${this.base}/bon-sorties/by-date-range`,
      { params }
    );
  }

  createBonSortie(dto: CreateBonSortieRequest): Observable<BonSortieResponse> {
    return this.http.post<BonSortieResponse>(`${this.base}/bon-sorties`, dto);
  }

  updateBonSortie(id: string, dto: UpdateBonSortieRequest): Observable<BonSortieResponse> {
    return this.http.put<BonSortieResponse>(`${this.base}/bon-sorties/${id}`, dto);
  }

  deleteBonSortie(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/bon-sorties/${id}`);
  }

  // ===========================================================================
  // BON RETOURS
  // ===========================================================================

  getBonRetours(page = 1, size = 10): Observable<PagedResult<BonRetourResponse>> {
    return this.http.get<PagedResult<BonRetourResponse>>(
      `${this.base}/bon-retours`,
      { params: this.pagedParams(page, size) }
    );
  }

  getBonRetourById(id: string): Observable<BonRetourResponse> {
    return this.http.get<BonRetourResponse>(`${this.base}/bon-retours/${id}`);
  }

  getDeletedBonRetours(page = 1, size = 10): Observable<PagedResult<BonRetourResponse>> {
    return this.http.get<PagedResult<BonRetourResponse>>(
      `${this.base}/bon-retours/deleted`,
      { params: this.pagedParams(page, size) }
    );
  }

  getBonRetoursBySource(sourceId: string, page = 1, size = 10): Observable<PagedResult<BonRetourResponse>> {
    return this.http.get<PagedResult<BonRetourResponse>>(
      `${this.base}/bon-retours/by-source/${sourceId}`,
      { params: this.pagedParams(page, size) }
    );
  }

  getBonRetoursByDateRange(from: Date, to: Date, page = 1, size = 10): Observable<PagedResult<BonRetourResponse>> {
    const params = this.pagedParams(page, size)
      .set('from', from.toISOString().split('T')[0])
      .set('to', to.toISOString().split('T')[0]);
    return this.http.get<PagedResult<BonRetourResponse>>(
      `${this.base}/bon-retours/by-date-range`,
      { params }
    );
  }

  createBonRetour(dto: CreateBonRetourRequest): Observable<BonRetourResponse> {
    return this.http.post<BonRetourResponse>(`${this.base}/bon-retours`, dto);
  }

  updateBonRetour(id: string, dto: UpdateBonRetourRequest): Observable<BonRetourResponse> {
    return this.http.put<BonRetourResponse>(`${this.base}/bon-retours/${id}`, dto);
  }

  deleteBonRetour(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/bon-retours/${id}`);
  }


  ///////////////////////////
  // BON STATS
  ///////////////////////////

  getBonEntreStats(): Observable<BonStatsDto> {
    return this.http.get<BonStatsDto>(`${this.base}/bon-entres/stats`);
  }

  getBonSortieStats(): Observable<BonStatsDto> {
    return this.http.get<BonStatsDto>(`${this.base}/bon-sorties/stats`);
  }

  getBonRetourStats(): Observable<BonStatsDto> {
    return this.http.get<BonStatsDto>(`${this.base}/bon-retours/stats`);
  }
}
