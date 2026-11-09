import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PASSWORD_MIN_LENGTH, passwordProblems } from '../../../core/models/auth.model';
import { AuthService } from '../../../core/services/auth.service';
import { apiErrorMessage } from '../../../core/utils/api-error';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrl: '../login/login.component.scss'
})
export class ResetPasswordComponent {
  readonly minLength = PASSWORD_MIN_LENGTH;
  readonly email: string;
  private readonly token: string;

  newPassword = signal('');
  confirmNewPassword = '';
  showPassword = signal(false);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  readonly linkIsValid: boolean;
  readonly rules = computed(() => {
    const value = this.newPassword();
    return [
      { label: `${PASSWORD_MIN_LENGTH} caracteres`, met: value.length >= PASSWORD_MIN_LENGTH },
      { label: 'uma letra', met: /[A-Za-zÀ-ÿ]/.test(value) },
      { label: 'um número', met: /[0-9]/.test(value) }
    ];
  });

  constructor(
    private authService: AuthService,
    private router: Router,
    route: ActivatedRoute
  ) {
    this.email = route.snapshot.queryParamMap.get('email') ?? '';
    this.token = route.snapshot.queryParamMap.get('token') ?? '';
    this.linkIsValid = !!this.email && !!this.token;
  }

  submit(): void {
    this.errorMessage.set(null);

    const problems = passwordProblems(this.newPassword());
    if (problems.length > 0) {
      this.errorMessage.set(`A palavra-passe precisa de ter ${problems.join(', ')}.`);
      return;
    }
    if (this.newPassword() !== this.confirmNewPassword) {
      this.errorMessage.set('As palavras-passe não coincidem.');
      return;
    }

    this.loading.set(true);
    this.authService
      .resetPassword({
        email: this.email,
        token: this.token,
        newPassword: this.newPassword(),
        confirmNewPassword: this.confirmNewPassword
      })
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.router.navigate(['/login'], { queryParams: { email: this.email, reset: 1 } });
        },
        error: (err) => {
          this.loading.set(false);
          this.errorMessage.set(apiErrorMessage(err, 'Não foi possível alterar a palavra-passe.'));
        }
      });
  }
}
