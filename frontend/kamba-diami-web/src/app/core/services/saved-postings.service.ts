import { Injectable, computed, effect, signal, untracked } from '@angular/core';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class SavedPostingsService {
  private readonly idsSignal = signal<number[]>([]);

  readonly ids = this.idsSignal.asReadonly();
  readonly count = computed(() => this.idsSignal().length);

  constructor(private authService: AuthService) {
    effect(
      () => {
        this.authService.currentUser()?.id;
        untracked(() => this.idsSignal.set(this.readStored()));
      },
      { allowSignalWrites: true }
    );
  }

  isSaved(postingId: number): boolean {
    return this.idsSignal().includes(postingId);
  }

  toggle(postingId: number): void {
    const current = this.idsSignal();
    const next = current.includes(postingId)
      ? current.filter((id) => id !== postingId)
      : [postingId, ...current];
    this.idsSignal.set(next);
    this.persist(next);
  }

  private storageKey(): string {
    const userId = this.authService.currentUser()?.id ?? 'anon';
    return `kd_saved_postings_${userId}`;
  }

  private readStored(): number[] {
    try {
      const raw = localStorage.getItem(this.storageKey());
      if (!raw) return [];
      const parsed = JSON.parse(raw) as unknown;
      return Array.isArray(parsed) ? parsed.filter((id): id is number => typeof id === 'number') : [];
    } catch {
      return [];
    }
  }

  private persist(ids: number[]): void {
    localStorage.setItem(this.storageKey(), JSON.stringify(ids));
  }
}
