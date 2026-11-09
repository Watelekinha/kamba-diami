import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { UserRole } from '../../../core/models/user.model';
import { PASSWORD_MIN_LENGTH, isAngolanMobile, passwordProblems } from '../../../core/models/auth.model';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { LocationPickerComponent } from '../../../shared/components/location-picker/location-picker.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LocationPickerComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  role: UserRole = 'Familia';
  firstName = '';
  lastName = '';
  phoneNumber = '';
  email = '';
  readonly passwordValue = signal('');
  get password(): string {
    return this.passwordValue();
  }
  set password(value: string) {
    this.passwordValue.set(value);
  }
  readonly passwordRules = computed(() => {
    const value = this.passwordValue();
    return [
      { label: `${PASSWORD_MIN_LENGTH} caracteres`, met: value.length >= PASSWORD_MIN_LENGTH },
      { label: 'uma letra', met: /[A-Za-zÀ-ÿ]/.test(value) },
      { label: 'um número', met: /[0-9]/.test(value) }
    ];
  });
  readonly minLength = PASSWORD_MIN_LENGTH;
  showPassword = signal(false);
  confirmPassword = '';
  streetDetail = '';
  neighborhoodId: number | null = null;

  loading = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(
    private authService: AuthService,
    private router: Router,
    route: ActivatedRoute
  ) {
    const role = route.snapshot.queryParamMap.get('role');
    if (role === 'Familia' || role === 'Secretaria') {
      this.role = role;
    }
  }

  onNeighborhoodChange(neighborhoodId: number | null): void {
    this.neighborhoodId = neighborhoodId;
  }

  submit(): void {
    this.errorMessage.set(null);

    if (!isAngolanMobile(this.phoneNumber)) {
      this.errorMessage.set('Indica um número móvel angolano válido (ex.: 923 456 789).');
      return;
    }

    const problems = passwordProblems(this.password);
    if (problems.length > 0) {
      this.errorMessage.set(`A palavra-passe precisa de ter ${problems.join(', ')}.`);
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage.set('As palavras-passe não coincidem.');
      return;
    }

    if (!this.neighborhoodId) {
      this.errorMessage.set('Por favor selecciona a tua morada (bairro).');
      return;
    }

    this.loading.set(true);

    this.authService
      .register({
        firstName: this.firstName,
        lastName: this.lastName,
        phoneNumber: this.phoneNumber,
        email: this.email.trim(),
        password: this.password,
        confirmPassword: this.confirmPassword,
        role: this.role,
        neighborhoodId: this.neighborhoodId,
        streetDetail: this.streetDetail || null
      })
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.router.navigateByUrl(this.authService.homeRoute());
        },
        error: (err) => {
          this.loading.set(false);
          this.errorMessage.set(apiErrorMessage(err, 'Não foi possível criar a conta. Verifica os dados.'));
        }
      });
  }
}
