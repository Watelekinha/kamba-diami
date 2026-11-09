import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnInit, Output, computed, signal } from '@angular/core';
import { AdminCount, AdminDailyPoint, AdminMetric, AdminMetricKey, AdminOverview } from '../../../core/models/admin.model';
import { AdminService } from '../../../core/services/admin.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { AdminLineChartComponent, ChartPoint, compact } from '../admin-line-chart/admin-line-chart.component';
import {
  CONTRACT_STATUS_LABELS,
  METRIC_COLORS,
  METRIC_HINTS,
  METRIC_ICONS,
  METRIC_LABELS,
  METRICS_WHERE_UP_IS_BAD,
  label
} from '../admin-labels';

type SeriesKey = Exclude<AdminMetricKey, 'failedLogins'>;

const PERIODS = [
  { days: 7, label: '7 dias' },
  { days: 30, label: '30 dias' },
  { days: 90, label: '90 dias' },
  { days: 365, label: '12 meses' }
];

const SERIES_KEYS: SeriesKey[] = ['registrations', 'logins', 'activeUsers', 'postings', 'matches', 'contracts', 'messages'];

// Rampa ordinal de um só azul, validada com o script da skill de dataviz (--ordinal, superfície branca).
const FUNNEL_COLORS = ['#86b6ef', '#5598e7', '#2a78d6', '#1c5cab', '#104281'];

// Cores de estado da skill de dataviz; vão sempre com o nome do estado ao lado.
const CONTRACT_STATUS_COLORS: Record<string, string> = {
  Rascunho: '#b8bcc4',
  PdfGerado: '#86b6ef',
  AguardandoAssinaturas: '#fab219',
  Ativo: '#0ca30c',
  Concluido: '#1c5cab',
  Cancelado: '#d03b3b'
};

/** Actividade recente com a cor do indicador correspondente. */
export const ACTIVITY_COLORS: Record<string, string> = {
  registration: METRIC_COLORS.registrations,
  posting: METRIC_COLORS.postings,
  match: METRIC_COLORS.matches,
  contract: METRIC_COLORS.contracts
};

const ACTIVITY_ICONS: Record<string, string> = {
  registration: 'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm-7 8a7 7 0 0 1 14 0',
  posting: 'M5 4h14v16H5zM8.5 9h7M8.5 13h7M8.5 17h4',
  match: 'M12 20s-7-4.4-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 10c0 5.6-7 10-7 10Z',
  contract: 'M6 3.5h9l4 4V20a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1V4.5a1 1 0 0 1 1-1ZM9 13.5l2 2 4.5-4.5'
};

interface Tile {
  key: AdminMetricKey;
  label: string;
  hint: string;
  value: number;
  previous: number;
  delta: string;
  tone: 'good' | 'bad' | 'neutral';
  color: string;
  icon: string;
  spark: string | null;
  sparkEnd: { x: number; y: number } | null;
  selectable: boolean;
}

@Component({
  selector: 'app-admin-overview',
  standalone: true,
  imports: [CommonModule, AdminLineChartComponent],
  templateUrl: './admin-overview.component.html',
  styleUrl: './admin-overview.component.scss'
})
export class AdminOverviewComponent implements OnInit {
  @Output() openUser = new EventEmitter<string>();

  readonly periods = PERIODS;
  readonly seriesKeys = SERIES_KEYS;
  readonly metricLabels = METRIC_LABELS;
  readonly activityIcons = ACTIVITY_ICONS;
  readonly activityColors = ACTIVITY_COLORS;
  readonly metricColors = METRIC_COLORS;

  readonly days = signal(30);
  readonly selectedSeries = signal<SeriesKey>('registrations');
  readonly overview = signal<AdminOverview | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly tiles = computed<Tile[]>(() => {
    const data = this.overview();
    if (!data) return [];
    return data.metrics.map((metric) => buildTile(metric, data.series, data.periodDays));
  });

  readonly chartPoints = computed<ChartPoint[]>(() => {
    const key = this.selectedSeries();
    return (this.overview()?.series ?? []).map((p) => ({ date: p.date, value: p[key] }));
  });

  readonly funnel = computed(() => {
    const f = this.overview()?.funnel;
    if (!f) return [];
    const steps = [
      { label: 'Anúncios publicados', value: f.postings },
      { label: 'Interesses manifestados', value: f.matches },
      { label: 'Interesses aceites', value: f.acceptedMatches },
      { label: 'Contratos iniciados', value: f.contracts },
      { label: 'Contratos assinados', value: f.signedContracts }
    ];
    const max = Math.max(...steps.map((s) => s.value), 1);
    return steps.map((step, i) => ({
      ...step,
      color: FUNNEL_COLORS[i],
      width: (step.value / max) * 100,
      // Conversão em relação ao passo anterior (o primeiro não tem).
      conversion: i === 0 || steps[i - 1].value === 0 ? null : Math.round((step.value / steps[i - 1].value) * 100)
    }));
  });

  readonly contractStatus = computed(() =>
    bars(
      (this.overview()?.contractsByStatus ?? []).map((c) => ({
        ...c,
        label: label(CONTRACT_STATUS_LABELS, c.label),
        color: CONTRACT_STATUS_COLORS[c.label] ?? '#b8bcc4'
      }))
    )
  );
  readonly provinces = computed(() =>
    bars((this.overview()?.usersByProvince ?? []).map((c) => ({ ...c, color: METRIC_COLORS.registrations })))
  );
  readonly categories = computed(() =>
    bars((this.overview()?.topCategories ?? []).map((c) => ({ ...c, color: METRIC_COLORS.postings })))
  );

  readonly isEmpty = computed(() => {
    const t = this.overview()?.totals;
    return !!t && t.users === 0 && t.postings === 0;
  });

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.load();
  }

  setPeriod(days: number): void {
    if (days === this.days()) return;
    this.days.set(days);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.adminService.getOverview(this.days()).subscribe({
      next: (data) => {
        this.overview.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(apiErrorMessage(err, 'Não foi possível carregar as estatísticas.'));
      }
    });
  }

  selectTile(tile: Tile): void {
    if (tile.selectable) this.selectedSeries.set(tile.key as SeriesKey);
  }

  onSeriesChange(event: Event): void {
    this.selectedSeries.set((event.target as HTMLSelectElement).value as SeriesKey);
  }

  /** Descarrega a série diária do período em CSV (abre no Excel). */
  exportCsv(): void {
    const data = this.overview();
    if (!data) return;
    const header = ['Dia', ...SERIES_KEYS.map((k) => METRIC_LABELS[k])];
    const rows = data.series.map((p) => [p.date, ...SERIES_KEYS.map((k) => String(p[k]))]);
    const csv = [header, ...rows].map((r) => r.map((cell) => `"${cell.replace(/"/g, '""')}"`).join(';')).join('\r\n');
    const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `kamba-diami-estatisticas-${data.from}-a-${data.to}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }

  timeAgo(iso: string): string {
    const minutes = Math.floor((Date.now() - new Date(iso).getTime()) / 60000);
    if (minutes < 1) return 'agora mesmo';
    if (minutes < 60) return `há ${minutes} min`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `há ${hours} h`;
    const days = Math.floor(hours / 24);
    return days === 1 ? 'ontem' : `há ${days} dias`;
  }

  compact = compact;
}

function bars<T extends AdminCount & { color: string }>(items: T[]) {
  const max = Math.max(...items.map((i) => i.count), 1);
  return items.map((item) => ({ ...item, width: (item.count / max) * 100 }));
}

function buildTile(metric: AdminMetric, series: AdminDailyPoint[], days: number): Tile {
  const upIsBad = METRICS_WHERE_UP_IS_BAD.has(metric.key);
  let delta: string;
  let tone: Tile['tone'] = 'neutral';

  if (metric.changePercent === null) {
    delta = `novo (0 nos ${days} dias anteriores)`;
    tone = upIsBad ? 'bad' : 'good';
  } else if (metric.changePercent === 0) {
    delta = `igual aos ${days} dias anteriores`;
  } else {
    const up = metric.changePercent > 0;
    const value = Math.abs(metric.changePercent).toLocaleString('pt-PT', { maximumFractionDigits: 1 });
    delta = `${up ? '▲ +' : '▼ −'}${value}% vs ${days} dias anteriores`;
    tone = up !== upIsBad ? 'good' : 'bad';
  }

  const selectable = metric.key !== 'failedLogins';
  const values = selectable ? series.map((p) => p[metric.key as SeriesKey]) : [];
  const spark = values.length > 1 ? sparkline(values) : null;

  return {
    key: metric.key,
    label: METRIC_LABELS[metric.key],
    hint: METRIC_HINTS[metric.key],
    value: metric.current,
    previous: metric.previous,
    delta,
    tone,
    color: METRIC_COLORS[metric.key],
    icon: METRIC_ICONS[metric.key],
    spark: spark?.path ?? null,
    sparkEnd: spark?.end ?? null,
    selectable
  };
}

function sparkline(values: number[]): { path: string; end: { x: number; y: number } } {
  const w = 96;
  const h = 28;
  const max = Math.max(...values, 1);
  const pts = values.map((v, i) => ({ x: (i / (values.length - 1)) * w, y: h - 2 - (v / max) * (h - 4) }));
  return {
    path: pts.map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' '),
    end: pts[pts.length - 1]
  };
}
