param(
	[Parameter(Mandatory = $true)]
	[string]$SiteRoot
)

$apiDirectory = Join-Path $SiteRoot 'api'
if (-not (Test-Path $apiDirectory)) {
	throw "API directory not found: $apiDirectory"
}

$singleline = [System.Text.RegularExpressions.RegexOptions]::Singleline
$files = Get-ChildItem -Path $apiDirectory -Filter '*.html' -File
foreach ($file in $files) {
	$html = Get-Content $file.FullName -Raw
	$articleMatch = [regex]::Match($html, '<article class="content wrap" id="_content" data-uid="(?<uid>[^"]+)">(?<body>.*?)</article>', $singleline)
	if (-not $articleMatch.Success) {
		continue
	}

	$uid = $articleMatch.Groups['uid'].Value
	$articleBody = $articleMatch.Groups['body'].Value
	$namespaceHeadingMatch = [regex]::Match($articleBody, '^(?<header>\s*<h1[^>]*>.*?</h1>\s*<div class="markdown level0 summary"></div>\s*<div class="markdown level0 conceptual"></div>\s*<div class="markdown level0 remarks"></div>)', $singleline)
	if (-not $namespaceHeadingMatch.Success) {
		continue
	}

	$header = $namespaceHeadingMatch.Groups['header'].Value
	$bodyWithoutHeader = $articleBody.Substring($namespaceHeadingMatch.Length)

	$sidebarSections = [System.Collections.Generic.List[string]]::new()
	$sectionMatches = [regex]::Matches($bodyWithoutHeader, '<h3 id="(?<id>[^"]+)">\s*(?<title>.*?)\s*</h3>(?<content>.*?)(?=(<h3 id=")|$)', $singleline)
	foreach ($sectionMatch in $sectionMatches) {
		$title = [regex]::Replace($sectionMatch.Groups['title'].Value, '<.*?>', '').Trim()
		$content = $sectionMatch.Groups['content'].Value
		$itemMatches = [regex]::Matches($content, '<h4><a class="xref" href="(?<href>[^"]+)">(?<text>.*?)</a></h4>', $singleline)
		if ($itemMatches.Count -eq 0) {
			continue
		}

		$items = foreach ($itemMatch in $itemMatches) {
			$href = $itemMatch.Groups['href'].Value
			$text = [regex]::Replace($itemMatch.Groups['text'].Value, '<.*?>', '').Trim()
			"<li><a href=`"$href`">$text</a></li>"
		}

		$sidebarSections.Add(@"
<div class="api-sidebar-group">
  <div class="api-sidebar-group-title">$title</div>
  <ul class="api-sidebar-list">
    $($items -join "`n    ")
  </ul>
</div>
"@)
	}

	if ($sidebarSections.Count -eq 0) {
		continue
	}

	$replacement = @"
<article class="content wrap api-enhanced-layout" id="_content" data-uid="$uid">
  <nav class="api-sidebar" aria-label="API page navigation">
    <div class="api-sidebar-title">Index</div>
    $($sidebarSections -join "`n    ")
  </nav>
  <div class="api-main">
    $header
    $bodyWithoutHeader
  </div>
</article>
"@

	$updatedHtml = [regex]::Replace($html, '<article class="content wrap" id="_content" data-uid="[^"]+">.*?</article>', [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $replacement }, $singleline)
	Set-Content -Path $file.FullName -Value $updatedHtml -NoNewline
}
