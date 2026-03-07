import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatCardContent, MatCard } from "@angular/material/card";
import { MatIcon } from "@angular/material/icon";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';
import { UserStatsDto } from '../../../../interfaces/AuthDto';

@Component({
  selector: 'app-stats',
  imports: [MatCardContent, MatIcon, MatCard, MatProgressSpinner],
  templateUrl: './stats.html',
  styleUrl: './stats.scss',
})
export class Stats implements OnInit{
    // Stats
  stats : UserStatsDto = {
      totalUsers: 0,
      activeUsers: 0,
      deactivatedUsers: 0
  };

  isLoading: boolean= false;
  constructor(private authService: AuthService,
              private snackbar: MatSnackBar,
              private router: Router,
              private cdr: ChangeDetectorRef) {}

  ngOnInit(){
    this.loadStats();
  }

  loadStats(): void {
    this.isLoading= true;
    this.authService.getStats().subscribe({
      next: (stats) => {
        this.isLoading= false;
        this.stats = {
          activeUsers: stats.activeUsers-1,
          deactivatedUsers: stats.deactivatedUsers,
          totalUsers: stats.totalUsers-1
        };
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading= false;
        this.snackbar.open("Failed to load stats.", "Dismiss", {duration: 3000});
      }
    });
  }


  goToDeactivated(){
    this.router.navigate(['/users/deactivated']);
  }

  goToUsers(){
    this.router.navigate(['/users']);
  }

  goToCompleted(){
      this.router.navigate(['/users'], { queryParams: { status: true } });
  }

  goToIncompleted(){
      this.router.navigate(['/users'], { queryParams: { status: false } });
  }

}
