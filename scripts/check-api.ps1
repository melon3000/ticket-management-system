<#
  Checks the ticket endpoints against a running API and prints PASS or FAIL for every rule.
  The script creates one ticket, works with it and deletes it at the end.

  1) Start the API:      cd src\TicketSystem.Api ; dotnet run
  2) In a second window: powershell -ExecutionPolicy Bypass -File .\scripts\check-api.ps1
  Another address:       .\scripts\check-api.ps1 -Base "http://localhost:5119"

  Uses the seeded development accounts, so run it only against a development database.
#>
param([string]$Base = "http://localhost:5119")

Add-Type -AssemblyName System.Net.Http
$http = New-Object System.Net.Http.HttpClient
$script:pass = 0
$script:fail = 0

function Call([string]$Method, [string]$Path, [string]$Token = $null, $Body = $null) {
  $req = New-Object System.Net.Http.HttpRequestMessage ([System.Net.Http.HttpMethod]::new($Method), "$Base$Path")
  if ($Token) { $req.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $Token) }
  if ($null -ne $Body) {
    $json = $Body | ConvertTo-Json -Compress
    $req.Content = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, "application/json")
  }
  $resp = $http.SendAsync($req).GetAwaiter().GetResult()
  $text = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
  $parsed = $null
  if ($text) { try { $parsed = $text | ConvertFrom-Json } catch { $parsed = $null } }
  [pscustomobject]@{ Status = [int]$resp.StatusCode; Body = $parsed }
}

function Check([string]$Name, $Expected, $Actual) {
  if ("$Expected" -eq "$Actual") {
    $script:pass++
    Write-Host "PASS  $Name" -ForegroundColor Green
  } else {
    $script:fail++
    Write-Host "FAIL  $Name (expected '$Expected', got '$Actual')" -ForegroundColor Red
  }
}

function Login([string]$Email) {
  $r = Call "POST" "/api/auth/login" $null @{ email = $Email; password = "Password123!" }
  if ($r.Status -ne 200) { throw "Login failed for $Email (status $($r.Status)). Is the API running at $Base ?" }
  return $r.Body.token
}

function Connect-TicketSocket([string]$Token) {
  $ws = New-Object System.Net.WebSockets.ClientWebSocket
  $address = ($Base -replace '^http', 'ws') + "/ws/tickets"
  if ($Token) { $address += "?access_token=$Token" }
  $cts = New-Object System.Threading.CancellationTokenSource 5000
  try {
    $null = $ws.ConnectAsync([Uri]$address, $cts.Token).GetAwaiter().GetResult()
    return $ws
  } catch {
    return $null
  }
}

function Receive-TicketMessage($Socket) {
  $buffer = New-Object byte[] 16384
  $segment = [System.ArraySegment[byte]]::new($buffer)
  $cts = New-Object System.Threading.CancellationTokenSource 5000
  try {
    $result = $Socket.ReceiveAsync($segment, $cts.Token).GetAwaiter().GetResult()
    return ([System.Text.Encoding]::UTF8.GetString($buffer, 0, $result.Count) | ConvertFrom-Json)
  } catch {
    return $null
  }
}

$kati = Login "kati.kask@ticketsystem.local"
$toomas = Login "toomas.mets@ticketsystem.local"
$mari = Login "mari.maasikas@ticketsystem.local"
$jaan = Login "jaan.tamm@ticketsystem.local"
$admin = Login "admin@ticketsystem.local"
$service = Login "service@ticketsystem.local"

Write-Host "`n== Creating a ticket ==" -ForegroundColor Cyan
$new = @{ title = "Smoke test ticket"; description = "Created by the check script."; categoryId = 1; priorityId = 2 }
$r = Call "POST" "/api/tickets" $kati $new
Check "create ticket: 201" 201 $r.Status
$id = $r.Body.id
$katiId = $r.Body.createdBy.id
Check "new ticket has status New" "New" $r.Body.status
Check "deadline is 24 hours after creation (priority 2)" 24 ([math]::Round(([datetime]$r.Body.deadline - [datetime]$r.Body.createdAt).TotalHours, 2))
Check "create without a token: 401" 401 (Call "POST" "/api/tickets" $null $new).Status
Check "create with an unknown category: 400" 400 (Call "POST" "/api/tickets" $kati @{ title = "Smoke test ticket"; description = "Created by the check script."; categoryId = 999; priorityId = 2 }).Status
Check "create with a short title: 400" 400 (Call "POST" "/api/tickets" $kati @{ title = "abc"; description = "Created by the check script."; categoryId = 1; priorityId = 2 }).Status

Write-Host "`n== Reading ==" -ForegroundColor Cyan
Check "author reads own ticket: 200" 200 (Call "GET" "/api/tickets/$id" $kati).Status
Check "other user reads the ticket: 403" 403 (Call "GET" "/api/tickets/$id" $toomas).Status
Check "support reads any ticket: 200" 200 (Call "GET" "/api/tickets/$id" $mari).Status
Check "unknown ticket: 404" 404 (Call "GET" "/api/tickets/999999" $admin).Status

Write-Host "`n== List, filters, sorting, paging ==" -ForegroundColor Cyan
$r = Call "GET" "/api/tickets" $kati
Check "user list: 200" 200 $r.Status
Check "user list contains only own tickets" 0 @($r.Body.items | Where-Object { $_.createdBy.id -ne $katiId }).Count
$r = Call "GET" "/api/tickets?status=New" $admin
Check "filter by status New: only New" 0 @($r.Body.items | Where-Object { $_.status -ne "New" }).Count
$r = Call "GET" "/api/tickets?pageSize=2&page=1" $admin
Check "page size is respected" $true (@($r.Body.items).Count -le 2)
Check "total pages is at least 1" $true ($r.Body.totalPages -ge 1)
$r = Call "GET" "/api/tickets?sortBy=Priority&descending=true&pageSize=100" $admin
$levels = @($r.Body.items | ForEach-Object { $_.priority.level })
Check "sorted by priority, highest first" (($levels | Sort-Object -Descending) -join ",") ($levels -join ",")
$r = Call "GET" "/api/tickets?search=Smoke%20test" $admin
Check "search finds the ticket" $true (@($r.Body.items | Where-Object { $_.id -eq $id }).Count -eq 1)
Check "page size 500: 400" 400 (Call "GET" "/api/tickets?pageSize=500" $admin).Status
Check "unknown status: 400" 400 (Call "GET" "/api/tickets?status=Foo" $admin).Status

Write-Host "`n== Taking into work ==" -ForegroundColor Cyan
Check "author cannot assign: 403" 403 (Call "POST" "/api/tickets/$id/assign" $kati).Status
Check "administrator cannot assign: 403" 403 (Call "POST" "/api/tickets/$id/assign" $admin).Status
$r = Call "POST" "/api/tickets/$id/assign" $mari
Check "support takes the ticket: 200" 200 $r.Status
Check "status becomes InProgress" "InProgress" $r.Body.status
Check "assignee is the support person" "Mari Maasikas" $r.Body.assignee.displayName
Check "taking it again: 409" 409 (Call "POST" "/api/tickets/$id/assign" $jaan).Status

Write-Host "`n== Changing the status ==" -ForegroundColor Cyan
Check "other support person: 403" 403 (Call "PATCH" "/api/tickets/$id/status" $jaan @{ status = "Resolved" }).Status
Check "author cannot change status: 403" 403 (Call "PATCH" "/api/tickets/$id/status" $kati @{ status = "Resolved" }).Status
Check "missing status: 400" 400 (Call "PATCH" "/api/tickets/$id/status" $mari @{}).Status
Check "unknown status: 400" 400 (Call "PATCH" "/api/tickets/$id/status" $mari @{ status = "Foo" }).Status
Check "back to New: 409" 409 (Call "PATCH" "/api/tickets/$id/status" $mari @{ status = "New" }).Status
$r = Call "PATCH" "/api/tickets/$id/status" $mari @{ status = "Resolved" }
Check "assignee resolves the ticket: 200" 200 $r.Status
Check "resolved ticket has the resolution time" $true ($null -ne $r.Body.resolvedAt)
Check "resolved ticket cannot be edited: 409" 409 (Call "PUT" "/api/tickets/$id" $admin $new).Status
$r = Call "PATCH" "/api/tickets/$id/status" $admin @{ status = "InProgress" }
Check "administrator reopens the ticket: 200" 200 $r.Status
Check "reopened ticket has no resolution time" $true ($null -eq $r.Body.resolvedAt)

Write-Host "`n== Change log ==" -ForegroundColor Cyan
$r = Call "GET" "/api/tickets/$id/log" $kati
Check "author reads the log: 200" 200 $r.Status
Check "log actions in order" "Created,Assigned,StatusChanged,StatusChanged,StatusChanged" (@($r.Body | ForEach-Object { $_.action }) -join ",")
Check "log shows who made the first change" "Kati Kask" $r.Body[0].user.displayName
Check "other user reads the log: 403" 403 (Call "GET" "/api/tickets/$id/log" $toomas).Status
Check "log of an unknown ticket: 404" 404 (Call "GET" "/api/tickets/999999/log" $admin).Status

Write-Host "`n== Comments ==" -ForegroundColor Cyan
Check "author comments: 201" 201 (Call "POST" "/api/tickets/$id/comments" $kati @{ text = "Printer ei reageeri ikka." }).Status
$r = Call "POST" "/api/tickets/$id/comments" $mari @{ text = "  Vaatan seda t66 ajal.  " }
Check "support comments: 201" 201 $r.Status
Check "comment text is trimmed" "Vaatan seda t66 ajal." $r.Body.text
Check "comment shows the author" "Mari Maasikas" $r.Body.author.displayName
Check "other user cannot comment: 403" 403 (Call "POST" "/api/tickets/$id/comments" $toomas @{ text = "Mina ka." }).Status
Check "empty comment: 400" 400 (Call "POST" "/api/tickets/$id/comments" $kati @{ text = "" }).Status
Check "comment on an unknown ticket: 404" 404 (Call "POST" "/api/tickets/999999/comments" $kati @{ text = "Tere." }).Status
$r = Call "GET" "/api/tickets/$id/comments" $kati
Check "author reads the comments: 200" 200 $r.Status
Check "comments in order" "Kati Kask,Mari Maasikas" (@($r.Body | ForEach-Object { $_.author.displayName }) -join ",")
Check "other user cannot read the comments: 403" 403 (Call "GET" "/api/tickets/$id/comments" $toomas).Status
Check "comments of an unknown ticket: 404" 404 (Call "GET" "/api/tickets/999999/comments" $admin).Status

Write-Host "`n== Overdue tickets and escalation ==" -ForegroundColor Cyan
$r = Call "GET" "/api/tickets/overdue" $service
Check "service reads the overdue list: 200" 200 $r.Status
Check "overdue list has no resolved tickets" 0 @($r.Body | Where-Object { $_.status -eq "Resolved" }).Count
Check "overdue list has only late tickets" 0 @($r.Body | Where-Object { ([datetime]$_.deadline).ToUniversalTime() -ge [datetime]::UtcNow }).Count
Check "support reads the overdue list: 200" 200 (Call "GET" "/api/tickets/overdue" $mari).Status
Check "ordinary user cannot read the overdue list: 403" 403 (Call "GET" "/api/tickets/overdue" $kati).Status
Check "ticket that is not late yet cannot be escalated: 409" 409 (Call "PATCH" "/api/tickets/$id/escalate" $service).Status
Check "support cannot escalate: 403" 403 (Call "PATCH" "/api/tickets/$id/escalate" $mari).Status
Check "administrator cannot escalate: 403" 403 (Call "PATCH" "/api/tickets/$id/escalate" $admin).Status
Check "escalating an unknown ticket: 404" 404 (Call "PATCH" "/api/tickets/999999/escalate" $service).Status

Write-Host "`n== Real-time messages (WebSocket) ==" -ForegroundColor Cyan
Check "socket without a token is refused" $null (Connect-TicketSocket "")
Check "ordinary user cannot open the socket" $null (Connect-TicketSocket $kati)
$socket = Connect-TicketSocket $mari
Check "support opens the socket" "Open" $socket.State
$live = (Call "POST" "/api/tickets" $kati $new).Body
$message = Receive-TicketMessage $socket
Check "support gets TicketCreated" "TicketCreated" $message.type
Check "message carries the new ticket" $live.id $message.ticket.id
Call "POST" "/api/tickets/$($live.id)/assign" $mari | Out-Null
$message = Receive-TicketMessage $socket
Check "support gets TicketUpdated after taking it into work" "TicketUpdated" $message.type
Check "update shows the new status" "InProgress" $message.ticket.status
if ($socket) { $socket.Dispose() }
Call "DELETE" "/api/tickets/$($live.id)" $admin | Out-Null

Write-Host "`n== Statistics ==" -ForegroundColor Cyan
Check "statistics without a token: 401" 401 (Call "GET" "/api/statistics/resolution-time").Status
Check "ordinary user cannot read statistics: 403" 403 (Call "GET" "/api/statistics/resolution-time" $kati).Status
Check "support cannot read statistics: 403" 403 (Call "GET" "/api/statistics/resolution-time" $mari).Status
$r = Call "GET" "/api/statistics/resolution-time" $admin
Check "administrator reads statistics: 200" 200 $r.Status
$before = $r.Body
$supportId = ($before.bySupport | Where-Object { $_.name -eq "Mari Maasikas" }).id
$beforeMari = ($before.bySupport | Where-Object { $_.id -eq $supportId }).resolvedCount
$beforeCategory = ($before.byCategory | Where-Object { $_.id -eq 1 }).resolvedCount
Check "every category is listed" (Call "GET" "/api/categories" $admin).Body.Count $before.byCategory.Count
$stat = (Call "POST" "/api/tickets" $kati $new).Body
Call "POST" "/api/tickets/$($stat.id)/assign" $mari | Out-Null
Check "support resolves the ticket: 200" 200 (Call "PATCH" "/api/tickets/$($stat.id)/status" $mari @{ status = "Resolved" }).Status
$after = (Call "GET" "/api/statistics/resolution-time" $admin).Body
Check "total resolved count grows by one" ($before.resolvedCount + 1) $after.resolvedCount
Check "category count grows by one" ($beforeCategory + 1) ($after.byCategory | Where-Object { $_.id -eq 1 }).resolvedCount
Check "support person count grows by one" ($beforeMari + 1) ($after.bySupport | Where-Object { $_.id -eq $supportId }).resolvedCount
Check "average time is not negative" $true ($after.averageHours -ge 0)
Call "DELETE" "/api/tickets/$($stat.id)" $admin | Out-Null

Write-Host "`n== Editing and deleting ==" -ForegroundColor Cyan
Check "author cannot edit a ticket in work: 403" 403 (Call "PUT" "/api/tickets/$id" $kati $new).Status
Check "user cannot delete: 403" 403 (Call "DELETE" "/api/tickets/$id" $kati).Status
Check "support cannot delete: 403" 403 (Call "DELETE" "/api/tickets/$id" $mari).Status
Check "administrator deletes: 204" 204 (Call "DELETE" "/api/tickets/$id" $admin).Status
Check "deleted ticket is gone: 404" 404 (Call "GET" "/api/tickets/$id" $admin).Status

Write-Host ""
$color = if ($script:fail -eq 0) { "Green" } else { "Red" }
Write-Host "Passed: $($script:pass)  Failed: $($script:fail)" -ForegroundColor $color
exit $script:fail
