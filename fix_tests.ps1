# fix_tests.ps1
$items = @(
    @{ File = "ChatServiceTests.cs"; Old = "ChatService"; New = "SupportChatService" },
    @{ File = "AuthServiceTests.cs"; Old = "AuthService"; New = "IdentityService" },
    @{ File = "PolicyServiceTests.cs"; Old = "PolicyService"; New = "PolicyManager" },
    @{ File = "ClaimServiceTests.cs"; Old = "ClaimService"; New = "ClaimProcessor" },
    @{ File = "NotificationServiceTests.cs"; Old = "NotificationService"; New = "SystemNotifier" }
)

foreach ($item in $items) {
    $path = "C:\Sanjay\InsurancePlatform\Infrastructure.Tests\" + $item.File
    if (Test-Path $path) {
        $content = Get-Content $path
        
        # Add using directives at top if not present
        $usings = @(
            "using Application.Services;",
            "using Application.Interfaces;",
            "using Application.Interfaces.Services;",
            "using Application.Interfaces.Infrastructure;"
        )
        foreach ($u in $usings) {
            if ($content -notmatch [regex]::Escape($u)) {
                $content = @($u) + $content
            }
        }

        # Replace Class Names
        $old = $item.Old
        $new = $item.New
        $content = $content -replace "\b$old\b", $new
        
        # Lowercase vars (e.g., _chatService)
        $oldLower = $old.Substring(0,1).ToLower() + $old.Substring(1)
        $newLower = $new.Substring(0,1).ToLower() + $new.Substring(1)
        $content = $content -replace "\b$oldLower\b", $newLower

        # Bulk interface fixes that might be referenced
        $content = $content -replace "\bINotificationService\b", "ISystemNotifier"
        $content = $content -replace "\bIFileStorageService\b", "IFileStorageService"
        $content = $content -replace "\bIPolicyService\b", "IPolicyManager"
        $content = $content -replace "\bIClaimService\b", "IClaimProcessor"

        Set-Content $path $content
        Write-Host "Updated $path"
    } else {
        Write-Host "File not found: $path"
    }
}
