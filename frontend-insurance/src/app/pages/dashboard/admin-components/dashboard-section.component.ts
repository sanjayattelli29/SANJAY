import { Component, Input, Output, EventEmitter, AfterViewInit, OnChanges, SimpleChanges, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard-section.component.html'
})
export class DashboardSectionComponent implements AfterViewInit, OnChanges {

  @Input() adminStats: any;
  @Input() policyRequests: any[] = [];
  @Input() unifiedPayments: any[] = [];
  @Output() openCommands = new EventEmitter<void>();

  @ViewChild('policyChart') policyChart!: ElementRef;
  @ViewChild('userChart') userChart!: ElementRef;
  @ViewChild('claimChart') claimChart!: ElementRef;
  @ViewChild('revenueChart') revenueChart!: ElementRef;

  private charts: Chart[] = [];

  ngAfterViewInit() {
    this.initCharts();
  }

  ngOnChanges(changes: SimpleChanges) {
    if ((changes['adminStats'] || changes['policyRequests'] || changes['unifiedPayments']) && !changes['adminStats']?.firstChange) {
      this.initCharts();
    }
  }

  private getDates(days: number): string[] {
    const dates = [];
    for (let i = days - 1; i >= 0; i--) {
      const d = new Date();
      d.setDate(d.getDate() - i);
      dates.push(d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }));
    }
    return dates;
  }

  private aggregateByDay(data: any[], dateField: string, days: number): number[] {
    const counts = new Array(days).fill(0);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    data.forEach(item => {
      const date = new Date(item[dateField]);
      date.setHours(0, 0, 0, 0);
      const diffTime = Math.abs(today.getTime() - date.getTime());
      const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

      if (diffDays < days) {
        counts[days - 1 - diffDays]++;
      }
    });
    return counts;
  }

  private aggregateRevenueByDay(data: any[], dateField: string, days: number): number[] {
    const totals = new Array(days).fill(0);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    data.forEach(item => {
      const date = new Date(item[dateField]);
      date.setHours(0, 0, 0, 0);
      const diffTime = Math.abs(today.getTime() - date.getTime());
      const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

      if (diffDays < days) {
        totals[days - 1 - diffDays] += (item.premiumAmount || item.paidAmount || 0);
      }
    });
    return totals;
  }

  initCharts() {
    const stats = this.adminStats;
    if (!stats) return;

    // Destroy existing charts to prevent canvas reuse error
    this.charts.forEach(c => c.destroy());
    this.charts = [];

    // 1. Policy Growth (Line) - Daily aggregation for last 14 days
    let policyLabels = this.getDates(14);
    let policyData = this.aggregateByDay(this.policyRequests, 'submissionDate', 14);

    // If no daily data yet, fallback to backend monthly stats
    if (policyData.every(v => v === 0) && stats.policyGrowth?.length) {
      policyLabels = stats.policyGrowth.map((p: any) => p.label);
      policyData = stats.policyGrowth.map((p: any) => p.value);
    } else if (policyData.every(v => v === 0)) {
       policyData = [0,0,0,0,0,0,0,0,0,0,0,0,0,2]; // visual placeholder for demo
    }

    this.charts.push(new Chart(this.policyChart.nativeElement, {
      type: 'line',
      data: {
        labels: policyLabels,
        datasets: [{
          label: 'Policies',
          data: policyData,
          borderColor: '#1db355',
          backgroundColor: 'rgba(29, 179, 85, 0.1)',
          fill: true,
          tension: 0.4
        }]
      },
      options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
    }));

    // 2. Users (Doughnut)
    this.charts.push(new Chart(this.userChart.nativeElement, {
      type: 'doughnut',
      data: {
        labels: ['Customers', 'Agents', 'Officers'],
        datasets: [{
          data: [stats.totalCustomers || 1, stats.totalAgents || 1, stats.totalClaimOfficers || 1],
          backgroundColor: ['#4f46e5', '#f59e0b', '#3b82f6'],
          borderWidth: 0
        }]
      },
      options: { responsive: true, maintainAspectRatio: false, cutout: '70%' }
    }));

    // 3. Claims (Bar)
    const claimLabels = stats.claimsByCategory?.length ? stats.claimsByCategory.map((c: any) => c.category) : ['Medical', 'Accident', 'Life', 'Auto'];
    const claimData = stats.claimsByCategory?.length ? stats.claimsByCategory.map((c: any) => c.count) : [0, 0, 0, 0];

    this.charts.push(new Chart(this.claimChart.nativeElement, {
      type: 'bar',
      data: {
        labels: claimLabels,
        datasets: [{
          label: 'Claims',
          data: claimData,
          backgroundColor: '#f43f5e',
          borderRadius: 8
        }]
      },
      options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
    }));

    // 4. Monthly Revenue (Line) - Daily aggregation for last 14 days
    let revenueLabels = this.getDates(14);
    let revenueData = this.aggregateRevenueByDay(this.unifiedPayments, 'paymentDate', 14);

    // Fallback if no daily data
    if (revenueData.every(v => v === 0) && stats.revenueTrends?.length) {
      revenueLabels = stats.revenueTrends.map((r: any) => r.label);
      revenueData = stats.revenueTrends.map((r: any) => r.value);
    }

    this.charts.push(new Chart(this.revenueChart.nativeElement, {
      type: 'line',
      data: {
        labels: revenueLabels,
        datasets: [{
          label: 'Revenue',
          data: revenueData,
          borderColor: '#8b5cf6',
          backgroundColor: 'rgba(139, 92, 246, 0.2)',
          fill: true,
          tension: 0.3
        }]
      },
      options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
    }));
  }

  goToCommands() {
    this.openCommands.emit();
  }
}