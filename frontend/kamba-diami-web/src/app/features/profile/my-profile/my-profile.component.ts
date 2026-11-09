import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { UsersService } from '../../../core/services/users.service';
import { ProfileRating, UserProfile } from '../../../core/models/user.model';
import { passwordProblems } from '../../../core/models/auth.model';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { LocationPickerComponent } from '../../../shared/components/location-picker/location-picker.component';

@Component({
  selector: 'app-my-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, LocationPickerComponent],
  templateUrl: './my-profile.component.html',
  styleUrl: './my-profile.component.scss'
})
export class MyProfileComponent implements OnInit {
  user: UserProfile | null = null;
  editMode = signal(false);

  firstName = '';
  lastName = '';
  email = '';
  phoneNumber = '';
  streetDetail = '';
  municipalitySlug: string | null = null;

  currentPassword = '';
  newPassword = '';
  confirmNewPassword = '';

  savingProfile = signal(false);
  savingPassword = signal(false);
  uploadingPhoto = signal(false);

  profileMessage = signal<string | null>(null);
  profileError = signal<string | null>(null);
  passwordMessage = signal<string | null>(null);
  passwordError = signal<string | null>(null);
  ratings = signal<ProfileRating[]>([]);

  constructor(
    public authService: AuthService,
    private usersService: UsersService
  ) {}

  ngOnInit(): void {
    this.authService.refreshMe().subscribe((user) => {
      this.loadFromUser(user);
      // O administrador não é avaliado: não tem contratos.
      if (user.role !== 'Admin') {
        this.usersService.getRatings(user.id).subscribe((ratings) => this.ratings.set(ratings));
      }
    });
  }

  roleLabel(role: string): string {
    return role === 'Familia' ? 'Família' : role === 'Secretaria' ? 'Secretária do lar' : 'Administrador';
  }

  starLabel(score: number): string {
    return '★'.repeat(score) + '☆'.repeat(5 - score);
  }

  private loadFromUser(user: UserProfile): void {
    this.user = user;
    this.firstName = user.firstName;
    this.lastName = user.lastName;
    this.email = user.email;
    this.phoneNumber = user.phoneNumber;
    this.streetDetail = user.address.streetDetail ?? '';
    this.municipalitySlug = user.address.municipalitySlug;
  }

  enterEditMode(): void {
    this.profileMessage.set(null);
    this.profileError.set(null);
    this.editMode.set(true);
  }

  cancelEdit(): void {
    if (this.user) this.loadFromUser(this.user);
    this.editMode.set(false);
  }

  onMunicipalityChange(municipalitySlug: string | null): void {
    this.municipalitySlug = municipalitySlug;
  }

  saveProfile(): void {
    this.profileMessage.set(null);
    this.profileError.set(null);

    if (!this.municipalitySlug) {
      this.profileError.set('Selecciona a província e o município da tua morada.');
      return;
    }

    this.savingProfile.set(true);
    this.usersService
      .updateMe({
        firstName: this.firstName,
        lastName: this.lastName,
        email: this.email,
        phoneNumber: this.phoneNumber,
        municipalitySlug: this.municipalitySlug,
        streetDetail: this.streetDetail || null
      })
      .subscribe({
        next: (user) => {
          this.savingProfile.set(false);
          this.authService.setUser(user);
          this.loadFromUser(user);
          this.editMode.set(false);
          this.profileMessage.set('Perfil actualizado com sucesso.');
        },
        error: (err) => {
          this.savingProfile.set(false);
          this.profileError.set(apiErrorMessage(err, 'Não foi possível actualizar o perfil.'));
        }
      });
  }

  changePassword(): void {
    this.passwordMessage.set(null);
    this.passwordError.set(null);

    if (this.newPassword !== this.confirmNewPassword) {
      this.passwordError.set('As novas palavras-passe não coincidem.');
      return;
    }

    const problems = passwordProblems(this.newPassword);
    if (problems.length > 0) {
      this.passwordError.set(`A nova palavra-passe precisa de ter ${problems.join(', ')}.`);
      return;
    }

    this.savingPassword.set(true);
    this.usersService
      .changePassword({
        currentPassword: this.currentPassword,
        newPassword: this.newPassword,
        confirmNewPassword: this.confirmNewPassword
      })
      .subscribe({
        next: (token) => {
          this.authService.replaceToken(token);
          this.savingPassword.set(false);
          this.passwordMessage.set('Palavra-passe alterada. As sessões abertas noutros dispositivos foram terminadas.');
          this.currentPassword = '';
          this.newPassword = '';
          this.confirmNewPassword = '';
        },
        error: (err) => {
          this.savingPassword.set(false);
          this.passwordError.set(apiErrorMessage(err, 'Não foi possível alterar a palavra-passe.'));
        }
      });
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploadingPhoto.set(true);
    this.usersService.uploadPhoto(file).subscribe({
      next: (user) => {
        this.uploadingPhoto.set(false);
        this.authService.setUser(user);
        this.loadFromUser(user);
      },
      error: () => {
        this.uploadingPhoto.set(false);
      }
    });
  }
}
