# Sends `dllPath` to the WebSocket server running in the mod for evaluation.
# Enables a REPL-like experience.

param (
    [Parameter(Mandatory=$true)]
    [string]$dllPath
)

function IsUnexpectedException {
    param (
        [System.Exception]$Exception
    )

    # Infinite Beat Saber's WebSocketServer implementation is prototype-quality.
    # It doesn't close connections properly. Swallow the exception.

    return $Exception -isnot [System.Net.WebSockets.WebSocketException] -or $Exception.WebSocketErrorCode -ne [System.Net.WebSockets.WebSocketError]::ConnectionClosedPrematurely
}

$uri = "ws://127.0.0.1:2019/"

$commandName = "evalDll"
$commandArgs = @{
    dllPath = $dllPath
}
$message = ConvertTo-Json @($commandName, (ConvertTo-Json $commandArgs))

$client = New-Object System.Net.WebSockets.ClientWebSocket
$client.ConnectAsync($uri, [System.Threading.CancellationToken]::None).Wait()
Write-Output "WebSocket connection opened"

$buffer = [System.Text.Encoding]::UTF8.GetBytes($message)
$client.SendAsync(
    [System.ArraySegment[byte]]::new($buffer),
    [System.Net.WebSockets.WebSocketMessageType]::Text,
    $true,
    [System.Threading.CancellationToken]::None).Wait()
Write-Output "WebSocket message sent"

# Close the connection, swallowing any expected exceptions.
try {
    $client.CloseAsync(
        [System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure,
        "Bye",
        [System.Threading.CancellationToken]::None).Wait()
} catch [System.AggregateException] {
    $ex = $_.Exception
    foreach ($innerEx in $ex.InnerExceptions) {
        if (IsUnexpectedException -Exception $innerEx) {
            throw
        }
    }
} catch {
    $ex = $_.Exception
    if (IsUnexpectedException -Exception $ex) {
        throw
    }
}
Write-Output "WebSocket connection closed"
