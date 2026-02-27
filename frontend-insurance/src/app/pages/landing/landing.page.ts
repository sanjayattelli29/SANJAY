import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './landing.page.html',
  styleUrls: ['./landing.page.css']
})
export class LandingComponent {
  activePolicy: 'individual' | 'family' = 'individual';

  constructor(private router: Router) {}

  goRegister() {
    this.router.navigate(['/register']);
  }
}