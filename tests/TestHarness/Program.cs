using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

// Figure out where we are and where the solution root is
var harnessDir = AppContext.BaseDirectory;
var solutionRoot = Path.GetFullPath(Path.Combine(harnessDir, "../../../../.."));

// All artifacts (trx + html) will live under solutionRoot/artifacts/...
var resultsDir = Path.Combine(solutionRoot, "artifacts", "test-results");
Directory.CreateDirectory(resultsDir);

// Path to the test project
var testProjPath = Path.Combine(solutionRoot, "tests", "Mathy.Tests", "Mathy.Tests.csproj");

int Run(string file, string args)
{
    var p = Process.Start(new ProcessStartInfo
    {
        FileName = file,
        Arguments = args,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    })!;
    p.BeginOutputReadLine();
    p.BeginErrorReadLine();
    p.WaitForExit();
    return p.ExitCode;
}

void GenerateHtmlReport(string solutionRoot)
{
    var coverageFile = Path.Combine(solutionRoot, "artifacts", "coverage", "coverage.xml");
    var reportDir = Path.Combine(solutionRoot, "artifacts", "coverage-report");
    Directory.CreateDirectory(reportDir);

    var args = $"tool run reportgenerator -reports:\"{coverageFile}\" -targetdir:\"{reportDir}\" -reporttypes:Html";

    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = args,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    var p = Process.Start(psi)!;

    p.BeginOutputReadLine();
    p.BeginErrorReadLine();
    p.WaitForExit();

    Console.WriteLine($"\nCoverage HTML report ready: {Path.Combine(reportDir, "index.html")}\n");
    Console.WriteLine(new string('-', 60));
    Console.WriteLine(new string('-', 60));
}

var trxPath = Path.Combine(resultsDir, "unit.trx");
var coverageDir = Path.Combine(solutionRoot, "artifacts", "coverage");
Directory.CreateDirectory(coverageDir);

var coverageFile = Path.Combine(coverageDir, "coverage.xml");

var exit = Run("dotnet",
    $"test \"{testProjPath}\" " +
    $"-c Release " +
    $"--logger \"trx;LogFileName=unit.trx\" " +
    $"--results-directory \"{resultsDir}\" " +
    $"/p:CollectCoverage=true " +
    $"/p:CoverletOutput=\"{coverageFile}\" " +
    "/p:CoverletOutputFormat=cobertura"
);

if (File.Exists(trxPath))
{
    try
    {
        var doc = XDocument.Load(trxPath);
        var results = doc.Descendants().Where(e => e.Name.LocalName == "UnitTestResult").ToList();
        int passed = results.Count(r => (string)r.Attribute("outcome") == "Passed");
        int failed = results.Count(r => (string)r.Attribute("outcome") == "Failed");
        int skipped = results.Count(r => (string)r.Attribute("outcome") == "NotExecuted");

        Console.WriteLine(new string('-', 60));
        Console.WriteLine(new string('-', 60));

        Console.WriteLine("TEST SUMMARY:");
        Console.WriteLine($"Passed:  {passed}");
        Console.WriteLine($"Failed:  {failed}");
        Console.WriteLine($"Skipped: {skipped}");
        Console.WriteLine();
        Console.WriteLine(new string('-', 60));

        try
        {
            GenerateHtmlReport(solutionRoot);
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to generate HTML report:");
            Console.WriteLine(e.Message);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not parse TRX file: {ex.Message}");
    }
}
else
{
    Console.WriteLine("No TRX results found.");
}

Environment.Exit(exit);
