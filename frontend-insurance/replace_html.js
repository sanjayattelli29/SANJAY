const fs = require('fs');
const lines = fs.readFileSync('src/app/pages/dashboard/customer-dashboard.page.html', 'utf8').split('\n');

const newContent = `            <!-- ============================================================ -->
            <!-- BUY POLICY VIEW  ✦ FULLY REWORKED                           -->
            <!-- ============================================================ -->
            @if (activeView() === 'buy-policy') {
            <app-buy-policy
                [config]="config"
                [user]="user"
                [myPolicies]="myPolicies"
                [isKycVerified]="isKycVerified()"
                (viewChange)="switchView($any($event))"
                (openChatHelperEvent)="openChatHelperFromBuyPolicy($event)"
                (openKycModal)="showKycModal.set(true)"
                (applicationSuccess)="handleApplicationSuccess()">
            </app-buy-policy>
            }`;

lines.splice(154, 1118, newContent);
fs.writeFileSync('src/app/pages/dashboard/customer-dashboard.page.html', lines.join('\n'));
console.log('HTML successfully updated.');
