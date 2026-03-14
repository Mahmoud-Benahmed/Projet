import { Component } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { LoadingService } from '../../services/loading.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-loading-overlay',
  standalone: true,
  template: `
    @if (loading$ | async) {
      <div class="overlay">
        <div class="spinner"></div>
      </div>
    }
  `,
  styleUrls: ['./loading-overlay.scss'],
  imports:[AsyncPipe]
})
export class LoadingOverlayComponent {
  readonly loading$: Observable<boolean>;

  constructor(private loader: LoadingService) {
    this.loading$ = this.loader.loading$;
  }

}
