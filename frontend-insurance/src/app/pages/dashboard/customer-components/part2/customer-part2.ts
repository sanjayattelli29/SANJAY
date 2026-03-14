import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { GooglePlacesInputComponent } from '../../../../components/incident-location/incident-location.component';
import { LocationMapComponent } from '../../../../components/location-map/location-map.component';
import { NomineeVerificationComponent } from '../nominee-verification/nominee-verification.component';
import { SafePipe } from '../../../../pipes/safe.pipe';

@Component({
    selector: 'app-customer-part2',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule,
              GooglePlacesInputComponent, LocationMapComponent,
              NomineeVerificationComponent],
    templateUrl: './customer-part2.html'
})
export class CustomerPart2Component {
    @Input() ctx: any;
}