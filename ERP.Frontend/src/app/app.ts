import { TranslateService } from '@ngx-translate/core';
import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LoadingOverlayComponent } from "./components/loading-overlay/loading-overlay";
import { UserSettingsService } from './services/user-settings.service';
import { take } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, LoadingOverlayComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('ERP.Frontend');
  protected readonly userSettings = inject(UserSettingsService);
  protected readonly translate = inject(TranslateService);

  constructor() {
    this.translate.reloadLang('en').pipe(take(1)).subscribe(() => {
      this.userSettings.init();
    });
  }
}
