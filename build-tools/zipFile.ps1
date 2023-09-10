param (
    [Parameter(Mandatory=$true)]
    [string]$fileToZip,

    [Parameter(Mandatory=$true)]
    [string]$zipFile
)


# Define the directory structure
$dirStructure = "Plugins"

# Create a temporary directory to store the file
$tempDir = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.Guid]::NewGuid().ToString())
New-Item -ItemType Directory -Force -Path "$tempDir\$dirStructure"

# Copy the file to be zipped to the temporary directory
$fileName = $fileName = Split-Path $fileToZip -Leaf
Copy-Item $fileToZip "$tempDir\$dirStructure\$fileName"

# Create the zip file
Compress-Archive -Force -Path "$tempDir\Plugins" -DestinationPath $zipFile

# Remove the temporary directory
Remove-Item -Recurse -Force $tempDir

Write-Host "File zipped to $zipFile"
