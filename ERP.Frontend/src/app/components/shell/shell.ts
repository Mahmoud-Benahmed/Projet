import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { HeaderComponent } from '../header/header';
import { AuthService } from '../../services/auth.service';
import { UsersService } from '../../services/users.service';
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class ShellComponent implements OnInit {
  currentUser = {
    fullName: '',
    email: '',
    role: '',
  };

  constructor(private router: Router, private authService: AuthService, private usersService: UsersService) {}

  ngOnInit(): void {
    const userId= this.authService.UserId;
    if(!userId) return;

    this.usersService.getByAuthUserId(userId).subscribe({
      next: (user) => {
      this.currentUser = {
        fullName: user.fullName ?? user.email,
        email: this.authService.Email!,
        role: this.authService.Role!,
      };
    },
      error: () => {
        // fallback to token data if profile fetch fails
        this.currentUser = {
          fullName: this.authService.Email!,
          email: this.authService.Email!,
          role: this.authService.Role!,
        };
      }

    })
  }

  onLogout(): void {
    this.authService.logout();
  }
}
