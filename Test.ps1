$rootUrl = "http://localhost:7071"
$prefix = "table" # ef, memory, blob, table, cosmos

function Make-RestCall() {
    -ContentType "application/json"
}

# add TODO item
$createModel = @{ TaskDescription="First task"}
$body = $createModel | ConvertTo-Json
Invoke-RestMethod -Uri "$rootUrl/api/$($prefix)todo" -Body $body -Method Post -ContentType "application/json"

$createModel = @{ TaskDescription="Second task"}
$body = $createModel | ConvertTo-Json
Invoke-RestMethod -Uri "$rootUrl/api/$($prefix)todo" -Body $body -Method Post -ContentType "application/json"


# get the TODO items
$items = Invoke-RestMethod -Uri "$rootUrl/api/$($prefix)todo" -Method Get

ForEach ($todo in $items) {
    Write-Host "Updating $($todo.Id) - $($todo.TaskDescription)"
    $updateModel = @{ IsCompleted=$true}
    $body = $updateModel | ConvertTo-Json
    Invoke-RestMethod -Uri "$rootUrl/api/$($prefix)todo/$($todo.id)" -Body $body -Method Put

    Write-Host "Retrieving $($todo.Id) - $($todo.TaskDescription)"
    Invoke-RestMethod -Uri "$rootUrl/api/$($prefix)todo/$($todo.id)" -Method Get

    Write-Host "Deleting $($todo.Id) - $($todo.TaskDescription)"
    Invoke-RestMethod -Uri "$rootUrl/api/$($prefix)todo/$($todo.id)" -Method Delete
}

Write-Host "Check they are all gone..."
Invoke-RestMethod -Uri "$rootUrl/api/$($prefix)todo" -Method Get