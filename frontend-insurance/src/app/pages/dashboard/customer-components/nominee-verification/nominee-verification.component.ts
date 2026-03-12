import { Component, Output, EventEmitter, signal, inject, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PolicyService } from '../../../../services/policy.service';

@Component({
  selector: 'app-nominee-verification',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './nominee-verification.component.html',
  styleUrls: []
})
export class NomineeVerificationComponent {
  private policyService = inject(PolicyService);
  
  private _nomineeData: any;
  @Input() set nomineeData(value: any) {
    this._nomineeData = value;
    if (value?.aadharCardUrl) {
      this.aadharPreview.set(value.aadharCardUrl);
      this.aadharUrl.set(value.aadharCardUrl);
    }
  }
  get nomineeData() { return this._nomineeData; }

  @Output() verificationSuccess = new EventEmitter<{aadhar: File | null, photo: File, aadharUrl?: string, photoUrl?: string}>();

  aadharFile = signal<File | null>(null);
  aadharPreview = signal<string | null>(null);
  aadharUrl = signal<string | null>(null);
  
  photoFile = signal<File | null>(null);
  photoPreview = signal<string | null>(null);
  photoUrl = signal<string | null>(null);
  
  isCameraActive = signal<boolean>(false);
  cameraStream = signal<MediaStream | null>(null);
  
  isVerifying = signal<boolean>(false);
  verificationError = signal<string | null>(null);
  verificationSuccessMsg = signal<string | null>(null);

  async onAadharUpload(event: any) {
    if (event.target.files && event.target.files.length > 0) {
      const file = event.target.files[0];
      this.aadharFile.set(file);
      
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.aadharPreview.set(e.target.result);
        // Upload to ImageKit
        this.uploadToImageKit(file, 'aadhar');
      };
      reader.readAsDataURL(file);
    }
  }

  async uploadToImageKit(file: File, type: 'aadhar' | 'photo') {
    try {
      this.policyService.uploadDocument(file, 'nominee-verification').subscribe({
        next: (res: { url: string }) => {
          if (type === 'aadhar') {
            this.aadharUrl.set(res.url);
          } else {
            this.photoUrl.set(res.url);
          }
          console.log(`Nominee ${type} uploaded successfully:`, res.url);
        },
        error: (err: any) => {
          console.error(`Error uploading Nominee ${type} to ImageKit:`, err);
        }
      });
    } catch (error) {
      console.error('Error in uploadToImageKit:', error);
    }
  }

  onPhotoUpload(event: any) {
    if (event.target.files && event.target.files.length > 0) {
      const file = event.target.files[0];
      this.photoFile.set(file);
      
      const reader = new FileReader();
      reader.onload = (e: any) => this.photoPreview.set(e.target.result);
      reader.readAsDataURL(file);
    }
  }

  async activateCamera() {
    this.isCameraActive.set(true);
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: true });
        this.cameraStream.set(stream);
        setTimeout(() => {
            const video = document.getElementById('nominee-camera-video') as HTMLVideoElement;
            if (video) video.srcObject = stream;
        }, 100);
    } catch (err) {
        console.error('Error accessing camera:', err);
        alert('Could not access camera.');
        this.isCameraActive.set(false);
    }
  }

  takePicture() {
    const video = document.getElementById('nominee-camera-video') as HTMLVideoElement;
    if (!video) return;
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext('2d')?.drawImage(video, 0, 0);

    canvas.toBlob(async (blob) => {
        if (blob) {
            const file = new File([blob], 'nominee_selfie.jpg', { type: 'image/jpeg' });
            this.photoFile.set(file);
            this.photoPreview.set(URL.createObjectURL(blob));
            
            // Upload to ImageKit
            await this.uploadToImageKit(file, 'photo');
            
            this.stopCamera();
        }
    }, 'image/jpeg');
  }

  stopCamera() {
    if (this.cameraStream()) {
        this.cameraStream()?.getTracks().forEach(track => track.stop());
        this.cameraStream.set(null);
    }
    this.isCameraActive.set(false);
  }

  async verify() {
    this.verificationError.set(null);
    this.verificationSuccessMsg.set(null);

    if ((!this.aadharFile() && !this.aadharUrl()) || !this.photoFile()) {
      this.verificationError.set('Please provide both Aadhar card and a Live Picture.');
      return;
    }

    this.isVerifying.set(true);

    try {
        const formData = new FormData();
        formData.append('api_key', 'uY335TET_DRXQ0_t8pRDeVJj-CySDDIx');
        formData.append('api_secret', 'OJhLwUWImSdiMM5GwC0w_w2GZys32bdl');
        formData.append('image_file1', this.photoFile()!);
        
        if (this.aadharFile()) {
            formData.append('image_file2', this.aadharFile()!);
        } else {
            formData.append('image_url2', this.aadharUrl()!);
        }

        const response = await fetch('https://api-us.faceplusplus.com/facepp/v3/compare', {
            method: 'POST',
            body: formData
        });

        const result = await response.json();
        console.log('Nominee Face++ API Response:', result);

        if (result.confidence !== undefined) {
            if (result.confidence > 75) {
                this.verificationSuccessMsg.set('Thank you for the verification! You can proceed to Next Step.');
                this.isVerifying.set(false);

                // delay before emit so user can see success message
                setTimeout(() => {
                    this.verificationSuccess.emit({
                        aadhar: this.aadharFile(),
                        photo: this.photoFile()!,
                        aadharUrl: this.aadharUrl() || undefined,
                        photoUrl: this.photoUrl() || undefined
                    });
                }, 2000);
            } else {
                this.verificationError.set('Sorry, the face is not recognized as per the Aadhar. Please re-take.');
                this.isVerifying.set(false);
            }
        } else {
            this.verificationError.set('Biometric capture failed. Please ensure your face is clearly visible and try again.');
            this.isVerifying.set(false);
        }
    } catch (e: any) {
        console.error('Nominee Verification Error:', e);
        this.verificationError.set('Failed to connect to verification service. Please try again.');
        this.isVerifying.set(false);
    }
  }
}
