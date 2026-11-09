import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProfileRating, UserProfile } from '../../../core/models/user.model';
import { AuthService } from '../../../core/services/auth.service';
import { UsersService } from '../../../core/services/users.service';

@Component({
  selector: 'app-public-profile',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './public-profile.component.html',
  styleUrl: './public-profile.component.scss'
})
export class PublicProfileComponent implements OnInit {
  profile = signal<UserProfile | null>(null);
  ratings = signal<ProfileRating[]>([]);
  notFound = signal(false);
  loading = signal(true);

  constructor(
    private route: ActivatedRoute,
    public auth: AuthService,
    private users: UsersService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.users.getById(id).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.loading.set(false);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
    this.users.getRatings(id).subscribe({
      next: (ratings) => this.ratings.set(ratings),
      error: () => this.ratings.set([])
    });
  }

  get isOwnProfile(): boolean {
    return this.profile()?.id === this.auth.currentUser()?.id;
  }

  roleLabel(role: string): string {
    if (role === 'Familia') return 'Família';
    if (role === 'Secretaria') return 'Secretária do lar';
    return role;
  }

  starLabel(score: number): string {
    return '★'.repeat(score) + '☆'.repeat(5 - score);
  }
}
