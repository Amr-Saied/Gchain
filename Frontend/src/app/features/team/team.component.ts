import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-team',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page">
      <h2>Team {{ teamId }}</h2>
      <pre *ngIf="data(); else loading">{{ data() | json }}</pre>
      <ng-template #loading>Loading...</ng-template>
    </div>
  `,
  styles: [
    `
      .page {
        max-width: 800px;
        margin: 0 auto;
        padding: 1rem;
      }
    `,
  ],
})
export class TeamComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiService);
  data = signal<unknown | null>(null);
  teamId = '';

  constructor() {
    this.teamId = this.route.snapshot.params['id'];
    this.api.get(`team/${this.teamId}`).subscribe((res) => this.data.set(res));
  }
}
