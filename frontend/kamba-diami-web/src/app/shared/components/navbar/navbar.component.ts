import { CommonModule } from '@angular/common';
import { Component, computed, effect, ElementRef, HostListener, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { scrollToAnchor } from '../../../core/utils/scroll-to-anchor';
import { ChatService } from '../../../core/services/chat.service';
import { NotificationsService } from '../../../core/services/notifications.service';
import { NotificationType } from '../../../core/models/notification.model';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss'
})
export class NavbarComponent {
  notificationsOpen = signal(false);

  readonly homeLink = computed(() => {
    this.authService.currentUser();
    return this.authService.isAuthenticated() ? this.authService.homeRoute() : '/';
  });
  readonly showLoggedInNav = computed(() => this.authService.isAuthenticated());
  readonly displayName = computed(() => {
    const user = this.authService.currentUser();
    return user ? `${user.firstName} ${user.lastName}`.trim() : 'Perfil';
  });
  readonly initials = computed(() => {
    const user = this.authService.currentUser();
    if (!user) {
      return '?';
    }

    const first = user.firstName?.trim().charAt(0) ?? '';
    const last = user.lastName?.trim().charAt(0) ?? '';
    return `${first}${last}`.toUpperCase() || '?';
  });
  readonly photoUrl = computed(() => {
    if (this.photoBroken()) {
      return null;
    }

    return this.authService.currentUser()?.photoPath || null;
  });

  private readonly photoBroken = signal(false);

  constructor(
    public authService: AuthService,
    public notificationsService: NotificationsService,
    public chatService: ChatService,
    private router: Router,
    private host: ElementRef<HTMLElement>
  ) {
    effect(
      () => {
        this.authService.currentUser()?.photoPath;
        untracked(() => this.photoBroken.set(false));
      },
      { allowSignalWrites: true }
    );

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe(() => {
        if (this.showLoggedInNav() && this.authService.currentUser()?.role !== 'Admin') {
          this.chatService.refreshUnreadCount();
        }
      });

    if (this.showLoggedInNav()) {
      if (this.authService.currentUser()?.role !== 'Admin') {
        this.chatService.refreshUnreadCount();
      }
      this.authService.refreshMe().subscribe({ error: () => undefined });
    }
  }

  onPhotoError(): void {
    this.photoBroken.set(true);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }

  scrollToFragment(event: Event, fragmentId: string): void {
    const currentPath = this.router.url.split('#')[0].split('?')[0];
    if (currentPath !== '/' && currentPath !== '') {
      return;
    }

    event.preventDefault();
    scrollToAnchor(fragmentId);
  }

  toggleNotifications(): void {
    this.notificationsOpen.update((open) => !open);
  }

  markAllAsRead(): void {
    this.notificationsService.markAllAsRead();
  }

  notificationIcon(type: NotificationType): string {
    switch (type) {
      case 'nearby-job':
        return 'pin';
      case 'contract-ending':
        return 'contract';
      case 'match-accepted':
        return 'check';
      case 'payment':
        return 'receipt';
    }
  }

  timeAgo(date: Date): string {
    const diffMs = Date.now() - date.getTime();
    const minutes = Math.floor(diffMs / 60000);
    if (minutes < 1) return 'agora mesmo';
    if (minutes < 60) return `há ${minutes} min`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `há ${hours}h`;
    const days = Math.floor(hours / 24);
    return `há ${days}d`;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.notificationsOpen() && !this.host.nativeElement.contains(event.target as Node)) {
      this.notificationsOpen.set(false);
    }
  }
}
