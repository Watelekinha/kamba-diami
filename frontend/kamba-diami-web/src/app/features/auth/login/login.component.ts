import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { apiErrorMessage } from '../../../core/utils/api-error';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  email = '';
  password = '';
  showPassword = signal(false);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  infoMessage = signal<string | null>(null);

  constructor(
    private authService: AuthService,
    private router: Router,
    route: ActivatedRoute
  ) {
    const email = route.snapshot.queryParamMap.get('email');
    if (email) {
      this.email = email;
    }
    if (route.snapshot.queryParamMap.get('reset') === '1') {
      this.infoMessage.set('Palavra-passe alterada. Já podes entrar com a nova palavra-passe.');
    }
  }

  submit(): void {
    this.errorMessage.set(null);
    this.infoMessage.set(null);
    this.loading.set(true);

    this.authService.login({ email: this.email.trim(), password: this.password }).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigateByUrl(this.authService.homeRoute());
      },
      error: (err) => {
        this.loading.set(false);
        this.password = '';
        this.errorMessage.set(apiErrorMessage(err, 'Email ou palavra-passe inválidos.'));
      }
    });
  }
}
