import { TranslateService } from '@ngx-translate/core';
import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LoadingOverlayComponent } from "./components/loading-overlay/loading-overlay";
import { UserSettingsService } from './services/user-settings.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, LoadingOverlayComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('ERP.Frontend');

  private readonly userSettings = inject(UserSettingsService);
  private readonly translate = inject(TranslateService);

  constructor() {
    this.userSettings.init(); // ✅ single init point
  }
}
