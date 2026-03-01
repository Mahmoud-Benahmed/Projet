import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatCardContent, MatCard } from "@angular/material/card";
import { MatIcon } from "@angular/material/icon";
import { UsersService } from '../../../../services/users.service';
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { UserStatsDto } from '../../../../interfaces/UserProfileDto';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';

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
      deactivatedUsers: 0,
      completedProfiles: 0,
      incompletedProfiles: 0
  };

  isLoading: boolean= false;
  constructor(private userProfileService: UsersService,
              private snackbar: MatSnackBar,
              private router: Router,
              private cdr: ChangeDetectorRef) {}

  ngOnInit(){
    this.loadStats();
  }

  loadStats(): void {
    this.isLoading= true;
    this.userProfileService.getStats().subscribe({
      next: (stats) => {
        this.isLoading= false;
        this.stats = stats;
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
