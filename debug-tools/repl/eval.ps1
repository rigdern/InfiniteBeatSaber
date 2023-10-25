# Sends `dllPath` to the WebSocket server running in the mod for evaluation.
# Enables a REPL-like experience.

param (
    [Parameter(Mandatory=$true)]
    [string]$dllPath
)

# Immediately exit the PowerShell script if an exception is thrown.
$ErrorActionPreference = 'Stop'

function IsUnexpectedException {
    param (
        [System.Exception]$Exception
    )

    # Infinite Beat Saber's WebSocketServer implementation is prototype-quality.
    # It doesn't close connections properly. Swallow the exception.

    $expected = $Exception -is [System.Net.WebSockets.WebSocketException] -and (
        $Exception.WebSocketErrorCode -eq [System.Net.WebSockets.WebSocketError]::ConnectionClosedPrematurely -or

        # Probably received a message of type 'Text' after calling WebSocket.CloseAsync.
        $Exception.WebSocketErrorCode -eq [System.Net.WebSockets.WebSocketError]::InvalidMessageType
    )

    return -not $expected
}

try {
    # Write-Output "dllPath: $dllPath"

    $uri = "ws://127.0.0.1:2019/"

    $commandName = "evalDll"
    $commandArgs = @{
        dllPath = $dllPath
    }
    $message = ConvertTo-Json @($commandName, (ConvertTo-Json $commandArgs))

    $client = New-Object System.Net.WebSockets.ClientWebSocket
    $client.Options.SetRequestHeader('Origin', 'http://localhost/')
    $client.ConnectAsync($uri, [System.Threading.CancellationToken]::None).Wait()
    Write-Output "WebSocket connection opened"

    $buffer = [System.Text.Encoding]::UTF8.GetBytes($message)
    $client.SendAsync(
        [System.ArraySegment[byte]]::new($buffer),
        [System.Net.WebSockets.WebSocketMessageType]::Text,
        $true, # endOfMessage
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
} catch [System.AggregateException] {
    # Log the aggregate exception and all its inner exceptions
    $exception = $_.Exception
    Write-Host "Fatal AggregateException: $($_.Exception.Message)"
    foreach ($innerException in $exception.InnerExceptions) {
        Write-Host "  InnerException: $($innerException.GetType().Name): $($innerException.Message)"
        if ($innerException.GetType() -eq [System.Net.WebSockets.WebSocketException]) {
            $webSocketException = [System.Net.WebSockets.WebSocketException]$innerException
            Write-Host "    WebSocketErrorCode: $($webSocketException.WebSocketErrorCode)"
        }
    }
    throw
}
