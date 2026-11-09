import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { apiErrorMessage } from '../../../core/utils/api-error';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: '../login/login.component.scss'
})
export class ForgotPasswordComponent {
  email = '';
  loading = signal(false);
  sentMessage = signal<string | null>(null);
  errorMessage = signal<string | null>(null);

  constructor(
    private authService: AuthService,
    route: ActivatedRoute
  ) {
    this.email = route.snapshot.queryParamMap.get('email') ?? '';
  }

  submit(): void {
    this.errorMessage.set(null);
    this.loading.set(true);

    this.authService.forgotPassword({ email: this.email.trim() }).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.sentMessage.set(response.message);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'Não foi possível enviar o pedido. Tenta de novo.'));
      }
    });
  }
}
