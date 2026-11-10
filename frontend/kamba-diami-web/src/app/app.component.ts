import { Component, computed, inject } from '@angular/core';
import { ViewportScroller } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { SideMenuComponent } from './shared/components/side-menu/side-menu.component';
import { AuthService } from './core/services/auth.service';

/** Páginas que mostram o menu lateral (Início, As minhas publicações, Guardadas, Contratos). */
const SIDE_MENU_PATHS = ['/feed', '/job-postings/mine', '/job-postings/saved', '/contracts'];

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, SideMenuComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'kamba-diami-web';

  private readonly authService = inject(AuthService);

  private readonly path = toSignal(
    inject(Router).events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects.split(/[?#]/)[0])
    ),
    { initialValue: '' }
  );

  readonly showSideMenu = computed(() => {
    const role = this.authService.currentUser()?.role;
    return (role === 'Familia' || role === 'Secretaria') && SIDE_MENU_PATHS.includes(this.path());
  });

  constructor(viewportScroller: ViewportScroller) {
    viewportScroller.setOffset(() => {
      const navbar = document.querySelector('.navbar');
      const height = navbar instanceof HTMLElement ? navbar.offsetHeight : 72;
      return [0, height + 8];
    });
  }
}
