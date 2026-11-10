import { CommonModule } from '@angular/common';
import { Component, computed, effect, ElementRef, HostListener, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { scrollToAnchor } from '../../../core/utils/scroll-to-anchor';
import { ChatService } from '../../../core/services/chat.service';
import { NotificationsService } from '../../../core/services/notifications.service';
import { NotificationType } from '../../../core/models/notification.model';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
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
  /** A pesquisa da navbar procura no feed, por isso só aparece a famílias e secretárias. */
  readonly showSearch = computed(() => {
    const role = this.authService.currentUser()?.role;
    return this.authService.isAuthenticated() && (role === 'Familia' || role === 'Secretaria');
  });
  /** Texto da pesquisa actual (?q= no feed), para a caixa não ficar vazia depois de pesquisar. */
  readonly searchQuery = signal('');
  readonly displayName = computed(() => {
    const user = this.authService.currentUser();
    return user ? `${user.firstName} ${user.lastName}`.trim() : 'Perfil';
  });
  readonly userMenuOpen = signal(false);
  readonly roleLabel = computed(() => {
    switch (this.authService.currentUser()?.role) {
      case 'Familia':
        return 'Família';
      case 'Secretaria':
        return 'Secretária do lar';
      case 'Admin':
        return 'Administração';
      default:
        return '';
    }
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
      .subscribe((event) => {
        const tree = this.router.parseUrl(event.urlAfterRedirects);
        const onFeed = event.urlAfterRedirects.split(/[?#]/)[0] === '/feed';
        this.searchQuery.set(onFeed ? tree.queryParams['q'] ?? '' : '');
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

  search(event: Event, value: string): void {
    event.preventDefault();
    const q = value.trim();
    this.router.navigate(['/feed'], { queryParams: q ? { q } : {} });
  }

  onPhotoError(): void {
    this.photoBroken.set(true);
  }

  toggleUserMenu(): void {
    this.userMenuOpen.update((open) => !open);
    this.notificationsOpen.set(false);
  }

  logout(): void {
    this.userMenuOpen.set(false);
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
    this.userMenuOpen.set(false);
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
    const target = event.target as Element;
    if (this.notificationsOpen() && !this.host.nativeElement.contains(target)) {
      this.notificationsOpen.set(false);
    }
    // O menu do utilizador fecha ao clicar fora dele (incluindo noutro sítio da navbar).
    if (this.userMenuOpen() && !target.closest?.('.user-wrap')) {
      this.userMenuOpen.set(false);
    }
  }
}
