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
    if (changes['adminStats'] && !changes['adminStats'].firstChange) {
      this.initCharts();
    }
  }

  initCharts() {
    const stats = this.adminStats;
    if (!stats) return;

    // Destroy existing charts to prevent canvas reuse error
    this.charts.forEach(c => c.destroy());
    this.charts = [];

    // 1. Policy Growth (Line)
    const policyLabels = stats.policyGrowth?.length ? stats.policyGrowth.map((p: any) => p.label) : ['No Data'];
    const policyData = stats.policyGrowth?.length ? stats.policyGrowth.map((p: any) => p.value) : [10]; // fallback non-zero for visual

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

    // 4. Monthly Revenue (Line)
    const revenueLabels = stats.revenueTrends?.length ? stats.revenueTrends.map((r: any) => r.label) : ['No Data'];
    const revenueData = stats.revenueTrends?.length ? stats.revenueTrends.map((r: any) => r.value) : [0];

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