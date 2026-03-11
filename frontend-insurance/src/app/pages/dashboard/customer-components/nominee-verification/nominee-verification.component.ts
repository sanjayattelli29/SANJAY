import { Component, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-nominee-verification',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './nominee-verification.component.html',
  styleUrls: []
})
export class NomineeVerificationComponent {
  @Output() verificationSuccess = new EventEmitter<{aadhar: File, photo: File}>();

  aadharFile = signal<File | null>(null);
  aadharPreview = signal<string | null>(null);
  
  photoFile = signal<File | null>(null);
  photoPreview = signal<string | null>(null);
  
  isCameraActive = signal<boolean>(false);
  cameraStream = signal<MediaStream | null>(null);
  
  isVerifying = signal<boolean>(false);
  verificationError = signal<string | null>(null);
  verificationSuccessMsg = signal<string | null>(null);

  onAadharUpload(event: any) {
    if (event.target.files && event.target.files.length > 0) {
      const file = event.target.files[0];
      this.aadharFile.set(file);
      
      const reader = new FileReader();
      reader.onload = (e: any) => this.aadharPreview.set(e.target.result);
      reader.readAsDataURL(file);
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

    canvas.toBlob((blob) => {
        if (blob) {
            const file = new File([blob], 'nominee_selfie.jpg', { type: 'image/jpeg' });
            this.photoFile.set(file);
            this.photoPreview.set(URL.createObjectURL(blob));
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

    if (!this.aadharFile() || !this.photoFile()) {
      this.verificationError.set('Please provide both Aadhar card and a Live Picture.');
      return;
    }

    this.isVerifying.set(true);

    try {
        const formData = new FormData();
        formData.append('api_key', 'uY335TET_DRXQ0_t8pRDeVJj-CySDDIx');
        formData.append('api_secret', 'OJhLwUWImSdiMM5GwC0w_w2GZys32bdl');
        formData.append('image_file1', this.photoFile()!);
        formData.append('image_file2', this.aadharFile()!);

        const response = await fetch('https://api-us.faceplusplus.com/facepp/v3/compare', {
            method: 'POST',
            body: formData
        });

        const result = await response.json();
        console.log('Nominee Face++ API Response:', result);

        if (result.confidence !== undefined) {
            if (result.confidence > 80) {
                this.verificationSuccessMsg.set('Face Verified Successfully! Same Person. Similarity Score: ' + result.confidence);
                this.isVerifying.set(false);

                // delay before emit so user can see success message
                setTimeout(() => {
                    this.verificationSuccess.emit({
                        aadhar: this.aadharFile()!,
                        photo: this.photoFile()!
                    });
                }, 2000);
            } else {
                this.verificationError.set('Face match failed. The person in the live picture does not closely match the Aadhar photo. Similarity Score: ' + result.confidence);
                this.isVerifying.set(false);
            }
        } else {
            this.verificationError.set('Could not verify face. Please ensure both pictures clearly show the face and try again.');
            this.isVerifying.set(false);
        }
    } catch (e: any) {
        console.error('Nominee Verification Error:', e);
        this.verificationError.set('Failed to connect to verification service. Please try again.');
        this.isVerifying.set(false);
    }
  }
}
