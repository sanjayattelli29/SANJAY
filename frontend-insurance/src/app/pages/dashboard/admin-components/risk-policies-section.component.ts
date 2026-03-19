import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-risk-policies-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './risk-policies-section.component.html'
})
export class RiskPoliciesSectionComponent {
  // Config passed from parent dashboard
  @Input() config: any;

  // Flattened list of all tiers across all categories
  get allPolicies() {
    if (!this.config?.policyCategories) return [];
    const policies: any[] = [];
    this.config.policyCategories.forEach((cat: any) => {
      cat.tiers?.forEach((tier: any) => {
        policies.push({
          ...tier,
          categoryName: cat.categoryName,
          categoryId: cat.categoryId
        });
      });
    });
    return policies;
  }

  // Get risk factors safely
  get riskFactors() {
    return this.config?.riskFactors || {};
  }

  // Get color scale class for tier name
  getTierColor(tierName: string): string {
    const name = tierName.toLowerCase();
    if (name.includes('silver')) return 'from-slate-300 to-slate-400 text-slate-800';
    if (name.includes('gold')) return 'from-amber-300 to-amber-500 text-amber-900';
    if (name.includes('premium') || name.includes('platinum')) return 'from-indigo-500 to-purple-600 text-white';
    return 'from-emerald-400 to-teal-500 text-white';
  }

  // Get badge style for tier name
  getTierBadgeStyle(tierName: string): string {
    const name = tierName.toLowerCase();
    if (name.includes('silver')) return 'bg-slate-100 text-slate-700 border-slate-200';
    if (name.includes('gold')) return 'bg-amber-100 text-amber-700 border-amber-200';
    if (name.includes('premium')) return 'bg-purple-100 text-purple-700 border-purple-200';
    return 'bg-blue-100 text-blue-700 border-blue-200';
  }
}
