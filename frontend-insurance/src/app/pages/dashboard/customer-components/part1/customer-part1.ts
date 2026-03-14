import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { GooglePlacesInputComponent } from '../../../../components/incident-location/incident-location.component';
import { SafePipe } from '../../../../pipes/safe.pipe';

@Component({
    selector: 'app-customer-part1',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, GooglePlacesInputComponent, SafePipe],
    templateUrl: './customer-part1.html'
})
export class CustomerPart1Component {
    @Input() ctx: any;
}
