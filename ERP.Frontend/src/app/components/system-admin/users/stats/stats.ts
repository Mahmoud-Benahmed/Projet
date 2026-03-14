import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { MatCardContent, MatCard } from "@angular/material/card";
import { MatIcon } from "@angular/material/icon";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterEvent, RouterLink, RouterLinkActive, RouterLinkWithHref } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';
import { UserStatsDto } from '../../../../interfaces/AuthDto';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-stats',
  imports: [MatCardContent, MatIcon, MatCard, MatProgressSpinner, RouterLinkActive, RouterLinkWithHref],
  templateUrl: './stats.html',
  styleUrl: './stats.scss',
})
export class Stats implements OnInit{

  private readonly destroyRef = inject(DestroyRef);

    // Stats
  stats : UserStatsDto = {
      totalUsers: 0,
      activeUsers: 0,
      deactivatedUsers: 0,
      deletedUsers: 0
  };

  isLoading: boolean= false;
  constructor(private authService: AuthService,
              private snackbar: MatSnackBar,
              private cdr: ChangeDetectorRef) {}

  ngOnInit(){
    this.loadStats();
  }

  loadStats(): void {
    this.isLoading= true;
    this.authService.getStats().pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
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

}
