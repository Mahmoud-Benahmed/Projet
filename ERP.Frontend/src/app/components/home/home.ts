import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatTooltipModule, RouterModule],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class HomeComponent implements OnInit {
  login: string | null = null;
  role: string | null = null;

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn) {
      this.authService.logout();
      return;
    }
    this.login = this.authService.Login;
    this.role  = this.authService.Role;
  }

  logout(): void {
    this.authService.logout();
  }

  get firstName(): string {
    return this.login?.split('@')[0] ?? 'there';
  }
}
