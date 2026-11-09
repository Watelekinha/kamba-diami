import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnDestroy, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime } from 'rxjs';
import { AdminAuditLog, AdminLoginEvent } from '../../../core/models/admin.model';
import { AdminService } from '../../../core/services/admin.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { AUDIT_ACTION_LABELS, LOGIN_FAILURE_LABELS, describeUserAgent, label } from '../admin-labels';

const PAGE_SIZE = 30;

type View = 'logins' | 'audit';

@Component({
  selector: 'app-admin-security',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-security.component.html'
})
export class AdminSecurityComponent implements OnInit, OnDestroy {
  @Output() openUser = new EventEmitter<string>();

  readonly failureLabels = LOGIN_FAILURE_LABELS;
  readonly auditLabels = AUDIT_ACTION_LABELS;
  readonly label = label;
  readonly describeUserAgent = describeUserAgent;

  readonly view = signal<View>('logins');

  search = '';
  outcome: '' | 'true' | 'false' = '';
  readonly loginPage = signal(1);
  readonly logins = signal<AdminLoginEvent[]>([]);
  readonly loginTotal = signal(0);

  readonly auditPage = signal(1);
  readonly audit = signal<AdminAuditLog[]>([]);
  readonly auditTotal = signal(0);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  private readonly search$ = new Subject<void>();

  constructor(private adminService: AdminService) {
    this.search$.pipe(debounceTime(300)).subscribe(() => {
      this.loginPage.set(1);
      this.loadLogins();
    });
  }

  get loginPages(): number {
    return Math.max(1, Math.ceil(this.loginTotal() / PAGE_SIZE));
  }

  get auditPages(): number {
    return Math.max(1, Math.ceil(this.auditTotal() / PAGE_SIZE));
  }

  ngOnInit(): void {
    this.loadLogins();
  }

  ngOnDestroy(): void {
    this.search$.complete();
  }

  setView(view: View): void {
    this.view.set(view);
    if (view === 'audit') this.loadAudit();
    else this.loadLogins();
  }

  onSearchInput(): void {
    this.search$.next();
  }

  onOutcomeChange(): void {
    this.loginPage.set(1);
    this.loadLogins();
  }

  goToLoginPage(page: number): void {
    this.loginPage.set(Math.min(Math.max(page, 1), this.loginPages));
    this.loadLogins();
  }

  goToAuditPage(page: number): void {
    this.auditPage.set(Math.min(Math.max(page, 1), this.auditPages));
    this.loadAudit();
  }

  loadLogins(): void {
    this.loading.set(true);
    this.error.set(null);
    this.adminService
      .getLoginEvents({
        search: this.search.trim(),
        succeeded: this.outcome === '' ? '' : this.outcome === 'true',
        page: this.loginPage(),
        pageSize: PAGE_SIZE
      })
      .subscribe({
        next: (result) => {
          this.logins.set(result.items);
          this.loginTotal.set(result.totalCount);
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(apiErrorMessage(err, 'Não foi possível carregar o registo de logins.'));
        }
      });
  }

  loadAudit(): void {
    this.loading.set(true);
    this.error.set(null);
    this.adminService.getAuditLog(this.auditPage(), PAGE_SIZE).subscribe({
      next: (result) => {
        this.audit.set(result.items);
        this.auditTotal.set(result.totalCount);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(apiErrorMessage(err, 'Não foi possível carregar o registo de auditoria.'));
      }
    });
  }
}
