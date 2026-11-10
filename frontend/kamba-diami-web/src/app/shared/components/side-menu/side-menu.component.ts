import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
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
  /** Uma citação sobre família e bem-estar, escolhida ao abrir a página. */
  readonly quote = QUOTES[Math.floor(Math.random() * QUOTES.length)];

  constructor(public savedPostings: SavedPostingsService) {}
}

const QUOTES: { text: string; source: string }[] = [
  { text: 'É preciso uma aldeia inteira para educar uma criança.', source: 'Provérbio africano' },
  { text: 'Uma casa cuidada com carinho é uma família em paz.', source: 'Kamba Diami' },
  { text: 'Quem cuida de nós também merece ser cuidado.', source: 'Kamba Diami' },
  { text: 'Se queres ir rápido, vai sozinho. Se queres ir longe, vai acompanhado.', source: 'Provérbio africano' },
  { text: 'O lar não é um lugar, é a paz de quem lá vive.', source: 'Kamba Diami' },
  { text: 'Um amigo em casa vale mais do que mil palavras.', source: 'Kamba Diami' }
];
