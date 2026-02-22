import { Component, OnInit } from '@angular/core';
import { MatCardContent, MatCard } from "@angular/material/card";
import { MatIcon } from "@angular/material/icon";
import { UsersService } from '../../../../services/users.service';
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { UserStatsDto } from '../../../../interfaces/UserProfileDto';
import { MatSnackBar } from '@angular/material/snack-bar';

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
      completedProfiles: 0
  };

  isLoading: boolean= false;
  constructor(private userProfileService: UsersService, private snackbar: MatSnackBar) {}

  ngOnInit(){
    this.loadStats();
  }

  loadStats(): void {
    this.isLoading= true;
    this.userProfileService.getStats().subscribe({
      next: (stats) => {
        this.isLoading= false;
        this.stats = stats;
      },
      error: () => {
        this.isLoading= false;
        this.snackbar.open("Failed to load stats.", "Dismiss", {duration: 3000});
      }
    });
  }

}
