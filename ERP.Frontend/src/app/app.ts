import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LoginComponent } from "./components/login/login";
import { LoadingOverlayComponent } from "./components/loading-overlay/loading-overlay";
import { ThemeService } from './services/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, LoadingOverlayComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('ERP.Frontend');
  constructor(private theme: ThemeService) {
    this.theme.init(); // call before any rendering
  }
}
