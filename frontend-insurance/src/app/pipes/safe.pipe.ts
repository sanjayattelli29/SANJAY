import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml, SafeStyle, SafeScript, SafeUrl, SafeResourceUrl } from '@angular/platform-browser';

/**
 * Pipe to bypass security and trust the given value.
 * Used primarily for iframes (Google Maps) and other external resources.
 */
@Pipe({
  name: 'safe',
  standalone: true
})
export class SafePipe implements PipeTransform {
  private sanitizer = inject(DomSanitizer);

  /**
   * Transforms the value to a safe type based on the provided type.
   * @param value The content to sanitize
   * @param type The type of content (html, style, script, url, resourceUrl)
   */
  transform(value: any, type: string = 'resourceUrl'): SafeHtml | SafeStyle | SafeScript | SafeUrl | SafeResourceUrl {
    switch (type) {
      case 'html': return this.sanitizer.bypassSecurityTrustHtml(value);
      case 'style': return this.sanitizer.bypassSecurityTrustStyle(value);
      case 'script': return this.sanitizer.bypassSecurityTrustScript(value);
      case 'url': return this.sanitizer.bypassSecurityTrustUrl(value);
      case 'resourceUrl': return this.sanitizer.bypassSecurityTrustResourceUrl(value);
      default: throw new Error(`Invalid safe type: ${type}`);
    }
  }
}
