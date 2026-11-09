import { Component } from '@angular/core';
import { ViewportScroller } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './shared/components/navbar/navbar.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'kamba-diami-web';

  constructor(viewportScroller: ViewportScroller) {
    viewportScroller.setOffset(() => {
      const navbar = document.querySelector('.navbar');
      const height = navbar instanceof HTMLElement ? navbar.offsetHeight : 72;
      return [0, height + 8];
    });
  }
}
