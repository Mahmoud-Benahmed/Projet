// loading.service.ts
import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private _count = 0;
  private _loading$ = new BehaviorSubject<boolean>(false);
  readonly loading$ = this._loading$.asObservable();

  show() {
    this._count++;
    this._loading$.next(true);
  }

  hide() {
    this._count = Math.max(0, this._count - 1); // never go negative
    if (this._count === 0) this._loading$.next(false);
  }
}
