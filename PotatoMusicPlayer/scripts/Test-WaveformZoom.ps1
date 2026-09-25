$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$modelSource = Get-Content -LiteralPath (Join-Path $projectRoot 'Models/WaveformZoomState.cs') -Raw
$serviceSource = (Get-Content -LiteralPath (Join-Path $projectRoot 'Services/WaveformZoomService.cs') -Raw) -replace '(?m)^using .*;\r?\n', ''
$settingsStub = 'namespace PotatoMusicPlayer.Models { public class WaveformZoomSettings { public double MinZoomLevel { get; set; } = 1; public double ZoomFactor { get; set; } = 2; } }'
Add-Type -TypeDefinition ('using System; using PotatoMusicPlayer.Models;' + $modelSource + $settingsStub + $serviceSource)

function Assert-Near([double] $actual, [double] $expected, [string] $message) {
    if ([Math]::Abs($actual - $expected) -gt 0.0001) {
        throw "$message (expected $expected, got $actual)"
    }
}

function Assert-Range($state, [double] $start, [double] $end, [string] $message) {
    Assert-Near $state.VisibleRangeStart $start "$message start"
    Assert-Near $state.VisibleRangeEnd $end "$message end"
}

$service = [PotatoMusicPlayer.Services.WaveformZoomService]::new()
$state = $service.CreateInitialState(100)

$service.SetVisibleRangeCentered($state, 30, 20)
$service.FollowCenterFixed($state, 25)
Assert-Range $state 20 40 'Position left of center must hold'
$service.FollowCenterFixed($state, 35)
Assert-Range $state 25 45 'Position right of center must follow immediately'

$service.CenterOnPositionAllowingEdges($state, 0)
Assert-Range $state -10 10 'Beginning must be centered with blank time'
$clipped = $service.GetVisibleRangeWithinTrack($state)
Assert-Near $clipped.Item1 0 'Minimap clipped start'
Assert-Near $clipped.Item2 10 'Minimap clipped end'
$service.FollowCenterFixed($state, 0)
Assert-Range $state -10 10 'Following at beginning must remain stable'
$service.ZoomPreservingRatio($state, 10, 0)
Assert-Range $state -5 5 'Zooming at track start must preserve the centered blank area'
$service.SetVisibleRangeCentered($state, 10, 20)
$service.FollowCenterFixed($state, 0)
Assert-Range $state -10 10 'Seeking to track start must establish the centered blank range'
$service.CenterOnPositionAllowingEdges($state, 100)
Assert-Range $state 90 110 'End must be centered with blank time'

$service.SetVisibleRangeCentered($state, 30, 20)
$service.ZoomPreservingRatio($state, 20, 40, 25, 0.1)
Assert-Range $state 24.75 25.75 'Handle width must be clamped before preserving ratio'

$service.SetVisibleRangeCentered($state, 30, 20)
$service.ZoomPreservingRatio($state, 20, 40, 25, 10)
Assert-Range $state 22.5 32.5 'Explicit range zoom must preserve position ratio'

$service.PanBeyondEdges($state, -100)
Assert-Range $state -5 5 'Manual pan must stop when beginning reaches center'

$service.SetVisibleRangeCentered($state, 50, 100)
$service.SetRangeStart($state, 25)
Assert-Range $state 0 100 'A full-width minimap move inside the track must not change the range'
$service.PanBeyondEdges($state, -100)
Assert-Range $state -50 50 'A full-width range may show the beginning in the center'
$service.PanBeyondEdges($state, -100)
Assert-Range $state -50 50 'Repeated movement past the edge must not change the range'

Write-Output 'Waveform zoom behavior checks passed.'
