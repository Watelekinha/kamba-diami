import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostBinding,
  Input,
  NgZone,
  OnDestroy,
  ViewChild,
  computed,
  signal
} from '@angular/core';

export interface ChartPoint {
  /** Data no formato AAAA-MM-DD. */
  date: string;
  value: number;
}

const HEIGHT = 240;
const MARGIN = { top: 16, right: 44, bottom: 28, left: 40 };
const MONTHS = ['jan', 'fev', 'mar', 'abr', 'mai', 'jun', 'jul', 'ago', 'set', 'out', 'nov', 'dez'];

/**
 * Gráfico de linha de uma série com área suave, linha vertical que acompanha o ponteiro
 * e tooltip. É uma só série, por isso não tem legenda: o título do cartão diz o que mostra.
 */
@Component({
  selector: 'app-admin-line-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-line-chart.component.html',
  styleUrl: './admin-line-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminLineChartComponent implements AfterViewInit, OnDestroy {
  @Input({ required: true }) set points(value: ChartPoint[]) {
    this.data.set(value ?? []);
    this.hoverIndex.set(null);
  }
  @Input() seriesLabel = '';
  /** Cor da série; a linha, a área, os pontos e a chave do tooltip usam-na. */
  @Input() @HostBinding('style.--series') color = '#2a78d6';

  @ViewChild('frame', { static: true }) frame!: ElementRef<HTMLDivElement>;

  readonly height = HEIGHT;
  readonly margin = MARGIN;
  readonly data = signal<ChartPoint[]>([]);
  readonly width = signal(640);
  readonly hoverIndex = signal<number | null>(null);
  readonly showTable = signal(false);

  private observer?: ResizeObserver;

  constructor(private zone: NgZone) {}

  readonly plotWidth = computed(() => Math.max(this.width() - MARGIN.left - MARGIN.right, 10));
  readonly plotHeight = HEIGHT - MARGIN.top - MARGIN.bottom;

  /** Máximo "redondo" do eixo Y e os 4 traços da grelha. */
  readonly yTicks = computed(() => {
    const max = Math.max(...this.data().map((p) => p.value), 0);
    const niceMax = niceCeil(Math.max(max, 4));
    const step = niceMax / 4;
    return [0, 1, 2, 3, 4].map((i) => i * step);
  });

  readonly yMax = computed(() => this.yTicks()[this.yTicks().length - 1]);

  readonly coords = computed(() => {
    const points = this.data();
    const n = points.length;
    const w = this.plotWidth();
    const yMax = this.yMax();
    return points.map((p, i) => ({
      x: MARGIN.left + (n <= 1 ? w / 2 : (i / (n - 1)) * w),
      y: MARGIN.top + this.plotHeight - (p.value / yMax) * this.plotHeight
    }));
  });

  readonly linePath = computed(() =>
    this.coords().map((c, i) => `${i === 0 ? 'M' : 'L'}${c.x.toFixed(1)},${c.y.toFixed(1)}`).join(' ')
  );

  readonly areaPath = computed(() => {
    const c = this.coords();
    if (c.length === 0) return '';
    const base = MARGIN.top + this.plotHeight;
    return `${this.linePath()} L${c[c.length - 1].x.toFixed(1)},${base} L${c[0].x.toFixed(1)},${base} Z`;
  });

  /** Poucas datas no eixo X, igualmente espaçadas, para não se sobreporem. */
  readonly xLabels = computed(() => {
    const points = this.data();
    const coords = this.coords();
    if (points.length === 0) return [];
    const maxLabels = Math.max(2, Math.floor(this.plotWidth() / 90));
    const step = Math.max(1, Math.ceil((points.length - 1) / (maxLabels - 1)));
    const indexes = new Set<number>();
    for (let i = 0; i < points.length; i += step) indexes.add(i);
    indexes.add(points.length - 1);
    // Evita que o último rótulo cole ao penúltimo.
    const sorted = [...indexes].sort((a, b) => a - b);
    if (sorted.length > 2 && sorted[sorted.length - 1] - sorted[sorted.length - 2] < step / 2) {
      sorted.splice(sorted.length - 2, 1);
    }
    return sorted.map((i) => ({ x: coords[i].x, text: shortDate(points[i].date), anchor: i === 0 ? 'start' : i === points.length - 1 ? 'end' : 'middle' }));
  });

  readonly last = computed(() => {
    const c = this.coords();
    const d = this.data();
    return c.length ? { ...c[c.length - 1], value: d[d.length - 1].value } : null;
  });

  readonly peak = computed(() => {
    const d = this.data();
    if (d.length < 3) return null;
    let best = 0;
    d.forEach((p, i) => { if (p.value > d[best].value) best = i; });
    if (d[best].value === 0 || best === d.length - 1) return null;
    return { ...this.coords()[best], value: d[best].value };
  });

  readonly hover = computed(() => {
    const i = this.hoverIndex();
    if (i === null) return null;
    const point = this.data()[i];
    const c = this.coords()[i];
    if (!point || !c) return null;
    const tooltipWidth = 150;
    const left = Math.min(Math.max(c.x - tooltipWidth / 2, 0), this.width() - tooltipWidth);
    return { ...c, point, left, label: longDate(point.date) };
  });

  readonly total = computed(() => this.data().reduce((sum, p) => sum + p.value, 0));

  ngAfterViewInit(): void {
    this.observer = new ResizeObserver((entries) => {
      const w = Math.round(entries[0].contentRect.width);
      // O ResizeObserver corre fora da zona do Angular; sem zone.run o gráfico não se redesenhava.
      if (w > 0 && w !== this.width()) this.zone.run(() => this.width.set(w));
    });
    this.observer.observe(this.frame.nativeElement);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  onPointerMove(event: PointerEvent): void {
    const rect = (event.currentTarget as SVGElement).getBoundingClientRect();
    const x = event.clientX - rect.left;
    const coords = this.coords();
    if (coords.length === 0) return;
    let nearest = 0;
    coords.forEach((c, i) => {
      if (Math.abs(c.x - x) < Math.abs(coords[nearest].x - x)) nearest = i;
    });
    this.hoverIndex.set(nearest);
  }

  onKeydown(event: KeyboardEvent): void {
    const n = this.data().length;
    if (n === 0) return;
    const current = this.hoverIndex() ?? n - 1;
    let next = current;
    if (event.key === 'ArrowLeft') next = Math.max(0, current - 1);
    else if (event.key === 'ArrowRight') next = Math.min(n - 1, current + 1);
    else if (event.key === 'Home') next = 0;
    else if (event.key === 'End') next = n - 1;
    else return;
    event.preventDefault();
    this.hoverIndex.set(next);
  }

  formatTick(value: number): string {
    return compact(value);
  }

  longDate = longDate;
}

function niceCeil(value: number): number {
  const exponent = Math.floor(Math.log10(value));
  const fraction = value / 10 ** exponent;
  const nice = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 2.5 ? 2.5 : fraction <= 5 ? 5 : 10;
  const result = nice * 10 ** exponent;
  // A grelha tem 4 divisões: o máximo tem de dar números inteiros em cada traço.
  return Number.isInteger(result / 4) ? result : Math.ceil(result / 4) * 4;
}

export function compact(value: number): string {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1).replace('.0', '')} M`;
  if (value >= 1_000) return `${(value / 1_000).toFixed(1).replace('.0', '')} mil`;
  return String(Math.round(value));
}

function parse(date: string): Date {
  const [y, m, d] = date.split('-').map(Number);
  return new Date(y, m - 1, d);
}

function shortDate(date: string): string {
  const d = parse(date);
  return `${d.getDate()} ${MONTHS[d.getMonth()]}`;
}

function longDate(date: string): string {
  const d = parse(date);
  const weekday = ['dom', 'seg', 'ter', 'qua', 'qui', 'sex', 'sáb'][d.getDay()];
  return `${weekday}, ${d.getDate()} ${MONTHS[d.getMonth()]} ${d.getFullYear()}`;
}
