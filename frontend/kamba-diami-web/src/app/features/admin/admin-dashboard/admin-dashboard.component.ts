import { registerLocaleData } from '@angular/common';
import localePtAO from '@angular/common/locales/pt-AO';
import { Component, LOCALE_ID, ViewEncapsulation, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { AdminContractsComponent } from '../admin-contracts/admin-contracts.component';
import { AdminOverviewComponent } from '../admin-overview/admin-overview.component';
import { AdminPostingsComponent } from '../admin-postings/admin-postings.component';
import { AdminSecurityComponent } from '../admin-security/admin-security.component';
import { AdminUsersComponent } from '../admin-users/admin-users.component';

registerLocaleData(localePtAO);

export type AdminTab = 'overview' | 'users' | 'postings' | 'contracts' | 'security';

const TABS: { id: AdminTab; label: string }[] = [
  { id: 'overview', label: 'Visão geral' },
  { id: 'users', label: 'Utilizadores' },
  { id: 'postings', label: 'Anúncios' },
  { id: 'contracts', label: 'Contratos' },
  { id: 'security', label: 'Segurança e auditoria' }
];

/**
 * Sem encapsulamento: os estilos partilhados (_admin-shared.scss) ficam restritos a `.admin-shell`
 * e servem todos os separadores, em vez de serem copiados para cada componente.
 */
@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    AdminOverviewComponent,
    AdminUsersComponent,
    AdminPostingsComponent,
    AdminContractsComponent,
    AdminSecurityComponent
  ],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
  encapsulation: ViewEncapsulation.None,
  // Datas e números do painel em português de Angola ("29 out.", "1 234").
  providers: [{ provide: LOCALE_ID, useValue: 'pt-AO' }]
})
export class AdminDashboardComponent {
  readonly tabs = TABS;
  readonly activeTab = signal<AdminTab>('overview');
  /** Utilizador a abrir no separador Utilizadores quando se vem de outro separador. */
  readonly focusUserId = signal<string | null>(null);

  readonly adminName = computed(() => this.authService.currentUser()?.firstName ?? 'Administrador');

  constructor(
    private authService: AuthService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      const tab = params.get('tab') as AdminTab | null;
      this.activeTab.set(tab && TABS.some((t) => t.id === tab) ? tab : 'overview');
      this.focusUserId.set(params.get('user'));
    });
  }

  select(tab: AdminTab): void {
    this.router.navigate([], { queryParams: { tab: tab === 'overview' ? null : tab, user: null } });
  }

  openUser(userId: string): void {
    this.router.navigate([], { queryParams: { tab: 'users', user: userId } });
  }

  onTabKeydown(event: KeyboardEvent, index: number): void {
    const delta = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0;
    if (!delta) return;
    event.preventDefault();
    const next = TABS[(index + delta + TABS.length) % TABS.length];
    this.select(next.id);
    queueMicrotask(() => document.getElementById(`adm-tab-${next.id}`)?.focus());
  }
}
