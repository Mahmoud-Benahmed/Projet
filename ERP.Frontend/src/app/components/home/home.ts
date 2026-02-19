import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { MatIcon } from "@angular/material/icon";
import { MatToolbar } from '@angular/material/toolbar';

@Component({
  selector: 'app-home',
  imports: [MatIcon, MatToolbar],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class HomeComponent implements OnInit {
  email: string | null = null;
  role: string | null = null;

  constructor(private authService: AuthService) {}


  ngOnInit(): void {
    if(!this.authService.isLoggedIn()) {
      this.authService.logout();
    }
      this.email= this.authService.getEmail();
      this.role = this.authService.getRole();
  }

  logout(): void {
    this.authService.logout();
  }

}
