import { CommonModule } from '@angular/common';
import { Component, computed, OnDestroy, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { UsersService } from '../../../core/services/users.service';
import { UserRole } from '../../../core/models/user.model';
import { PASSWORD_MIN_LENGTH, isAngolanMobile, passwordProblems } from '../../../core/models/auth.model';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { LocationPickerComponent } from '../../../shared/components/location-picker/location-picker.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LocationPickerComponent],
  templateUrl: './register.component.html',
  styleUrls: ['../auth-layout.scss', './register.component.scss']
})
export class RegisterComponent implements OnDestroy {
  /** Limite e formatos aceites pelo backend em POST /api/users/me/photo. */
  private static readonly MAX_PHOTO_BYTES = 5 * 1024 * 1024;
  private static readonly PHOTO_TYPES = ['image/jpeg', 'image/png'];

  role: UserRole = 'Familia';
  readonly photoFile = signal<File | null>(null);
  readonly photoPreview = signal<string | null>(null);
  readonly photoError = signal<string | null>(null);
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
  municipalitySlug: string | null = null;

  loading = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(
    private authService: AuthService,
    private usersService: UsersService,
    private router: Router,
    route: ActivatedRoute
  ) {
    const role = route.snapshot.queryParamMap.get('role');
    if (role === 'Familia' || role === 'Secretaria') {
      this.role = role;
    }
  }

  ngOnDestroy(): void {
    this.clearPreview();
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.photoError.set(null);
    if (!RegisterComponent.PHOTO_TYPES.includes(file.type)) {
      this.photoError.set('Escolhe uma foto em JPEG ou PNG.');
      return;
    }
    if (file.size > RegisterComponent.MAX_PHOTO_BYTES) {
      this.photoError.set('A foto tem mais de 5 MB. Escolhe uma mais pequena.');
      return;
    }

    this.clearPreview();
    this.photoFile.set(file);
    this.photoPreview.set(URL.createObjectURL(file));
  }

  removePhoto(): void {
    this.clearPreview();
    this.photoFile.set(null);
    this.photoError.set(null);
  }

  private clearPreview(): void {
    const url = this.photoPreview();
    if (url) URL.revokeObjectURL(url);
    this.photoPreview.set(null);
  }

  private goHome(): void {
    this.loading.set(false);
    this.router.navigateByUrl(this.authService.homeRoute());
  }

  onMunicipalityChange(municipalitySlug: string | null): void {
    this.municipalitySlug = municipalitySlug;
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

    if (!this.municipalitySlug) {
      this.errorMessage.set('Por favor selecciona a tua morada (província e município).');
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
        municipalitySlug: this.municipalitySlug,
        streetDetail: this.streetDetail || null
      })
      .subscribe({
        next: () => {
          const photo = this.photoFile();
          if (!photo) {
            this.goHome();
            return;
          }
          // A conta já existe e a sessão está aberta: envia a foto. Se falhar, segue na mesma;
          // a pessoa pode pôr a foto mais tarde em "O meu perfil".
          this.usersService.uploadPhoto(photo).subscribe({
            next: (user) => {
              this.authService.setUser(user);
              this.goHome();
            },
            error: () => this.goHome()
          });
        },
        error: (err) => {
          this.loading.set(false);
          this.errorMessage.set(apiErrorMessage(err, 'Não foi possível criar a conta. Verifica os dados.'));
        }
      });
  }
}
