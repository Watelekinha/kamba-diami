import { Component, computed } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SavedPostingsService } from '../../../core/services/saved-postings.service';

/** Menu lateral das páginas de anúncios (feed, publicações e guardadas). */
@Component({
  selector: 'app-side-menu',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './side-menu.component.html',
  styleUrl: './side-menu.component.scss'
})
export class SideMenuComponent {
  readonly publishHint = computed(() =>
    this.authService.currentUser()?.role === 'Familia'
      ? 'Diz do que precisas e recebe contactos de secretárias.'
      : 'Mostra os teus serviços às famílias perto de ti.'
  );

  constructor(
    private authService: AuthService,
    public savedPostings: SavedPostingsService
  ) {}
}
