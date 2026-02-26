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

  constructor(private router: Router, private authService: AuthService, private usersService: UsersService) {}

  ngOnInit(): void {}

  onLogout(): void {
    this.authService.logout();
  }
}
