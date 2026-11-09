import { CommonModule } from '@angular/common';
import { Component, HostListener, Input, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime } from 'rxjs';
import { AdminUserDetail, AdminUserFilter, AdminUserListItem } from '../../../core/models/admin.model';
import { isAngolanMobile, passwordProblems } from '../../../core/models/auth.model';
import { AccountStatus, UserRole } from '../../../core/models/user.model';
import { AdminService } from '../../../core/services/admin.service';
import { AuthService } from '../../../core/services/auth.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { KwanzaCurrencyPipe } from '../../../shared/pipes/kwanza-currency.pipe';
import {
  ACCOUNT_STATUS_LABELS,
  CONTRACT_STATUS_LABELS,
  LOGIN_FAILURE_LABELS,
  POSTING_STATUS_LABELS,
  POSTING_TYPE_LABELS,
  ROLE_LABELS,
  contractReference,
  describeUserAgent,
  label
} from '../admin-labels';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, KwanzaCurrencyPipe],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.scss'
})
export class AdminUsersComponent implements OnInit, OnDestroy {
  @Input() set focusUserId(id: string | null) {
    if (id) this.openDetail(id);
  }

  readonly roleLabels = ROLE_LABELS;
  readonly statusLabels = ACCOUNT_STATUS_LABELS;
  readonly postingStatusLabels = POSTING_STATUS_LABELS;
  readonly postingTypeLabels = POSTING_TYPE_LABELS;
  readonly contractStatusLabels = CONTRACT_STATUS_LABELS;
  readonly failureLabels = LOGIN_FAILURE_LABELS;
  readonly label = label;
  readonly contractReference = contractReference;
  readonly describeUserAgent = describeUserAgent;

  filter: Required<Pick<AdminUserFilter, 'search' | 'role' | 'status' | 'sort'>> = {
    search: '',
    role: '',
    status: '',
    sort: 'recent'
  };
  readonly page = signal(1);
  readonly items = signal<AdminUserListItem[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly detail = signal<AdminUserDetail | null>(null);
  readonly detailLoading = signal(false);
  readonly actionBusy = signal(false);
  readonly actionMessage = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly suspendOpen = signal(false);
  suspendReason = '';

  readonly createOpen = signal(false);
  readonly createError = signal<string | null>(null);
  readonly createBusy = signal(false);
  newAdmin = { firstName: '', lastName: '', email: '', phoneNumber: '', password: '' };

  private readonly search$ = new Subject<void>();

  constructor(
    private adminService: AdminService,
    private authService: AuthService
  ) {
    this.search$.pipe(debounceTime(300)).subscribe(() => {
      this.page.set(1);
      this.load();
    });
  }

  get currentAdminId(): string | undefined {
    return this.authService.currentUser()?.id;
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE));
  }

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.search$.complete();
  }

  onSearchInput(): void {
    this.search$.next();
  }

  onFilterChange(): void {
    this.page.set(1);
    this.load();
  }

  goToPage(page: number): void {
    this.page.set(Math.min(Math.max(page, 1), this.totalPages));
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.adminService
      .getUsers({ ...this.filter, search: this.filter.search.trim(), page: this.page(), pageSize: PAGE_SIZE })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(apiErrorMessage(err, 'Não foi possível carregar os utilizadores.'));
        }
      });
  }

  openDetail(id: string): void {
    this.actionMessage.set(null);
    this.actionError.set(null);
    this.suspendOpen.set(false);
    this.detailLoading.set(true);
    this.adminService.getUser(id).subscribe({
      next: (user) => {
        this.detail.set(user);
        this.detailLoading.set(false);
      },
      error: (err) => {
        this.detailLoading.set(false);
        this.actionError.set(apiErrorMessage(err, 'Não foi possível abrir o utilizador.'));
      }
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.createOpen()) this.createOpen.set(false);
    else if (this.detail() || this.detailLoading()) this.closeDetail();
  }

  closeDetail(): void {
    this.detail.set(null);
    this.detailLoading.set(false);
    this.suspendOpen.set(false);
  }

  suspend(): void {
    const user = this.detail();
    if (!user) return;
    if (this.suspendReason.trim().length < 5) {
      this.actionError.set('Indica o motivo da suspensão (pelo menos 5 caracteres).');
      return;
    }
    this.runAction(this.adminService.updateUserStatus(user.id, 'Suspenso', this.suspendReason.trim()), user.id);
  }

  reactivate(): void {
    const user = this.detail();
    if (user) this.runAction(this.adminService.updateUserStatus(user.id, 'Ativo'), user.id);
  }

  unlock(): void {
    const user = this.detail();
    if (user) this.runAction(this.adminService.unlockUser(user.id), user.id);
  }

  sendReset(): void {
    const user = this.detail();
    if (user) this.runAction(this.adminService.sendPasswordReset(user.id), user.id);
  }

  openCreate(): void {
    this.newAdmin = { firstName: '', lastName: '', email: '', phoneNumber: '', password: '' };
    this.createError.set(null);
    this.createOpen.set(true);
  }

  createAdmin(): void {
    this.createError.set(null);
    if (!isAngolanMobile(this.newAdmin.phoneNumber)) {
      this.createError.set('Indica um número móvel angolano válido (ex.: 923 456 789).');
      return;
    }
    const problems = passwordProblems(this.newAdmin.password);
    if (problems.length > 0) {
      this.createError.set(`A palavra-passe precisa de ter ${problems.join(', ')}.`);
      return;
    }

    this.createBusy.set(true);
    this.adminService.createAdmin(this.newAdmin).subscribe({
      next: (admin) => {
        this.createBusy.set(false);
        this.createOpen.set(false);
        this.load();
        this.openDetail(admin.id);
        this.actionMessage.set('Administrador criado. Partilha a palavra-passe por um canal seguro e pede-lhe que a mude.');
      },
      error: (err) => {
        this.createBusy.set(false);
        this.createError.set(apiErrorMessage(err, 'Não foi possível criar o administrador.'));
      }
    });
  }

  statusTone(status: AccountStatus): string {
    return status === 'Ativo' ? 'good' : status === 'Suspenso' ? 'bad' : 'warn';
  }

  roleTone(role: UserRole): string {
    return role === 'Admin' ? 'info' : role === 'Familia' ? 'fam' : 'sec';
  }

  initials(name: string): string {
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join('')
      .toUpperCase();
  }

  private runAction(request: ReturnType<AdminService['unlockUser']>, userId: string): void {
    this.actionBusy.set(true);
    this.actionMessage.set(null);
    this.actionError.set(null);
    request.subscribe({
      next: (response) => {
        this.actionBusy.set(false);
        this.suspendOpen.set(false);
        this.suspendReason = '';
        this.openDetail(userId);
        this.actionMessage.set(response.message);
        this.load();
      },
      error: (err) => {
        this.actionBusy.set(false);
        this.actionError.set(apiErrorMessage(err, 'A acção falhou. Tenta de novo.'));
      }
    });
  }
}
